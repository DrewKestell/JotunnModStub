using System.Collections.Generic;
using UnityEngine;

public class Corpse : MonoBehaviour
{
	private static float m_updateDt = 2f;

	public float m_emptyDespawnDelaySec = 10f;

	public float m_DespawnDelayMin = 20f;

	private float m_emptyTimer;

	private Container m_container;

	private ZNetView m_nview;

	private SkinnedMeshRenderer m_model;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_container = GetComponent<Container>();
		m_model = GetComponentInChildren<SkinnedMeshRenderer>();
		if (m_nview.IsOwner() && m_nview.GetZDO().GetLong("timeOfDeath", 0L) == 0L)
		{
			m_nview.GetZDO().Set("timeOfDeath", ZNet.instance.GetTime().Ticks);
		}
		InvokeRepeating("UpdateDespawn", m_updateDt, m_updateDt);
	}

	public void SetEquipedItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData item in items)
		{
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
			{
				m_nview.GetZDO().Set("ChestItem", item.m_shared.m_name);
			}
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
			{
				m_nview.GetZDO().Set("LegItem", item.m_shared.m_name);
			}
		}
	}

	private void UpdateDespawn()
	{
		if (!m_nview.IsOwner() || m_container.IsInUse())
		{
			return;
		}
		if (m_container.GetInventory().NrOfItems() <= 0)
		{
			m_emptyTimer += m_updateDt;
			if (m_emptyTimer >= m_emptyDespawnDelaySec)
			{
				ZLog.Log("Despawning looted corpse");
				m_nview.Destroy();
			}
		}
		else
		{
			m_emptyTimer = 0f;
		}
	}
}
