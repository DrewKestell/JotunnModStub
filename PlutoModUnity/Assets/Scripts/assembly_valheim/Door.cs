using UnityEngine;

public class Door : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "door";

	public GameObject m_doorObject;

	public ItemDrop m_keyItem;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public EffectList m_lockedEffects = new EffectList();

	private ZNetView m_nview;

	private Animator m_animator;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_animator = GetComponent<Animator>();
			if ((bool)m_nview)
			{
				m_nview.Register<bool>("UseDoor", RPC_UseDoor);
			}
			InvokeRepeating("UpdateState", 0f, 0.2f);
		}
	}

	private void UpdateState()
	{
		if (m_nview.IsValid())
		{
			int @int = m_nview.GetZDO().GetInt("state");
			SetState(@int);
		}
	}

	private void SetState(int state)
	{
		if (m_animator.GetInteger("state") != state)
		{
			if (state != 0)
			{
				m_openEffects.Create(base.transform.position, base.transform.rotation);
			}
			else
			{
				m_closeEffects.Create(base.transform.position, base.transform.rotation);
			}
			m_animator.SetInteger("state", state);
		}
	}

	private bool CanInteract()
	{
		if (m_keyItem != null && m_nview.GetZDO().GetInt("state") != 0)
		{
			return false;
		}
		if (!m_animator.GetCurrentAnimatorStateInfo(0).IsTag("open"))
		{
			return m_animator.GetCurrentAnimatorStateInfo(0).IsTag("closed");
		}
		return true;
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		if (CanInteract())
		{
			if (m_nview.GetZDO().GetInt("state") != 0)
			{
				return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_door_close");
			}
			return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_door_open");
		}
		return Localization.instance.Localize(m_name);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!CanInteract())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (m_keyItem != null)
		{
			if (!HaveKey(character))
			{
				m_lockedEffects.Create(base.transform.position, base.transform.rotation);
				character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", m_keyItem.m_itemData.m_shared.m_name));
				return true;
			}
			character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", m_keyItem.m_itemData.m_shared.m_name));
		}
		Vector3 normalized = (character.transform.position - base.transform.position).normalized;
		bool flag = Vector3.Dot(base.transform.forward, normalized) < 0f;
		m_nview.InvokeRPC("UseDoor", flag);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private bool HaveKey(Humanoid player)
	{
		if (m_keyItem == null)
		{
			return true;
		}
		return player.GetInventory().HaveItem(m_keyItem.m_itemData.m_shared.m_name);
	}

	private void RPC_UseDoor(long uid, bool forward)
	{
		if (!CanInteract())
		{
			return;
		}
		if (m_nview.GetZDO().GetInt("state") == 0)
		{
			if (forward)
			{
				m_nview.GetZDO().Set("state", 1);
			}
			else
			{
				m_nview.GetZDO().Set("state", -1);
			}
		}
		else
		{
			m_nview.GetZDO().Set("state", 0);
		}
		UpdateState();
	}
}
