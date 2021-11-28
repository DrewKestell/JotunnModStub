using System.Collections.Generic;
using UnityEngine;

public class LiquidSurface : MonoBehaviour
{
	private LiquidVolume m_liquid;

	private List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	private void Awake()
	{
		m_liquid = GetComponentInParent<LiquidVolume>();
	}

	private void FixedUpdate()
	{
		UpdateFloaters();
	}

	public LiquidType GetLiquidType()
	{
		return m_liquid.m_liquidType;
	}

	public float GetSurface(Vector3 p)
	{
		return m_liquid.GetSurface(p);
	}

	private void OnTriggerEnter(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null && !m_inWater.Contains(component))
		{
			m_inWater.Add(component);
		}
	}

	private void UpdateFloaters()
	{
		if (m_inWater.Count == 0)
		{
			return;
		}
		IWaterInteractable waterInteractable = null;
		foreach (IWaterInteractable item in m_inWater)
		{
			Transform transform = item.GetTransform();
			if ((bool)transform)
			{
				float surface = m_liquid.GetSurface(transform.position);
				item.SetLiquidLevel(surface, m_liquid.m_liquidType);
			}
			else
			{
				waterInteractable = item;
			}
		}
		if (waterInteractable != null)
		{
			m_inWater.Remove(waterInteractable);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.SetLiquidLevel(-10000f, m_liquid.m_liquidType);
			m_inWater.Remove(component);
		}
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable item in m_inWater)
		{
			item?.SetLiquidLevel(-10000f, m_liquid.m_liquidType);
		}
		m_inWater.Clear();
	}
}
