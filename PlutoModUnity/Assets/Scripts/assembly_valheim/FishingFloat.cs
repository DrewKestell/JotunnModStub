using System.Collections.Generic;
using UnityEngine;

public class FishingFloat : MonoBehaviour, IProjectile
{
	public float m_maxDistance = 30f;

	public float m_moveForce = 10f;

	public float m_pullLineSpeed = 1f;

	public float m_pullStaminaUse = 10f;

	public float m_hookedStaminaPerSec = 1f;

	public float m_breakDistance = 4f;

	public float m_range = 10f;

	public float m_nibbleForce = 10f;

	public EffectList m_nibbleEffect = new EffectList();

	public EffectList m_lineBreakEffect = new EffectList();

	public float m_maxLineSlack = 0.3f;

	public LineConnect m_rodLine;

	public LineConnect m_hookLine;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Floating m_floating;

	private float m_lineLength;

	private float m_msgTime;

	private Fish m_nibbler;

	private float m_nibbleTime;

	private static List<FishingFloat> m_allInstances = new List<FishingFloat>();

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_floating = GetComponent<Floating>();
		m_nview.Register<ZDOID>("Nibble", RPC_Nibble);
		m_allInstances.Add(this);
	}

	private void OnDestroy()
	{
		m_allInstances.Remove(this);
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		FishingFloat fishingFloat = FindFloat(owner);
		if ((bool)fishingFloat)
		{
			ZNetScene.instance.Destroy(fishingFloat.gameObject);
		}
		ZDOID zDOID = owner.GetZDOID();
		m_nview.GetZDO().Set("RodOwner", zDOID);
		Transform rodTop = GetRodTop(owner);
		if (rodTop == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return;
		}
		m_rodLine.SetPeer(owner.GetZDOID());
		m_lineLength = Vector3.Distance(rodTop.position, base.transform.position);
		owner.Message(MessageHud.MessageType.Center, m_lineLength.ToString("0m"));
	}

	public Character GetOwner()
	{
		if (!m_nview.IsValid())
		{
			return null;
		}
		ZDOID zDOID = m_nview.GetZDO().GetZDOID("RodOwner");
		GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	private Transform GetRodTop(Character owner)
	{
		Transform transform = Utils.FindChild(owner.transform, "_RodTop");
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return null;
		}
		return transform;
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		Character owner = GetOwner();
		if (!owner)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			m_nview.Destroy();
			return;
		}
		Transform rodTop = GetRodTop(owner);
		if (!rodTop)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			m_nview.Destroy();
			return;
		}
		if (owner.InAttack() || owner.IsHoldingAttack())
		{
			m_nview.Destroy();
			return;
		}
		float magnitude = (rodTop.transform.position - base.transform.position).magnitude;
		Fish fish = GetCatch();
		if (!owner.HaveStamina() && fish != null)
		{
			SetCatch(null);
			fish = null;
			Message("$msg_fishing_lost", prioritized: true);
		}
		if ((bool)fish)
		{
			owner.UseStamina(m_hookedStaminaPerSec * fixedDeltaTime);
		}
		if (!fish && Utils.LengthXZ(m_body.velocity) > 2f)
		{
			TryToHook();
		}
		if (owner.IsBlocking() && owner.HaveStamina())
		{
			float num = m_pullStaminaUse;
			if (fish != null)
			{
				num += fish.m_staminaUse;
			}
			owner.UseStamina(num * fixedDeltaTime);
			if (m_lineLength > magnitude - 0.2f)
			{
				float lineLength = m_lineLength;
				m_lineLength -= fixedDeltaTime * m_pullLineSpeed;
				TryToHook();
				if ((int)m_lineLength != (int)lineLength)
				{
					Message(m_lineLength.ToString("0m"));
				}
			}
			if (m_lineLength <= 0.5f)
			{
				if ((bool)fish)
				{
					if (fish.Pickup(owner as Humanoid))
					{
						Message("$msg_fishing_catched " + fish.GetHoverName(), prioritized: true);
						SetCatch(null);
					}
				}
				else
				{
					m_nview.Destroy();
				}
				return;
			}
		}
		m_rodLine.SetSlack((1f - Utils.LerpStep(m_lineLength / 2f, m_lineLength, magnitude)) * m_maxLineSlack);
		if (magnitude - m_lineLength > m_breakDistance || magnitude > m_maxDistance)
		{
			Message("$msg_fishing_linebroke", prioritized: true);
			m_nview.Destroy();
			m_lineBreakEffect.Create(base.transform.position, Quaternion.identity);
			return;
		}
		if ((bool)fish)
		{
			Utils.Pull(m_body, fish.transform.position, 0.5f, m_moveForce, 0.5f, 0.3f);
		}
		Utils.Pull(m_body, rodTop.transform.position, m_lineLength, m_moveForce, 1f, 0.3f);
	}

	private void TryToHook()
	{
		if (m_nibbler != null && Time.time - m_nibbleTime < 0.5f && GetCatch() == null)
		{
			Message("$msg_fishing_hooked", prioritized: true);
			SetCatch(m_nibbler);
			m_nibbler = null;
		}
	}

	private void SetCatch(Fish fish)
	{
		if ((bool)fish)
		{
			m_nview.GetZDO().Set("CatchID", fish.GetZDOID());
			m_hookLine.SetPeer(fish.GetZDOID());
		}
		else
		{
			m_nview.GetZDO().Set("CatchID", ZDOID.None);
			m_hookLine.SetPeer(ZDOID.None);
		}
	}

	public Fish GetCatch()
	{
		if (!m_nview.IsValid())
		{
			return null;
		}
		ZDOID zDOID = m_nview.GetZDO().GetZDOID("CatchID");
		if (!zDOID.IsNone())
		{
			GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
			if ((bool)gameObject)
			{
				return gameObject.GetComponent<Fish>();
			}
		}
		return null;
	}

	public bool IsInWater()
	{
		return m_floating.HaveLiquidLevel();
	}

	public void Nibble(Fish fish)
	{
		m_nview.InvokeRPC("Nibble", fish.GetZDOID());
	}

	public void RPC_Nibble(long sender, ZDOID fishID)
	{
		if (!(Time.time - m_nibbleTime < 1f) && !(GetCatch() != null))
		{
			m_nibbleEffect.Create(base.transform.position, Quaternion.identity, base.transform);
			m_body.AddForce(Vector3.down * m_nibbleForce, ForceMode.VelocityChange);
			GameObject gameObject = ZNetScene.instance.FindInstance(fishID);
			if ((bool)gameObject)
			{
				m_nibbler = gameObject.GetComponent<Fish>();
				m_nibbleTime = Time.time;
			}
		}
	}

	public static List<FishingFloat> GetAllInstances()
	{
		return m_allInstances;
	}

	private static FishingFloat FindFloat(Character owner)
	{
		foreach (FishingFloat allInstance in m_allInstances)
		{
			if (owner == allInstance.GetOwner())
			{
				return allInstance;
			}
		}
		return null;
	}

	public static FishingFloat FindFloat(Fish fish)
	{
		foreach (FishingFloat allInstance in m_allInstances)
		{
			if (allInstance.GetCatch() == fish)
			{
				return allInstance;
			}
		}
		return null;
	}

	private void Message(string msg, bool prioritized = false)
	{
		if (prioritized || !(Time.time - m_msgTime < 1f))
		{
			m_msgTime = Time.time;
			Character owner = GetOwner();
			if ((bool)owner)
			{
				owner.Message(MessageHud.MessageType.Center, Localization.instance.Localize(msg));
			}
		}
	}
}
