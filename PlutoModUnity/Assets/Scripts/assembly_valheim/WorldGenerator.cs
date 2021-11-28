using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{
	public class River
	{
		public Vector2 p0;

		public Vector2 p1;

		public Vector2 center;

		public float widthMin;

		public float widthMax;

		public float curveWidth;

		public float curveWavelength;
	}

	public struct RiverPoint
	{
		public Vector2 p;

		public float w;

		public float w2;

		public RiverPoint(Vector2 p_p, float p_w)
		{
			p = p_p;
			w = p_w;
			w2 = p_w * p_w;
		}
	}

	private const float m_waterTreshold = 0.05f;

	private static WorldGenerator m_instance;

	private World m_world;

	private int m_version;

	private float m_offset0;

	private float m_offset1;

	private float m_offset2;

	private float m_offset3;

	private float m_offset4;

	private int m_riverSeed;

	private int m_streamSeed;

	private List<Vector2> m_mountains;

	private List<Vector2> m_lakes;

	private List<River> m_rivers = new List<River>();

	private List<River> m_streams = new List<River>();

	private Dictionary<Vector2i, RiverPoint[]> m_riverPoints = new Dictionary<Vector2i, RiverPoint[]>();

	private RiverPoint[] m_cachedRiverPoints;

	private Vector2i m_cachedRiverGrid = new Vector2i(-999999, -999999);

	private ReaderWriterLockSlim m_riverCacheLock = new ReaderWriterLockSlim();

	private List<Heightmap.Biome> m_biomes = new List<Heightmap.Biome>();

	private const float riverGridSize = 64f;

	private const float minRiverWidth = 60f;

	private const float maxRiverWidth = 100f;

	private const float minRiverCurveWidth = 50f;

	private const float maxRiverCurveWidth = 80f;

	private const float minRiverCurveWaveLength = 50f;

	private const float maxRiverCurveWaveLength = 70f;

	private const int streams = 3000;

	private const float streamWidth = 20f;

	private const float meadowsMaxDistance = 5000f;

	private const float minDeepForestNoise = 0.4f;

	private const float minDeepForestDistance = 600f;

	private const float maxDeepForestDistance = 6000f;

	private const float deepForestForestFactorMax = 0.9f;

	private const float marshBiomeScale = 0.001f;

	private const float minMarshNoise = 0.6f;

	private const float minMarshDistance = 2000f;

	private const float maxMarshDistance = 8000f;

	private const float minMarshHeight = 0.05f;

	private const float maxMarshHeight = 0.25f;

	private const float heathBiomeScale = 0.001f;

	private const float minHeathNoise = 0.4f;

	private const float minHeathDistance = 3000f;

	private const float maxHeathDistance = 8000f;

	private const float darklandBiomeScale = 0.001f;

	private const float minDarklandNoise = 0.5f;

	private const float minDarklandDistance = 6000f;

	private const float maxDarklandDistance = 10000f;

	private const float oceanBiomeScale = 0.0005f;

	private const float oceanBiomeMinNoise = 0.4f;

	private const float oceanBiomeMaxNoise = 0.6f;

	private const float oceanBiomeMinDistance = 1000f;

	private const float oceanBiomeMinDistanceBuffer = 256f;

	private float m_minMountainDistance = 1000f;

	private const float mountainBaseHeightMin = 0.4f;

	private const float deepNorthMinDistance = 12000f;

	private const float deepNorthYOffset = 4000f;

	private const float ashlandsMinDistance = 12000f;

	private const float ashlandsYOffset = -4000f;

	public const float worldSize = 10000f;

	public const float waterEdge = 10500f;

	public static WorldGenerator instance => m_instance;

	public static void Initialize(World world)
	{
		m_instance = new WorldGenerator(world);
	}

	public static void Deitialize()
	{
		m_instance = null;
	}

	private WorldGenerator(World world)
	{
		m_world = world;
		m_version = m_world.m_worldGenVersion;
		VersionSetup(m_version);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_world.m_seed);
		m_offset0 = UnityEngine.Random.Range(-10000, 10000);
		m_offset1 = UnityEngine.Random.Range(-10000, 10000);
		m_offset2 = UnityEngine.Random.Range(-10000, 10000);
		m_offset3 = UnityEngine.Random.Range(-10000, 10000);
		m_riverSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		m_streamSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		m_offset4 = UnityEngine.Random.Range(-10000, 10000);
		if (!m_world.m_menu)
		{
			Pregenerate();
		}
		UnityEngine.Random.state = state;
	}

	private void VersionSetup(int version)
	{
		if (version < 1)
		{
			m_minMountainDistance = 1500f;
		}
	}

	private void Pregenerate()
	{
		FindMountains();
		FindLakes();
		m_rivers = PlaceRivers();
		m_streams = PlaceStreams();
	}

	public List<Vector2> GetMountains()
	{
		return m_mountains;
	}

	public List<Vector2> GetLakes()
	{
		return m_lakes;
	}

	public List<River> GetRivers()
	{
		return m_rivers;
	}

	public List<River> GetStreams()
	{
		return m_streams;
	}

	private void FindMountains()
	{
		DateTime now = DateTime.Now;
		List<Vector2> list = new List<Vector2>();
		for (float num = -10000f; num <= 10000f; num += 128f)
		{
			for (float num2 = -10000f; num2 <= 10000f; num2 += 128f)
			{
				if (!(new Vector2(num2, num).magnitude > 10000f) && GetBaseHeight(num2, num, menuTerrain: false) > 0.45f)
				{
					list.Add(new Vector2(num2, num));
				}
			}
		}
		m_mountains = MergePoints(list, 800f);
		_ = DateTime.Now - now;
	}

	private void FindLakes()
	{
		DateTime now = DateTime.Now;
		List<Vector2> list = new List<Vector2>();
		for (float num = -10000f; num <= 10000f; num += 128f)
		{
			for (float num2 = -10000f; num2 <= 10000f; num2 += 128f)
			{
				if (!(new Vector2(num2, num).magnitude > 10000f) && GetBaseHeight(num2, num, menuTerrain: false) < 0.05f)
				{
					list.Add(new Vector2(num2, num));
				}
			}
		}
		m_lakes = MergePoints(list, 800f);
		_ = DateTime.Now - now;
	}

	private List<Vector2> MergePoints(List<Vector2> points, float range)
	{
		List<Vector2> list = new List<Vector2>();
		while (points.Count > 0)
		{
			Vector2 vector = points[0];
			points.RemoveAt(0);
			while (points.Count > 0)
			{
				int num = FindClosest(points, vector, range);
				if (num == -1)
				{
					break;
				}
				vector = (vector + points[num]) * 0.5f;
				points[num] = points[points.Count - 1];
				points.RemoveAt(points.Count - 1);
			}
			list.Add(vector);
		}
		return list;
	}

	private int FindClosest(List<Vector2> points, Vector2 p, float maxDistance)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	private List<River> PlaceStreams()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_streamSeed);
		List<River> list = new List<River>();
		int num = 0;
		DateTime now = DateTime.Now;
		for (int i = 0; i < 3000; i++)
		{
			if (FindStreamStartPoint(100, 26f, 31f, out var p, out var _) && FindStreamEndPoint(100, 36f, 44f, p, 80f, 200f, out var end))
			{
				Vector2 center = (p + end) * 0.5f;
				float height = GetHeight(center.x, center.y);
				if (!(height < 26f) && !(height > 44f))
				{
					River river = new River();
					river.p0 = p;
					river.p1 = end;
					river.center = center;
					river.widthMax = 20f;
					river.widthMin = 20f;
					float num2 = Vector2.Distance(river.p0, river.p1);
					river.curveWidth = num2 / 15f;
					river.curveWavelength = num2 / 20f;
					list.Add(river);
					num++;
				}
			}
		}
		RenderRivers(list);
		UnityEngine.Random.state = state;
		_ = DateTime.Now - now;
		return list;
	}

	private bool FindStreamEndPoint(int iterations, float minHeight, float maxHeight, Vector2 start, float minLength, float maxLength, out Vector2 end)
	{
		float num = (maxLength - minLength) / (float)iterations;
		float num2 = maxLength;
		for (int i = 0; i < iterations; i++)
		{
			num2 -= num;
			float f = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
			Vector2 vector = start + new Vector2(Mathf.Sin(f), Mathf.Cos(f)) * num2;
			float height = GetHeight(vector.x, vector.y);
			if (height > minHeight && height < maxHeight)
			{
				end = vector;
				return true;
			}
		}
		end = Vector2.zero;
		return false;
	}

	private bool FindStreamStartPoint(int iterations, float minHeight, float maxHeight, out Vector2 p, out float starth)
	{
		for (int i = 0; i < iterations; i++)
		{
			float num = UnityEngine.Random.Range(-10000f, 10000f);
			float num2 = UnityEngine.Random.Range(-10000f, 10000f);
			float height = GetHeight(num, num2);
			if (height > minHeight && height < maxHeight)
			{
				p = new Vector2(num, num2);
				starth = height;
				return true;
			}
		}
		p = Vector2.zero;
		starth = 0f;
		return false;
	}

	private List<River> PlaceRivers()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_riverSeed);
		DateTime now = DateTime.Now;
		List<River> list = new List<River>();
		List<Vector2> list2 = new List<Vector2>(m_lakes);
		while (list2.Count > 1)
		{
			Vector2 vector = list2[0];
			int num = FindRandomRiverEnd(list, m_lakes, vector, 2000f, 0.4f, 128f);
			if (num == -1 && !HaveRiver(list, vector))
			{
				num = FindRandomRiverEnd(list, m_lakes, vector, 5000f, 0.4f, 128f);
			}
			if (num != -1)
			{
				River river = new River();
				river.p0 = vector;
				river.p1 = m_lakes[num];
				river.center = (river.p0 + river.p1) * 0.5f;
				river.widthMax = UnityEngine.Random.Range(60f, 100f);
				river.widthMin = UnityEngine.Random.Range(60f, river.widthMax);
				float num2 = Vector2.Distance(river.p0, river.p1);
				river.curveWidth = num2 / 15f;
				river.curveWavelength = num2 / 20f;
				list.Add(river);
			}
			else
			{
				list2.RemoveAt(0);
			}
		}
		RenderRivers(list);
		_ = DateTime.Now - now;
		UnityEngine.Random.state = state;
		return list;
	}

	private int FindClosestRiverEnd(List<River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num && !HaveRiver(rivers, p, points[i]) && IsRiverAllowed(p, points[i], checkStep, heightLimit))
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	private int FindRandomRiverEnd(List<River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p) && Vector2.Distance(p, points[i]) < maxDistance && !HaveRiver(rivers, p, points[i]) && IsRiverAllowed(p, points[i], checkStep, heightLimit))
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return -1;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private bool HaveRiver(List<River> rivers, Vector2 p0)
	{
		foreach (River river in rivers)
		{
			if (river.p0 == p0 || river.p1 == p0)
			{
				return true;
			}
		}
		return false;
	}

	private bool HaveRiver(List<River> rivers, Vector2 p0, Vector2 p1)
	{
		foreach (River river in rivers)
		{
			if ((river.p0 == p0 && river.p1 == p1) || (river.p0 == p1 && river.p1 == p0))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsRiverAllowed(Vector2 p0, Vector2 p1, float step, float heightLimit)
	{
		float num = Vector2.Distance(p0, p1);
		Vector2 normalized = (p1 - p0).normalized;
		bool flag = true;
		for (float num2 = step; num2 <= num - step; num2 += step)
		{
			Vector2 vector = p0 + normalized * num2;
			float baseHeight = GetBaseHeight(vector.x, vector.y, menuTerrain: false);
			if (baseHeight > heightLimit)
			{
				return false;
			}
			if (baseHeight > 0.05f)
			{
				flag = false;
			}
		}
		if (flag)
		{
			return false;
		}
		return true;
	}

	private void RenderRivers(List<River> rivers)
	{
		DateTime now = DateTime.Now;
		Dictionary<Vector2i, List<RiverPoint>> dictionary = new Dictionary<Vector2i, List<RiverPoint>>();
		foreach (River river in rivers)
		{
			float num = river.widthMin / 8f;
			Vector2 normalized = (river.p1 - river.p0).normalized;
			Vector2 vector = new Vector2(0f - normalized.y, normalized.x);
			float num2 = Vector2.Distance(river.p0, river.p1);
			for (float num3 = 0f; num3 <= num2; num3 += num)
			{
				float num4 = num3 / river.curveWavelength;
				float num5 = Mathf.Sin(num4) * Mathf.Sin(num4 * 0.63412f) * Mathf.Sin(num4 * 0.33412f) * river.curveWidth;
				float r = UnityEngine.Random.Range(river.widthMin, river.widthMax);
				Vector2 p = river.p0 + normalized * num3 + vector * num5;
				AddRiverPoint(dictionary, p, r, river);
			}
		}
		foreach (KeyValuePair<Vector2i, List<RiverPoint>> item in dictionary)
		{
			if (m_riverPoints.TryGetValue(item.Key, out var value))
			{
				List<RiverPoint> list = new List<RiverPoint>(value);
				list.AddRange(item.Value);
				m_riverPoints[item.Key] = list.ToArray();
			}
			else
			{
				RiverPoint[] value2 = item.Value.ToArray();
				m_riverPoints.Add(item.Key, value2);
			}
		}
		_ = DateTime.Now - now;
	}

	private void AddRiverPoint(Dictionary<Vector2i, List<RiverPoint>> riverPoints, Vector2 p, float r, River river)
	{
		Vector2i riverGrid = GetRiverGrid(p.x, p.y);
		int num = Mathf.CeilToInt(r / 64f);
		for (int i = riverGrid.y - num; i <= riverGrid.y + num; i++)
		{
			for (int j = riverGrid.x - num; j <= riverGrid.x + num; j++)
			{
				Vector2i grid = new Vector2i(j, i);
				if (InsideRiverGrid(grid, p, r))
				{
					AddRiverPoint(riverPoints, grid, p, r, river);
				}
			}
		}
	}

	private void AddRiverPoint(Dictionary<Vector2i, List<RiverPoint>> riverPoints, Vector2i grid, Vector2 p, float r, River river)
	{
		if (riverPoints.TryGetValue(grid, out var value))
		{
			value.Add(new RiverPoint(p, r));
			return;
		}
		value = new List<RiverPoint>();
		value.Add(new RiverPoint(p, r));
		riverPoints.Add(grid, value);
	}

	public bool InsideRiverGrid(Vector2i grid, Vector2 p, float r)
	{
		Vector2 vector = new Vector2((float)grid.x * 64f, (float)grid.y * 64f);
		Vector2 vector2 = p - vector;
		if (Mathf.Abs(vector2.x) < r + 32f)
		{
			return Mathf.Abs(vector2.y) < r + 32f;
		}
		return false;
	}

	public Vector2i GetRiverGrid(float wx, float wy)
	{
		int x = Mathf.FloorToInt((wx + 32f) / 64f);
		int y = Mathf.FloorToInt((wy + 32f) / 64f);
		return new Vector2i(x, y);
	}

	private void GetRiverWeight(float wx, float wy, out float weight, out float width)
	{
		Vector2i riverGrid = GetRiverGrid(wx, wy);
		m_riverCacheLock.EnterReadLock();
		if (riverGrid == m_cachedRiverGrid)
		{
			if (m_cachedRiverPoints != null)
			{
				GetWeight(m_cachedRiverPoints, wx, wy, out weight, out width);
				m_riverCacheLock.ExitReadLock();
			}
			else
			{
				weight = 0f;
				width = 0f;
				m_riverCacheLock.ExitReadLock();
			}
			return;
		}
		m_riverCacheLock.ExitReadLock();
		if (m_riverPoints.TryGetValue(riverGrid, out var value))
		{
			GetWeight(value, wx, wy, out weight, out width);
			m_riverCacheLock.EnterWriteLock();
			m_cachedRiverGrid = riverGrid;
			m_cachedRiverPoints = value;
			m_riverCacheLock.ExitWriteLock();
		}
		else
		{
			m_riverCacheLock.EnterWriteLock();
			m_cachedRiverGrid = riverGrid;
			m_cachedRiverPoints = null;
			m_riverCacheLock.ExitWriteLock();
			weight = 0f;
			width = 0f;
		}
	}

	private void GetWeight(RiverPoint[] points, float wx, float wy, out float weight, out float width)
	{
		Vector2 vector = new Vector2(wx, wy);
		weight = 0f;
		width = 0f;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < points.Length; i++)
		{
			RiverPoint riverPoint = points[i];
			float num3 = Vector2.SqrMagnitude(riverPoint.p - vector);
			if (num3 < riverPoint.w2)
			{
				float num4 = Mathf.Sqrt(num3);
				float num5 = 1f - num4 / riverPoint.w;
				if (num5 > weight)
				{
					weight = num5;
				}
				num += riverPoint.w * num5;
				num2 += num5;
			}
		}
		if (num2 > 0f)
		{
			width = num / num2;
		}
	}

	private void GenerateBiomes()
	{
		m_biomes = new List<Heightmap.Biome>();
		int num = 400000000;
		for (int i = 0; i < num; i++)
		{
			m_biomes[i] = Heightmap.Biome.Meadows;
		}
	}

	public Heightmap.BiomeArea GetBiomeArea(Vector3 point)
	{
		Heightmap.Biome biome = GetBiome(point);
		Heightmap.Biome biome2 = GetBiome(point - new Vector3(-64f, 0f, -64f));
		Heightmap.Biome biome3 = GetBiome(point - new Vector3(64f, 0f, -64f));
		Heightmap.Biome biome4 = GetBiome(point - new Vector3(64f, 0f, 64f));
		Heightmap.Biome biome5 = GetBiome(point - new Vector3(-64f, 0f, 64f));
		Heightmap.Biome biome6 = GetBiome(point - new Vector3(-64f, 0f, 0f));
		Heightmap.Biome biome7 = GetBiome(point - new Vector3(64f, 0f, 0f));
		Heightmap.Biome biome8 = GetBiome(point - new Vector3(0f, 0f, -64f));
		Heightmap.Biome biome9 = GetBiome(point - new Vector3(0f, 0f, 64f));
		if (biome == biome2 && biome == biome3 && biome == biome4 && biome == biome5 && biome == biome6 && biome == biome7 && biome == biome8 && biome == biome9)
		{
			return Heightmap.BiomeArea.Median;
		}
		return Heightmap.BiomeArea.Edge;
	}

	public Heightmap.Biome GetBiome(Vector3 point)
	{
		return GetBiome(point.x, point.z);
	}

	public Heightmap.Biome GetBiome(float wx, float wy)
	{
		if (m_world.m_menu)
		{
			if (GetBaseHeight(wx, wy, menuTerrain: true) >= 0.4f)
			{
				return Heightmap.Biome.Mountain;
			}
			return Heightmap.Biome.BlackForest;
		}
		float magnitude = new Vector2(wx, wy).magnitude;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		float num = WorldAngle(wx, wy) * 100f;
		if (new Vector2(wx, wy + -4000f).magnitude > 12000f + num)
		{
			return Heightmap.Biome.AshLands;
		}
		if ((double)baseHeight <= 0.02)
		{
			return Heightmap.Biome.Ocean;
		}
		if (new Vector2(wx, wy + 4000f).magnitude > 12000f + num)
		{
			if (baseHeight > 0.4f)
			{
				return Heightmap.Biome.Mountain;
			}
			return Heightmap.Biome.DeepNorth;
		}
		if (baseHeight > 0.4f)
		{
			return Heightmap.Biome.Mountain;
		}
		if (Mathf.PerlinNoise((m_offset0 + wx) * 0.001f, (m_offset0 + wy) * 0.001f) > 0.6f && magnitude > 2000f && magnitude < 8000f && baseHeight > 0.05f && baseHeight < 0.25f)
		{
			return Heightmap.Biome.Swamp;
		}
		if (Mathf.PerlinNoise((m_offset4 + wx) * 0.001f, (m_offset4 + wy) * 0.001f) > 0.5f && magnitude > 6000f + num && magnitude < 10000f)
		{
			return Heightmap.Biome.Mistlands;
		}
		if (Mathf.PerlinNoise((m_offset1 + wx) * 0.001f, (m_offset1 + wy) * 0.001f) > 0.4f && magnitude > 3000f + num && magnitude < 8000f)
		{
			return Heightmap.Biome.Plains;
		}
		if (Mathf.PerlinNoise((m_offset2 + wx) * 0.001f, (m_offset2 + wy) * 0.001f) > 0.4f && magnitude > 600f + num && magnitude < 6000f)
		{
			return Heightmap.Biome.BlackForest;
		}
		if (magnitude > 5000f + num)
		{
			return Heightmap.Biome.BlackForest;
		}
		return Heightmap.Biome.Meadows;
	}

	private float WorldAngle(float wx, float wy)
	{
		return Mathf.Sin(Mathf.Atan2(wx, wy) * 20f);
	}

	private float GetBaseHeight(float wx, float wy, bool menuTerrain)
	{
		if (menuTerrain)
		{
			wx += 100000f + m_offset0;
			wy += 100000f + m_offset1;
			float num = 0f;
			num += Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
			num += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * num * 0.9f;
			num += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * num;
			return num - 0.07f;
		}
		float num2 = Utils.Length(wx, wy);
		wx += 100000f + m_offset0;
		wy += 100000f + m_offset1;
		float num3 = 0f;
		num3 += Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
		num3 += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * num3 * 0.9f;
		num3 += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * num3;
		num3 -= 0.07f;
		float num4 = Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.123f, wy * 0.002f * 0.25f + 0.15123f);
		float num5 = Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.321f, wy * 0.002f * 0.25f + 0.231f);
		float v = Mathf.Abs(num4 - num5);
		float num6 = 1f - Utils.LerpStep(0.02f, 0.12f, v);
		num6 *= Utils.SmoothStep(744f, 1000f, num2);
		num3 *= 1f - num6;
		if (num2 > 10000f)
		{
			float t = Utils.LerpStep(10000f, 10500f, num2);
			num3 = Mathf.Lerp(num3, -0.2f, t);
			float num7 = 10490f;
			if (num2 > num7)
			{
				float t2 = Utils.LerpStep(num7, 10500f, num2);
				num3 = Mathf.Lerp(num3, -2f, t2);
			}
		}
		if (num2 < m_minMountainDistance && num3 > 0.28f)
		{
			float t3 = Mathf.Clamp01((num3 - 0.28f) / 0.099999994f);
			num3 = Mathf.Lerp(Mathf.Lerp(0.28f, 0.38f, t3), num3, Utils.LerpStep(m_minMountainDistance - 400f, m_minMountainDistance, num2));
		}
		return num3;
	}

	private float AddRivers(float wx, float wy, float h)
	{
		GetRiverWeight(wx, wy, out var weight, out var width);
		if (weight <= 0f)
		{
			return h;
		}
		float t = Utils.LerpStep(20f, 60f, width);
		float num = Mathf.Lerp(0.14f, 0.12f, t);
		float num2 = Mathf.Lerp(0.139f, 0.128f, t);
		if (h > num)
		{
			h = Mathf.Lerp(h, num, weight);
		}
		if (h > num2)
		{
			float t2 = Utils.LerpStep(0.85f, 1f, weight);
			h = Mathf.Lerp(h, num2, t2);
		}
		return h;
	}

	public float GetHeight(float wx, float wy)
	{
		Heightmap.Biome biome = GetBiome(wx, wy);
		return GetBiomeHeight(biome, wx, wy);
	}

	public float GetBiomeHeight(Heightmap.Biome biome, float wx, float wy)
	{
		if (m_world.m_menu)
		{
			if (biome == Heightmap.Biome.Mountain)
			{
				return GetSnowMountainHeight(wx, wy, menu: true) * 200f;
			}
			return GetMenuHeight(wx, wy) * 200f;
		}
		switch (biome)
		{
		case Heightmap.Biome.Swamp:
			return GetMarshHeight(wx, wy) * 200f;
		case Heightmap.Biome.DeepNorth:
			return GetDeepNorthHeight(wx, wy) * 200f;
		case Heightmap.Biome.Mountain:
			return GetSnowMountainHeight(wx, wy, menu: false) * 200f;
		case Heightmap.Biome.BlackForest:
			return GetForestHeight(wx, wy) * 200f;
		case Heightmap.Biome.Ocean:
			return GetOceanHeight(wx, wy) * 200f;
		case Heightmap.Biome.AshLands:
			return GetAshlandsHeight(wx, wy) * 200f;
		case Heightmap.Biome.Plains:
			return GetPlainsHeight(wx, wy) * 200f;
		case Heightmap.Biome.Meadows:
			return GetMeadowsHeight(wx, wy) * 200f;
		case Heightmap.Biome.Mistlands:
			return GetForestHeight(wx, wy) * 200f;
		default:
			return 0f;
		}
	}

	private float GetMarshHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = 0.137f;
		wx += 100000f;
		wy += 100000f;
		float num2 = Mathf.PerlinNoise(wx * 0.04f, wy * 0.04f) * Mathf.PerlinNoise(wx * 0.08f, wy * 0.08f);
		num += num2 * 0.03f;
		num = AddRivers(wx2, wy2, num);
		num += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		return num + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	private float GetMeadowsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
		float num2 = baseHeight;
		num2 += num * 0.1f;
		float num3 = 0.15f;
		float num4 = num2 - num3;
		float num5 = Mathf.Clamp01(baseHeight / 0.4f);
		if (num4 > 0f)
		{
			num2 -= num4 * (1f - num5) * 0.75f;
		}
		num2 = AddRivers(wx2, wy2, num2);
		num2 += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		return num2 + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	private float GetForestHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
		baseHeight += num * 0.1f;
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		return baseHeight + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	private float GetPlainsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
		float num2 = baseHeight;
		num2 += num * 0.1f;
		float num3 = 0.15f;
		float num4 = num2 - num3;
		float num5 = Mathf.Clamp01(baseHeight / 0.4f);
		if (num4 > 0f)
		{
			num2 -= num4 * (1f - num5) * 0.75f;
		}
		num2 = AddRivers(wx2, wy2, num2);
		num2 += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		return num2 + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	private float GetMenuHeight(float wx, float wy)
	{
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: true);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
		return baseHeight + num * 0.1f + Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	private float GetAshlandsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num * 0.5f;
		baseHeight += num * 0.1f;
		baseHeight += 0.1f;
		baseHeight += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		baseHeight += Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
		return AddRivers(wx2, wy2, baseHeight);
	}

	private float GetEdgeHeight(float wx, float wy)
	{
		float magnitude = new Vector2(wx, wy).magnitude;
		float num = 10490f;
		if (magnitude > num)
		{
			float num2 = Utils.LerpStep(num, 10500f, magnitude);
			return -2f * num2;
		}
		float t = Utils.LerpStep(10000f, 10100f, magnitude);
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		baseHeight = Mathf.Lerp(baseHeight, 0f, t);
		return AddRivers(wx, wy, baseHeight);
	}

	private float GetOceanHeight(float wx, float wy)
	{
		return GetBaseHeight(wx, wy, menuTerrain: false);
	}

	private float BaseHeightTilt(float wx, float wy)
	{
		float baseHeight = GetBaseHeight(wx - 1f, wy, menuTerrain: false);
		float baseHeight2 = GetBaseHeight(wx + 1f, wy, menuTerrain: false);
		float baseHeight3 = GetBaseHeight(wx, wy - 1f, menuTerrain: false);
		float baseHeight4 = GetBaseHeight(wx, wy + 1f, menuTerrain: false);
		return Mathf.Abs(baseHeight2 - baseHeight) + Mathf.Abs(baseHeight3 - baseHeight4);
	}

	private float GetSnowMountainHeight(float wx, float wy, bool menu)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menu);
		float num = BaseHeightTilt(wx, wy);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num2 = baseHeight - 0.4f;
		baseHeight += num2;
		float num3 = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num3 += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num3 * 0.5f;
		baseHeight += num3 * 0.2f;
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		baseHeight += Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
		return baseHeight + Mathf.PerlinNoise(wx * 0.2f, wy * 0.2f) * 2f * num;
	}

	private float GetDeepNorthHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx += 100000f + m_offset3;
		wy += 100000f + m_offset3;
		float num = Mathf.Max(0f, baseHeight - 0.4f);
		baseHeight += num;
		float num2 = Mathf.PerlinNoise(wx * 0.01f, wy * 0.01f) * Mathf.PerlinNoise(wx * 0.02f, wy * 0.02f);
		num2 += Mathf.PerlinNoise(wx * 0.05f, wy * 0.05f) * Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * num2 * 0.5f;
		baseHeight += num2 * 0.2f;
		baseHeight *= 1.2f;
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight += Mathf.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.01f;
		return baseHeight + Mathf.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003f;
	}

	public static bool InForest(Vector3 pos)
	{
		return GetForestFactor(pos) < 1.15f;
	}

	public static float GetForestFactor(Vector3 pos)
	{
		float num = 0.4f;
		return Utils.Fbm(pos * 0.01f * num, 3, 1.6f, 0.7f);
	}

	public void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 vector = center;
		Vector3 vector2 = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector3 = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector4 = center + new Vector3(vector3.x, 0f, vector3.y);
			float height = GetHeight(vector4.x, vector4.z);
			if (height < num3)
			{
				num3 = height;
				vector2 = vector4;
			}
			if (height > num2)
			{
				num2 = height;
				vector = vector4;
			}
		}
		delta = num2 - num3;
		slopeDirection = Vector3.Normalize(vector2 - vector);
	}

	public int GetSeed()
	{
		return m_world.m_seed;
	}
}
