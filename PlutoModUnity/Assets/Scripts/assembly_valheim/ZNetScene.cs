using System;
using System.Collections.Generic;
using UnityEngine;

public class ZNetScene : MonoBehaviour
{
	private static ZNetScene m_instance;

	private const int m_maxCreatedPerFrame = 10;

	private const int m_maxDestroyedPerFrame = 20;

	private const float m_createDestroyFps = 30f;

	public List<GameObject> m_prefabs = new List<GameObject>();

	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	private Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	private Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>(new ZDOComparer());

	private List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	private List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	private List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	private List<ZNetView> m_tempRemoved = new List<ZNetView>();

	private HashSet<ZDO> m_tempActiveZDOs = new HashSet<ZDO>(new ZDOComparer());

	private float m_createDestroyTimer;

	public static ZNetScene instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		foreach (GameObject prefab in m_prefabs)
		{
			m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
		}
		foreach (GameObject nonNetViewPrefab in m_nonNetViewPrefabs)
		{
			m_namedPrefabs.Add(nonNetViewPrefab.name.GetStableHashCode(), nonNetViewPrefab);
		}
		ZDOMan zDOMan = ZDOMan.instance;
		zDOMan.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(zDOMan.m_onZDODestroyed, new Action<ZDO>(OnZDODestroyed));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", RPC_SpawnObject);
	}

	private void OnDestroy()
	{
		ZLog.Log("Net scene destroyed");
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value)
			{
				instance.Value.ResetZDO();
				UnityEngine.Object.Destroy(instance.Value.gameObject);
			}
		}
		m_instances.Clear();
		base.enabled = false;
	}

	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		m_instances[zdo] = nview;
	}

	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return false;
		}
		if (GetPrefab(prefab) == null)
		{
			return false;
		}
		return true;
	}

	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject result = UnityEngine.Object.Instantiate(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			ZLog.LogWarning(string.Concat("ZDO ", zdo.m_uid, " not used when creating object ", prefab2.name));
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return result;
	}

	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if ((bool)component && component.GetZDO() != null)
		{
			ZDO zDO = component.GetZDO();
			component.ResetZDO();
			m_instances.Remove(zDO);
			if (zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	public GameObject GetPrefab(int hash)
	{
		if (m_namedPrefabs.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	public GameObject GetPrefab(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		return GetPrefab(stableHashCode);
	}

	public int GetPrefabHash(GameObject go)
	{
		return go.name.GetStableHashCode();
	}

	public bool IsAreaReady(Vector3 point)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, m_tempCurrentObjects);
		foreach (ZDO tempCurrentObject in m_tempCurrentObjects)
		{
			if (IsPrefabZDOValid(tempCurrentObject) && !FindInstance(tempCurrentObject))
			{
				return false;
			}
		}
		return true;
	}

	private bool InLoadingScreen()
	{
		if (Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting())
		{
			return true;
		}
		return false;
	}

	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int maxCreatedPerFrame = 10;
		if (InLoadingScreen())
		{
			maxCreatedPerFrame = 100;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO key in m_instances.Keys)
		{
			key.m_tempCreateEarmark = frameCount;
		}
		int created = 0;
		CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref created);
		CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref created);
	}

	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		if (!ZoneSystem.instance.IsActiveAreaLoaded())
		{
			return;
		}
		m_tempCurrentObjects2.Clear();
		int frameCount = Time.frameCount;
		Vector3 referencePosition = ZNet.instance.GetReferencePosition();
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			if (currentNearObject.m_tempCreateEarmark != frameCount)
			{
				currentNearObject.m_tempSortValue = Utils.DistanceSqr(referencePosition, currentNearObject.GetPosition());
				m_tempCurrentObjects2.Add(currentNearObject);
			}
		}
		int num = Mathf.Max(m_tempCurrentObjects2.Count / 100, maxCreatedPerFrame);
		m_tempCurrentObjects2.Sort(ZDOCompare);
		foreach (ZDO item in m_tempCurrentObjects2)
		{
			if (CreateObject(item) != null)
			{
				created++;
				if (created > num)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				item.SetOwner(ZDOMan.instance.GetMyID());
				ZLog.Log("Destroyed invalid predab ZDO:" + item.m_uid);
				ZDOMan.instance.DestroyZDO(item);
			}
		}
	}

	private static int ZDOCompare(ZDO x, ZDO y)
	{
		if (x.m_type == y.m_type)
		{
			return x.m_tempSortValue.CompareTo(y.m_tempSortValue);
		}
		return y.m_type.CompareTo(x.m_type);
	}

	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		int frameCount = Time.frameCount;
		foreach (ZDO @object in objects)
		{
			if (@object.m_tempCreateEarmark == frameCount)
			{
				continue;
			}
			if (CreateObject(@object) != null)
			{
				created++;
				if (created > maxCreatedPerFrame)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				@object.SetOwner(ZDOMan.instance.GetMyID());
				ZLog.Log(string.Concat("Destroyed invalid predab ZDO:", @object.m_uid, "  prefab hash:", @object.GetPrefab()));
				ZDOMan.instance.DestroyZDO(@object);
			}
		}
	}

	private void OnZDODestroyed(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			value.ResetZDO();
			UnityEngine.Object.Destroy(value.gameObject);
			m_instances.Remove(zdo);
		}
	}

	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int frameCount = Time.frameCount;
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			currentNearObject.m_tempRemoveEarmark = frameCount;
		}
		foreach (ZDO currentDistantObject in currentDistantObjects)
		{
			currentDistantObject.m_tempRemoveEarmark = frameCount;
		}
		m_tempRemoved.Clear();
		foreach (ZNetView value in m_instances.Values)
		{
			if (value.GetZDO().m_tempRemoveEarmark != frameCount)
			{
				m_tempRemoved.Add(value);
			}
		}
		for (int i = 0; i < m_tempRemoved.Count; i++)
		{
			ZNetView zNetView = m_tempRemoved[i];
			ZDO zDO = zNetView.GetZDO();
			zNetView.ResetZDO();
			UnityEngine.Object.Destroy(zNetView.gameObject);
			if (!zDO.m_persistent && zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
			m_instances.Remove(zDO);
		}
	}

	public ZNetView FindInstance(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			return value;
		}
		return null;
	}

	public bool HaveInstance(ZDO zdo)
	{
		return m_instances.ContainsKey(zdo);
	}

	public GameObject FindInstance(ZDOID id)
	{
		ZDO zDO = ZDOMan.instance.GetZDO(id);
		if (zDO != null)
		{
			ZNetView zNetView = FindInstance(zDO);
			if ((bool)zNetView)
			{
				return zNetView.gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		m_createDestroyTimer += deltaTime;
		if (m_createDestroyTimer >= 71f / (678f * (float)Math.PI))
		{
			m_createDestroyTimer = 0f;
			CreateDestroyObjects();
		}
	}

	private void CreateDestroyObjects()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
		m_tempCurrentObjects.Clear();
		m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, m_tempCurrentObjects, m_tempCurrentDistantObjects);
		CreateObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
		RemoveObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
	}

	public bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		Vector2i zone2 = ZoneSystem.instance.GetZone(refPoint);
		return InActiveArea(zone, zone2);
	}

	public bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		int num = ZoneSystem.instance.m_activeArea - 1;
		if (zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num)
		{
			return zone.y >= refCenterZone.y - num;
		}
		return false;
	}

	public bool OutsideActiveArea(Vector3 point)
	{
		return OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	public bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.instance.GetZone(point);
		if (zone2.x > zone.x - ZoneSystem.instance.m_activeArea && zone2.x < zone.x + ZoneSystem.instance.m_activeArea && zone2.y < zone.y + ZoneSystem.instance.m_activeArea)
		{
			return zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
		}
		return true;
	}

	public bool HaveInstanceInSector(Vector2i sector)
	{
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value && !instance.Value.m_distant && ZoneSystem.instance.GetZone(instance.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	public int NrOfInstances()
	{
		return m_instances.Count;
	}

	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", pos, rot, prefabHash);
	}

	public List<string> GetPrefabNames()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<int, GameObject> namedPrefab in m_namedPrefabs)
		{
			list.Add(namedPrefab.Value.name);
		}
		return list;
	}

	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log("Missing prefab " + prefabHash);
		}
		else
		{
			UnityEngine.Object.Instantiate(prefab, pos, rot);
		}
	}
}
