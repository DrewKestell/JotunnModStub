using System.Collections.Generic;
using UnityEngine;

public class EffectArea : MonoBehaviour
{
	public enum Type
	{
		Heat = 1,
		Fire = 2,
		PlayerBase = 4,
		Burning = 8,
		Teleport = 0x10,
		NoMonsters = 0x20,
		WarmCozyArea = 0x40,
		None = 999
	}

	[BitMask(typeof(Type))]
	public Type m_type = Type.None;

	public string m_statusEffect = "";

	private Collider m_collider;

	private static int m_characterMask = 0;

	private static List<EffectArea> m_allAreas = new List<EffectArea>();

	private static Collider[] m_tempColliders = new Collider[128];

	private void Awake()
	{
		if (m_characterMask == 0)
		{
			m_characterMask = LayerMask.GetMask("character_trigger");
		}
		m_collider = GetComponent<Collider>();
		m_allAreas.Add(this);
	}

	private void OnDestroy()
	{
		m_allAreas.Remove(this);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (ZNet.instance == null)
		{
			return;
		}
		Character component = collider.GetComponent<Character>();
		if ((bool)component && component.IsOwner())
		{
			if (!string.IsNullOrEmpty(m_statusEffect))
			{
				component.GetSEMan().AddStatusEffect(m_statusEffect, resetTime: true);
			}
			if ((m_type & Type.Heat) != 0)
			{
				component.OnNearFire(base.transform.position);
			}
		}
	}

	public float GetRadius()
	{
		SphereCollider sphereCollider = m_collider as SphereCollider;
		if (sphereCollider != null)
		{
			return sphereCollider.radius;
		}
		return m_collider.bounds.size.magnitude;
	}

	public static EffectArea IsPointInsideArea(Vector3 p, Type type, float radius = 0f)
	{
		int num = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, m_characterMask);
		for (int i = 0; i < num; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & type) != 0)
			{
				return component;
			}
		}
		return null;
	}

	public static int GetBaseValue(Vector3 p, float radius)
	{
		int num = 0;
		int num2 = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, m_characterMask);
		for (int i = 0; i < num2; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & Type.PlayerBase) != 0)
			{
				num++;
			}
		}
		return num;
	}

	public static List<EffectArea> GetAllAreas()
	{
		return m_allAreas;
	}
}
