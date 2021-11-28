using System.Collections.Generic;
using UnityEngine;

public class OfferingBowl : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "Ancient bowl";

	public string m_useItemText = "Burn item";

	public ItemDrop m_bossItem;

	public int m_bossItems = 1;

	public GameObject m_bossPrefab;

	public ItemDrop m_itemPrefab;

	public Transform m_itemSpawnPoint;

	public string m_setGlobalKey = "";

	[Header("Boss")]
	public float m_spawnBossDelay = 5f;

	public float m_spawnBossMaxDistance = 40f;

	public float m_spawnBossMaxYDistance = 9999f;

	public float m_spawnOffset = 1f;

	[Header("Use itemstands")]
	public bool m_useItemStands;

	public string m_itemStandPrefix = "";

	public float m_itemstandMaxRange = 20f;

	[Header("Effects")]
	public EffectList m_fuelAddedEffects = new EffectList();

	public EffectList m_spawnBossStartEffects = new EffectList();

	public EffectList m_spawnBossDoneffects = new EffectList();

	private Vector3 m_bossSpawnPoint;

	private void Awake()
	{
	}

	public string GetHoverText()
	{
		if (m_useItemStands)
		{
			return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] ") + Localization.instance.Localize(m_useItemText);
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>1-8</b></color>] " + m_useItemText);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (IsBossSpawnQueued())
		{
			return false;
		}
		if (m_useItemStands)
		{
			List<ItemStand> list = FindItemStands();
			foreach (ItemStand item in list)
			{
				if (!item.HaveAttachment())
				{
					user.Message(MessageHud.MessageType.Center, "$msg_incompleteoffering");
					return false;
				}
			}
			if (SpawnBoss(base.transform.position))
			{
				user.Message(MessageHud.MessageType.Center, "$msg_offerdone");
				foreach (ItemStand item2 in list)
				{
					item2.DestroyAttachment();
				}
				if ((bool)m_itemSpawnPoint)
				{
					m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
				}
			}
			return true;
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_useItemStands)
		{
			return false;
		}
		if (IsBossSpawnQueued())
		{
			return true;
		}
		if (m_bossItem != null)
		{
			if (item.m_shared.m_name == m_bossItem.m_itemData.m_shared.m_name)
			{
				int num = user.GetInventory().CountItems(m_bossItem.m_itemData.m_shared.m_name);
				if (num < m_bossItems)
				{
					user.Message(MessageHud.MessageType.Center, "$msg_incompleteoffering: " + m_bossItem.m_itemData.m_shared.m_name + " " + num + " / " + m_bossItems);
					return true;
				}
				if (m_bossPrefab != null)
				{
					if (SpawnBoss(base.transform.position))
					{
						user.GetInventory().RemoveItem(item.m_shared.m_name, m_bossItems);
						user.ShowRemovedMessage(m_bossItem.m_itemData, m_bossItems);
						user.Message(MessageHud.MessageType.Center, "$msg_offerdone");
						if ((bool)m_itemSpawnPoint)
						{
							m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
						}
					}
				}
				else if (m_itemPrefab != null && SpawnItem(m_itemPrefab, user as Player))
				{
					user.GetInventory().RemoveItem(item.m_shared.m_name, m_bossItems);
					user.ShowRemovedMessage(m_bossItem.m_itemData, m_bossItems);
					user.Message(MessageHud.MessageType.Center, "$msg_offerdone");
					m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
				}
				if (!string.IsNullOrEmpty(m_setGlobalKey))
				{
					ZoneSystem.instance.SetGlobalKey(m_setGlobalKey);
				}
				return true;
			}
			user.Message(MessageHud.MessageType.Center, "$msg_offerwrong");
			return true;
		}
		return false;
	}

	private bool SpawnItem(ItemDrop item, Player player)
	{
		if (item.m_itemData.m_shared.m_questItem && player.HaveUniqueKey(item.m_itemData.m_shared.m_name))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_cantoffer");
			return false;
		}
		Object.Instantiate(item, m_itemSpawnPoint.position, Quaternion.identity);
		return true;
	}

	private bool SpawnBoss(Vector3 point)
	{
		for (int i = 0; i < 100; i++)
		{
			Vector2 vector = Random.insideUnitCircle * m_spawnBossMaxDistance;
			Vector3 vector2 = point + new Vector3(vector.x, 0f, vector.y);
			float solidHeight = ZoneSystem.instance.GetSolidHeight(vector2);
			if (!(solidHeight < 0f) && !(Mathf.Abs(solidHeight - base.transform.position.y) > m_spawnBossMaxYDistance))
			{
				vector2.y = solidHeight + m_spawnOffset;
				m_spawnBossStartEffects.Create(vector2, Quaternion.identity);
				m_bossSpawnPoint = vector2;
				Invoke("DelayedSpawnBoss", m_spawnBossDelay);
				return true;
			}
		}
		return false;
	}

	private bool IsBossSpawnQueued()
	{
		return IsInvoking("DelayedSpawnBoss");
	}

	private void DelayedSpawnBoss()
	{
		BaseAI component = Object.Instantiate(m_bossPrefab, m_bossSpawnPoint, Quaternion.identity).GetComponent<BaseAI>();
		if (component != null)
		{
			component.SetPatrolPoint();
		}
		m_spawnBossDoneffects.Create(m_bossSpawnPoint, Quaternion.identity);
	}

	private List<ItemStand> FindItemStands()
	{
		List<ItemStand> list = new List<ItemStand>();
		ItemStand[] array = Object.FindObjectsOfType<ItemStand>();
		foreach (ItemStand itemStand in array)
		{
			if (!(Vector3.Distance(base.transform.position, itemStand.transform.position) > m_itemstandMaxRange) && itemStand.gameObject.name.StartsWith(m_itemStandPrefix))
			{
				list.Add(itemStand);
			}
		}
		return list;
	}
}
