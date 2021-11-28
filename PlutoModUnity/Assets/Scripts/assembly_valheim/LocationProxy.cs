using UnityEngine;

public class LocationProxy : MonoBehaviour
{
	private GameObject m_instance;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		SpawnLocation();
	}

	public void SetLocation(string location, int seed, bool spawnNow, int pgw)
	{
		int stableHashCode = location.GetStableHashCode();
		m_nview.GetZDO().Set("location", stableHashCode);
		m_nview.GetZDO().Set("seed", seed);
		m_nview.GetZDO().SetPGWVersion(pgw);
		if (spawnNow)
		{
			SpawnLocation();
		}
	}

	private bool SpawnLocation()
	{
		int @int = m_nview.GetZDO().GetInt("location");
		int int2 = m_nview.GetZDO().GetInt("seed");
		if (@int == 0)
		{
			return false;
		}
		m_instance = ZoneSystem.instance.SpawnProxyLocation(@int, int2, base.transform.position, base.transform.rotation);
		if (m_instance == null)
		{
			return false;
		}
		m_instance.transform.SetParent(base.transform, worldPositionStays: true);
		return true;
	}
}
