using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
	[Serializable]
	public class DoorDef
	{
		public GameObject m_prefab;

		public string m_connectionType = "";
	}

	public enum Algorithm
	{
		Dungeon,
		CampGrid,
		CampRadial
	}

	public Algorithm m_algorithm;

	public int m_maxRooms = 3;

	public int m_minRooms = 20;

	public int m_minRequiredRooms;

	public List<string> m_requiredRooms = new List<string>();

	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_themes = Room.Theme.Crypt;

	[Header("Dungeon")]
	public List<DoorDef> m_doorTypes = new List<DoorDef>();

	[Range(0f, 1f)]
	public float m_doorChance = 0.5f;

	[Header("Camp")]
	public float m_maxTilt = 10f;

	public float m_tileWidth = 8f;

	public int m_gridSize = 4;

	public float m_spawnChance = 1f;

	[Header("Camp radial")]
	public float m_campRadiusMin = 15f;

	public float m_campRadiusMax = 30f;

	public float m_minAltitude = 1f;

	public int m_perimeterSections;

	public float m_perimeterBuffer = 2f;

	[Header("Misc")]
	public Vector3 m_zoneCenter = new Vector3(0f, 0f, 0f);

	public Vector3 m_zoneSize = new Vector3(64f, 64f, 64f);

	private static List<Room> m_placedRooms = new List<Room>();

	private static List<RoomConnection> m_openConnections = new List<RoomConnection>();

	private static List<RoomConnection> m_doorConnections = new List<RoomConnection>();

	private static List<DungeonDB.RoomData> m_availableRooms = new List<DungeonDB.RoomData>();

	private static List<DungeonDB.RoomData> m_tempRooms = new List<DungeonDB.RoomData>();

	private BoxCollider m_colliderA;

	private BoxCollider m_colliderB;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		Load();
	}

	public void Clear()
	{
		while (base.transform.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
	}

	public void Generate(ZoneSystem.SpawnMode mode)
	{
		int seed = WorldGenerator.instance.GetSeed();
		Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
		int seed2 = seed + zone.x * 4271 + zone.y * 9187;
		Generate(seed2, mode);
	}

	public void Generate(int seed, ZoneSystem.SpawnMode mode)
	{
		DateTime now = DateTime.Now;
		Clear();
		SetupColliders();
		SetupAvailableRooms();
		if ((bool)ZoneSystem.instance)
		{
			Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
			m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
			m_zoneCenter.y = base.transform.position.y;
		}
		ZLog.Log("Available rooms:" + m_availableRooms.Count);
		ZLog.Log("To place:" + m_maxRooms);
		m_placedRooms.Clear();
		m_openConnections.Clear();
		m_doorConnections.Clear();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		GenerateRooms(mode);
		Save();
		ZLog.Log("Placed " + m_placedRooms.Count + " rooms");
		UnityEngine.Random.state = state;
		SnapToGround.SnappAll();
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (Room placedRoom in m_placedRooms)
			{
				UnityEngine.Object.DestroyImmediate(placedRoom.gameObject);
			}
		}
		m_placedRooms.Clear();
		m_openConnections.Clear();
		m_doorConnections.Clear();
		UnityEngine.Object.DestroyImmediate(m_colliderA);
		UnityEngine.Object.DestroyImmediate(m_colliderB);
		_ = DateTime.Now - now;
	}

	private void GenerateRooms(ZoneSystem.SpawnMode mode)
	{
		switch (m_algorithm)
		{
		case Algorithm.Dungeon:
			GenerateDungeon(mode);
			break;
		case Algorithm.CampGrid:
			GenerateCampGrid(mode);
			break;
		case Algorithm.CampRadial:
			GenerateCampRadial(mode);
			break;
		}
	}

	private void GenerateDungeon(ZoneSystem.SpawnMode mode)
	{
		PlaceStartRoom(mode);
		PlaceRooms(mode);
		PlaceEndCaps(mode);
		PlaceDoors(mode);
	}

	private void GenerateCampGrid(ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		Vector3 vector = base.transform.position + new Vector3((float)(-m_gridSize) * m_tileWidth * 0.5f, 0f, (float)(-m_gridSize) * m_tileWidth * 0.5f);
		for (int i = 0; i < m_gridSize; i++)
		{
			for (int j = 0; j < m_gridSize; j++)
			{
				if (UnityEngine.Random.value > m_spawnChance)
				{
					continue;
				}
				Vector3 p = vector + new Vector3((float)j * m_tileWidth, 0f, (float)i * m_tileWidth);
				DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: false);
				if (randomWeightedRoom == null)
				{
					continue;
				}
				if ((bool)ZoneSystem.instance)
				{
					ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
					if (normal.y < num)
					{
						continue;
					}
				}
				Quaternion rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
				PlaceRoom(randomWeightedRoom, p, rot, null, mode);
			}
		}
	}

	private void GenerateCampRadial(ZoneSystem.SpawnMode mode)
	{
		float num = UnityEngine.Random.Range(m_campRadiusMin, m_campRadiusMax);
		float num2 = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		int num3 = UnityEngine.Random.Range(m_minRooms, m_maxRooms);
		int num4 = num3 * 20;
		int num5 = 0;
		for (int i = 0; i < num4; i++)
		{
			Vector3 p = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, num - m_perimeterBuffer);
			DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: false);
			if (randomWeightedRoom == null)
			{
				continue;
			}
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
				if (normal.y < num2 || p.y - ZoneSystem.instance.m_waterLevel < m_minAltitude)
				{
					continue;
				}
			}
			Quaternion campRoomRotation = GetCampRoomRotation(randomWeightedRoom, p);
			if (!TestCollision(randomWeightedRoom.m_room, p, campRoomRotation))
			{
				PlaceRoom(randomWeightedRoom, p, campRoomRotation, null, mode);
				num5++;
				if (num5 >= num3)
				{
					break;
				}
			}
		}
		if (m_perimeterSections > 0)
		{
			PlaceWall(num, m_perimeterSections, mode);
		}
	}

	private Quaternion GetCampRoomRotation(DungeonDB.RoomData room, Vector3 pos)
	{
		if (room.m_room.m_faceCenter)
		{
			Vector3 vector = base.transform.position - pos;
			vector.y = 0f;
			if (vector == Vector3.zero)
			{
				vector = Vector3.forward;
			}
			vector.Normalize();
			float y = Mathf.Round(Utils.YawFromDirection(vector) / 22.5f) * 22.5f;
			return Quaternion.Euler(0f, y, 0f);
		}
		return Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
	}

	private void PlaceWall(float radius, int sections, ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		int num2 = 0;
		int num3 = sections * 20;
		for (int i = 0; i < num3; i++)
		{
			DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: true);
			if (randomWeightedRoom == null)
			{
				continue;
			}
			Vector3 p = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * radius;
			Quaternion campRoomRotation = GetCampRoomRotation(randomWeightedRoom, p);
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
				if (normal.y < num || p.y - ZoneSystem.instance.m_waterLevel < m_minAltitude)
				{
					continue;
				}
			}
			if (!TestCollision(randomWeightedRoom.m_room, p, campRoomRotation))
			{
				PlaceRoom(randomWeightedRoom, p, campRoomRotation, null, mode);
				num2++;
				if (num2 >= sections)
				{
					break;
				}
			}
		}
	}

	private void Save()
	{
		if (!(m_nview == null))
		{
			ZDO zDO = m_nview.GetZDO();
			zDO.Set("rooms", m_placedRooms.Count);
			for (int i = 0; i < m_placedRooms.Count; i++)
			{
				Room room = m_placedRooms[i];
				string text = "room" + i;
				zDO.Set(text, room.GetHash());
				zDO.Set(text + "_pos", room.transform.position);
				zDO.Set(text + "_rot", room.transform.rotation);
			}
		}
	}

	private void Load()
	{
		if (m_nview == null)
		{
			return;
		}
		DateTime now = DateTime.Now;
		ZLog.Log("Loading dungeon");
		ZDO zDO = m_nview.GetZDO();
		int @int = zDO.GetInt("rooms");
		for (int i = 0; i < @int; i++)
		{
			string text = "room" + i;
			int int2 = zDO.GetInt(text);
			Vector3 vec = zDO.GetVec3(text + "_pos", Vector3.zero);
			Quaternion quaternion = zDO.GetQuaternion(text + "_rot", Quaternion.identity);
			DungeonDB.RoomData room = DungeonDB.instance.GetRoom(int2);
			if (room == null)
			{
				ZLog.LogWarning("Missing room:" + int2);
			}
			else
			{
				PlaceRoom(room, vec, quaternion, null, ZoneSystem.SpawnMode.Client);
			}
		}
		ZLog.Log("Dungeon loaded " + @int);
		ZLog.Log("Dungeon load time " + (DateTime.Now - now).TotalMilliseconds + " ms");
	}

	private void SetupAvailableRooms()
	{
		m_availableRooms.Clear();
		foreach (DungeonDB.RoomData room in DungeonDB.GetRooms())
		{
			if ((room.m_room.m_theme & m_themes) != 0 && room.m_room.m_enabled)
			{
				m_availableRooms.Add(room);
			}
		}
	}

	private DoorDef FindDoorType(string type)
	{
		List<DoorDef> list = new List<DoorDef>();
		foreach (DoorDef doorType in m_doorTypes)
		{
			if (doorType.m_connectionType == type)
			{
				list.Add(doorType);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void PlaceDoors(ZoneSystem.SpawnMode mode)
	{
		int num = 0;
		foreach (RoomConnection doorConnection in m_doorConnections)
		{
			if (UnityEngine.Random.value > m_doorChance)
			{
				continue;
			}
			DoorDef doorDef = FindDoorType(doorConnection.m_type);
			if (doorDef == null)
			{
				ZLog.Log("No door type for connection:" + doorConnection.m_type);
				continue;
			}
			GameObject obj = UnityEngine.Object.Instantiate(doorDef.m_prefab, doorConnection.transform.position, doorConnection.transform.rotation);
			if (mode == ZoneSystem.SpawnMode.Ghost)
			{
				UnityEngine.Object.Destroy(obj);
			}
			num++;
		}
		ZLog.Log("placed " + num + " doors");
	}

	private void PlaceEndCaps(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < m_openConnections.Count; i++)
		{
			RoomConnection roomConnection = m_openConnections[i];
			bool flag = false;
			for (int j = 0; j < m_openConnections.Count; j++)
			{
				if (j != i && roomConnection.TestContact(m_openConnections[j]))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				ZLog.Log("cyclic detected , cool");
				continue;
			}
			FindEndCaps(roomConnection, m_tempRooms);
			IOrderedEnumerable<DungeonDB.RoomData> orderedEnumerable = m_tempRooms.OrderByDescending((DungeonDB.RoomData item) => item.m_room.m_endCapPrio);
			bool flag2 = false;
			foreach (DungeonDB.RoomData item in orderedEnumerable)
			{
				if (PlaceRoom(roomConnection, item, mode))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				ZLog.LogWarning("Failed to place end cap " + roomConnection.name + " " + roomConnection.transform.parent.gameObject.name);
			}
		}
	}

	private void FindEndCaps(RoomConnection connection, List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.m_room.m_endCap && availableRoom.m_room.HaveConnection(connection))
			{
				rooms.Add(availableRoom);
			}
		}
		rooms.Shuffle();
	}

	private DungeonDB.RoomData FindEndCap(RoomConnection connection)
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.m_room.m_endCap && availableRoom.m_room.HaveConnection(connection))
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private void PlaceRooms(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < m_maxRooms; i++)
		{
			PlaceOneRoom(mode);
			if (CheckRequiredRooms() && m_placedRooms.Count > m_minRooms)
			{
				ZLog.Log("All required rooms have been placed, stopping generation");
				break;
			}
		}
	}

	private void PlaceStartRoom(ZoneSystem.SpawnMode mode)
	{
		DungeonDB.RoomData roomData = FindStartRoom();
		RoomConnection entrance = roomData.m_room.GetEntrance();
		Quaternion rotation = base.transform.rotation;
		CalculateRoomPosRot(entrance, base.transform.position, rotation, out var pos, out var rot);
		PlaceRoom(roomData, pos, rot, entrance, mode);
	}

	private bool PlaceOneRoom(ZoneSystem.SpawnMode mode)
	{
		RoomConnection openConnection = GetOpenConnection();
		if (openConnection == null)
		{
			return false;
		}
		for (int i = 0; i < 10; i++)
		{
			DungeonDB.RoomData randomRoom = GetRandomRoom(openConnection);
			if (randomRoom == null)
			{
				break;
			}
			if (PlaceRoom(openConnection, randomRoom, mode))
			{
				return true;
			}
		}
		return false;
	}

	private void CalculateRoomPosRot(RoomConnection roomCon, Vector3 exitPos, Quaternion exitRot, out Vector3 pos, out Quaternion rot)
	{
		Quaternion quaternion = Quaternion.Inverse(roomCon.transform.localRotation);
		rot = exitRot * quaternion;
		Vector3 localPosition = roomCon.transform.localPosition;
		pos = exitPos - rot * localPosition;
	}

	private bool PlaceRoom(RoomConnection connection, DungeonDB.RoomData roomData, ZoneSystem.SpawnMode mode)
	{
		Room room = roomData.m_room;
		Quaternion rotation = connection.transform.rotation;
		rotation *= Quaternion.Euler(0f, 180f, 0f);
		RoomConnection connection2 = room.GetConnection(connection);
		CalculateRoomPosRot(connection2, connection.transform.position, rotation, out var pos, out var rot);
		if (room.m_size.x != 0 && room.m_size.z != 0 && TestCollision(room, pos, rot))
		{
			return false;
		}
		PlaceRoom(roomData, pos, rot, connection, mode);
		if (!room.m_endCap)
		{
			if (connection.m_allowDoor)
			{
				m_doorConnections.Add(connection);
			}
			m_openConnections.Remove(connection);
		}
		return true;
	}

	private void PlaceRoom(DungeonDB.RoomData room, Vector3 pos, Quaternion rot, RoomConnection fromConnection, ZoneSystem.SpawnMode mode)
	{
		int seed = (int)pos.x * 4271 + (int)pos.y * 9187 + (int)pos.z * 2134;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (ZNetView netView in room.m_netViews)
			{
				netView.gameObject.SetActive(value: true);
			}
			foreach (RandomSpawn randomSpawn in room.m_randomSpawns)
			{
				randomSpawn.Randomize();
			}
			Vector3 position = room.m_room.transform.position;
			Quaternion quaternion = Quaternion.Inverse(room.m_room.transform.rotation);
			foreach (ZNetView netView2 in room.m_netViews)
			{
				if (netView2.gameObject.activeSelf)
				{
					Vector3 vector = quaternion * (netView2.gameObject.transform.position - position);
					Vector3 position2 = pos + rot * vector;
					Quaternion quaternion2 = quaternion * netView2.gameObject.transform.rotation;
					Quaternion rotation = rot * quaternion2;
					GameObject gameObject = UnityEngine.Object.Instantiate(netView2.gameObject, position2, rotation);
					ZNetView component = gameObject.GetComponent<ZNetView>();
					if (component.GetZDO() != null)
					{
						component.GetZDO().SetPGWVersion(ZoneSystem.instance.m_pgwVersion);
					}
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						UnityEngine.Object.Destroy(gameObject);
					}
				}
			}
		}
		else
		{
			foreach (RandomSpawn randomSpawn2 in room.m_randomSpawns)
			{
				randomSpawn2.Randomize();
			}
		}
		foreach (ZNetView netView3 in room.m_netViews)
		{
			netView3.gameObject.SetActive(value: false);
		}
		Room component2 = UnityEngine.Object.Instantiate(room.m_room.gameObject, pos, rot, base.transform).GetComponent<Room>();
		component2.gameObject.name = room.m_room.gameObject.name;
		if (mode != ZoneSystem.SpawnMode.Client)
		{
			component2.m_placeOrder = (fromConnection ? (fromConnection.m_placeOrder + 1) : 0);
			m_placedRooms.Add(component2);
			AddOpenConnections(component2, fromConnection);
		}
		UnityEngine.Random.state = state;
	}

	private void AddOpenConnections(Room newRoom, RoomConnection skipConnection)
	{
		RoomConnection[] connections = newRoom.GetConnections();
		if (skipConnection != null)
		{
			RoomConnection[] array = connections;
			foreach (RoomConnection roomConnection in array)
			{
				if (!roomConnection.m_entrance && !(Vector3.Distance(roomConnection.transform.position, skipConnection.transform.position) < 0.1f))
				{
					roomConnection.m_placeOrder = newRoom.m_placeOrder;
					m_openConnections.Add(roomConnection);
				}
			}
		}
		else
		{
			RoomConnection[] array = connections;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].m_placeOrder = newRoom.m_placeOrder;
			}
			m_openConnections.AddRange(connections);
		}
	}

	private void SetupColliders()
	{
		if (!(m_colliderA != null))
		{
			BoxCollider[] componentsInChildren = base.gameObject.GetComponentsInChildren<BoxCollider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
			}
			m_colliderA = base.gameObject.AddComponent<BoxCollider>();
			m_colliderB = base.gameObject.AddComponent<BoxCollider>();
		}
	}

	public void Derp()
	{
	}

	private bool IsInsideDungeon(Room room, Vector3 pos, Quaternion rot)
	{
		Bounds bounds = new Bounds(m_zoneCenter, m_zoneSize);
		Vector3 vector = room.m_size;
		vector *= 0.5f;
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, 0f - vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, 0f - vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, 0f - vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, 0f - vector.y, vector.z)))
		{
			return false;
		}
		return true;
	}

	private bool TestCollision(Room room, Vector3 pos, Quaternion rot)
	{
		if (!IsInsideDungeon(room, pos, rot))
		{
			return true;
		}
		m_colliderA.size = new Vector3((float)room.m_size.x - 0.1f, (float)room.m_size.y - 0.1f, (float)room.m_size.z - 0.1f);
		foreach (Room placedRoom in m_placedRooms)
		{
			m_colliderB.size = placedRoom.m_size;
			if (Physics.ComputePenetration(m_colliderA, pos, rot, m_colliderB, placedRoom.transform.position, placedRoom.transform.rotation, out var _, out var _))
			{
				return true;
			}
		}
		return false;
	}

	private DungeonDB.RoomData GetRandomWeightedRoom(bool perimeterRoom)
	{
		m_tempRooms.Clear();
		float num = 0f;
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (!availableRoom.m_room.m_entrance && !availableRoom.m_room.m_endCap && availableRoom.m_room.m_perimeter == perimeterRoom)
			{
				num += availableRoom.m_room.m_weight;
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData tempRoom in m_tempRooms)
		{
			num3 += tempRoom.m_room.m_weight;
			if (num2 <= num3)
			{
				return tempRoom;
			}
		}
		return m_tempRooms[0];
	}

	private DungeonDB.RoomData GetRandomRoom(RoomConnection connection)
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (!availableRoom.m_room.m_entrance && !availableRoom.m_room.m_endCap && (!connection || (availableRoom.m_room.HaveConnection(connection) && connection.m_placeOrder >= availableRoom.m_room.m_minPlaceOrder)))
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private RoomConnection GetOpenConnection()
	{
		if (m_openConnections.Count == 0)
		{
			return null;
		}
		return m_openConnections[UnityEngine.Random.Range(0, m_openConnections.Count)];
	}

	private DungeonDB.RoomData FindStartRoom()
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.m_room.m_entrance)
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private bool CheckRequiredRooms()
	{
		if (m_minRequiredRooms == 0 || m_requiredRooms.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (Room placedRoom in m_placedRooms)
		{
			if (m_requiredRooms.Contains(placedRoom.gameObject.name))
			{
				num++;
			}
		}
		return num >= m_minRequiredRooms;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0f, 1.5f, 0f, 0.5f);
		Gizmos.DrawWireCube(m_zoneCenter, new Vector3(m_zoneSize.x, m_zoneSize.y, m_zoneSize.z));
		Gizmos.matrix = Matrix4x4.identity;
	}
}
