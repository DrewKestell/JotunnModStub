using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
	private List<ZDO> m_tempPortalList = new List<ZDO>();

	private static Game m_instance;

	public GameObject m_playerPrefab;

	public GameObject m_portalPrefab;

	public GameObject m_consolePrefab;

	public string m_StartLocation = "StartTemple";

	private static string m_profileFilename;

	private PlayerProfile m_playerProfile;

	private bool m_requestRespawn;

	private float m_respawnWait;

	private const float m_respawnLoadDuration = 8f;

	private bool m_haveSpawned;

	private bool m_firstSpawn = true;

	private bool m_shuttingDown;

	private Vector3 m_randomStartPoint = Vector3.zero;

	private UnityEngine.Random.State m_spawnRandomState;

	private bool m_sleeping;

	private const float m_collectResourcesInterval = 600f;

	private float m_saveTimer;

	private const float m_saveInterval = 1200f;

	private const float m_preSaveWarning = 30f;

	private const float m_difficultyScaleRange = 100f;

	private const int m_difficultyScaleMaxPlayers = 5;

	private const float m_damageScalePerPlayer = 0.04f;

	private const float m_healthScalePerPlayer = 0.3f;

	private int m_forcePlayers;

	public static Game instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		ZInput.Initialize();
		if (!Console.instance)
		{
			UnityEngine.Object.Instantiate(m_consolePrefab);
		}
		Settings.ApplyStartupSettings();
		if (string.IsNullOrEmpty(m_profileFilename))
		{
			m_playerProfile = new PlayerProfile("Developer");
			m_playerProfile.SetName("Odev");
			m_playerProfile.Load();
		}
		else
		{
			ZLog.Log("Loading player profile " + m_profileFilename);
			m_playerProfile = new PlayerProfile(m_profileFilename);
			m_playerProfile.Load();
		}
		InvokeRepeating("CollectResources", 600f, 600f);
		Gogan.LogEvent("Screen", "Enter", "InGame", 0L);
		Gogan.LogEvent("Game", "InputMode", ZInput.IsGamepadActive() ? "Gamepad" : "MK", 0L);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = -1;
		ZRoutedRpc.instance.Register("SleepStart", SleepStart);
		ZRoutedRpc.instance.Register("SleepStop", SleepStop);
		ZRoutedRpc.instance.Register<float>("Ping", RPC_Ping);
		ZRoutedRpc.instance.Register<float>("Pong", RPC_Pong);
		ZRoutedRpc.instance.Register<string, int, Vector3, bool>("DiscoverLocationRespons", RPC_DiscoverLocationRespons);
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string, Vector3, string, int, bool>("DiscoverClosestLocation", RPC_DiscoverClosestLocation);
			StartCoroutine("ConnectPortals");
			InvokeRepeating("UpdateSleeping", 2f, 2f);
		}
	}

	private void ServerLog()
	{
		int peerConnections = ZNet.instance.GetPeerConnections();
		int num = ZDOMan.instance.NrOfObjects();
		int sentZDOs = ZDOMan.instance.GetSentZDOs();
		int recvZDOs = ZDOMan.instance.GetRecvZDOs();
		ZLog.Log(" Connections " + peerConnections + " ZDOS:" + num + "  sent:" + sentZDOs + " recv:" + recvZDOs);
	}

	private void CollectResources()
	{
		Resources.UnloadUnusedAssets();
	}

	public void Logout()
	{
		if (!m_shuttingDown)
		{
			Shutdown();
			SceneManager.LoadScene("start");
		}
	}

	public bool IsShuttingDown()
	{
		return m_shuttingDown;
	}

	private void OnApplicationQuit()
	{
		if (!m_shuttingDown)
		{
			ZLog.Log("Game - OnApplicationQuit");
			Shutdown();
			Thread.Sleep(2000);
		}
	}

	private void Shutdown()
	{
		if (!m_shuttingDown)
		{
			ZLog.Log("Shuting down");
			m_shuttingDown = true;
			SavePlayerProfile(setLogoutPoint: true);
			ZNetScene.instance.Shutdown();
			ZNet.instance.Shutdown();
		}
	}

	private void SavePlayerProfile(bool setLogoutPoint)
	{
		if ((bool)Player.m_localPlayer)
		{
			m_playerProfile.SavePlayerData(Player.m_localPlayer);
			Minimap.instance.SaveMapData();
			if (setLogoutPoint)
			{
				m_playerProfile.SaveLogoutPoint();
			}
		}
		m_playerProfile.Save();
	}

	private Player SpawnPlayer(Vector3 spawnPoint)
	{
		ZLog.DevLog("Spawning player:" + Time.frameCount);
		Player component = UnityEngine.Object.Instantiate(m_playerPrefab, spawnPoint, Quaternion.identity).GetComponent<Player>();
		component.SetLocalPlayer();
		m_playerProfile.LoadPlayerData(component);
		ZNet.instance.SetCharacterID(component.GetZDOID());
		component.OnSpawned();
		return component;
	}

	private Bed FindBedNearby(Vector3 point, float maxDistance)
	{
		Bed[] array = UnityEngine.Object.FindObjectsOfType<Bed>();
		foreach (Bed bed in array)
		{
			if (bed.IsCurrent())
			{
				return bed;
			}
		}
		return null;
	}

	private bool FindSpawnPoint(out Vector3 point, out bool usedLogoutPoint, float dt)
	{
		m_respawnWait += dt;
		usedLogoutPoint = false;
		if (m_playerProfile.HaveLogoutPoint())
		{
			Vector3 logoutPoint = m_playerProfile.GetLogoutPoint();
			ZNet.instance.SetReferencePosition(logoutPoint);
			if (m_respawnWait > 8f && ZNetScene.instance.IsAreaReady(logoutPoint))
			{
				if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out var height))
				{
					ZLog.Log("Invalid spawn point, no ground " + logoutPoint);
					m_respawnWait = 0f;
					m_playerProfile.ClearLoguoutPoint();
					point = Vector3.zero;
					return false;
				}
				m_playerProfile.ClearLoguoutPoint();
				point = logoutPoint;
				if (point.y < height)
				{
					point.y = height;
				}
				point.y += 0.25f;
				usedLogoutPoint = true;
				ZLog.Log("Spawned after " + m_respawnWait);
				return true;
			}
			point = Vector3.zero;
			return false;
		}
		if (m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = m_playerProfile.GetCustomSpawnPoint();
			ZNet.instance.SetReferencePosition(customSpawnPoint);
			if (m_respawnWait > 8f && ZNetScene.instance.IsAreaReady(customSpawnPoint))
			{
				Bed bed = FindBedNearby(customSpawnPoint, 5f);
				if (bed != null)
				{
					ZLog.Log("Found bed at custom spawn point");
					point = bed.GetSpawnPoint();
					return true;
				}
				ZLog.Log("Failed to find bed at custom spawn point, using original");
				m_playerProfile.ClearCustomSpawnPoint();
				m_respawnWait = 0f;
				point = Vector3.zero;
				return false;
			}
			point = Vector3.zero;
			return false;
		}
		if (ZoneSystem.instance.GetLocationIcon(m_StartLocation, out var pos))
		{
			point = pos + Vector3.up * 2f;
			ZNet.instance.SetReferencePosition(point);
			return ZNetScene.instance.IsAreaReady(point);
		}
		ZNet.instance.SetReferencePosition(Vector3.zero);
		point = Vector3.zero;
		return false;
	}

	private static Vector3 GetPointOnCircle(float distance, float angle)
	{
		return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
	}

	public void RequestRespawn(float delay)
	{
		CancelInvoke("_RequestRespawn");
		Invoke("_RequestRespawn", delay);
	}

	private void _RequestRespawn()
	{
		ZLog.Log("Starting respawn");
		if ((bool)Player.m_localPlayer)
		{
			m_playerProfile.SavePlayerData(Player.m_localPlayer);
		}
		if ((bool)Player.m_localPlayer)
		{
			ZNetScene.instance.Destroy(Player.m_localPlayer.gameObject);
			ZNet.instance.SetCharacterID(ZDOID.None);
		}
		m_respawnWait = 0f;
		m_requestRespawn = true;
		MusicMan.instance.TriggerMusic("respawn");
	}

	private void Update()
	{
		if (!m_shuttingDown)
		{
			ZInput.Update(Time.deltaTime);
			UpdateSaving(Time.deltaTime);
			LightLod.UpdateLights(Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (!m_haveSpawned && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			m_haveSpawned = true;
			RequestRespawn(0f);
		}
		ZInput.FixedUpdate(Time.fixedDeltaTime);
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connecting && ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			ZLog.Log("Lost connection to server:" + ZNet.GetConnectionStatus());
			Logout();
		}
		else
		{
			UpdateRespawn(Time.fixedDeltaTime);
		}
	}

	private void UpdateSaving(float dt)
	{
		if (1200f - m_saveTimer > 30f && 1200f - (m_saveTimer + dt) <= 30f && (bool)MessageHud.instance && ZNet.instance.IsServer())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarning " + 30f + "s");
		}
		m_saveTimer += dt;
		if (m_saveTimer > 1200f)
		{
			m_saveTimer = 0f;
			SavePlayerProfile(setLogoutPoint: false);
			if ((bool)ZNet.instance)
			{
				ZNet.instance.Save(sync: false);
			}
		}
	}

	private void UpdateRespawn(float dt)
	{
		if (m_requestRespawn && FindSpawnPoint(out var point, out var usedLogoutPoint, dt))
		{
			if (!usedLogoutPoint)
			{
				m_playerProfile.SetHomePoint(point);
			}
			SpawnPlayer(point);
			m_requestRespawn = false;
			if (m_firstSpawn)
			{
				m_firstSpawn = false;
				Chat.instance.SendText(Talker.Type.Shout, "I have arrived!");
			}
			GC.Collect();
		}
	}

	public bool WaitingForRespawn()
	{
		return m_requestRespawn;
	}

	public PlayerProfile GetPlayerProfile()
	{
		return m_playerProfile;
	}

	public static void SetProfile(string filename)
	{
		m_profileFilename = filename;
	}

	private IEnumerator ConnectPortals()
	{
		while (true)
		{
			m_tempPortalList.Clear();
			int index = 0;
			bool done;
			do
			{
				done = ZDOMan.instance.GetAllZDOsWithPrefabIterative(m_portalPrefab.name, m_tempPortalList, ref index);
				yield return null;
			}
			while (!done);
			foreach (ZDO tempPortal in m_tempPortalList)
			{
				ZDOID zDOID = tempPortal.GetZDOID("target");
				string @string = tempPortal.GetString("tag");
				if (!zDOID.IsNone())
				{
					ZDO zDO = ZDOMan.instance.GetZDO(zDOID);
					if (zDO == null || zDO.GetString("tag") != @string)
					{
						tempPortal.SetOwner(ZDOMan.instance.GetMyID());
						tempPortal.Set("target", ZDOID.None);
						ZDOMan.instance.ForceSendZDO(tempPortal.m_uid);
					}
				}
			}
			foreach (ZDO tempPortal2 in m_tempPortalList)
			{
				string string2 = tempPortal2.GetString("tag");
				if (tempPortal2.GetZDOID("target").IsNone())
				{
					ZDO zDO2 = FindRandomUnconnectedPortal(m_tempPortalList, tempPortal2, string2);
					if (zDO2 != null)
					{
						tempPortal2.SetOwner(ZDOMan.instance.GetMyID());
						zDO2.SetOwner(ZDOMan.instance.GetMyID());
						tempPortal2.Set("target", zDO2.m_uid);
						zDO2.Set("target", tempPortal2.m_uid);
						ZDOMan.instance.ForceSendZDO(tempPortal2.m_uid);
						ZDOMan.instance.ForceSendZDO(zDO2.m_uid);
					}
				}
			}
			yield return new WaitForSeconds(5f);
		}
	}

	private ZDO FindRandomUnconnectedPortal(List<ZDO> portals, ZDO skip, string tag)
	{
		List<ZDO> list = new List<ZDO>();
		foreach (ZDO portal in portals)
		{
			if (portal != skip && portal.GetZDOID("target").IsNone() && !(portal.GetString("tag") != tag))
			{
				list.Add(portal);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private ZDO FindClosestUnconnectedPortal(List<ZDO> portals, ZDO skip, Vector3 refPos)
	{
		ZDO zDO = null;
		float num = 99999f;
		foreach (ZDO portal in portals)
		{
			if (portal != skip && portal.GetZDOID("target").IsNone())
			{
				float num2 = Vector3.Distance(refPos, portal.GetPosition());
				if (zDO == null || num2 < num)
				{
					zDO = portal;
					num = num2;
				}
			}
		}
		return zDO;
	}

	private void UpdateSleeping()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (m_sleeping)
		{
			if (!EnvMan.instance.IsTimeSkipping())
			{
				m_sleeping = false;
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStop");
			}
		}
		else if (!EnvMan.instance.IsTimeSkipping() && (EnvMan.instance.IsAfternoon() || EnvMan.instance.IsNight()) && EverybodyIsTryingToSleep())
		{
			EnvMan.instance.SkipToMorning();
			m_sleeping = true;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStart");
		}
	}

	private bool EverybodyIsTryingToSleep()
	{
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		if (allCharacterZDOS.Count == 0)
		{
			return false;
		}
		foreach (ZDO item in allCharacterZDOS)
		{
			if (!item.GetBool("inBed"))
			{
				return false;
			}
		}
		return true;
	}

	private void SleepStart(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			localPlayer.SetSleeping(sleep: true);
		}
	}

	private void SleepStop(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			localPlayer.SetSleeping(sleep: false);
			localPlayer.AttachStop();
		}
	}

	public void DiscoverClosestLocation(string name, Vector3 point, string pinName, int pinType, bool showMap = true)
	{
		ZLog.Log("DiscoverClosestLocation");
		ZRoutedRpc.instance.InvokeRoutedRPC("DiscoverClosestLocation", name, point, pinName, pinType, showMap);
	}

	private void RPC_DiscoverClosestLocation(long sender, string name, Vector3 point, string pinName, int pinType, bool showMap)
	{
		if (ZoneSystem.instance.FindClosestLocation(name, point, out var closest))
		{
			ZLog.Log("Found location of type " + name);
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "DiscoverLocationRespons", pinName, pinType, closest.m_position, showMap);
		}
		else
		{
			ZLog.LogWarning("Failed to find location of type " + name);
		}
	}

	private void RPC_DiscoverLocationRespons(long sender, string pinName, int pinType, Vector3 pos, bool showMap)
	{
		Minimap.instance.DiscoverLocation(pos, (Minimap.PinType)pinType, pinName, showMap);
	}

	public void Ping()
	{
		if ((bool)Console.instance)
		{
			Console.instance.Print("Ping sent to server");
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("Ping", Time.time);
	}

	private void RPC_Ping(long sender, float time)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(sender, "Pong", time);
	}

	private void RPC_Pong(long sender, float time)
	{
		float num = Time.time - time;
		string text = "Got ping reply from server: " + (int)(num * 1000f) + " ms";
		ZLog.Log(text);
		if ((bool)Console.instance)
		{
			Console.instance.Print(text);
		}
		if ((bool)Chat.instance)
		{
			Chat.instance.AddString(text);
		}
	}

	public void SetForcePlayerDifficulty(int players)
	{
		m_forcePlayers = players;
	}

	public int GetPlayerDifficulty(Vector3 pos)
	{
		if (m_forcePlayers > 0)
		{
			return m_forcePlayers;
		}
		int num = Player.GetPlayersInRangeXZ(pos, 100f);
		if (num < 1)
		{
			num = 1;
		}
		if (num > 5)
		{
			num = 5;
		}
		return num;
	}

	public float GetDifficultyDamageScalePlayer(Vector3 pos)
	{
		int playerDifficulty = GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * 0.04f;
	}

	public float GetDifficultyDamageScaleEnemy(Vector3 pos)
	{
		int playerDifficulty = GetPlayerDifficulty(pos);
		float num = 1f + (float)(playerDifficulty - 1) * 0.3f;
		return 1f / num;
	}
}
