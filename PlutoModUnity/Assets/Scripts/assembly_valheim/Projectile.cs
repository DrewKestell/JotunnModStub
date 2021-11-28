using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IProjectile
{
	public HitData.DamageTypes m_damage;

	public float m_aoe;

	public bool m_dodgeable;

	public bool m_blockable;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	public bool m_canHitWater;

	public float m_ttl = 4f;

	public float m_gravity;

	public float m_rayRadius;

	public float m_hitNoise = 50f;

	public bool m_stayAfterHitStatic;

	public GameObject m_hideOnHit;

	public bool m_stopEmittersOnHit = true;

	public EffectList m_hitEffects = new EffectList();

	public EffectList m_hitWaterEffects = new EffectList();

	[Header("Spawn on hit")]
	public bool m_respawnItemOnHit;

	public GameObject m_spawnOnHit;

	[Range(0f, 1f)]
	public float m_spawnOnHitChance = 1f;

	public bool m_showBreakMessage;

	public bool m_staticHitOnly;

	public bool m_groundHitOnly;

	public Vector3 m_spawnOffset = Vector3.zero;

	public bool m_spawnRandomRotation;

	public EffectList m_spawnOnHitEffects = new EffectList();

	[Header("Rotate projectile")]
	public float m_rotateVisual;

	public GameObject m_visual;

	private ZNetView m_nview;

	private Vector3 m_vel = Vector3.zero;

	private Character m_owner;

	private Skills.SkillType m_skill;

	private ItemDrop.ItemData m_spawnItem;

	private bool m_didHit;

	private static int m_rayMaskSolids;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_rayMaskSolids == 0)
		{
			m_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		m_nview.Register("OnHit", RPC_OnHit);
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		UpdateRotation(Time.fixedDeltaTime);
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (!m_didHit)
		{
			Vector3 position = base.transform.position;
			m_vel += Vector3.down * m_gravity * Time.fixedDeltaTime;
			base.transform.position += m_vel * Time.fixedDeltaTime;
			if (m_rotateVisual == 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(m_vel);
			}
			if (m_canHitWater)
			{
				float liquidLevel = Floating.GetLiquidLevel(base.transform.position);
				if (base.transform.position.y < liquidLevel)
				{
					OnHit(null, base.transform.position, water: true);
				}
			}
			if (!m_didHit)
			{
				Vector3 vector = base.transform.position - position;
				RaycastHit[] array = Physics.SphereCastAll(position - vector, m_rayRadius, vector.normalized, vector.magnitude * 2f, m_rayMaskSolids);
				for (int i = 0; i < array.Length; i++)
				{
					RaycastHit raycastHit = array[i];
					OnHit(raycastHit.collider, raycastHit.point, water: false);
					if (m_didHit)
					{
						break;
					}
				}
			}
		}
		if (m_ttl > 0f)
		{
			m_ttl -= Time.fixedDeltaTime;
			if (m_ttl <= 0f)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	public Vector3 GetVelocity()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return Vector3.zero;
		}
		if (m_didHit)
		{
			return Vector3.zero;
		}
		return m_vel;
	}

	private void UpdateRotation(float dt)
	{
		if ((double)m_rotateVisual != 0.0 && !(m_visual == null))
		{
			m_visual.transform.Rotate(new Vector3(m_rotateVisual * dt, 0f, 0f));
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		m_owner = owner;
		m_vel = velocity;
		if (hitNoise >= 0f)
		{
			m_hitNoise = hitNoise;
		}
		if (hitData != null)
		{
			m_damage = hitData.m_damage;
			m_blockable = hitData.m_blockable;
			m_dodgeable = hitData.m_dodgeable;
			m_attackForce = hitData.m_pushForce;
			m_backstabBonus = hitData.m_backstabBonus;
			m_statusEffect = hitData.m_statusEffect;
			m_skill = hitData.m_skill;
		}
		if (m_respawnItemOnHit)
		{
			m_spawnItem = item;
		}
		LineConnect component = GetComponent<LineConnect>();
		if ((bool)component)
		{
			component.SetPeer(owner.GetZDOID());
		}
	}

	private void DoAOE(Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
	{
		Collider[] array = Physics.OverlapSphere(hitPoint, m_aoe, m_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			GameObject gameObject = FindHitObject(collider);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null && !hashSet.Contains(gameObject))
			{
				hashSet.Add(gameObject);
				if (IsValidTarget(component, ref hitCharacter))
				{
					Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
					Vector3 vector2 = ((Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : m_vel);
					vector2.y = 0f;
					vector2.Normalize();
					HitData hitData = new HitData();
					hitData.m_hitCollider = collider;
					hitData.m_damage = m_damage;
					hitData.m_pushForce = m_attackForce;
					hitData.m_backstabBonus = m_backstabBonus;
					hitData.m_ranged = true;
					hitData.m_point = vector;
					hitData.m_dir = vector2.normalized;
					hitData.m_statusEffect = m_statusEffect;
					hitData.m_dodgeable = m_dodgeable;
					hitData.m_blockable = m_blockable;
					hitData.m_skill = m_skill;
					hitData.SetAttacker(m_owner);
					component.Damage(hitData);
					didDamage = true;
				}
			}
		}
	}

	private bool IsValidTarget(IDestructible destr, ref bool hitCharacter)
	{
		Character character = destr as Character;
		if ((bool)character)
		{
			if (character == m_owner)
			{
				return false;
			}
			if (m_owner != null)
			{
				bool flag = BaseAI.IsEnemy(m_owner, character);
				if (!m_owner.IsPlayer() && !flag)
				{
					return false;
				}
				if (m_owner.IsPlayer() && !m_owner.IsPVPEnabled() && !flag)
				{
					return false;
				}
			}
			if (m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
			hitCharacter = true;
		}
		return true;
	}

	private void OnHit(Collider collider, Vector3 hitPoint, bool water)
	{
		GameObject gameObject = (collider ? FindHitObject(collider) : null);
		bool didDamage = false;
		bool hitCharacter = false;
		if (m_aoe > 0f)
		{
			DoAOE(hitPoint, ref hitCharacter, ref didDamage);
		}
		else
		{
			IDestructible destructible = (gameObject ? gameObject.GetComponent<IDestructible>() : null);
			if (destructible != null)
			{
				if (!IsValidTarget(destructible, ref hitCharacter))
				{
					return;
				}
				HitData hitData = new HitData();
				hitData.m_hitCollider = collider;
				hitData.m_damage = m_damage;
				hitData.m_pushForce = m_attackForce;
				hitData.m_backstabBonus = m_backstabBonus;
				hitData.m_point = hitPoint;
				hitData.m_dir = base.transform.forward;
				hitData.m_statusEffect = m_statusEffect;
				hitData.m_dodgeable = m_dodgeable;
				hitData.m_blockable = m_blockable;
				hitData.m_ranged = true;
				hitData.m_skill = m_skill;
				hitData.SetAttacker(m_owner);
				destructible.Damage(hitData);
				didDamage = true;
			}
		}
		if (water)
		{
			m_hitWaterEffects.Create(hitPoint, Quaternion.identity);
		}
		else
		{
			m_hitEffects.Create(hitPoint, Quaternion.identity);
		}
		if (m_spawnOnHit != null || m_spawnItem != null)
		{
			SpawnOnHit(gameObject, collider);
		}
		if (m_hitNoise > 0f)
		{
			BaseAI.DoProjectileHitNoise(base.transform.position, m_hitNoise, m_owner);
		}
		if (didDamage && m_owner != null && m_owner.IsPlayer() && hitCharacter)
		{
			m_owner.RaiseSkill(m_skill);
		}
		m_didHit = true;
		base.transform.position = hitPoint;
		m_nview.InvokeRPC("OnHit");
		if (!m_stayAfterHitStatic)
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
		else if ((bool)collider && collider.attachedRigidbody != null)
		{
			m_ttl = Mathf.Min(1f, m_ttl);
		}
	}

	private void RPC_OnHit(long sender)
	{
		if ((bool)m_hideOnHit)
		{
			m_hideOnHit.SetActive(value: false);
		}
		if (m_stopEmittersOnHit)
		{
			ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.EmissionModule emission = componentsInChildren[i].emission;
				emission.enabled = false;
			}
		}
	}

	private void SpawnOnHit(GameObject go, Collider collider)
	{
		if ((!m_groundHitOnly || !(go.GetComponent<Heightmap>() == null)) && (!m_staticHitOnly || ((!collider || !(collider.attachedRigidbody != null)) && (!go || go.GetComponent<IDestructible>() == null))) && (!(m_spawnOnHitChance < 1f) || !(Random.value > m_spawnOnHitChance)))
		{
			Vector3 vector = base.transform.position + base.transform.TransformDirection(m_spawnOffset);
			Quaternion rotation = base.transform.rotation;
			if (m_spawnRandomRotation)
			{
				rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
			}
			if (m_spawnOnHit != null)
			{
				Object.Instantiate(m_spawnOnHit, vector, rotation).GetComponent<IProjectile>()?.Setup(m_owner, m_vel, m_hitNoise, null, null);
			}
			if (m_spawnItem != null)
			{
				ItemDrop.DropItem(m_spawnItem, 0, vector, base.transform.rotation);
			}
			m_spawnOnHitEffects.Create(vector, Quaternion.identity);
		}
	}

	public static GameObject FindHitObject(Collider collider)
	{
		IDestructible componentInParent = collider.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			return (componentInParent as MonoBehaviour).gameObject;
		}
		if ((bool)collider.attachedRigidbody)
		{
			return collider.attachedRigidbody.gameObject;
		}
		return collider.gameObject;
	}
}
