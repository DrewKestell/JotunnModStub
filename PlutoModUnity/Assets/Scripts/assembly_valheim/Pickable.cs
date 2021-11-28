using System;
using UnityEngine;

public class Pickable : MonoBehaviour, Hoverable, Interactable
{
	public GameObject m_hideWhenPicked;

	public GameObject m_itemPrefab;

	public int m_amount = 1;

	public DropTable m_extraDrops = new DropTable();

	public string m_overrideName = "";

	public int m_respawnTimeMinutes;

	public float m_spawnOffset = 0.5f;

	public EffectList m_pickEffector = new EffectList();

	public bool m_pickEffectAtSpawnPoint;

	public bool m_useInteractAnimation;

	public bool m_tarPreventsPicking;

	private ZNetView m_nview;

	private Floating m_floating;

	private bool m_picked;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null)
		{
			m_nview.Register<bool>("SetPicked", RPC_SetPicked);
			m_nview.Register("Pick", RPC_Pick);
			m_picked = zDO.GetBool("picked");
			if (m_picked && (bool)m_hideWhenPicked)
			{
				m_hideWhenPicked.SetActive(value: false);
			}
			if (m_respawnTimeMinutes > 0)
			{
				InvokeRepeating("UpdateRespawn", UnityEngine.Random.Range(1f, 5f), 60f);
			}
		}
	}

	public string GetHoverText()
	{
		if (m_picked)
		{
			return "";
		}
		return Localization.instance.Localize(GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		if (!string.IsNullOrEmpty(m_overrideName))
		{
			return m_overrideName;
		}
		return m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
	}

	private void UpdateRespawn()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && m_picked)
		{
			long @long = m_nview.GetZDO().GetLong("picked_time", 0L);
			DateTime dateTime = new DateTime(@long);
			if ((ZNet.instance.GetTime() - dateTime).TotalMinutes > (double)m_respawnTimeMinutes)
			{
				m_nview.InvokeRPC(ZNetView.Everybody, "SetPicked", false);
			}
		}
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_tarPreventsPicking)
		{
			if (m_floating == null)
			{
				m_floating = GetComponent<Floating>();
			}
			if ((bool)m_floating && m_floating.IsInTar())
			{
				character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar");
				return m_useInteractAnimation;
			}
		}
		m_nview.InvokeRPC("Pick");
		return m_useInteractAnimation;
	}

	private void RPC_Pick(long sender)
	{
		if (!m_nview.IsOwner() || m_picked)
		{
			return;
		}
		Vector3 basePos = (m_pickEffectAtSpawnPoint ? (base.transform.position + Vector3.up * m_spawnOffset) : base.transform.position);
		m_pickEffector.Create(basePos, Quaternion.identity);
		int num = 0;
		for (int i = 0; i < m_amount; i++)
		{
			Drop(m_itemPrefab, num++, 1);
		}
		if (!m_extraDrops.IsEmpty())
		{
			foreach (ItemDrop.ItemData dropListItem in m_extraDrops.GetDropListItems())
			{
				Drop(dropListItem.m_dropPrefab, num++, dropListItem.m_stack);
			}
		}
		m_nview.InvokeRPC(ZNetView.Everybody, "SetPicked", true);
	}

	private void RPC_SetPicked(long sender, bool picked)
	{
		SetPicked(picked);
	}

	private void SetPicked(bool picked)
	{
		m_picked = picked;
		if ((bool)m_hideWhenPicked)
		{
			m_hideWhenPicked.SetActive(!picked);
		}
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (m_respawnTimeMinutes > 0 || m_hideWhenPicked != null)
		{
			m_nview.GetZDO().Set("picked", m_picked);
			if (picked && m_respawnTimeMinutes > 0)
			{
				DateTime time = ZNet.instance.GetTime();
				m_nview.GetZDO().Set("picked_time", time.Ticks);
			}
		}
		else if (picked)
		{
			m_nview.Destroy();
		}
	}

	private void Drop(GameObject prefab, int offset, int stack)
	{
		Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.2f;
		Vector3 position = base.transform.position + Vector3.up * m_spawnOffset + new Vector3(vector.x, 0.5f * (float)offset, vector.y);
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		GameObject obj = UnityEngine.Object.Instantiate(prefab, position, rotation);
		obj.GetComponent<ItemDrop>().SetStack(stack);
		obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
