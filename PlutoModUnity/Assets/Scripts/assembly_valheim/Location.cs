using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Location : MonoBehaviour
{
	[FormerlySerializedAs("m_radius")]
	public float m_exteriorRadius = 20f;

	public bool m_noBuild = true;

	public bool m_clearArea = true;

	[Header("Other")]
	public bool m_applyRandomDamage;

	[Header("Interior")]
	public bool m_hasInterior;

	public float m_interiorRadius = 20f;

	public string m_interiorEnvironment = "";

	public GameObject m_interiorPrefab;

	private static List<Location> m_allLocations = new List<Location>();

	private void Awake()
	{
		m_allLocations.Add(this);
		if (m_hasInterior)
		{
			Vector3 zoneCenter = GetZoneCenter();
			GameObject obj = Object.Instantiate(position: new Vector3(zoneCenter.x, base.transform.position.y + 5000f, zoneCenter.z), original: m_interiorPrefab, rotation: Quaternion.identity, parent: base.transform);
			obj.transform.localScale = new Vector3(ZoneSystem.instance.m_zoneSize, 500f, ZoneSystem.instance.m_zoneSize);
			obj.GetComponent<EnvZone>().m_environment = m_interiorEnvironment;
		}
	}

	private Vector3 GetZoneCenter()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
		return ZoneSystem.instance.GetZonePos(zone);
	}

	private void OnDestroy()
	{
		m_allLocations.Remove(this);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
		Gizmos.DrawSphere(Vector3.zero, m_exteriorRadius);
		Gizmos.matrix = Matrix4x4.identity;
		Utils.DrawGizmoCircle(base.transform.position, m_exteriorRadius, 32);
		if (m_hasInterior)
		{
			Utils.DrawGizmoCircle(base.transform.position + new Vector3(0f, 5000f, 0f), m_interiorRadius, 32);
			Utils.DrawGizmoCircle(base.transform.position, m_interiorRadius, 32);
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position + new Vector3(0f, 5000f, 0f), Quaternion.identity, new Vector3(1f, 0.001f, 1f));
			Gizmos.DrawSphere(Vector3.zero, m_interiorRadius);
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	public float GetMaxRadius()
	{
		if (!m_hasInterior)
		{
			return m_exteriorRadius;
		}
		return Mathf.Max(m_exteriorRadius, m_interiorRadius);
	}

	public bool IsInside(Vector3 point, float radius)
	{
		float maxRadius = GetMaxRadius();
		return Utils.DistanceXZ(base.transform.position, point) < maxRadius + radius;
	}

	public static bool IsInsideLocation(Vector3 point, float distance)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.IsInside(point, distance))
			{
				return true;
			}
		}
		return false;
	}

	public static Location GetLocation(Vector3 point)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.IsInside(point, 0f))
			{
				return allLocation;
			}
		}
		return null;
	}

	public static bool IsInsideNoBuildLocation(Vector3 point)
	{
		foreach (Location allLocation in m_allLocations)
		{
			if (allLocation.m_noBuild && allLocation.IsInside(point, 0f))
			{
				return true;
			}
		}
		return false;
	}
}
