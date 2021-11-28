using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonDB : MonoBehaviour
{
	public class RoomData
	{
		public Room m_room;

		[NonSerialized]
		public List<ZNetView> m_netViews = new List<ZNetView>();

		[NonSerialized]
		public List<RandomSpawn> m_randomSpawns = new List<RandomSpawn>();
	}

	private static DungeonDB m_instance;

	public List<string> m_roomScenes = new List<string>();

	private List<RoomData> m_rooms = new List<RoomData>();

	private Dictionary<int, RoomData> m_roomByHash = new Dictionary<int, RoomData>();

	private bool m_error;

	public static DungeonDB instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		foreach (string roomScene in m_roomScenes)
		{
			SceneManager.LoadScene(roomScene, LoadSceneMode.Additive);
		}
		ZLog.Log("DungeonDB Awake " + Time.frameCount);
	}

	public bool SkipSaving()
	{
		return m_error;
	}

	private void Start()
	{
		ZLog.Log("DungeonDB Start " + Time.frameCount);
		m_rooms = SetupRooms();
		GenerateHashList();
	}

	public static List<RoomData> GetRooms()
	{
		return m_instance.m_rooms;
	}

	private static List<RoomData> SetupRooms()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		GameObject gameObject = null;
		List<RoomData> list = new List<RoomData>();
		GameObject[] array2 = array;
		foreach (GameObject gameObject2 in array2)
		{
			if (!(gameObject2.name == "_Rooms"))
			{
				continue;
			}
			gameObject = gameObject2;
			if (gameObject == null || ((bool)m_instance && gameObject.activeSelf))
			{
				if ((bool)m_instance)
				{
					m_instance.m_error = true;
				}
				ZLog.LogError("Rooms are fucked, missing _Rooms or its enabled");
			}
			for (int j = 0; j < gameObject.transform.childCount; j++)
			{
				Room component = gameObject.transform.GetChild(j).GetComponent<Room>();
				RoomData roomData = new RoomData();
				roomData.m_room = component;
				ZoneSystem.PrepareNetViews(component.gameObject, roomData.m_netViews);
				ZoneSystem.PrepareRandomSpawns(component.gameObject, roomData.m_randomSpawns);
				list.Add(roomData);
			}
		}
		return list;
	}

	public RoomData GetRoom(int hash)
	{
		if (m_roomByHash.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	private void GenerateHashList()
	{
		m_roomByHash.Clear();
		foreach (RoomData room in m_rooms)
		{
			int stableHashCode = room.m_room.gameObject.name.GetStableHashCode();
			m_roomByHash.Add(stableHashCode, room);
		}
	}
}
