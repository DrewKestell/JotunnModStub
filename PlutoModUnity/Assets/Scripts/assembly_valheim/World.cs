using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class World
{
	public string m_name = "";

	public string m_seedName = "";

	public int m_seed;

	public long m_uid;

	public int m_worldGenVersion;

	public bool m_menu;

	public bool m_loadError;

	public bool m_versionError;

	private string m_worldSavePath = "";

	public World()
	{
		m_worldSavePath = GetWorldSavePath();
	}

	public World(string name, bool loadError, bool versionError)
	{
		m_name = name;
		m_loadError = loadError;
		m_versionError = versionError;
		m_worldSavePath = GetWorldSavePath();
	}

	public World(string name, string seed)
	{
		m_name = name;
		m_seedName = seed;
		m_seed = ((!(m_seedName == "")) ? m_seedName.GetStableHashCode() : 0);
		m_uid = name.GetStableHashCode() + Utils.GenerateUID();
		m_worldGenVersion = Version.m_worldGenVersion;
		m_worldSavePath = GetWorldSavePath();
	}

	private static string GetWorldSavePath()
	{
		return Utils.GetSaveDataPath() + "/worlds";
	}

	public static List<World> GetWorldList()
	{
		string[] array;
		try
		{
			array = Directory.GetFiles(GetWorldSavePath(), "*.fwl");
		}
		catch
		{
			array = new string[0];
		}
		List<World> list = new List<World>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			World world = LoadWorld(Path.GetFileNameWithoutExtension(array2[i]));
			if (world != null)
			{
				list.Add(world);
			}
		}
		return list;
	}

	public static void RemoveWorld(string name)
	{
		try
		{
			string text = GetWorldSavePath() + "/" + name;
			File.Delete(text + ".fwl");
			File.Delete(text + ".db");
		}
		catch
		{
		}
	}

	public string GetDBPath()
	{
		return m_worldSavePath + "/" + m_name + ".db";
	}

	public string GetMetaPath()
	{
		return m_worldSavePath + "/" + m_name + ".fwl";
	}

	public static string GetMetaPath(string name)
	{
		return GetWorldSavePath() + "/" + name + ".fwl";
	}

	public static bool HaveWorld(string name)
	{
		return File.Exists(string.Concat(GetWorldSavePath() + "/" + name, ".fwl"));
	}

	public static World GetMenuWorld()
	{
		return new World("menu", "")
		{
			m_menu = true
		};
	}

	public static World GetEditorWorld()
	{
		return new World("editor", "");
	}

	public static string GenerateSeed()
	{
		string text = "";
		for (int i = 0; i < 10; i++)
		{
			text += "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789"[Random.Range(0, "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789".Length)];
		}
		return text;
	}

	public static World GetCreateWorld(string name)
	{
		ZLog.Log("Get create world " + name);
		World world = LoadWorld(name);
		if (!world.m_loadError && !world.m_versionError)
		{
			return world;
		}
		ZLog.Log(" creating");
		world = new World(name, GenerateSeed());
		world.SaveWorldMetaData();
		return world;
	}

	public static World GetDevWorld()
	{
		World world = LoadWorld("DevWorld");
		if (!world.m_loadError && !world.m_versionError)
		{
			return world;
		}
		world = new World("DevWorld", "");
		world.SaveWorldMetaData();
		return world;
	}

	public void SaveWorldMetaData()
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(Version.m_worldVersion);
		zPackage.Write(m_name);
		zPackage.Write(m_seedName);
		zPackage.Write(m_seed);
		zPackage.Write(m_uid);
		zPackage.Write(m_worldGenVersion);
		Directory.CreateDirectory(m_worldSavePath);
		string metaPath = GetMetaPath();
		string text = metaPath + ".new";
		string text2 = metaPath + ".old";
		byte[] array = zPackage.GetArray();
		FileStream fileStream = File.Create(text);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
		binaryWriter.Flush();
		fileStream.Flush(flushToDisk: true);
		fileStream.Close();
		fileStream.Dispose();
		if (File.Exists(metaPath))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(metaPath, text2);
		}
		File.Move(text, metaPath);
	}

	public static World LoadWorld(string name)
	{
		FileStream fileStream = null;
		try
		{
			fileStream = File.OpenRead(GetMetaPath(name));
		}
		catch
		{
			fileStream?.Dispose();
			ZLog.Log("  failed to load " + name);
			return new World(name, loadError: true, versionError: false);
		}
		try
		{
			BinaryReader binaryReader = new BinaryReader(fileStream);
			int count = binaryReader.ReadInt32();
			ZPackage zPackage = new ZPackage(binaryReader.ReadBytes(count));
			int num = zPackage.ReadInt();
			if (!Version.IsWorldVersionCompatible(num))
			{
				ZLog.Log("incompatible world version " + num);
				return new World(name, loadError: false, versionError: true);
			}
			World world = new World();
			world.m_name = zPackage.ReadString();
			world.m_seedName = zPackage.ReadString();
			world.m_seed = zPackage.ReadInt();
			world.m_uid = zPackage.ReadLong();
			if (num >= 26)
			{
				world.m_worldGenVersion = zPackage.ReadInt();
			}
			return world;
		}
		catch
		{
			ZLog.LogWarning("  error loading world " + name);
			return new World(name, loadError: true, versionError: false);
		}
		finally
		{
			fileStream?.Dispose();
		}
	}
}
