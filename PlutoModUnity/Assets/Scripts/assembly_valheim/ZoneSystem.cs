using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneSystem : MonoBehaviour
{
	private class ZoneData
	{
		public GameObject m_root;

		public float m_ttl;
	}

	private class ClearArea
	{
		public Vector3 m_center;

		public float m_radius;

		public ClearArea(Vector3 p, float r)
		{
			m_center = p;
			m_radius = r;
		}
	}

	[Serializable]
	public class ZoneVegetation
	{
		public string m_name = "veg";

		public GameObject m_prefab;

		public bool m_enable = true;

		public float m_min;

		public float m_max = 10f;

		public bool m_forcePlacement;

		public float m_scaleMin = 1f;

		public float m_scaleMax = 1f;

		public float m_randTilt;

		public float m_chanceToUseGroundTilt;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public bool m_blockCheck = true;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		public float m_minTilt;

		public float m_maxTilt = 90f;

		public float m_terrainDeltaRadius;

		public float m_maxTerrainDelta = 2f;

		public float m_minTerrainDelta;

		public bool m_snapToWater;

		public float m_groundOffset;

		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[HideInInspector]
		public bool m_foldout;

		public ZoneVegetation Clone()
		{
			return MemberwiseClone() as ZoneVegetation;
		}
	}

	[Serializable]
	public class ZoneLocation
	{
		public bool m_enable = true;

		public string m_prefabName;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public int m_quantity;

		public float m_chanceToSpawn = 10f;

		public bool m_prioritized;

		public bool m_centerFirst;

		public bool m_unique;

		public string m_group = "";

		public float m_minDistanceFromSimilar;

		public bool m_iconAlways;

		public bool m_iconPlaced;

		public bool m_randomRotation = true;

		public bool m_slopeRotation;

		public bool m_snapToWater;

		public float m_maxTerrainDelta = 2f;

		public float m_minTerrainDelta;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[Space(10f)]
		public float m_minDistance;

		public float m_maxDistance;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[NonSerialized]
		public GameObject m_prefab;

		[NonSerialized]
		public int m_hash;

		[NonSerialized]
		public Location m_location;

		[NonSerialized]
		public float m_interiorRadius = 10f;

		[NonSerialized]
		public float m_exteriorRadius = 10f;

		[NonSerialized]
		public List<ZNetView> m_netViews = new List<ZNetView>();

		[NonSerialized]
		public List<RandomSpawn> m_randomSpawns = new List<RandomSpawn>();

		[HideInInspector]
		public bool m_foldout;

		public ZoneLocation Clone()
		{
			return MemberwiseClone() as ZoneLocation;
		}
	}

	public struct LocationInstance
	{
		public ZoneLocation m_location;

		public Vector3 m_position;

		public bool m_placed;
	}

	public enum SpawnMode
	{
		Full,
		Client,
		Ghost
	}

	private Dictionary<Vector3, string> tempIconList = new Dictionary<Vector3, string>();

	private RaycastHit[] rayHits = new RaycastHit[200];

	private static ZoneSystem m_instance;

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_vegetationFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_locationFolded = new List<Heightmap.Biome>();

	[NonSerialized]
	public bool m_drawLocations;

	[NonSerialized]
	public string m_drawLocationsFilter = "";

	[Tooltip("Zones to load around center sector")]
	public int m_activeArea = 1;

	public int m_activeDistantArea = 1;

	[Tooltip("Zone size, should match netscene sector size")]
	public float m_zoneSize = 64f;

	[Tooltip("Time before destroying inactive zone")]
	public float m_zoneTTL = 4f;

	[Tooltip("Time before spawning active zone")]
	public float m_zoneTTS = 4f;

	public GameObject m_zonePrefab;

	public GameObject m_zoneCtrlPrefab;

	public GameObject m_locationProxyPrefab;

	public float m_waterLevel = 30f;

	[Header("Versions")]
	public int m_pgwVersion = 53;

	public int m_locationVersion = 1;

	[Header("Generation data")]
	public List<string> m_locationScenes = new List<string>();

	public List<ZoneVegetation> m_vegetation = new List<ZoneVegetation>();

	public List<ZoneLocation> m_locations = new List<ZoneLocation>();

	private Dictionary<int, ZoneLocation> m_locationsByHash = new Dictionary<int, ZoneLocation>();

	private bool m_error;

	private bool m_didZoneTest;

	private int m_terrainRayMask;

	private int m_blockRayMask;

	private int m_solidRayMask;

	private float m_updateTimer;

	private Dictionary<Vector2i, ZoneData> m_zones = new Dictionary<Vector2i, ZoneData>();

	private HashSet<Vector2i> m_generatedZones = new HashSet<Vector2i>();

	private bool m_locationsGenerated;

	[HideInInspector]
	public Dictionary<Vector2i, LocationInstance> m_locationInstances = new Dictionary<Vector2i, LocationInstance>();

	private Dictionary<Vector3, string> m_locationIcons = new Dictionary<Vector3, string>();

	private HashSet<string> m_globalKeys = new HashSet<string>();

	private HashSet<Vector2i> m_tempGeneratedZonesSaveClone;

	private HashSet<string> m_tempGlobalKeysSaveClone;

	private List<LocationInstance> m_tempLocationsSaveClone;

	private bool m_tempLocationsGeneratedSaveClone;

	private List<ClearArea> m_tempClearAreas = new List<ClearArea>();

	private List<GameObject> m_tempSpawnedObjects = new List<GameObject>();

	public static ZoneSystem instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_terrainRayMask = LayerMask.GetMask("terrain");
		m_blockRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
		m_solidRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
		foreach (string locationScene in m_locationScenes)
		{
			if (SceneManager.GetSceneByName(locationScene).IsValid())
			{
				ZLog.Log("Location scene " + locationScene + " already loaded");
			}
			else
			{
				SceneManager.LoadScene(locationScene, LoadSceneMode.Additive);
			}
		}
		ZLog.Log("Zonesystem Awake " + Time.frameCount);
	}

	private void Start()
	{
		ZLog.Log("Zonesystem Start " + Time.frameCount);
		SetupLocations();
		ValidateVegetation();
		ZRoutedRpc zRoutedRpc = ZRoutedRpc.instance;
		zRoutedRpc.m_onNewPeer = (Action<long>)Delegate.Combine(zRoutedRpc.m_onNewPeer, new Action<long>(OnNewPeer));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string>("SetGlobalKey", RPC_SetGlobalKey);
			return;
		}
		ZRoutedRpc.instance.Register<List<string>>("GlobalKeys", RPC_GlobalKeys);
		ZRoutedRpc.instance.Register<ZPackage>("LocationIcons", RPC_LocationIcons);
	}

	public void GenerateLocationsIfNeeded()
	{
		if (!m_locationsGenerated && ZNet.instance.IsServer())
		{
			GenerateLocations();
		}
	}

	private void SendGlobalKeys(long peer)
	{
		List<string> list = new List<string>(m_globalKeys);
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "GlobalKeys", list);
	}

	private void RPC_GlobalKeys(long sender, List<string> keys)
	{
		ZLog.Log("client got keys " + keys.Count);
		m_globalKeys.Clear();
		foreach (string key in keys)
		{
			m_globalKeys.Add(key);
		}
	}

	private void SendLocationIcons(long peer)
	{
		ZPackage zPackage = new ZPackage();
		tempIconList.Clear();
		GetLocationIcons(tempIconList);
		zPackage.Write(tempIconList.Count);
		foreach (KeyValuePair<Vector3, string> tempIcon in tempIconList)
		{
			zPackage.Write(tempIcon.Key);
			zPackage.Write(tempIcon.Value);
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "LocationIcons", zPackage);
	}

	private void RPC_LocationIcons(long sender, ZPackage pkg)
	{
		ZLog.Log("client got location icons");
		m_locationIcons.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Vector3 key = pkg.ReadVector3();
			string value = pkg.ReadString();
			m_locationIcons[key] = value;
		}
		ZLog.Log("Icons:" + num);
	}

	private void OnNewPeer(long peerID)
	{
		if (ZNet.instance.IsServer())
		{
			ZLog.Log("Server: New peer connected,sending global keys");
			SendGlobalKeys(peerID);
			SendLocationIcons(peerID);
		}
	}

	private void SetupLocations()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		List<Location> list = new List<Location>();
		GameObject[] array2 = array;
		foreach (GameObject gameObject in array2)
		{
			if (gameObject.name == "_Locations")
			{
				Location[] componentsInChildren = gameObject.GetComponentsInChildren<Location>(includeInactive: true);
				list.AddRange(componentsInChildren);
			}
		}
		List<LocationList> allLocationLists = LocationList.GetAllLocationLists();
		allLocationLists.Sort((LocationList a, LocationList b) => a.m_sortOrder.CompareTo(b.m_sortOrder));
		foreach (LocationList item in allLocationLists)
		{
			m_locations.AddRange(item.m_locations);
			m_vegetation.AddRange(item.m_vegetation);
			foreach (EnvSetup environment in item.m_environments)
			{
				EnvMan.instance.AppendEnvironment(environment);
			}
			foreach (BiomeEnvSetup biomeEnvironment in item.m_biomeEnvironments)
			{
				EnvMan.instance.AppendBiomeSetup(biomeEnvironment);
			}
			ZLog.Log($"Added {item.m_locations.Count} locations, {item.m_vegetation.Count} vegetations, {item.m_environments.Count} environments, {item.m_biomeEnvironments.Count} biome env-setups from " + item.gameObject.scene.name);
		}
		foreach (Location item2 in list)
		{
			if (item2.transform.gameObject.activeInHierarchy)
			{
				m_error = true;
			}
		}
		foreach (ZoneLocation location2 in m_locations)
		{
			Transform transform = null;
			foreach (Location item3 in list)
			{
				if (item3.gameObject.name == location2.m_prefabName)
				{
					transform = item3.transform;
					break;
				}
			}
			if (transform == null && !location2.m_enable)
			{
				continue;
			}
			location2.m_prefab = transform.gameObject;
			location2.m_hash = location2.m_prefab.name.GetStableHashCode();
			Location location = (location2.m_location = location2.m_prefab.GetComponentInChildren<Location>());
			location2.m_interiorRadius = (location.m_hasInterior ? location.m_interiorRadius : 0f);
			location2.m_exteriorRadius = location.m_exteriorRadius;
			if (Application.isPlaying)
			{
				PrepareNetViews(location2.m_prefab, location2.m_netViews);
				PrepareRandomSpawns(location2.m_prefab, location2.m_randomSpawns);
				if (!m_locationsByHash.ContainsKey(location2.m_hash))
				{
					m_locationsByHash.Add(location2.m_hash, location2);
				}
			}
		}
	}

	public static void PrepareNetViews(GameObject root, List<ZNetView> views)
	{
		views.Clear();
		ZNetView[] componentsInChildren = root.GetComponentsInChildren<ZNetView>(includeInactive: true);
		foreach (ZNetView zNetView in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(zNetView.gameObject, root))
			{
				views.Add(zNetView);
			}
		}
	}

	public static void PrepareRandomSpawns(GameObject root, List<RandomSpawn> randomSpawns)
	{
		randomSpawns.Clear();
		RandomSpawn[] componentsInChildren = root.GetComponentsInChildren<RandomSpawn>(includeInactive: true);
		foreach (RandomSpawn randomSpawn in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(randomSpawn.gameObject, root))
			{
				randomSpawns.Add(randomSpawn);
				randomSpawn.Prepare();
			}
		}
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void ValidateVegetation()
	{
		foreach (ZoneVegetation item in m_vegetation)
		{
			if (item.m_enable && (bool)item.m_prefab && item.m_prefab.GetComponent<ZNetView>() == null)
			{
				ZLog.LogError("Vegetation " + item.m_prefab.name + " [ " + item.m_name + "] is missing ZNetView");
			}
		}
	}

	public void PrepareSave()
	{
		m_tempGeneratedZonesSaveClone = new HashSet<Vector2i>(m_generatedZones);
		m_tempGlobalKeysSaveClone = new HashSet<string>(m_globalKeys);
		m_tempLocationsSaveClone = new List<LocationInstance>(m_locationInstances.Values);
		m_tempLocationsGeneratedSaveClone = m_locationsGenerated;
	}

	public void SaveASync(BinaryWriter writer)
	{
		writer.Write(m_tempGeneratedZonesSaveClone.Count);
		foreach (Vector2i item in m_tempGeneratedZonesSaveClone)
		{
			writer.Write(item.x);
			writer.Write(item.y);
		}
		writer.Write(m_pgwVersion);
		writer.Write(m_locationVersion);
		writer.Write(m_tempGlobalKeysSaveClone.Count);
		foreach (string item2 in m_tempGlobalKeysSaveClone)
		{
			writer.Write(item2);
		}
		writer.Write(m_tempLocationsGeneratedSaveClone);
		writer.Write(m_tempLocationsSaveClone.Count);
		foreach (LocationInstance item3 in m_tempLocationsSaveClone)
		{
			writer.Write(item3.m_location.m_prefabName);
			writer.Write(item3.m_position.x);
			writer.Write(item3.m_position.y);
			writer.Write(item3.m_position.z);
			writer.Write(item3.m_placed);
		}
		m_tempGeneratedZonesSaveClone.Clear();
		m_tempGeneratedZonesSaveClone = null;
		m_tempGlobalKeysSaveClone.Clear();
		m_tempGlobalKeysSaveClone = null;
		m_tempLocationsSaveClone.Clear();
		m_tempLocationsSaveClone = null;
	}

	public void Load(BinaryReader reader, int version)
	{
		m_generatedZones.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			Vector2i item = default(Vector2i);
			item.x = reader.ReadInt32();
			item.y = reader.ReadInt32();
			m_generatedZones.Add(item);
		}
		if (version < 13)
		{
			return;
		}
		int num2 = reader.ReadInt32();
		int num3 = ((version >= 21) ? reader.ReadInt32() : 0);
		if (num2 != m_pgwVersion)
		{
			m_generatedZones.Clear();
		}
		if (version >= 14)
		{
			m_globalKeys.Clear();
			int num4 = reader.ReadInt32();
			for (int j = 0; j < num4; j++)
			{
				string item2 = reader.ReadString();
				m_globalKeys.Add(item2);
			}
		}
		if (version < 18)
		{
			return;
		}
		if (version >= 20)
		{
			m_locationsGenerated = reader.ReadBoolean();
		}
		m_locationInstances.Clear();
		int num5 = reader.ReadInt32();
		for (int k = 0; k < num5; k++)
		{
			string text = reader.ReadString();
			Vector3 zero = Vector3.zero;
			zero.x = reader.ReadSingle();
			zero.y = reader.ReadSingle();
			zero.z = reader.ReadSingle();
			bool generated = false;
			if (version >= 19)
			{
				generated = reader.ReadBoolean();
			}
			ZoneLocation location = GetLocation(text);
			if (location != null)
			{
				RegisterLocation(location, zero, generated);
			}
			else
			{
				ZLog.DevLog("Failed to find location " + text);
			}
		}
		ZLog.Log("Loaded " + num5 + " locations");
		if (num2 != m_pgwVersion)
		{
			m_locationInstances.Clear();
			m_locationsGenerated = false;
		}
		if (num3 != m_locationVersion)
		{
			m_locationsGenerated = false;
		}
	}

	private void Update()
	{
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		m_updateTimer += Time.deltaTime;
		if (!(m_updateTimer > 0.1f))
		{
			return;
		}
		m_updateTimer = 0f;
		bool flag = CreateLocalZones(ZNet.instance.GetReferencePosition());
		UpdateTTL(0.1f);
		if (!ZNet.instance.IsServer() || flag)
		{
			return;
		}
		CreateGhostZones(ZNet.instance.GetReferencePosition());
		foreach (ZNetPeer peer in ZNet.instance.GetPeers())
		{
			CreateGhostZones(peer.GetRefPos());
		}
	}

	private bool CreateGhostZones(Vector3 refPoint)
	{
		Vector2i zone = GetZone(refPoint);
		if (!IsZoneGenerated(zone) && SpawnZone(zone, SpawnMode.Ghost, out var _))
		{
			return true;
		}
		int num = m_activeArea + m_activeDistantArea;
		for (int i = zone.y - num; i <= zone.y + num; i++)
		{
			for (int j = zone.x - num; j <= zone.x + num; j++)
			{
				Vector2i zoneID = new Vector2i(j, i);
				if (!IsZoneGenerated(zoneID) && SpawnZone(zoneID, SpawnMode.Ghost, out var _))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CreateLocalZones(Vector3 refPoint)
	{
		Vector2i zone = GetZone(refPoint);
		if (PokeLocalZone(zone))
		{
			return true;
		}
		for (int i = zone.y - m_activeArea; i <= zone.y + m_activeArea; i++)
		{
			for (int j = zone.x - m_activeArea; j <= zone.x + m_activeArea; j++)
			{
				Vector2i vector2i = new Vector2i(j, i);
				if (!(vector2i == zone) && PokeLocalZone(vector2i))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PokeLocalZone(Vector2i zoneID)
	{
		if (m_zones.TryGetValue(zoneID, out var value))
		{
			value.m_ttl = 0f;
			return false;
		}
		SpawnMode mode = ((!ZNet.instance.IsServer() || IsZoneGenerated(zoneID)) ? SpawnMode.Client : SpawnMode.Full);
		if (SpawnZone(zoneID, mode, out var root))
		{
			ZoneData zoneData = new ZoneData();
			zoneData.m_root = root;
			m_zones.Add(zoneID, zoneData);
			return true;
		}
		return false;
	}

	public bool IsZoneLoaded(Vector3 point)
	{
		Vector2i zone = GetZone(point);
		return IsZoneLoaded(zone);
	}

	public bool IsZoneLoaded(Vector2i zoneID)
	{
		return m_zones.ContainsKey(zoneID);
	}

	public bool IsActiveAreaLoaded()
	{
		Vector2i zone = GetZone(ZNet.instance.GetReferencePosition());
		for (int i = zone.y - m_activeArea; i <= zone.y + m_activeArea; i++)
		{
			for (int j = zone.x - m_activeArea; j <= zone.x + m_activeArea; j++)
			{
				if (!m_zones.ContainsKey(new Vector2i(j, i)))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool SpawnZone(Vector2i zoneID, SpawnMode mode, out GameObject root)
	{
		Vector3 zonePos = GetZonePos(zoneID);
		Heightmap componentInChildren = m_zonePrefab.GetComponentInChildren<Heightmap>();
		if (!HeightmapBuilder.instance.IsTerrainReady(zonePos, componentInChildren.m_width, componentInChildren.m_scale, componentInChildren.m_isDistantLod, WorldGenerator.instance))
		{
			root = null;
			return false;
		}
		root = UnityEngine.Object.Instantiate(m_zonePrefab, zonePos, Quaternion.identity);
		if ((mode == SpawnMode.Ghost || mode == SpawnMode.Full) && !IsZoneGenerated(zoneID))
		{
			Heightmap componentInChildren2 = root.GetComponentInChildren<Heightmap>();
			m_tempClearAreas.Clear();
			m_tempSpawnedObjects.Clear();
			PlaceLocations(zoneID, zonePos, root.transform, componentInChildren2, m_tempClearAreas, mode, m_tempSpawnedObjects);
			PlaceVegetation(zoneID, zonePos, root.transform, componentInChildren2, m_tempClearAreas, mode, m_tempSpawnedObjects);
			PlaceZoneCtrl(zoneID, zonePos, mode, m_tempSpawnedObjects);
			if (mode == SpawnMode.Ghost)
			{
				foreach (GameObject tempSpawnedObject in m_tempSpawnedObjects)
				{
					UnityEngine.Object.Destroy(tempSpawnedObject);
				}
				m_tempSpawnedObjects.Clear();
				UnityEngine.Object.Destroy(root);
				root = null;
			}
			SetZoneGenerated(zoneID);
		}
		return true;
	}

	private void PlaceZoneCtrl(Vector2i zoneID, Vector3 zoneCenterPos, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
		{
			if (mode == SpawnMode.Ghost)
			{
				ZNetView.StartGhostInit();
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(m_zoneCtrlPrefab, zoneCenterPos, Quaternion.identity);
			gameObject.GetComponent<ZNetView>().GetZDO().SetPGWVersion(m_pgwVersion);
			if (mode == SpawnMode.Ghost)
			{
				spawnedObjects.Add(gameObject);
				ZNetView.FinishGhostInit();
			}
		}
	}

	private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
	{
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		return center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private void PlaceVegetation(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ClearArea> clearAreas, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		int seed = WorldGenerator.instance.GetSeed();
		float num = m_zoneSize / 2f;
		int num2 = 1;
		foreach (ZoneVegetation item in m_vegetation)
		{
			num2++;
			if (!item.m_enable || !hmap.HaveBiome(item.m_biome))
			{
				continue;
			}
			UnityEngine.Random.InitState(seed + zoneID.x * 4271 + zoneID.y * 9187 + item.m_prefab.name.GetStableHashCode());
			int num3 = 1;
			if (item.m_max < 1f)
			{
				if (UnityEngine.Random.value > item.m_max)
				{
					continue;
				}
			}
			else
			{
				num3 = UnityEngine.Random.Range((int)item.m_min, (int)item.m_max + 1);
			}
			bool flag = item.m_prefab.GetComponent<ZNetView>() != null;
			float num4 = Mathf.Cos((float)Math.PI / 180f * item.m_maxTilt);
			float num5 = Mathf.Cos((float)Math.PI / 180f * item.m_minTilt);
			float num6 = num - item.m_groupRadius;
			int num7 = (item.m_forcePlacement ? (num3 * 50) : num3);
			int num8 = 0;
			for (int i = 0; i < num7; i++)
			{
				Vector3 vector = new Vector3(UnityEngine.Random.Range(zoneCenterPos.x - num6, zoneCenterPos.x + num6), 0f, UnityEngine.Random.Range(zoneCenterPos.z - num6, zoneCenterPos.z + num6));
				int num9 = UnityEngine.Random.Range(item.m_groupSizeMin, item.m_groupSizeMax + 1);
				bool flag2 = false;
				for (int j = 0; j < num9; j++)
				{
					Vector3 p = ((j == 0) ? vector : GetRandomPointInRadius(vector, item.m_groupRadius));
					float num10 = UnityEngine.Random.Range(0, 360);
					float num11 = UnityEngine.Random.Range(item.m_scaleMin, item.m_scaleMax);
					float x = UnityEngine.Random.Range(0f - item.m_randTilt, item.m_randTilt);
					float z = UnityEngine.Random.Range(0f - item.m_randTilt, item.m_randTilt);
					if (item.m_blockCheck && IsBlocked(p))
					{
						continue;
					}
					GetGroundData(ref p, out var normal, out var biome, out var biomeArea, out var hmap2);
					if ((item.m_biome & biome) == 0 || (item.m_biomeArea & biomeArea) == 0)
					{
						continue;
					}
					float num12 = p.y - m_waterLevel;
					if (num12 < item.m_minAltitude || num12 > item.m_maxAltitude)
					{
						continue;
					}
					if (item.m_minOceanDepth != item.m_maxOceanDepth)
					{
						float oceanDepth = hmap2.GetOceanDepth(p);
						if (oceanDepth < item.m_minOceanDepth || oceanDepth > item.m_maxOceanDepth)
						{
							continue;
						}
					}
					if (normal.y < num4 || normal.y > num5)
					{
						continue;
					}
					if (item.m_terrainDeltaRadius > 0f)
					{
						GetTerrainDelta(p, item.m_terrainDeltaRadius, out var delta, out var _);
						if (delta > item.m_maxTerrainDelta || delta < item.m_minTerrainDelta)
						{
							continue;
						}
					}
					if (item.m_inForest)
					{
						float forestFactor = WorldGenerator.GetForestFactor(p);
						if (forestFactor < item.m_forestTresholdMin || forestFactor > item.m_forestTresholdMax)
						{
							continue;
						}
					}
					if (InsideClearArea(clearAreas, p))
					{
						continue;
					}
					if (item.m_snapToWater)
					{
						p.y = m_waterLevel;
					}
					p.y += item.m_groundOffset;
					Quaternion identity = Quaternion.identity;
					identity = ((!(item.m_chanceToUseGroundTilt > 0f) || !(UnityEngine.Random.value <= item.m_chanceToUseGroundTilt)) ? Quaternion.Euler(x, num10, z) : Quaternion.AngleAxis(num10, normal));
					if (flag)
					{
						if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
						{
							if (mode == SpawnMode.Ghost)
							{
								ZNetView.StartGhostInit();
							}
							GameObject gameObject = UnityEngine.Object.Instantiate(item.m_prefab, p, identity);
							ZNetView component = gameObject.GetComponent<ZNetView>();
							component.SetLocalScale(new Vector3(num11, num11, num11));
							component.GetZDO().SetPGWVersion(m_pgwVersion);
							if (mode == SpawnMode.Ghost)
							{
								spawnedObjects.Add(gameObject);
								ZNetView.FinishGhostInit();
							}
						}
					}
					else
					{
						GameObject obj = UnityEngine.Object.Instantiate(item.m_prefab, p, identity);
						obj.transform.localScale = new Vector3(num11, num11, num11);
						obj.transform.SetParent(parent, worldPositionStays: true);
					}
					flag2 = true;
				}
				if (flag2)
				{
					num8++;
				}
				if (num8 >= num3)
				{
					break;
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	private bool InsideClearArea(List<ClearArea> areas, Vector3 point)
	{
		foreach (ClearArea area in areas)
		{
			if (point.x > area.m_center.x - area.m_radius && point.x < area.m_center.x + area.m_radius && point.z > area.m_center.z - area.m_radius && point.z < area.m_center.z + area.m_radius)
			{
				return true;
			}
		}
		return false;
	}

	private ZoneLocation GetLocation(int hash)
	{
		if (m_locationsByHash.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	private ZoneLocation GetLocation(string name)
	{
		foreach (ZoneLocation location in m_locations)
		{
			if (location.m_prefabName == name)
			{
				return location;
			}
		}
		return null;
	}

	private void ClearNonPlacedLocations()
	{
		Dictionary<Vector2i, LocationInstance> dictionary = new Dictionary<Vector2i, LocationInstance>();
		foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
		{
			if (locationInstance.Value.m_placed)
			{
				dictionary.Add(locationInstance.Key, locationInstance.Value);
			}
		}
		m_locationInstances = dictionary;
	}

	private void CheckLocationDuplicates()
	{
		ZLog.Log("Checking for location duplicates");
		for (int i = 0; i < m_locations.Count; i++)
		{
			ZoneLocation zoneLocation = m_locations[i];
			if (!zoneLocation.m_enable)
			{
				continue;
			}
			for (int j = i + 1; j < m_locations.Count; j++)
			{
				ZoneLocation zoneLocation2 = m_locations[j];
				if (zoneLocation2.m_enable && zoneLocation.m_prefabName == zoneLocation2.m_prefabName)
				{
					ZLog.LogWarning("Two locations points to the same location prefab " + zoneLocation.m_prefabName);
				}
			}
		}
	}

	public void GenerateLocations()
	{
		if (!Application.isPlaying)
		{
			ZLog.Log("Setting up locations");
			SetupLocations();
		}
		ZLog.Log("Generating locations");
		DateTime now = DateTime.Now;
		m_locationsGenerated = true;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		CheckLocationDuplicates();
		ClearNonPlacedLocations();
		foreach (ZoneLocation item in m_locations.OrderByDescending((ZoneLocation a) => a.m_prioritized))
		{
			if (item.m_enable && item.m_quantity != 0)
			{
				GenerateLocations(item);
			}
		}
		UnityEngine.Random.state = state;
		ZLog.Log(" Done generating locations, duration:" + (DateTime.Now - now).TotalMilliseconds + " ms");
	}

	private int CountNrOfLocation(ZoneLocation location)
	{
		int num = 0;
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			if (value.m_location.m_prefabName == location.m_prefabName)
			{
				num++;
			}
		}
		if (num > 0)
		{
			ZLog.Log("Old location found " + location.m_prefabName + " x " + num);
		}
		return num;
	}

	private void GenerateLocations(ZoneLocation location)
	{
		DateTime now = DateTime.Now;
		UnityEngine.Random.InitState(WorldGenerator.instance.GetSeed() + location.m_prefabName.GetStableHashCode());
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		float locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		int num9 = (location.m_prioritized ? 200000 : 100000);
		int num10 = 0;
		int num11 = CountNrOfLocation(location);
		float num12 = 10000f;
		if (location.m_centerFirst)
		{
			num12 = location.m_minDistance;
		}
		if (location.m_unique && num11 > 0)
		{
			return;
		}
		for (int i = 0; i < num9; i++)
		{
			if (num11 >= location.m_quantity)
			{
				break;
			}
			Vector2i randomZone = GetRandomZone(num12);
			if (location.m_centerFirst)
			{
				num12 += 1f;
			}
			if (m_locationInstances.ContainsKey(randomZone))
			{
				num++;
			}
			else
			{
				if (IsZoneGenerated(randomZone))
				{
					continue;
				}
				Vector3 zonePos = GetZonePos(randomZone);
				Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
				if ((location.m_biomeArea & biomeArea) == 0)
				{
					num4++;
					continue;
				}
				for (int j = 0; j < 20; j++)
				{
					num10++;
					Vector3 randomPointInZone = GetRandomPointInZone(randomZone, locationRadius);
					float magnitude = randomPointInZone.magnitude;
					if (location.m_minDistance != 0f && magnitude < location.m_minDistance)
					{
						num2++;
						continue;
					}
					if (location.m_maxDistance != 0f && magnitude > location.m_maxDistance)
					{
						num2++;
						continue;
					}
					Heightmap.Biome biome = WorldGenerator.instance.GetBiome(randomPointInZone);
					if ((location.m_biome & biome) == 0)
					{
						num3++;
						continue;
					}
					randomPointInZone.y = WorldGenerator.instance.GetHeight(randomPointInZone.x, randomPointInZone.z);
					float num13 = randomPointInZone.y - m_waterLevel;
					if (num13 < location.m_minAltitude || num13 > location.m_maxAltitude)
					{
						num5++;
						continue;
					}
					if (location.m_inForest)
					{
						float forestFactor = WorldGenerator.GetForestFactor(randomPointInZone);
						if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
						{
							num6++;
							continue;
						}
					}
					WorldGenerator.instance.GetTerrainDelta(randomPointInZone, location.m_exteriorRadius, out var delta, out var _);
					if (delta > location.m_maxTerrainDelta || delta < location.m_minTerrainDelta)
					{
						num8++;
						continue;
					}
					if (location.m_minDistanceFromSimilar > 0f && HaveLocationInRange(location.m_prefabName, location.m_group, randomPointInZone, location.m_minDistanceFromSimilar))
					{
						num7++;
						continue;
					}
					RegisterLocation(location, randomPointInZone, generated: false);
					num11++;
					break;
				}
			}
		}
		if (num11 < location.m_quantity)
		{
			ZLog.LogWarning("Failed to place all " + location.m_prefabName + ", placed " + num11 + " out of " + location.m_quantity);
			ZLog.DevLog("errorLocationInZone " + num);
			ZLog.DevLog("errorCenterDistance " + num2);
			ZLog.DevLog("errorBiome " + num3);
			ZLog.DevLog("errorBiomeArea " + num4);
			ZLog.DevLog("errorAlt " + num5);
			ZLog.DevLog("errorForest " + num6);
			ZLog.DevLog("errorSimilar " + num7);
			ZLog.DevLog("errorTerrainDelta " + num8);
		}
		_ = DateTime.Now - now;
	}

	private Vector2i GetRandomZone(float range)
	{
		int num = (int)range / (int)m_zoneSize;
		Vector2i vector2i;
		do
		{
			vector2i = new Vector2i(UnityEngine.Random.Range(-num, num), UnityEngine.Random.Range(-num, num));
		}
		while (!(GetZonePos(vector2i).magnitude < 10000f));
		return vector2i;
	}

	private Vector3 GetRandomPointInZone(Vector2i zone, float locationRadius)
	{
		Vector3 zonePos = GetZonePos(zone);
		float num = m_zoneSize / 2f;
		float x = UnityEngine.Random.Range(0f - num + locationRadius, num - locationRadius);
		float z = UnityEngine.Random.Range(0f - num + locationRadius, num - locationRadius);
		return zonePos + new Vector3(x, 0f, z);
	}

	private Vector3 GetRandomPointInZone(float locationRadius)
	{
		Vector3 point = new Vector3(UnityEngine.Random.Range(-10000f, 10000f), 0f, UnityEngine.Random.Range(-10000f, 10000f));
		Vector2i zone = GetZone(point);
		Vector3 zonePos = GetZonePos(zone);
		float num = m_zoneSize / 2f;
		return new Vector3(UnityEngine.Random.Range(zonePos.x - num + locationRadius, zonePos.x + num - locationRadius), 0f, UnityEngine.Random.Range(zonePos.z - num + locationRadius, zonePos.z + num - locationRadius));
	}

	private void PlaceLocations(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ClearArea> clearAreas, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		GenerateLocationsIfNeeded();
		DateTime now = DateTime.Now;
		if (m_locationInstances.TryGetValue(zoneID, out var value) && !value.m_placed)
		{
			Vector3 p = value.m_position;
			GetGroundData(ref p, out var _, out var _, out var _, out var _);
			if (value.m_location.m_snapToWater)
			{
				p.y = m_waterLevel;
			}
			if (value.m_location.m_location.m_clearArea)
			{
				ClearArea item = new ClearArea(p, value.m_location.m_exteriorRadius);
				clearAreas.Add(item);
			}
			Quaternion rot = Quaternion.identity;
			if (value.m_location.m_slopeRotation)
			{
				GetTerrainDelta(p, value.m_location.m_exteriorRadius, out var _, out var slopeDirection);
				Vector3 forward = new Vector3(slopeDirection.x, 0f, slopeDirection.z);
				forward.Normalize();
				rot = Quaternion.LookRotation(forward);
				Vector3 eulerAngles = rot.eulerAngles;
				eulerAngles.y = Mathf.Round(eulerAngles.y / 22.5f) * 22.5f;
				rot.eulerAngles = eulerAngles;
			}
			else if (value.m_location.m_randomRotation)
			{
				rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
			}
			int seed = WorldGenerator.instance.GetSeed() + zoneID.x * 4271 + zoneID.y * 9187;
			SpawnLocation(value.m_location, seed, p, rot, mode, spawnedObjects);
			value.m_placed = true;
			m_locationInstances[zoneID] = value;
			TimeSpan timeSpan = DateTime.Now - now;
			ZLog.Log(string.Concat("Placed locations in zone ", zoneID, "  duration ", timeSpan.TotalMilliseconds, " ms"));
			if (value.m_location.m_unique)
			{
				RemoveUnplacedLocations(value.m_location);
			}
			if (value.m_location.m_iconPlaced)
			{
				SendLocationIcons(ZRoutedRpc.Everybody);
			}
		}
	}

	private void RemoveUnplacedLocations(ZoneLocation location)
	{
		List<Vector2i> list = new List<Vector2i>();
		foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
		{
			if (locationInstance.Value.m_location == location && !locationInstance.Value.m_placed)
			{
				list.Add(locationInstance.Key);
			}
		}
		foreach (Vector2i item in list)
		{
			m_locationInstances.Remove(item);
		}
		ZLog.DevLog("Removed " + list.Count + " unplaced locations of type " + location.m_prefabName);
	}

	public bool TestSpawnLocation(string name, Vector3 pos)
	{
		if (!ZNet.instance.IsServer())
		{
			return false;
		}
		ZoneLocation location = GetLocation(name);
		if (location == null)
		{
			ZLog.Log("Missing location:" + name);
			Console.instance.Print("Missing location:" + name);
			return false;
		}
		if (location.m_prefab == null)
		{
			ZLog.Log("Missing prefab in location:" + name);
			Console.instance.Print("Missing location:" + name);
			return false;
		}
		float num = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		Vector2i zone = GetZone(pos);
		Vector3 zonePos = GetZonePos(zone);
		pos.x = Mathf.Clamp(pos.x, zonePos.x - m_zoneSize / 2f + num, zonePos.x + m_zoneSize / 2f - num);
		pos.z = Mathf.Clamp(pos.z, zonePos.z - m_zoneSize / 2f + num, zonePos.z + m_zoneSize / 2f - num);
		ZLog.Log(string.Concat("radius ", num, "  ", zonePos, " ", pos));
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location spawned, world saving DISABLED until restart");
		m_didZoneTest = true;
		float y = (float)UnityEngine.Random.Range(0, 16) * 22.5f;
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		SpawnLocation(location, UnityEngine.Random.Range(0, 99999), pos, Quaternion.Euler(0f, y, 0f), SpawnMode.Full, spawnedGhostObjects);
		return true;
	}

	public GameObject SpawnProxyLocation(int hash, int seed, Vector3 pos, Quaternion rot)
	{
		ZoneLocation location = GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash);
			return null;
		}
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		return SpawnLocation(location, seed, pos, rot, SpawnMode.Client, spawnedGhostObjects);
	}

	private GameObject SpawnLocation(ZoneLocation location, int seed, Vector3 pos, Quaternion rot, SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		Vector3 position = location.m_prefab.transform.position;
		Quaternion quaternion = Quaternion.Inverse(location.m_prefab.transform.rotation);
		UnityEngine.Random.InitState(seed);
		if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
		{
			foreach (ZNetView netView in location.m_netViews)
			{
				netView.gameObject.SetActive(value: true);
			}
			foreach (RandomSpawn randomSpawn in location.m_randomSpawns)
			{
				randomSpawn.Randomize();
			}
			WearNTear.m_randomInitialDamage = location.m_location.m_applyRandomDamage;
			foreach (ZNetView netView2 in location.m_netViews)
			{
				if (netView2.gameObject.activeSelf)
				{
					Vector3 vector = netView2.gameObject.transform.position - position;
					Vector3 position2 = pos + rot * vector;
					Quaternion quaternion2 = quaternion * netView2.gameObject.transform.rotation;
					Quaternion rotation = rot * quaternion2;
					if (mode == SpawnMode.Ghost)
					{
						ZNetView.StartGhostInit();
					}
					GameObject gameObject = UnityEngine.Object.Instantiate(netView2.gameObject, position2, rotation);
					gameObject.GetComponent<ZNetView>().GetZDO().SetPGWVersion(m_pgwVersion);
					DungeonGenerator component = gameObject.GetComponent<DungeonGenerator>();
					if ((bool)component)
					{
						component.Generate(mode);
					}
					if (mode == SpawnMode.Ghost)
					{
						spawnedGhostObjects.Add(gameObject);
						ZNetView.FinishGhostInit();
					}
				}
			}
			WearNTear.m_randomInitialDamage = false;
			CreateLocationProxy(location, seed, pos, rot, mode, spawnedGhostObjects);
			SnapToGround.SnappAll();
			return null;
		}
		foreach (RandomSpawn randomSpawn2 in location.m_randomSpawns)
		{
			randomSpawn2.Randomize();
		}
		foreach (ZNetView netView3 in location.m_netViews)
		{
			netView3.gameObject.SetActive(value: false);
		}
		GameObject obj = UnityEngine.Object.Instantiate(location.m_prefab, pos, rot);
		obj.SetActive(value: true);
		SnapToGround.SnappAll();
		return obj;
	}

	private void CreateLocationProxy(ZoneLocation location, int seed, Vector3 pos, Quaternion rotation, SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		if (mode == SpawnMode.Ghost)
		{
			ZNetView.StartGhostInit();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(m_locationProxyPrefab, pos, rotation);
		LocationProxy component = gameObject.GetComponent<LocationProxy>();
		bool spawnNow = mode == SpawnMode.Full;
		component.SetLocation(location.m_prefab.name, seed, spawnNow, m_pgwVersion);
		if (mode == SpawnMode.Ghost)
		{
			spawnedGhostObjects.Add(gameObject);
			ZNetView.FinishGhostInit();
		}
	}

	private void RegisterLocation(ZoneLocation location, Vector3 pos, bool generated)
	{
		LocationInstance value = default(LocationInstance);
		value.m_location = location;
		value.m_position = pos;
		value.m_placed = generated;
		Vector2i zone = GetZone(pos);
		if (m_locationInstances.ContainsKey(zone))
		{
			ZLog.LogWarning("Location already exist in zone " + zone);
		}
		else
		{
			m_locationInstances.Add(zone, value);
		}
	}

	private bool HaveLocationInRange(string prefabName, string group, Vector3 p, float radius)
	{
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			if ((value.m_location.m_prefabName == prefabName || (group.Length > 0 && group == value.m_location.m_group)) && Vector3.Distance(value.m_position, p) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public bool GetLocationIcon(string name, out Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
			{
				if ((locationInstance.Value.m_location.m_iconAlways || (locationInstance.Value.m_location.m_iconPlaced && locationInstance.Value.m_placed)) && locationInstance.Value.m_location.m_prefabName == name)
				{
					pos = locationInstance.Value.m_position;
					return true;
				}
			}
		}
		else
		{
			foreach (KeyValuePair<Vector3, string> locationIcon in m_locationIcons)
			{
				if (locationIcon.Value == name)
				{
					pos = locationIcon.Key;
					return true;
				}
			}
		}
		pos = Vector3.zero;
		return false;
	}

	public void GetLocationIcons(Dictionary<Vector3, string> icons)
	{
		if (ZNet.instance.IsServer())
		{
			foreach (LocationInstance value in m_locationInstances.Values)
			{
				if (value.m_location.m_iconAlways || (value.m_location.m_iconPlaced && value.m_placed))
				{
					icons[value.m_position] = value.m_location.m_prefabName;
				}
			}
			return;
		}
		foreach (KeyValuePair<Vector3, string> locationIcon in m_locationIcons)
		{
			icons.Add(locationIcon.Key, locationIcon.Value);
		}
	}

	private void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
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
			float groundHeight = GetGroundHeight(vector4);
			if (groundHeight < num3)
			{
				num3 = groundHeight;
				vector2 = vector4;
			}
			if (groundHeight > num2)
			{
				num2 = groundHeight;
				vector = vector4;
			}
		}
		delta = num2 - num3;
		slopeDirection = Vector3.Normalize(vector2 - vector);
	}

	public bool IsBlocked(Vector3 p)
	{
		p.y += 2000f;
		if (Physics.Raycast(p, Vector3.down, 10000f, m_blockRayMask))
		{
			return true;
		}
		return false;
	}

	public float GetAverageGroundHeight(Vector3 p, float radius)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			return hitInfo.point.y;
		}
		return p.y;
	}

	public float GetGroundHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			return hitInfo.point.y;
		}
		return p.y;
	}

	public bool GetGroundHeight(Vector3 p, out float height)
	{
		p.y = 6000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public float GetSolidHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y += 1000f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 2000f, m_solidRayMask))
		{
			return hitInfo.point.y;
		}
		return p.y;
	}

	public bool GetSolidHeight(Vector3 p, out float height)
	{
		p.y += 1000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 2000f, m_solidRayMask) && !hitInfo.collider.attachedRigidbody)
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public bool GetSolidHeight(Vector3 p, float radius, out float height, Transform ignore)
	{
		height = p.y - 1000f;
		p.y += 1000f;
		int num = ((!(radius <= 0f)) ? Physics.SphereCastNonAlloc(p, radius, Vector3.down, rayHits, 2000f, m_solidRayMask) : Physics.RaycastNonAlloc(p, Vector3.down, rayHits, 2000f, m_solidRayMask));
		bool result = false;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = rayHits[i];
			Collider collider = raycastHit.collider;
			if (!(collider.attachedRigidbody != null) && (!(ignore != null) || !Utils.IsParent(collider.transform, ignore)))
			{
				if (raycastHit.point.y > height)
				{
					height = raycastHit.point.y;
				}
				result = true;
			}
		}
		return result;
	}

	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal)
	{
		GameObject go;
		return GetSolidHeight(p, out height, out normal, out go);
	}

	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal, out GameObject go)
	{
		p.y += 1000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 2000f, m_solidRayMask) && !hitInfo.collider.attachedRigidbody)
		{
			height = hitInfo.point.y;
			normal = hitInfo.normal;
			go = hitInfo.collider.gameObject;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		go = null;
		return false;
	}

	public bool FindFloor(Vector3 p, out float height)
	{
		if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var hitInfo, 1000f, m_solidRayMask))
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public void GetGroundData(ref Vector3 p, out Vector3 normal, out Heightmap.Biome biome, out Heightmap.BiomeArea biomeArea, out Heightmap hmap)
	{
		biome = Heightmap.Biome.None;
		biomeArea = Heightmap.BiomeArea.Everything;
		hmap = null;
		if (Physics.Raycast(p + Vector3.up * 5000f, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			p.y = hitInfo.point.y;
			normal = hitInfo.normal;
			Heightmap component = hitInfo.collider.GetComponent<Heightmap>();
			if ((bool)component)
			{
				biome = component.GetBiome(hitInfo.point);
				biomeArea = component.GetBiomeArea();
				hmap = component;
			}
		}
		else
		{
			normal = Vector3.up;
		}
	}

	private void UpdateTTL(float dt)
	{
		foreach (KeyValuePair<Vector2i, ZoneData> zone in m_zones)
		{
			zone.Value.m_ttl += dt;
		}
		foreach (KeyValuePair<Vector2i, ZoneData> zone2 in m_zones)
		{
			if (zone2.Value.m_ttl > m_zoneTTL && !ZNetScene.instance.HaveInstanceInSector(zone2.Key))
			{
				UnityEngine.Object.Destroy(zone2.Value.m_root);
				m_zones.Remove(zone2.Key);
				break;
			}
		}
	}

	public void GetVegetation(Heightmap.Biome biome, List<ZoneVegetation> vegetation)
	{
		foreach (ZoneVegetation item in m_vegetation)
		{
			if ((item.m_biome & biome) != 0 || item.m_biome == biome)
			{
				vegetation.Add(item);
			}
		}
	}

	public void GetLocations(Heightmap.Biome biome, List<ZoneLocation> locations, bool skipDisabled)
	{
		foreach (ZoneLocation location in m_locations)
		{
			if (((location.m_biome & biome) != 0 || location.m_biome == biome) && (!skipDisabled || location.m_enable))
			{
				locations.Add(location);
			}
		}
	}

	public bool FindClosestLocation(string name, Vector3 point, out LocationInstance closest)
	{
		float num = 999999f;
		closest = default(LocationInstance);
		bool result = false;
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			float num2 = Vector3.Distance(value.m_position, point);
			if (value.m_location.m_prefabName == name && num2 < num)
			{
				num = num2;
				closest = value;
				result = true;
			}
		}
		return result;
	}

	public Vector2i GetZone(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + m_zoneSize / 2f) / m_zoneSize);
		int y = Mathf.FloorToInt((point.z + m_zoneSize / 2f) / m_zoneSize);
		return new Vector2i(x, y);
	}

	public Vector3 GetZonePos(Vector2i id)
	{
		return new Vector3((float)id.x * m_zoneSize, 0f, (float)id.y * m_zoneSize);
	}

	private void SetZoneGenerated(Vector2i zoneID)
	{
		m_generatedZones.Add(zoneID);
	}

	private bool IsZoneGenerated(Vector2i zoneID)
	{
		return m_generatedZones.Contains(zoneID);
	}

	public bool SkipSaving()
	{
		if (!m_error)
		{
			return m_didZoneTest;
		}
		return true;
	}

	public void ResetGlobalKeys()
	{
		m_globalKeys.Clear();
		SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	public void SetGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("SetGlobalKey", name);
	}

	public bool GetGlobalKey(string name)
	{
		return m_globalKeys.Contains(name);
	}

	private void RPC_SetGlobalKey(long sender, string name)
	{
		if (!m_globalKeys.Contains(name))
		{
			m_globalKeys.Add(name);
			SendGlobalKeys(ZRoutedRpc.Everybody);
		}
	}

	public List<string> GetGlobalKeys()
	{
		return new List<string>(m_globalKeys);
	}

	public Dictionary<Vector2i, LocationInstance>.ValueCollection GetLocationList()
	{
		return m_locationInstances.Values;
	}
}
