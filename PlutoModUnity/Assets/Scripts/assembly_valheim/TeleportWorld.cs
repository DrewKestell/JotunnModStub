using UnityEngine;

public class TeleportWorld : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	public float m_activationRange = 5f;

	public float m_exitDistance = 1f;

	public Transform m_proximityRoot;

	[ColorUsage(true, true)]
	public Color m_colorUnconnected = Color.white;

	[ColorUsage(true, true)]
	public Color m_colorTargetfound = Color.white;

	public EffectFade m_target_found;

	public MeshRenderer m_model;

	public EffectList m_connected;

	private ZNetView m_nview;

	private bool m_hadTarget;

	private float m_colorAlpha;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		m_hadTarget = HaveTarget();
		m_nview.Register<string>("SetTag", RPC_SetTag);
		InvokeRepeating("UpdatePortal", 0.5f, 0.5f);
	}

	public string GetHoverText()
	{
		string text = GetText();
		string text2 = (HaveTarget() ? "$piece_portal_connected" : "$piece_portal_unconnected");
		return Localization.instance.Localize("$piece_portal $piece_portal_tag:\"" + text + "\"  [" + text2 + "]\n[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag");
	}

	public string GetHoverName()
	{
		return "Teleport";
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			human.Message(MessageHud.MessageType.Center, "$piece_noaccess");
			return true;
		}
		TextInput.instance.RequestText(this, "$piece_portal_tag", 10);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void UpdatePortal()
	{
		if (m_nview.IsValid())
		{
			Player closestPlayer = Player.GetClosestPlayer(m_proximityRoot.position, m_activationRange);
			bool flag = HaveTarget();
			if (flag && !m_hadTarget)
			{
				m_connected.Create(base.transform.position, base.transform.rotation);
			}
			m_hadTarget = flag;
			m_target_found.SetActive((bool)closestPlayer && closestPlayer.IsTeleportable() && TargetFound());
		}
	}

	private void Update()
	{
		m_colorAlpha = Mathf.MoveTowards(m_colorAlpha, m_hadTarget ? 1f : 0f, Time.deltaTime);
		m_model.material.SetColor("_EmissionColor", Color.Lerp(m_colorUnconnected, m_colorTargetfound, m_colorAlpha));
	}

	public void Teleport(Player player)
	{
		if (!TargetFound())
		{
			return;
		}
		if (!player.IsTeleportable())
		{
			player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
			return;
		}
		ZLog.Log("Teleporting " + player.GetPlayerName());
		ZDOID zDOID = m_nview.GetZDO().GetZDOID("target");
		if (!(zDOID == ZDOID.None))
		{
			ZDO zDO = ZDOMan.instance.GetZDO(zDOID);
			Vector3 position = zDO.GetPosition();
			Quaternion rotation = zDO.GetRotation();
			Vector3 vector = rotation * Vector3.forward;
			Vector3 pos = position + vector * m_exitDistance + Vector3.up;
			player.TeleportTo(pos, rotation, distantTeleport: true);
		}
	}

	public string GetText()
	{
		return m_nview.GetZDO().GetString("tag");
	}

	public void SetText(string text)
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("SetTag", text);
		}
	}

	private void RPC_SetTag(long sender, string tag)
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !(GetText() == tag))
		{
			m_nview.GetZDO().Set("tag", tag);
		}
	}

	private bool HaveTarget()
	{
		return m_nview.GetZDO().GetZDOID("target") != ZDOID.None;
	}

	private bool TargetFound()
	{
		ZDOID zDOID = m_nview.GetZDO().GetZDOID("target");
		if (zDOID == ZDOID.None)
		{
			return false;
		}
		if (ZDOMan.instance.GetZDO(zDOID) == null)
		{
			ZDOMan.instance.RequestZDO(zDOID);
			return false;
		}
		return true;
	}
}
