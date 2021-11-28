using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
	[Serializable]
	public class SpawnData
	{
		public string m_name = "";

		public bool m_enabled = true;

		public GameObject m_prefab;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		[Header("Total nr of instances (if near player is set, only instances within the max spawn radius is counted)")]
		public int m_maxSpawned = 1;

		[Header("How often do we spawn")]
		public float m_spawnInterval = 4f;

		[Header("Chanse to spawn each spawn interval")]
		[Range(0f, 100f)]
		public float m_spawnChance = 100f;

		[Header("Minimum distance to another instance")]
		public float m_spawnDistance = 10f;

		[Header("Spawn range ( 0 = use global setting )")]
		public float m_spawnRadiusMin;

		public float m_spawnRadiusMax;

		[Header("Only spawn if this key is set")]
		public string m_requiredGlobalKey = "";

		[Header("Only spawn if this environment is active")]
		public List<string> m_requiredEnvironments = new List<string>();

		[Header("Group spawning")]
		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius = 3f;

		[Header("Time of day")]
		public bool m_spawnAtNight = true;

		public bool m_spawnAtDay = true;

		[Header("Altitude")]
		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[Header("Terrain tilt")]
		public float m_minTilt;

		public float m_maxTilt = 35f;

		[Header("Forest")]
		public bool m_inForest = true;

		public bool m_outsideForest = true;

		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		[Header("States")]
		public bool m_huntPlayer;

		public float m_groundOffset = 0.5f;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;

		public float m_levelUpMinCenterDistance;

		[HideInInspector]
		public bool m_foldout;

		public SpawnData Clone()
		{
			SpawnData obj = MemberwiseClone() as SpawnData;
			obj.m_requiredEnvironments = new List<string>(m_requiredEnvironments);
			return obj;
		}
	}

	private static List<SpawnSystem> m_instances = new List<SpawnSystem>();

	private const float m_spawnDistanceMin = 40f;

	private const float m_spawnDistanceMax = 80f;

	public const float m_levelupChance = 10f;

	public List<SpawnSystemList> m_spawnLists = new List<SpawnSystemList>();

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	private static List<Player> m_tempNearPlayers = new List<Player>();

	private ZNetView m_nview;

	private Heightmap m_heightmap;

	private void Awake()
	{
		m_instances.Add(this);
		m_nview = GetComponent<ZNetView>();
		m_heightmap = Heightmap.FindHeightmap(base.transform.position);
		InvokeRepeating("UpdateSpawning", 10f, 4f);
	}

	private void OnDestroy()
	{
		m_instances.Remove(this);
	}

	private void UpdateSpawning()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || Player.m_localPlayer == null)
		{
			return;
		}
		m_tempNearPlayers.Clear();
		GetPlayersInZone(m_tempNearPlayers);
		if (m_tempNearPlayers.Count == 0)
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		foreach (SpawnSystemList spawnList in m_spawnLists)
		{
			UpdateSpawnList(spawnList.m_spawners, time, eventSpawners: false);
		}
		List<SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
		if (currentSpawners != null)
		{
			UpdateSpawnList(currentSpawners, time, eventSpawners: true);
		}
	}

	private void UpdateSpawnList(List<SpawnData> spawners, DateTime currentTime, bool eventSpawners)
	{
		string text = (eventSpawners ? "e_" : "b_");
		int num = 0;
		foreach (SpawnData spawner in spawners)
		{
			num++;
			if (!spawner.m_enabled || !m_heightmap.HaveBiome(spawner.m_biome))
			{
				continue;
			}
			int stableHashCode = (text + spawner.m_prefab.name + num).GetStableHashCode();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(stableHashCode, 0L));
			int num2 = Mathf.Min(b: (int)((currentTime - dateTime).TotalSeconds / (double)spawner.m_spawnInterval), a: spawner.m_maxSpawned);
			if (num2 > 0)
			{
				m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
			}
			for (int i = 0; i < num2; i++)
			{
				if (UnityEngine.Random.Range(0f, 100f) > spawner.m_spawnChance)
				{
					continue;
				}
				if ((!string.IsNullOrEmpty(spawner.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawner.m_requiredGlobalKey)) || (spawner.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawner.m_requiredEnvironments)) || (!spawner.m_spawnAtDay && EnvMan.instance.IsDay()) || (!spawner.m_spawnAtNight && EnvMan.instance.IsNight()))
				{
					break;
				}
				int nrOfInstances = GetNrOfInstances(spawner.m_prefab, Vector3.zero, 0f, eventSpawners);
				if (nrOfInstances >= spawner.m_maxSpawned)
				{
					break;
				}
				if (!FindBaseSpawnPoint(spawner, m_tempNearPlayers, out var spawnCenter, out var _) || (spawner.m_spawnDistance > 0f && HaveInstanceInRange(spawner.m_prefab, spawnCenter, spawner.m_spawnDistance)))
				{
					continue;
				}
				int num3 = Mathf.Min(UnityEngine.Random.Range(spawner.m_groupSizeMin, spawner.m_groupSizeMax + 1), spawner.m_maxSpawned - nrOfInstances);
				float num4 = ((num3 > 1) ? spawner.m_groupRadius : 0f);
				int num5 = 0;
				for (int j = 0; j < num3 * 2; j++)
				{
					Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
					Vector3 spawnPoint = spawnCenter + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * num4;
					if (IsSpawnPointGood(spawner, ref spawnPoint))
					{
						Spawn(spawner, spawnPoint + Vector3.up * spawner.m_groundOffset, eventSpawners);
						num5++;
						if (num5 >= num3)
						{
							break;
						}
					}
				}
				ZLog.Log("Spawned " + spawner.m_prefab.name + " x " + num5);
			}
		}
	}

	private void Spawn(SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(critter.m_prefab, spawnPoint, Quaternion.identity);
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (!(component != null))
		{
			return;
		}
		if (critter.m_huntPlayer)
		{
			component.SetHuntPlayer(hunt: true);
		}
		if (critter.m_maxLevel > 1 && (critter.m_levelUpMinCenterDistance <= 0f || spawnPoint.magnitude > critter.m_levelUpMinCenterDistance))
		{
			Character component2 = gameObject.GetComponent<Character>();
			if ((bool)component2)
			{
				int i;
				for (i = critter.m_minLevel; i < critter.m_maxLevel; i++)
				{
					if (!(UnityEngine.Random.Range(0f, 100f) <= 10f))
					{
						break;
					}
				}
				if (i > 1)
				{
					component2.SetLevel(i);
				}
			}
		}
		MonsterAI monsterAI = component as MonsterAI;
		if ((bool)monsterAI)
		{
			if (!critter.m_spawnAtDay)
			{
				monsterAI.SetDespawnInDay(despawn: true);
			}
			if (eventSpawner)
			{
				monsterAI.SetEventCreature(despawn: true);
			}
		}
	}

	private bool IsSpawnPointGood(SpawnData spawn, ref Vector3 spawnPoint)
	{
		ZoneSystem.instance.GetGroundData(ref spawnPoint, out var normal, out var biome, out var biomeArea, out var hmap);
		if ((spawn.m_biome & biome) == 0)
		{
			return false;
		}
		if ((spawn.m_biomeArea & biomeArea) == 0)
		{
			return false;
		}
		if (ZoneSystem.instance.IsBlocked(spawnPoint))
		{
			return false;
		}
		float num = spawnPoint.y - ZoneSystem.instance.m_waterLevel;
		if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
		{
			return false;
		}
		float num2 = Mathf.Cos((float)Math.PI / 180f * spawn.m_maxTilt);
		float num3 = Mathf.Cos((float)Math.PI / 180f * spawn.m_minTilt);
		if (normal.y < num2 || normal.y > num3)
		{
			return false;
		}
		float range = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		if (Player.IsPlayerInRange(spawnPoint, range))
		{
			return false;
		}
		if ((bool)EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase))
		{
			return false;
		}
		if (!spawn.m_inForest || !spawn.m_outsideForest)
		{
			bool flag = WorldGenerator.InForest(spawnPoint);
			if (!spawn.m_inForest && flag)
			{
				return false;
			}
			if (!spawn.m_outsideForest && !flag)
			{
				return false;
			}
		}
		if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && hmap != null)
		{
			float oceanDepth = hmap.GetOceanDepth(spawnPoint);
			if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
			{
				return false;
			}
		}
		return true;
	}

	private bool FindBaseSpawnPoint(SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer)
	{
		float min = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		float max = ((spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f);
		for (int i = 0; i < 20; i++)
		{
			Player player = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
			Vector3 vector = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
			Vector3 spawnPoint = player.transform.position + vector * UnityEngine.Random.Range(min, max);
			if (IsSpawnPointGood(spawn, ref spawnPoint))
			{
				spawnCenter = spawnPoint;
				targetPlayer = player;
				return true;
			}
		}
		spawnCenter = Vector3.zero;
		targetPlayer = null;
		return false;
	}

	private int GetNrOfInstances(string prefabName)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		int num = 0;
		foreach (Character item in allCharacters)
		{
			if (item.gameObject.name.StartsWith(prefabName) && InsideZone(item.transform.position))
			{
				num++;
			}
		}
		return num;
	}

	private void GetPlayersInZone(List<Player> players)
	{
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (InsideZone(allPlayer.transform.position))
			{
				players.Add(allPlayer);
			}
		}
	}

	private void GetPlayersNearZone(List<Player> players, float marginDistance)
	{
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (InsideZone(allPlayer.transform.position, marginDistance))
			{
				players.Add(allPlayer);
			}
		}
	}

	private bool IsPlayerTooClose(List<Player> players, Vector3 point, float minDistance)
	{
		foreach (Player player in players)
		{
			if (Vector3.Distance(player.transform.position, point) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	private bool InPlayerRange(List<Player> players, Vector3 point, float minDistance, float maxDistance)
	{
		bool result = false;
		foreach (Player player in players)
		{
			float num = Utils.DistanceXZ(player.transform.position, point);
			if (num < minDistance)
			{
				return false;
			}
			if (num < maxDistance)
			{
				result = true;
			}
		}
		return result;
	}

	private static bool HaveInstanceInRange(GameObject prefab, Vector3 centerPoint, float minDistance)
	{
		string value = prefab.name;
		if (prefab.GetComponent<BaseAI>() != null)
		{
			foreach (BaseAI allInstance in BaseAI.GetAllInstances())
			{
				if (allInstance.gameObject.name.StartsWith(value) && Utils.DistanceXZ(allInstance.transform.position, centerPoint) < minDistance)
				{
					return true;
				}
			}
			return false;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.gameObject.name.StartsWith(value) && Utils.DistanceXZ(gameObject.transform.position, centerPoint) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetNrOfInstances(GameObject prefab)
	{
		return GetNrOfInstances(prefab, Vector3.zero, 0f);
	}

	public static int GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
	{
		string text = prefab.name + "(Clone)";
		if (prefab.GetComponent<BaseAI>() != null)
		{
			List<BaseAI> allInstances = BaseAI.GetAllInstances();
			int num = 0;
			{
				foreach (BaseAI item in allInstances)
				{
					if (item.gameObject.name != text || (maxRange > 0f && Vector3.Distance(center, item.transform.position) > maxRange))
					{
						continue;
					}
					if (eventCreaturesOnly)
					{
						MonsterAI monsterAI = item as MonsterAI;
						if ((bool)monsterAI && !monsterAI.IsEventCreature())
						{
							continue;
						}
					}
					if (procreationOnly)
					{
						Procreation component = item.GetComponent<Procreation>();
						if ((bool)component && !component.ReadyForProcreation())
						{
							continue;
						}
					}
					num++;
				}
				return num;
			}
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		int num2 = 0;
		GameObject[] array2 = array;
		foreach (GameObject gameObject in array2)
		{
			if (gameObject.name.StartsWith(text) && (!(maxRange > 0f) || !(Vector3.Distance(center, gameObject.transform.position) > maxRange)))
			{
				num2++;
			}
		}
		return num2;
	}

	private bool InsideZone(Vector3 point, float extra = 0f)
	{
		float num = ZoneSystem.instance.m_zoneSize * 0.5f + extra;
		Vector3 position = base.transform.position;
		if (point.x < position.x - num || point.x > position.x + num)
		{
			return false;
		}
		if (point.z < position.z - num || point.z > position.z + num)
		{
			return false;
		}
		return true;
	}

	private bool HaveGlobalKeys(SpawnData ev)
	{
		if (!string.IsNullOrEmpty(ev.m_requiredGlobalKey))
		{
			return ZoneSystem.instance.GetGlobalKey(ev.m_requiredGlobalKey);
		}
		return true;
	}
}
