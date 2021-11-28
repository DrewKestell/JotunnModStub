using System;
using UnityEngine;
using UnityEngine.UI;

public class TombStone : MonoBehaviour, Hoverable, Interactable
{
	private static float m_updateDt = 2f;

	public string m_text = "$piece_tombstone";

	public GameObject m_floater;

	public Text m_worldText;

	public float m_spawnUpVel = 5f;

	public StatusEffect m_lootStatusEffect;

	public EffectList m_removeEffect = new EffectList();

	private Container m_container;

	private ZNetView m_nview;

	private Floating m_floating;

	private Rigidbody m_body;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_container = GetComponent<Container>();
		m_floating = GetComponent<Floating>();
		m_body = GetComponent<Rigidbody>();
		m_body.maxDepenetrationVelocity = 1f;
		m_body.solverIterations = 10;
		Container container = m_container;
		container.m_onTakeAllSuccess = (Action)Delegate.Combine(container.m_onTakeAllSuccess, new Action(OnTakeAllSuccess));
		if (m_nview.IsOwner() && m_nview.GetZDO().GetLong("timeOfDeath", 0L) == 0L)
		{
			m_nview.GetZDO().Set("timeOfDeath", ZNet.instance.GetTime().Ticks);
			m_nview.GetZDO().Set("SpawnPoint", base.transform.position);
		}
		InvokeRepeating("UpdateDespawn", m_updateDt, m_updateDt);
	}

	private void Start()
	{
		string @string = m_nview.GetZDO().GetString("ownerName");
		GetComponent<Container>().m_name = @string;
		m_worldText.text = @string;
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		string @string = m_nview.GetZDO().GetString("ownerName");
		string text = m_text + " " + @string;
		if (m_container.GetInventory().NrOfItems() == 0)
		{
			return "";
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
	}

	public string GetHoverName()
	{
		return "";
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (m_container.GetInventory().NrOfItems() == 0)
		{
			return false;
		}
		if (IsOwner())
		{
			Player player = character as Player;
			if (EasyFitInInventory(player))
			{
				ZLog.Log("Grave should fit in inventory, loot all");
				m_container.TakeAll(character);
				return true;
			}
		}
		return m_container.Interact(character, hold: false, alt: false);
	}

	private void OnTakeAllSuccess()
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			localPlayer.m_pickupEffects.Create(localPlayer.transform.position, Quaternion.identity);
			localPlayer.Message(MessageHud.MessageType.Center, "$piece_tombstone_recovered");
		}
	}

	private bool EasyFitInInventory(Player player)
	{
		int emptySlots = player.GetInventory().GetEmptySlots();
		if (m_container.GetInventory().NrOfItems() > emptySlots)
		{
			return false;
		}
		if (player.GetInventory().GetTotalWeight() + m_container.GetInventory().GetTotalWeight() > player.GetMaxCarryWeight())
		{
			return false;
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void Setup(string ownerName, long ownerUID)
	{
		m_nview.GetZDO().Set("ownerName", ownerName);
		m_nview.GetZDO().Set("owner", ownerUID);
		if ((bool)m_body)
		{
			m_body.velocity = new Vector3(0f, m_spawnUpVel, 0f);
		}
	}

	public long GetOwner()
	{
		if (!m_nview.IsValid())
		{
			return 0L;
		}
		return m_nview.GetZDO().GetLong("owner", 0L);
	}

	public bool IsOwner()
	{
		long owner = GetOwner();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return owner == playerID;
	}

	private void UpdateDespawn()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (m_floater != null)
		{
			UpdateFloater();
		}
		if (m_nview.IsOwner())
		{
			PositionCheck();
			if (!m_container.IsInUse() && m_container.GetInventory().NrOfItems() <= 0)
			{
				GiveBoost();
				m_removeEffect.Create(base.transform.position, base.transform.rotation);
				m_nview.Destroy();
			}
		}
	}

	private void GiveBoost()
	{
		if (!(m_lootStatusEffect == null))
		{
			Player player = FindOwner();
			if ((bool)player)
			{
				player.GetSEMan().AddStatusEffect(m_lootStatusEffect.name, resetTime: true);
			}
		}
	}

	private Player FindOwner()
	{
		long owner = GetOwner();
		if (owner == 0L)
		{
			return null;
		}
		return Player.GetPlayer(owner);
	}

	private void PositionCheck()
	{
		Vector3 vec = m_nview.GetZDO().GetVec3("SpawnPoint", base.transform.position);
		if (Utils.DistanceXZ(vec, base.transform.position) > 4f)
		{
			ZLog.Log("Tombstone moved too far from spawn position, reseting position");
			base.transform.position = vec;
			m_body.position = vec;
			m_body.velocity = Vector3.zero;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y < groundHeight - 1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			m_body.position = position;
			m_body.velocity = Vector3.zero;
		}
	}

	private void UpdateFloater()
	{
		if (m_nview.IsOwner())
		{
			bool flag = m_floating.BeenFloating();
			m_nview.GetZDO().Set("inWater", flag);
			m_floater.SetActive(flag);
		}
		else
		{
			bool @bool = m_nview.GetZDO().GetBool("inWater");
			m_floater.SetActive(@bool);
		}
	}
}
