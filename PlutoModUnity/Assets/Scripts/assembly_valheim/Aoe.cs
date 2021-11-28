using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Aoe : MonoBehaviour, IProjectile
{
	[Header("Attack (overridden by item )")]
	public bool m_useAttackSettings = true;

	public HitData.DamageTypes m_damage;

	public bool m_dodgeable;

	public bool m_blockable;

	public int m_toolTier;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	[Header("Attack (other)")]
	public HitData.DamageTypes m_damagePerLevel;

	public bool m_attackForceForward;

	[Header("Damage self")]
	public float m_damageSelf;

	[Header("Ignore targets")]
	public bool m_hitOwner;

	public bool m_hitParent = true;

	public bool m_hitSame;

	public bool m_hitFriendly = true;

	public bool m_hitEnemy = true;

	public bool m_hitCharacters = true;

	public bool m_hitProps = true;

	[Header("Other")]
	public Skills.SkillType m_skill;

	public bool m_useTriggers;

	public bool m_triggerEnterOnly;

	public BoxCollider m_useCollider;

	public float m_radius = 4f;

	public float m_ttl = 4f;

	public float m_hitInterval = 1f;

	public EffectList m_hitEffects = new EffectList();

	public bool m_attachToCaster;

	private ZNetView m_nview;

	private Character m_owner;

	private List<GameObject> m_hitList = new List<GameObject>();

	private float m_hitTimer;

	private Vector3 m_offset = Vector3.zero;

	private Quaternion m_localRot = Quaternion.identity;

	private int m_level;

	private int m_rayMask;

	private bool m_gaveSkill;

	private void Awake()
	{
		m_nview = GetComponentInParent<ZNetView>();
		m_rayMask = 0;
		if (m_hitCharacters)
		{
			m_rayMask |= LayerMask.GetMask("character", "character_net", "character_ghost");
		}
		if (m_hitProps)
		{
			m_rayMask |= LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "hitbox", "character_noenv", "vehicle");
		}
	}

	public HitData.DamageTypes GetDamage()
	{
		return GetDamage(m_level);
	}

	public HitData.DamageTypes GetDamage(int itemQuality)
	{
		if (itemQuality <= 1)
		{
			return m_damage;
		}
		HitData.DamageTypes damage = m_damage;
		damage.Add(m_damagePerLevel, itemQuality - 1);
		return damage;
	}

	public string GetTooltipString(int itemQuality)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("AOE");
		stringBuilder.Append(GetDamage(itemQuality).GetTooltipString());
		stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", m_attackForce);
		stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", m_backstabBonus);
		return stringBuilder.ToString();
	}

	private void Start()
	{
		if ((!(m_nview != null) || (m_nview.IsValid() && m_nview.IsOwner())) && !m_useTriggers && m_hitInterval <= 0f)
		{
			CheckHits();
		}
	}

	private void FixedUpdate()
	{
		if (m_nview != null && (!m_nview.IsValid() || !m_nview.IsOwner()))
		{
			return;
		}
		if (m_hitInterval > 0f)
		{
			m_hitTimer -= Time.fixedDeltaTime;
			if (m_hitTimer <= 0f)
			{
				m_hitTimer = m_hitInterval;
				if (m_useTriggers)
				{
					m_hitList.Clear();
				}
				else
				{
					CheckHits();
				}
			}
		}
		if (m_owner != null && m_attachToCaster)
		{
			base.transform.position = m_owner.transform.TransformPoint(m_offset);
			base.transform.rotation = m_owner.transform.rotation * m_localRot;
		}
		if (m_ttl > 0f)
		{
			m_ttl -= Time.fixedDeltaTime;
			if (m_ttl <= 0f && (bool)ZNetScene.instance)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	private void CheckHits()
	{
		m_hitList.Clear();
		Collider[] array = ((m_useCollider != null) ? Physics.OverlapBox(base.transform.position + m_useCollider.center, m_useCollider.size / 2f, base.transform.rotation, m_rayMask) : Physics.OverlapSphere(base.transform.position, m_radius, m_rayMask));
		foreach (Collider collider in array)
		{
			OnHit(collider, collider.transform.position);
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		m_owner = owner;
		if (item != null)
		{
			m_level = item.m_quality;
		}
		if (m_attachToCaster && owner != null)
		{
			m_offset = owner.transform.InverseTransformPoint(base.transform.position);
			m_localRot = Quaternion.Inverse(owner.transform.rotation) * base.transform.rotation;
		}
		if (hitData != null && m_useAttackSettings)
		{
			m_damage = hitData.m_damage;
			m_blockable = hitData.m_blockable;
			m_dodgeable = hitData.m_dodgeable;
			m_attackForce = hitData.m_pushForce;
			m_backstabBonus = hitData.m_backstabBonus;
			m_statusEffect = hitData.m_statusEffect;
			m_toolTier = hitData.m_toolTier;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (m_triggerEnterOnly)
		{
			if (!m_useTriggers)
			{
				ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			}
			else if (!(m_nview != null) || (m_nview.IsValid() && m_nview.IsOwner()))
			{
				OnHit(collision.collider, collision.collider.transform.position);
			}
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!m_triggerEnterOnly)
		{
			if (!m_useTriggers)
			{
				ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			}
			else if (!(m_nview != null) || (m_nview.IsValid() && m_nview.IsOwner()))
			{
				OnHit(collision.collider, collision.collider.transform.position);
			}
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (m_triggerEnterOnly)
		{
			if (!m_useTriggers)
			{
				ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			}
			else if (!(m_nview != null) || (m_nview.IsValid() && m_nview.IsOwner()))
			{
				OnHit(collider, collider.transform.position);
			}
		}
	}

	private void OnTriggerStay(Collider collider)
	{
		if (!m_triggerEnterOnly)
		{
			if (!m_useTriggers)
			{
				ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			}
			else if (!(m_nview != null) || (m_nview.IsValid() && m_nview.IsOwner()))
			{
				OnHit(collider, collider.transform.position);
			}
		}
	}

	private bool OnHit(Collider collider, Vector3 hitPoint)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (m_hitList.Contains(gameObject))
		{
			return false;
		}
		m_hitList.Add(gameObject);
		float num = 1f;
		if ((bool)m_owner && m_owner.IsPlayer() && m_skill != 0)
		{
			num = m_owner.GetRandomSkillFactor(m_skill);
		}
		bool result = false;
		bool flag = false;
		IDestructible component = gameObject.GetComponent<IDestructible>();
		if (component != null)
		{
			if (!m_hitParent && base.gameObject.transform.parent != null && gameObject == base.gameObject.transform.parent.gameObject)
			{
				return false;
			}
			Character character = component as Character;
			if ((bool)character)
			{
				if (m_nview == null && !character.IsOwner())
				{
					return false;
				}
				if (m_owner != null)
				{
					if (!m_hitOwner && character == m_owner)
					{
						return false;
					}
					if (!m_hitSame && character.m_name == m_owner.m_name)
					{
						return false;
					}
					bool flag2 = BaseAI.IsEnemy(m_owner, character);
					if (!m_hitFriendly && !flag2)
					{
						return false;
					}
					if (!m_hitEnemy && flag2)
					{
						return false;
					}
				}
				if (!m_hitCharacters)
				{
					return false;
				}
				if (m_dodgeable && character.IsDodgeInvincible())
				{
					return false;
				}
				flag = true;
			}
			else if (!m_hitProps)
			{
				return false;
			}
			Vector3 dir = (m_attackForceForward ? base.transform.forward : (hitPoint - base.transform.position).normalized);
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = GetDamage();
			hitData.m_pushForce = m_attackForce * num;
			hitData.m_backstabBonus = m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = dir;
			hitData.m_statusEffect = m_statusEffect;
			hitData.m_dodgeable = m_dodgeable;
			hitData.m_blockable = m_blockable;
			hitData.m_ranged = true;
			hitData.m_toolTier = m_toolTier;
			hitData.SetAttacker(m_owner);
			hitData.m_damage.Modify(num);
			component.Damage(hitData);
			if (m_damageSelf > 0f)
			{
				IDestructible componentInParent = GetComponentInParent<IDestructible>();
				if (componentInParent != null)
				{
					HitData hitData2 = new HitData();
					hitData2.m_damage.m_damage = m_damageSelf;
					hitData2.m_point = hitPoint;
					hitData2.m_blockable = false;
					hitData2.m_dodgeable = false;
					componentInParent.Damage(hitData2);
				}
			}
			result = true;
		}
		m_hitEffects.Create(hitPoint, Quaternion.identity);
		if (!m_gaveSkill && (bool)m_owner && m_owner.IsPlayer() && m_skill != Skills.SkillType.None && flag)
		{
			m_owner.RaiseSkill(m_skill);
			m_gaveSkill = true;
		}
		return result;
	}

	private void OnDrawGizmos()
	{
		_ = m_useTriggers;
	}
}
