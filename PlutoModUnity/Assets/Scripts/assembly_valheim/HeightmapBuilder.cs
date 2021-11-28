using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class HeightmapBuilder
{
	public class HMBuildData
	{
		public Vector3 m_center;

		public int m_width;

		public float m_scale;

		public bool m_distantLod;

		public bool m_menu;

		public WorldGenerator m_worldGen;

		public Heightmap.Biome[] m_cornerBiomes;

		public List<float> m_baseHeights;

		public HMBuildData(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			m_center = center;
			m_width = width;
			m_scale = scale;
			m_distantLod = distantLod;
			m_worldGen = worldGen;
		}

		public bool IsEqual(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			if (m_center == center && m_width == width && m_scale == scale && m_distantLod == distantLod)
			{
				return m_worldGen == worldGen;
			}
			return false;
		}
	}

	private static HeightmapBuilder m_instance;

	private const int m_maxReadyQueue = 16;

	private List<HMBuildData> m_toBuild = new List<HMBuildData>();

	private List<HMBuildData> m_ready = new List<HMBuildData>();

	private Thread m_builder;

	private Mutex m_lock = new Mutex();

	private bool m_stop;

	public static HeightmapBuilder instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = new HeightmapBuilder();
			}
			return m_instance;
		}
	}

	public HeightmapBuilder()
	{
		m_instance = this;
		m_builder = new Thread(BuildThread);
		m_builder.Start();
	}

	public void Dispose()
	{
		if (m_builder != null)
		{
			ZLog.Log("Stoping build thread");
			m_lock.WaitOne();
			m_stop = true;
			m_builder.Abort();
			m_lock.ReleaseMutex();
			m_builder = null;
		}
		if (m_lock != null)
		{
			m_lock.Close();
			m_lock = null;
		}
	}

	private void BuildThread()
	{
		ZLog.Log("Builder started");
		while (!m_stop)
		{
			m_lock.WaitOne();
			bool num = m_toBuild.Count > 0;
			m_lock.ReleaseMutex();
			if (num)
			{
				m_lock.WaitOne();
				HMBuildData hMBuildData = m_toBuild[0];
				m_lock.ReleaseMutex();
				new Stopwatch().Start();
				Build(hMBuildData);
				m_lock.WaitOne();
				m_toBuild.Remove(hMBuildData);
				m_ready.Add(hMBuildData);
				while (m_ready.Count > 16)
				{
					m_ready.RemoveAt(0);
				}
				m_lock.ReleaseMutex();
			}
			Thread.Sleep(10);
		}
	}

	private void Build(HMBuildData data)
	{
		int num = data.m_width + 1;
		int num2 = num * num;
		Vector3 vector = data.m_center + new Vector3((float)data.m_width * data.m_scale * -0.5f, 0f, (float)data.m_width * data.m_scale * -0.5f);
		WorldGenerator worldGen = data.m_worldGen;
		data.m_cornerBiomes = new Heightmap.Biome[4];
		data.m_cornerBiomes[0] = worldGen.GetBiome(vector.x, vector.z);
		data.m_cornerBiomes[1] = worldGen.GetBiome(vector.x + (float)data.m_width * data.m_scale, vector.z);
		data.m_cornerBiomes[2] = worldGen.GetBiome(vector.x, vector.z + (float)data.m_width * data.m_scale);
		data.m_cornerBiomes[3] = worldGen.GetBiome(vector.x + (float)data.m_width * data.m_scale, vector.z + (float)data.m_width * data.m_scale);
		Heightmap.Biome biome = data.m_cornerBiomes[0];
		Heightmap.Biome biome2 = data.m_cornerBiomes[1];
		Heightmap.Biome biome3 = data.m_cornerBiomes[2];
		Heightmap.Biome biome4 = data.m_cornerBiomes[3];
		data.m_baseHeights = new List<float>(num * num);
		for (int i = 0; i < num2; i++)
		{
			data.m_baseHeights.Add(0f);
		}
		for (int j = 0; j < num; j++)
		{
			float wy = vector.z + (float)j * data.m_scale;
			float t = Mathf.SmoothStep(0f, 1f, (float)j / (float)data.m_width);
			for (int k = 0; k < num; k++)
			{
				float wx = vector.x + (float)k * data.m_scale;
				float t2 = Mathf.SmoothStep(0f, 1f, (float)k / (float)data.m_width);
				float num3 = 0f;
				if (data.m_distantLod)
				{
					Heightmap.Biome biome5 = worldGen.GetBiome(wx, wy);
					num3 = worldGen.GetBiomeHeight(biome5, wx, wy);
				}
				else if (biome3 == biome && biome2 == biome && biome4 == biome)
				{
					num3 = worldGen.GetBiomeHeight(biome, wx, wy);
				}
				else
				{
					float biomeHeight = worldGen.GetBiomeHeight(biome, wx, wy);
					float biomeHeight2 = worldGen.GetBiomeHeight(biome2, wx, wy);
					float biomeHeight3 = worldGen.GetBiomeHeight(biome3, wx, wy);
					float biomeHeight4 = worldGen.GetBiomeHeight(biome4, wx, wy);
					float a = Mathf.Lerp(biomeHeight, biomeHeight2, t2);
					float b = Mathf.Lerp(biomeHeight3, biomeHeight4, t2);
					num3 = Mathf.Lerp(a, b, t);
				}
				data.m_baseHeights[j * num + k] = num3;
			}
		}
		if (!data.m_distantLod)
		{
			return;
		}
		for (int l = 0; l < 4; l++)
		{
			List<float> list = new List<float>(data.m_baseHeights);
			for (int m = 1; m < num - 1; m++)
			{
				for (int n = 1; n < num - 1; n++)
				{
					float num4 = list[m * num + n];
					float num5 = list[(m - 1) * num + n];
					float num6 = list[(m + 1) * num + n];
					float num7 = list[m * num + n - 1];
					float num8 = list[m * num + n + 1];
					if (Mathf.Abs(num4 - num5) > 10f)
					{
						num4 = (num4 + num5) * 0.5f;
					}
					if (Mathf.Abs(num4 - num6) > 10f)
					{
						num4 = (num4 + num6) * 0.5f;
					}
					if (Mathf.Abs(num4 - num7) > 10f)
					{
						num4 = (num4 + num7) * 0.5f;
					}
					if (Mathf.Abs(num4 - num8) > 10f)
					{
						num4 = (num4 + num8) * 0.5f;
					}
					data.m_baseHeights[m * num + n] = num4;
				}
			}
		}
	}

	public HMBuildData RequestTerrainSync(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		HMBuildData hMBuildData;
		do
		{
			hMBuildData = RequestTerrain(center, width, scale, distantLod, worldGen);
		}
		while (hMBuildData == null);
		return hMBuildData;
	}

	public HMBuildData RequestTerrain(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		m_lock.WaitOne();
		for (int i = 0; i < m_ready.Count; i++)
		{
			HMBuildData hMBuildData = m_ready[i];
			if (hMBuildData.IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_ready.RemoveAt(i);
				m_lock.ReleaseMutex();
				return hMBuildData;
			}
		}
		for (int j = 0; j < m_toBuild.Count; j++)
		{
			if (m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return null;
			}
		}
		m_toBuild.Add(new HMBuildData(center, width, scale, distantLod, worldGen));
		m_lock.ReleaseMutex();
		return null;
	}

	public bool IsTerrainReady(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		m_lock.WaitOne();
		for (int i = 0; i < m_ready.Count; i++)
		{
			if (m_ready[i].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return true;
			}
		}
		for (int j = 0; j < m_toBuild.Count; j++)
		{
			if (m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return false;
			}
		}
		m_toBuild.Add(new HMBuildData(center, width, scale, distantLod, worldGen));
		m_lock.ReleaseMutex();
		return false;
	}
}
