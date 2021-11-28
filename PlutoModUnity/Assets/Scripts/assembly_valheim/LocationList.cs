using System.Collections.Generic;
using UnityEngine;

public class LocationList : MonoBehaviour
{
	private static List<LocationList> m_allLocationLists = new List<LocationList>();

	public int m_sortOrder;

	public List<ZoneSystem.ZoneLocation> m_locations = new List<ZoneSystem.ZoneLocation>();

	public List<ZoneSystem.ZoneVegetation> m_vegetation = new List<ZoneSystem.ZoneVegetation>();

	public List<EnvSetup> m_environments = new List<EnvSetup>();

	public List<BiomeEnvSetup> m_biomeEnvironments = new List<BiomeEnvSetup>();

	private void Awake()
	{
		m_allLocationLists.Add(this);
	}

	private void OnDestroy()
	{
		m_allLocationLists.Remove(this);
	}

	public static List<LocationList> GetAllLocationLists()
	{
		return m_allLocationLists;
	}
}
