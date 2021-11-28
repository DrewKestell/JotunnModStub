using UnityEngine;

public class Fish : MonoBehaviour, IWaterInteractable, Hoverable, Interactable
{
	public string m_name = "Fish";

	public float m_swimRange = 20f;

	public float m_minDepth = 1f;

	public float m_maxDepth = 4f;

	public float m_speed = 10f;

	public float m_acceleration = 5f;

	public float m_turnRate = 10f;

	public float m_wpDurationMin = 4f;

	public float m_wpDurationMax = 4f;

	public float m_avoidSpeedScale = 2f;

	public float m_avoidRange = 5f;

	public float m_height = 0.2f;

	public float m_eatDuration = 4f;

	public float m_hookForce = 4f;

	public float m_staminaUse = 1f;

	public float m_baseHookChance = 0.5f;

	public GameObject m_pickupItem;

	public int m_pickupItemStackSize = 1;

	private Vector3 m_spawnPoint;

	private Vector3 m_waypoint;

	private FishingFloat m_waypointFF;

	private bool m_haveWaypoint;

	private float m_swimTimer;

	private float m_lastNibbleTime;

	private float m_inWater = -10000f;

	private float m_pickupTime;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_spawnPoint = m_nview.GetZDO().GetVec3("spawnpoint", base.transform.position);
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set("spawnpoint", m_spawnPoint);
		}
		if (m_nview.IsOwner())
		{
			RandomizeWaypoint(canHook: true);
		}
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_nview.Register("RequestPickup", RPC_RequestPickup);
			m_nview.Register("Pickup", RPC_Pickup);
		}
	}

	public bool IsOwner()
	{
		if ((bool)m_nview && m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	public string GetHoverText()
	{
		string text = m_name;
		if (IsOutOfWater())
		{
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup";
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!IsOutOfWater())
		{
			return false;
		}
		if (Pickup(character))
		{
			return true;
		}
		return false;
	}

	public bool Pickup(Humanoid character)
	{
		if (!character.GetInventory().CanAddItem(m_pickupItem, m_pickupItemStackSize))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_noroom");
			return false;
		}
		m_nview.InvokeRPC("RequestPickup");
		return true;
	}

	private void RPC_RequestPickup(long uid)
	{
		if (Time.time - m_pickupTime > 2f)
		{
			m_pickupTime = Time.time;
			m_nview.InvokeRPC(uid, "Pickup");
		}
	}

	private void RPC_Pickup(long uid)
	{
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.PickupPrefab(m_pickupItem, m_pickupItemStackSize) != null)
		{
			m_nview.ClaimOwnership();
			m_nview.Destroy();
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetLiquidLevel(float level, LiquidType type)
	{
		if (type == LiquidType.Water)
		{
			m_inWater = level;
		}
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	private bool IsOutOfWater()
	{
		return m_inWater < base.transform.position.y - m_height;
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (!m_nview.IsOwner())
		{
			return;
		}
		FishingFloat fishingFloat = FishingFloat.FindFloat(this);
		if ((bool)fishingFloat)
		{
			Utils.Pull(m_body, fishingFloat.transform.position, 1f, m_hookForce, 1f, 0.5f);
		}
		if (m_inWater <= -10000f || m_inWater < base.transform.position.y + m_height)
		{
			m_body.useGravity = true;
			if (IsOutOfWater())
			{
				return;
			}
		}
		m_body.useGravity = false;
		bool flag = false;
		Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position);
		if ((bool)playerNoiseRange)
		{
			if (Vector3.Distance(base.transform.position, playerNoiseRange.transform.position) > m_avoidRange / 2f)
			{
				Vector3 normalized = (base.transform.position - playerNoiseRange.transform.position).normalized;
				SwimDirection(normalized, fast: true, avoidLand: true, fixedDeltaTime);
				return;
			}
			flag = true;
			if (m_swimTimer > 0.5f)
			{
				m_swimTimer = 0.5f;
			}
		}
		m_swimTimer -= fixedDeltaTime;
		if (m_swimTimer <= 0f)
		{
			RandomizeWaypoint(!flag);
		}
		if (m_haveWaypoint)
		{
			if ((bool)m_waypointFF)
			{
				m_waypoint = m_waypointFF.transform.position + Vector3.down;
			}
			if (Vector3.Distance(m_waypoint, base.transform.position) < 0.2f)
			{
				if (!m_waypointFF)
				{
					m_haveWaypoint = false;
					return;
				}
				if (Time.time - m_lastNibbleTime > 1f)
				{
					m_lastNibbleTime = Time.time;
					m_waypointFF.Nibble(this);
				}
			}
			Vector3 dir = Vector3.Normalize(m_waypoint - base.transform.position);
			SwimDirection(dir, flag, avoidLand: false, fixedDeltaTime);
		}
		else
		{
			Stop(fixedDeltaTime);
		}
	}

	private void Stop(float dt)
	{
		if (!(m_inWater < base.transform.position.y + m_height))
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
			Quaternion rot = Quaternion.RotateTowards(m_body.rotation, to, m_turnRate * dt);
			m_body.MoveRotation(rot);
			Vector3 force = -m_body.velocity * m_acceleration;
			m_body.AddForce(force, ForceMode.VelocityChange);
		}
	}

	private void SwimDirection(Vector3 dir, bool fast, bool avoidLand, float dt)
	{
		Vector3 forward = dir;
		forward.y = 0f;
		forward.Normalize();
		float num = m_turnRate;
		if (fast)
		{
			num *= m_avoidSpeedScale;
		}
		Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, to, num * dt);
		m_body.rotation = rotation;
		float num2 = m_speed;
		if (fast)
		{
			num2 *= m_avoidSpeedScale;
		}
		if (avoidLand && GetPointDepth(base.transform.position + base.transform.forward) < m_minDepth)
		{
			num2 = 0f;
		}
		if (fast && Vector3.Dot(dir, base.transform.forward) < 0f)
		{
			num2 = 0f;
		}
		Vector3 forward2 = base.transform.forward;
		forward2.y = dir.y;
		Vector3 vector = forward2 * num2 - m_body.velocity;
		if (m_inWater < base.transform.position.y + m_height && vector.y > 0f)
		{
			vector.y = 0f;
		}
		m_body.AddForce(vector * m_acceleration, ForceMode.VelocityChange);
	}

	private FishingFloat FindFloat()
	{
		foreach (FishingFloat allInstance in FishingFloat.GetAllInstances())
		{
			if (!(Vector3.Distance(base.transform.position, allInstance.transform.position) > allInstance.m_range) && allInstance.IsInWater() && !(allInstance.GetCatch() != null))
			{
				float baseHookChance = m_baseHookChance;
				if (Random.value < baseHookChance)
				{
					return allInstance;
				}
			}
		}
		return null;
	}

	private void RandomizeWaypoint(bool canHook)
	{
		Vector2 vector = Random.insideUnitCircle * m_swimRange;
		m_waypoint = m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		m_waypointFF = null;
		if (canHook)
		{
			FishingFloat fishingFloat = FindFloat();
			if ((bool)fishingFloat)
			{
				m_waypointFF = fishingFloat;
				m_waypoint = fishingFloat.transform.position + Vector3.down;
			}
		}
		float pointDepth = GetPointDepth(m_waypoint);
		if (!(pointDepth < m_minDepth))
		{
			Vector3 p = (m_waypoint + base.transform.position) * 0.5f;
			if (!(GetPointDepth(p) < m_minDepth))
			{
				float max = Mathf.Min(m_maxDepth, pointDepth - m_height);
				float liquidLevel = Floating.GetLiquidLevel(m_waypoint);
				m_waypoint.y = liquidLevel - Random.Range(m_minDepth, max);
				m_haveWaypoint = true;
				m_swimTimer = Random.Range(m_wpDurationMin, m_wpDurationMax);
			}
		}
	}

	private float GetPointDepth(Vector3 p)
	{
		if (ZoneSystem.instance.GetSolidHeight(p, out var height))
		{
			return ZoneSystem.instance.m_waterLevel - height;
		}
		return 0f;
	}

	private bool DangerNearby()
	{
		if (Player.GetPlayerNoiseRange(base.transform.position) != null)
		{
			return true;
		}
		return false;
	}

	public ZDOID GetZDOID()
	{
		return m_nview.GetZDO().m_uid;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_height, new Vector3(1f, 0.02f, 1f));
	}
}
