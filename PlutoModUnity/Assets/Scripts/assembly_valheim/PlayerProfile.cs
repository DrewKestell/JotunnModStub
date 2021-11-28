using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerProfile
{
	private class WorldPlayerData
	{
		public Vector3 m_spawnPoint = Vector3.zero;

		public bool m_haveCustomSpawnPoint;

		public Vector3 m_logoutPoint = Vector3.zero;

		public bool m_haveLogoutPoint;

		public Vector3 m_deathPoint = Vector3.zero;

		public bool m_haveDeathPoint;

		public Vector3 m_homePoint = Vector3.zero;

		public byte[] m_mapData;
	}

	public class PlayerStats
	{
		public int m_kills;

		public int m_deaths;

		public int m_crafts;

		public int m_builds;
	}

	private string m_filename = "";

	private string m_playerName = "";

	private long m_playerID;

	private string m_startSeed = "";

	public static Vector3 m_originalSpawnPoint = new Vector3(-676f, 50f, 299f);

	private Dictionary<long, WorldPlayerData> m_worldData = new Dictionary<long, WorldPlayerData>();

	public PlayerStats m_playerStats = new PlayerStats();

	private byte[] m_playerData;

	public PlayerProfile(string filename = null)
	{
		m_filename = filename;
		m_playerName = "Stranger";
		m_playerID = Utils.GenerateUID();
	}

	public bool Load()
	{
		if (m_filename == null)
		{
			return false;
		}
		return LoadPlayerFromDisk();
	}

	public bool Save()
	{
		if (m_filename == null)
		{
			return false;
		}
		return SavePlayerToDisk();
	}

	public bool HaveIncompatiblPlayerData()
	{
		if (m_filename == null)
		{
			return false;
		}
		ZPackage zPackage = LoadPlayerDataFromDisk();
		if (zPackage == null)
		{
			return false;
		}
		if (!Version.IsPlayerVersionCompatible(zPackage.ReadInt()))
		{
			ZLog.Log("Player data is not compatible, ignoring");
			return true;
		}
		return false;
	}

	public void SavePlayerData(Player player)
	{
		ZPackage zPackage = new ZPackage();
		player.Save(zPackage);
		m_playerData = zPackage.GetArray();
	}

	public void LoadPlayerData(Player player)
	{
		player.SetPlayerID(m_playerID, m_playerName);
		if (m_playerData != null)
		{
			ZPackage pkg = new ZPackage(m_playerData);
			player.Load(pkg);
		}
		else
		{
			player.GiveDefaultItems();
		}
	}

	public void SaveLogoutPoint()
	{
		if ((bool)Player.m_localPlayer && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.InIntro())
		{
			SetLogoutPoint(Player.m_localPlayer.transform.position);
		}
	}

	private bool SavePlayerToDisk()
	{
		Directory.CreateDirectory(Utils.GetSaveDataPath() + "/characters");
		string text = Utils.GetSaveDataPath() + "/characters/" + m_filename + ".fch";
		string text2 = Utils.GetSaveDataPath() + "/characters/" + m_filename + ".fch.old";
		string text3 = Utils.GetSaveDataPath() + "/characters/" + m_filename + ".fch.new";
		ZPackage zPackage = new ZPackage();
		zPackage.Write(Version.m_playerVersion);
		zPackage.Write(m_playerStats.m_kills);
		zPackage.Write(m_playerStats.m_deaths);
		zPackage.Write(m_playerStats.m_crafts);
		zPackage.Write(m_playerStats.m_builds);
		zPackage.Write(m_worldData.Count);
		foreach (KeyValuePair<long, WorldPlayerData> worldDatum in m_worldData)
		{
			zPackage.Write(worldDatum.Key);
			zPackage.Write(worldDatum.Value.m_haveCustomSpawnPoint);
			zPackage.Write(worldDatum.Value.m_spawnPoint);
			zPackage.Write(worldDatum.Value.m_haveLogoutPoint);
			zPackage.Write(worldDatum.Value.m_logoutPoint);
			zPackage.Write(worldDatum.Value.m_haveDeathPoint);
			zPackage.Write(worldDatum.Value.m_deathPoint);
			zPackage.Write(worldDatum.Value.m_homePoint);
			zPackage.Write(worldDatum.Value.m_mapData != null);
			if (worldDatum.Value.m_mapData != null)
			{
				zPackage.Write(worldDatum.Value.m_mapData);
			}
		}
		zPackage.Write(m_playerName);
		zPackage.Write(m_playerID);
		zPackage.Write(m_startSeed);
		if (m_playerData != null)
		{
			zPackage.Write(data: true);
			zPackage.Write(m_playerData);
		}
		else
		{
			zPackage.Write(data: false);
		}
		byte[] array = zPackage.GenerateHash();
		byte[] array2 = zPackage.GetArray();
		FileStream fileStream = File.Create(text3);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(array2.Length);
		binaryWriter.Write(array2);
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
		binaryWriter.Flush();
		fileStream.Flush(flushToDisk: true);
		fileStream.Close();
		fileStream.Dispose();
		if (File.Exists(text))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(text, text2);
		}
		File.Move(text3, text);
		return true;
	}

	private bool LoadPlayerFromDisk()
	{
		try
		{
			ZPackage zPackage = LoadPlayerDataFromDisk();
			if (zPackage == null)
			{
				ZLog.LogWarning("No player data");
				return false;
			}
			int num = zPackage.ReadInt();
			if (!Version.IsPlayerVersionCompatible(num))
			{
				ZLog.Log("Player data is not compatible, ignoring");
				return false;
			}
			if (num >= 28)
			{
				m_playerStats.m_kills = zPackage.ReadInt();
				m_playerStats.m_deaths = zPackage.ReadInt();
				m_playerStats.m_crafts = zPackage.ReadInt();
				m_playerStats.m_builds = zPackage.ReadInt();
			}
			m_worldData.Clear();
			int num2 = zPackage.ReadInt();
			for (int i = 0; i < num2; i++)
			{
				long key = zPackage.ReadLong();
				WorldPlayerData worldPlayerData = new WorldPlayerData();
				worldPlayerData.m_haveCustomSpawnPoint = zPackage.ReadBool();
				worldPlayerData.m_spawnPoint = zPackage.ReadVector3();
				worldPlayerData.m_haveLogoutPoint = zPackage.ReadBool();
				worldPlayerData.m_logoutPoint = zPackage.ReadVector3();
				if (num >= 30)
				{
					worldPlayerData.m_haveDeathPoint = zPackage.ReadBool();
					worldPlayerData.m_deathPoint = zPackage.ReadVector3();
				}
				worldPlayerData.m_homePoint = zPackage.ReadVector3();
				if (num >= 29 && zPackage.ReadBool())
				{
					worldPlayerData.m_mapData = zPackage.ReadByteArray();
				}
				m_worldData.Add(key, worldPlayerData);
			}
			m_playerName = zPackage.ReadString();
			m_playerID = zPackage.ReadLong();
			m_startSeed = zPackage.ReadString();
			if (zPackage.ReadBool())
			{
				m_playerData = zPackage.ReadByteArray();
			}
			else
			{
				m_playerData = null;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Exception while loading player profile:" + m_filename + " , " + ex.ToString());
		}
		return true;
	}

	private ZPackage LoadPlayerDataFromDisk()
	{
		string text = Utils.GetSaveDataPath() + "/characters/" + m_filename + ".fch";
		FileStream fileStream;
		try
		{
			fileStream = File.OpenRead(text);
		}
		catch
		{
			ZLog.Log("  failed to load " + text);
			return null;
		}
		byte[] data;
		try
		{
			BinaryReader binaryReader = new BinaryReader(fileStream);
			int count = binaryReader.ReadInt32();
			data = binaryReader.ReadBytes(count);
			int count2 = binaryReader.ReadInt32();
			binaryReader.ReadBytes(count2);
		}
		catch
		{
			ZLog.LogError("  error loading player.dat");
			fileStream.Dispose();
			return null;
		}
		fileStream.Dispose();
		return new ZPackage(data);
	}

	public void SetLogoutPoint(Vector3 point)
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = true;
		GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint = point;
	}

	public void SetDeathPoint(Vector3 point)
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint = true;
		GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint = point;
	}

	public void SetMapData(byte[] data)
	{
		long worldUID = ZNet.instance.GetWorldUID();
		if (worldUID != 0L)
		{
			GetWorldData(worldUID).m_mapData = data;
		}
	}

	public byte[] GetMapData()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_mapData;
	}

	public void ClearLoguoutPoint()
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = false;
	}

	public bool HaveLogoutPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint;
	}

	public Vector3 GetLogoutPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint;
	}

	public bool HaveDeathPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint;
	}

	public Vector3 GetDeathPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint;
	}

	public void SetCustomSpawnPoint(Vector3 point)
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = true;
		GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint = point;
	}

	public Vector3 GetCustomSpawnPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint;
	}

	public bool HaveCustomSpawnPoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint;
	}

	public void ClearCustomSpawnPoint()
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = false;
	}

	public void SetHomePoint(Vector3 point)
	{
		GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint = point;
	}

	public Vector3 GetHomePoint()
	{
		return GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint;
	}

	public void SetName(string name)
	{
		m_playerName = name;
	}

	public string GetName()
	{
		return m_playerName;
	}

	public long GetPlayerID()
	{
		return m_playerID;
	}

	public static List<PlayerProfile> GetAllPlayerProfiles()
	{
		string[] array;
		try
		{
			array = Directory.GetFiles(Utils.GetSaveDataPath() + "/characters", "*.fch");
		}
		catch
		{
			array = new string[0];
		}
		List<PlayerProfile> list = new List<PlayerProfile>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array2[i]);
			PlayerProfile playerProfile = new PlayerProfile(fileNameWithoutExtension);
			if (!playerProfile.Load())
			{
				ZLog.Log("Failed to load " + fileNameWithoutExtension);
			}
			else
			{
				list.Add(playerProfile);
			}
		}
		return list;
	}

	public static void RemoveProfile(string name)
	{
		try
		{
			File.Delete(Utils.GetSaveDataPath() + "/characters/" + name + ".fch");
		}
		catch
		{
		}
	}

	public static bool HaveProfile(string name)
	{
		return File.Exists(Utils.GetSaveDataPath() + "/characters/" + name + ".fch");
	}

	public string GetFilename()
	{
		return m_filename;
	}

	private WorldPlayerData GetWorldData(long worldUID)
	{
		if (m_worldData.TryGetValue(worldUID, out var value))
		{
			return value;
		}
		value = new WorldPlayerData();
		m_worldData.Add(worldUID, value);
		return value;
	}
}
