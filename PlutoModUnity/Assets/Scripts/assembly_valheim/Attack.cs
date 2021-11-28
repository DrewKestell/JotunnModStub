using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Attack
{
	private class HitPoint
	{
		public GameObject go;

		public Vector3 avgPoint = Vector3.zero;

		public int count;

		public Vector3 firstPoint;

		public Collider collider;

		public Dictionary<Collider, Vector3> allHits = new Dictionary<Collider, Vector3>();

		public Vector3 closestPoint;

		public float closestDistance = 999999f;
	}

	public enum AttackType
	{
		Horizontal,
		Vertical,
		Projectile,
		None,
		Area,
		TriggerProjectile
	}

	public enum HitPointType
	{
		Closest,
		Average,
		First
	}

	[Header("Common")]
	public AttackType m_attackType;

	public string m_attackAnimation = "";

	public int m_attackRandomAnimations;

	public int m_attackChainLevels;

	public bool m_consumeItem;

	public bool m_hitTerrain = true;

	public float m_attackStamina = 20f;

	public float m_speedFactor = 0.2f;

	public float m_speedFactorRotation = 0.2f;

	public float m_attackStartNoise = 10f;

	public float m_attackHitNoise = 30f;

	public float m_damageMultiplier = 1f;

	public float m_forceMultiplier = 1f;

	public float m_staggerMultiplier = 1f;

	[Header("Misc")]
	public string m_attackOriginJoint = "";

	public float m_attackRange = 1.5f;

	public float m_attackHeight = 0.6f;

	public float m_attackOffset;

	public GameObject m_spawnOnTrigger;

	[Header("Melee/AOE")]
	public float m_attackAngle = 90f;

	public float m_attackRayWidth;

	public float m_maxYAngle;

	public bool m_lowerDamagePerHit = true;

	public HitPointType m_hitPointtype;

	public bool m_hitThroughWalls;

	public bool m_multiHit = true;

	public bool m_pickaxeSpecial;

	public float m_lastChainDamageMultiplier = 2f;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_resetChainIfHit;

	[Header("Skill settings")]
	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_skillHitType = DestructibleType.Character;

	public Skills.SkillType m_specialHitSkill;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_specialHitType;

	[Header("Projectile")]
	public GameObject m_attackProjectile;

	public float m_projectileVel = 10f;

	public float m_projectileVelMin = 2f;

	public float m_projectileAccuracy = 10f;

	public float m_projectileAccuracyMin = 20f;

	public bool m_useCharacterFacing;

	public bool m_useCharacterFacingYAim;

	[FormerlySerializedAs("m_useCharacterFacingAngle")]
	public float m_launchAngle;

	public int m_projectiles = 1;

	public int m_projectileBursts = 1;

	public float m_burstInterval;

	public bool m_destroyPreviousProjectile;

	[Header("Attack-Effects")]
	public EffectList m_hitEffect = new EffectList();

	public EffectList m_hitTerrainEffect = new EffectList();

	public EffectList m_startEffect = new EffectList();

	public EffectList m_triggerEffect = new EffectList();

	public EffectList m_trailStartEffect = new EffectList();

	protected static int m_attackMask;

	protected static int m_attackMaskTerrain;

	private Humanoid m_character;

	private BaseAI m_baseAI;

	private Rigidbody m_body;

	private ZSyncAnimation m_zanim;

	private CharacterAnimEvent m_animEvent;

	[NonSerialized]
	private ItemDrop.ItemData m_weapon;

	private VisEquipment m_visEquipment;

	private float m_attackDrawPercentage;

	private const float m_freezeFrameDuration = 0.15f;

	private const float m_chainAttackMaxTime = 0.2f;

	private int m_nextAttackChainLevel;

	private int m_currentAttackCainLevel;

	private bool m_wasInAttack;

	private float m_time;

	private bool m_projectileAttackStarted;

	private float m_projectileFireTimer = -1f;

	private int m_projectileBurstsFired;

	[NonSerialized]
	private ItemDrop.ItemData m_ammoItem;

	public bool StartDraw(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!HaveAmmo(character, weapon))
		{
			return false;
		}
		EquipAmmoItem(character, weapon);
		return true;
	}

	public bool Start(Humanoid character, Rigidbody body, ZSyncAnimation zanim, CharacterAnimEvent animEvent, VisEquipment visEquipment, ItemDrop.ItemData weapon, Attack previousAttack, float timeSinceLastAttack, float attackDrawPercentage)
	{
		if (m_attackAnimation == "")
		{
			return false;
		}
		m_character = character;
		m_baseAI = m_character.GetComponent<BaseAI>();
		m_body = body;
		m_zanim = zanim;
		m_animEvent = animEvent;
		m_visEquipment = visEquipment;
		m_weapon = weapon;
		m_attackDrawPercentage = attackDrawPercentage;
		if (m_attackMask == 0)
		{
			m_attackMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
			m_attackMaskTerrain = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		float attackStamina = GetAttackStamina();
		if (attackStamina > 0f && !character.HaveStamina(attackStamina + 0.1f))
		{
			if (character.IsPlayer())
			{
				Hud.instance.StaminaBarNoStaminaFlash();
			}
			return false;
		}
		if (!HaveAmmo(character, m_weapon))
		{
			return false;
		}
		EquipAmmoItem(character, m_weapon);
		if (m_attackChainLevels > 1)
		{
			if (previousAttack != null && previousAttack.m_attackAnimation == m_attackAnimation)
			{
				m_currentAttackCainLevel = previousAttack.m_nextAttackChainLevel;
			}
			if (m_currentAttackCainLevel >= m_attackChainLevels || timeSinceLastAttack > 0.2f)
			{
				m_currentAttackCainLevel = 0;
			}
			m_zanim.SetTrigger(m_attackAnimation + m_currentAttackCainLevel);
		}
		else if (m_attackRandomAnimations >= 2)
		{
			int num = UnityEngine.Random.Range(0, m_attackRandomAnimations);
			m_zanim.SetTrigger(m_attackAnimation + num);
		}
		else
		{
			m_zanim.SetTrigger(m_attackAnimation);
		}
		if (character.IsPlayer() && m_attackType != AttackType.None && m_currentAttackCainLevel == 0)
		{
			if (ZInput.IsMouseActive() || m_attackType == AttackType.Projectile)
			{
				character.transform.rotation = character.GetLookYaw();
				m_body.rotation = character.transform.rotation;
			}
			else if (ZInput.IsGamepadActive() && !character.IsBlocking() && character.GetMoveDir().magnitude > 0.3f)
			{
				character.transform.rotation = Quaternion.LookRotation(character.GetMoveDir());
				m_body.rotation = character.transform.rotation;
			}
		}
		weapon.m_lastAttackTime = Time.time;
		m_animEvent.ResetChain();
		return true;
	}

	private float GetAttackStamina()
	{
		if (m_attackStamina <= 0f)
		{
			return 0f;
		}
		float attackStamina = m_attackStamina;
		float skillFactor = m_character.GetSkillFactor(m_weapon.m_shared.m_skillType);
		return attackStamina - attackStamina * 0.33f * skillFactor;
	}

	public void Update(float dt)
	{
		m_time += dt;
		if (m_character.InAttack())
		{
			if (!m_wasInAttack)
			{
				m_character.UseStamina(GetAttackStamina());
				Transform attackOrigin = GetAttackOrigin();
				m_weapon.m_shared.m_startEffect.Create(attackOrigin.position, m_character.transform.rotation, attackOrigin);
				m_startEffect.Create(attackOrigin.position, m_character.transform.rotation, attackOrigin);
				m_character.AddNoise(m_attackStartNoise);
				m_nextAttackChainLevel = m_currentAttackCainLevel + 1;
				if (m_nextAttackChainLevel >= m_attackChainLevels)
				{
					m_nextAttackChainLevel = 0;
				}
			}
			m_wasInAttack = true;
		}
		else if (m_wasInAttack)
		{
			OnAttackDone();
			m_wasInAttack = false;
		}
		UpdateProjectile(dt);
	}

	private void OnAttackDone()
	{
		if ((bool)m_visEquipment)
		{
			m_visEquipment.SetWeaponTrails(enabled: false);
		}
	}

	public void Stop()
	{
		if (m_wasInAttack)
		{
			OnAttackDone();
			m_wasInAttack = false;
		}
	}

	public void OnAttackTrigger()
	{
		if (UseAmmo())
		{
			switch (m_attackType)
			{
			case AttackType.Horizontal:
			case AttackType.Vertical:
				DoMeleeAttack();
				break;
			case AttackType.Area:
				DoAreaAttack();
				break;
			case AttackType.Projectile:
				ProjectileAttackTriggered();
				break;
			case AttackType.None:
				DoNonAttack();
				break;
			}
			if (m_consumeItem)
			{
				ConsumeItem();
			}
		}
	}

	private void ConsumeItem()
	{
		if (m_weapon.m_shared.m_maxStackSize > 1 && m_weapon.m_stack > 1)
		{
			m_weapon.m_stack--;
			return;
		}
		m_character.UnequipItem(m_weapon, triggerEquipEffects: false);
		m_character.GetInventory().RemoveItem(m_weapon);
	}

	private static bool EquipAmmoItem(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			ItemDrop.ItemData ammoItem = character.GetAmmoItem();
			if (ammoItem != null && character.GetInventory().ContainsItem(ammoItem) && ammoItem.m_shared.m_ammoType == weapon.m_shared.m_ammoType)
			{
				return true;
			}
			ItemDrop.ItemData ammoItem2 = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);
			if (ammoItem2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
			{
				return character.EquipItem(ammoItem2);
			}
		}
		return true;
	}

	private static bool HaveAmmo(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			ItemDrop.ItemData itemData = character.GetAmmoItem();
			if (itemData != null && (!character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
			{
				itemData = null;
			}
			if (itemData == null)
			{
				itemData = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType);
			}
			if (itemData == null)
			{
				character.Message(MessageHud.MessageType.Center, "$msg_outof " + weapon.m_shared.m_ammoType);
				return false;
			}
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
			{
				return character.CanConsumeItem(itemData);
			}
			return true;
		}
		return true;
	}

	private bool UseAmmo()
	{
		m_ammoItem = null;
		ItemDrop.ItemData itemData = null;
		if (!string.IsNullOrEmpty(m_weapon.m_shared.m_ammoType))
		{
			itemData = m_character.GetAmmoItem();
			if (itemData != null && (!m_character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != m_weapon.m_shared.m_ammoType))
			{
				itemData = null;
			}
			if (itemData == null)
			{
				itemData = m_character.GetInventory().GetAmmoItem(m_weapon.m_shared.m_ammoType);
			}
			if (itemData == null)
			{
				m_character.Message(MessageHud.MessageType.Center, "$msg_outof " + m_weapon.m_shared.m_ammoType);
				return false;
			}
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
			{
				bool num = m_character.ConsumeItem(m_character.GetInventory(), itemData);
				if (num)
				{
					m_ammoItem = itemData;
				}
				return num;
			}
			m_character.GetInventory().RemoveItem(itemData, 1);
			m_ammoItem = itemData;
			return true;
		}
		return true;
	}

	private void ProjectileAttackTriggered()
	{
		GetProjectileSpawnPoint(out var spawnPoint, out var aimDir);
		m_weapon.m_shared.m_triggerEffect.Create(spawnPoint, Quaternion.LookRotation(aimDir));
		m_triggerEffect.Create(spawnPoint, Quaternion.LookRotation(aimDir));
		if (m_weapon.m_shared.m_useDurability && m_character.IsPlayer())
		{
			m_weapon.m_durability -= m_weapon.m_shared.m_useDurabilityDrain;
		}
		if (m_projectileBursts == 1)
		{
			FireProjectileBurst();
		}
		else
		{
			m_projectileAttackStarted = true;
		}
	}

	private void UpdateProjectile(float dt)
	{
		if (m_projectileAttackStarted && m_projectileBurstsFired < m_projectileBursts)
		{
			m_projectileFireTimer -= dt;
			if (m_projectileFireTimer <= 0f)
			{
				m_projectileFireTimer = m_burstInterval;
				FireProjectileBurst();
				m_projectileBurstsFired++;
			}
		}
	}

	private Transform GetAttackOrigin()
	{
		if (m_attackOriginJoint.Length > 0)
		{
			return Utils.FindChild(m_character.GetVisual().transform, m_attackOriginJoint);
		}
		return m_character.transform;
	}

	private void GetProjectileSpawnPoint(out Vector3 spawnPoint, out Vector3 aimDir)
	{
		Transform attackOrigin = GetAttackOrigin();
		Transform transform = m_character.transform;
		spawnPoint = attackOrigin.position + transform.up * m_attackHeight + transform.forward * m_attackRange + transform.right * m_attackOffset;
		aimDir = m_character.GetAimDir(spawnPoint);
		if ((bool)m_baseAI)
		{
			Character targetCreature = m_baseAI.GetTargetCreature();
			if ((bool)targetCreature)
			{
				Vector3 normalized = (targetCreature.GetCenterPoint() - spawnPoint).normalized;
				aimDir = Vector3.RotateTowards(m_character.transform.forward, normalized, (float)Math.PI / 2f, 1f);
			}
		}
	}

	private void FireProjectileBurst()
	{
		ItemDrop.ItemData ammoItem = m_ammoItem;
		GameObject attackProjectile = m_attackProjectile;
		float num = m_projectileVel;
		float num2 = m_projectileVelMin;
		float num3 = m_projectileAccuracy;
		float num4 = m_projectileAccuracyMin;
		float num5 = m_attackHitNoise;
		if (ammoItem != null && (bool)ammoItem.m_shared.m_attack.m_attackProjectile)
		{
			attackProjectile = ammoItem.m_shared.m_attack.m_attackProjectile;
			num += ammoItem.m_shared.m_attack.m_projectileVel;
			num2 += ammoItem.m_shared.m_attack.m_projectileVelMin;
			num3 += ammoItem.m_shared.m_attack.m_projectileAccuracy;
			num4 += ammoItem.m_shared.m_attack.m_projectileAccuracyMin;
			num5 += ammoItem.m_shared.m_attack.m_attackHitNoise;
		}
		float num6 = m_character.GetRandomSkillFactor(m_weapon.m_shared.m_skillType);
		if (m_weapon.m_shared.m_holdDurationMin > 0f)
		{
			num3 = Mathf.Lerp(num4, num3, Mathf.Pow(m_attackDrawPercentage, 0.5f));
			num6 *= m_attackDrawPercentage;
			num = Mathf.Lerp(num2, num, m_attackDrawPercentage);
		}
		GetProjectileSpawnPoint(out var spawnPoint, out var aimDir);
		Transform transform = m_character.transform;
		if (m_useCharacterFacing)
		{
			Vector3 forward = Vector3.forward;
			if (m_useCharacterFacingYAim)
			{
				forward.y = aimDir.y;
			}
			aimDir = transform.TransformDirection(forward);
		}
		if (m_launchAngle != 0f)
		{
			Vector3 axis = Vector3.Cross(Vector3.up, aimDir);
			aimDir = Quaternion.AngleAxis(m_launchAngle, axis) * aimDir;
		}
		for (int i = 0; i < m_projectiles; i++)
		{
			if (m_destroyPreviousProjectile && (bool)m_weapon.m_lastProjectile)
			{
				ZNetScene.instance.Destroy(m_weapon.m_lastProjectile);
				m_weapon.m_lastProjectile = null;
			}
			Vector3 vector = aimDir;
			Vector3 axis2 = Vector3.Cross(vector, Vector3.up);
			Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - num3, num3), Vector3.up);
			vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - num3, num3), axis2) * vector;
			vector = quaternion * vector;
			GameObject gameObject = UnityEngine.Object.Instantiate(attackProjectile, spawnPoint, Quaternion.LookRotation(vector));
			HitData hitData = new HitData();
			hitData.m_toolTier = m_weapon.m_shared.m_toolTier;
			hitData.m_pushForce = m_weapon.m_shared.m_attackForce * m_forceMultiplier;
			hitData.m_backstabBonus = m_weapon.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = m_staggerMultiplier;
			hitData.m_damage.Add(m_weapon.GetDamage());
			hitData.m_statusEffect = (m_weapon.m_shared.m_attackStatusEffect ? m_weapon.m_shared.m_attackStatusEffect.name : "");
			hitData.m_blockable = m_weapon.m_shared.m_blockable;
			hitData.m_dodgeable = m_weapon.m_shared.m_dodgeable;
			hitData.m_skill = m_weapon.m_shared.m_skillType;
			hitData.SetAttacker(m_character);
			if (ammoItem != null)
			{
				hitData.m_damage.Add(ammoItem.GetDamage());
				hitData.m_pushForce += ammoItem.m_shared.m_attackForce;
				if (ammoItem.m_shared.m_attackStatusEffect != null)
				{
					hitData.m_statusEffect = ammoItem.m_shared.m_attackStatusEffect.name;
				}
				if (!ammoItem.m_shared.m_blockable)
				{
					hitData.m_blockable = false;
				}
				if (!ammoItem.m_shared.m_dodgeable)
				{
					hitData.m_dodgeable = false;
				}
			}
			hitData.m_pushForce *= num6;
			hitData.m_damage.Modify(m_damageMultiplier);
			hitData.m_damage.Modify(num6);
			hitData.m_damage.Modify(GetLevelDamageFactor());
			m_character.GetSEMan().ModifyAttack(m_weapon.m_shared.m_skillType, ref hitData);
			gameObject.GetComponent<IProjectile>()?.Setup(m_character, vector * num, num5, hitData, m_weapon);
			m_weapon.m_lastProjectile = gameObject;
		}
	}

	private void DoNonAttack()
	{
		if (m_weapon.m_shared.m_useDurability && m_character.IsPlayer())
		{
			m_weapon.m_durability -= m_weapon.m_shared.m_useDurabilityDrain;
		}
		Transform attackOrigin = GetAttackOrigin();
		m_weapon.m_shared.m_triggerEffect.Create(attackOrigin.position, m_character.transform.rotation, attackOrigin);
		m_triggerEffect.Create(attackOrigin.position, m_character.transform.rotation, attackOrigin);
		if ((bool)m_weapon.m_shared.m_consumeStatusEffect)
		{
			m_character.GetSEMan().AddStatusEffect(m_weapon.m_shared.m_consumeStatusEffect, resetTime: true);
		}
		m_character.AddNoise(m_attackHitNoise);
	}

	private float GetLevelDamageFactor()
	{
		return 1f + (float)Mathf.Max(0, m_character.GetLevel() - 1) * 0.5f;
	}

	private void DoAreaAttack()
	{
		Transform transform = m_character.transform;
		Transform attackOrigin = GetAttackOrigin();
		Vector3 vector = attackOrigin.position + Vector3.up * m_attackHeight + transform.forward * m_attackRange + transform.right * m_attackOffset;
		m_weapon.m_shared.m_triggerEffect.Create(vector, transform.rotation, attackOrigin);
		m_triggerEffect.Create(vector, transform.rotation, attackOrigin);
		Vector3 vector2 = vector - transform.position;
		vector2.y = 0f;
		vector2.Normalize();
		int num = 0;
		Vector3 zero = Vector3.zero;
		bool flag = false;
		float randomSkillFactor = m_character.GetRandomSkillFactor(m_weapon.m_shared.m_skillType);
		int layerMask = (m_hitTerrain ? m_attackMaskTerrain : m_attackMask);
		Collider[] array = Physics.OverlapSphere(vector, m_attackRayWidth, layerMask, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			if (collider.gameObject == m_character.gameObject)
			{
				continue;
			}
			GameObject gameObject = Projectile.FindHitObject(collider);
			if (gameObject == m_character.gameObject || hashSet.Contains(gameObject))
			{
				continue;
			}
			hashSet.Add(gameObject);
			Vector3 vector3 = ((!(collider is MeshCollider)) ? collider.ClosestPoint(vector) : collider.ClosestPointOnBounds(vector));
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null)
			{
				Vector3 vector4 = vector3 - vector;
				vector4.y = 0f;
				if (Vector3.Dot(vector2, vector4) < 0f)
				{
					vector4 = vector2;
				}
				vector4.Normalize();
				HitData hitData = new HitData();
				hitData.m_toolTier = m_weapon.m_shared.m_toolTier;
				hitData.m_statusEffect = (m_weapon.m_shared.m_attackStatusEffect ? m_weapon.m_shared.m_attackStatusEffect.name : "");
				hitData.m_pushForce = m_weapon.m_shared.m_attackForce * randomSkillFactor * m_forceMultiplier;
				hitData.m_backstabBonus = m_weapon.m_shared.m_backstabBonus;
				hitData.m_staggerMultiplier = m_staggerMultiplier;
				hitData.m_dodgeable = m_weapon.m_shared.m_dodgeable;
				hitData.m_blockable = m_weapon.m_shared.m_blockable;
				hitData.m_skill = m_weapon.m_shared.m_skillType;
				hitData.m_damage.Add(m_weapon.GetDamage());
				hitData.m_point = vector3;
				hitData.m_dir = vector4;
				hitData.m_hitCollider = collider;
				hitData.SetAttacker(m_character);
				hitData.m_damage.Modify(m_damageMultiplier);
				hitData.m_damage.Modify(randomSkillFactor);
				hitData.m_damage.Modify(GetLevelDamageFactor());
				if (m_attackChainLevels > 1 && m_currentAttackCainLevel == m_attackChainLevels - 1 && m_lastChainDamageMultiplier > 1f)
				{
					hitData.m_damage.Modify(m_lastChainDamageMultiplier);
					hitData.m_pushForce *= 1.2f;
				}
				m_character.GetSEMan().ModifyAttack(m_weapon.m_shared.m_skillType, ref hitData);
				Character character = component as Character;
				if ((bool)character)
				{
					bool flag2 = BaseAI.IsEnemy(m_character, character);
					if ((!m_character.IsPlayer() && !flag2) || (!m_weapon.m_shared.m_tamedOnly && m_character.IsPlayer() && !m_character.IsPVPEnabled() && !flag2) || (m_weapon.m_shared.m_tamedOnly && !character.IsTamed()) || (hitData.m_dodgeable && character.IsDodgeInvincible()))
					{
						continue;
					}
				}
				else if (m_weapon.m_shared.m_tamedOnly)
				{
					continue;
				}
				component.Damage(hitData);
				if ((component.GetDestructibleType() & m_skillHitType) != 0)
				{
					flag = true;
				}
			}
			num++;
			zero += vector3;
		}
		if (num > 0)
		{
			zero /= (float)num;
			m_weapon.m_shared.m_hitEffect.Create(zero, Quaternion.identity);
			m_hitEffect.Create(zero, Quaternion.identity);
			if (m_weapon.m_shared.m_useDurability && m_character.IsPlayer())
			{
				m_weapon.m_durability -= 1f;
			}
			m_character.AddNoise(m_attackHitNoise);
			if (flag)
			{
				m_character.RaiseSkill(m_weapon.m_shared.m_skillType);
			}
		}
		if ((bool)m_spawnOnTrigger)
		{
			UnityEngine.Object.Instantiate(m_spawnOnTrigger, vector, Quaternion.identity).GetComponent<IProjectile>()?.Setup(m_character, m_character.transform.forward, -1f, null, null);
		}
	}

	private void GetMeleeAttackDir(out Transform originJoint, out Vector3 attackDir)
	{
		originJoint = GetAttackOrigin();
		Vector3 forward = m_character.transform.forward;
		Vector3 aimDir = m_character.GetAimDir(originJoint.position);
		aimDir.x = forward.x;
		aimDir.z = forward.z;
		aimDir.Normalize();
		attackDir = Vector3.RotateTowards(m_character.transform.forward, aimDir, (float)Math.PI / 180f * m_maxYAngle, 10f);
	}

	private void AddHitPoint(List<HitPoint> list, GameObject go, Collider collider, Vector3 point, float distance, bool multiCollider)
	{
		HitPoint hitPoint = null;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if ((!multiCollider && list[num].go == go) || (multiCollider && list[num].collider == collider))
			{
				hitPoint = list[num];
				break;
			}
		}
		if (hitPoint == null)
		{
			hitPoint = new HitPoint();
			hitPoint.go = go;
			hitPoint.collider = collider;
			hitPoint.firstPoint = point;
			list.Add(hitPoint);
		}
		hitPoint.avgPoint += point;
		hitPoint.count++;
		if (distance < hitPoint.closestDistance)
		{
			hitPoint.closestPoint = point;
			hitPoint.closestDistance = distance;
		}
	}

	private void DoMeleeAttack()
	{
		GetMeleeAttackDir(out var originJoint, out var attackDir);
		Vector3 vector = m_character.transform.InverseTransformDirection(attackDir);
		Quaternion quaternion = Quaternion.LookRotation(attackDir, Vector3.up);
		m_weapon.m_shared.m_triggerEffect.Create(originJoint.position, quaternion, originJoint);
		m_triggerEffect.Create(originJoint.position, quaternion, originJoint);
		Vector3 vector2 = originJoint.position + Vector3.up * m_attackHeight + m_character.transform.right * m_attackOffset;
		float num = m_attackAngle / 2f;
		float num2 = 4f;
		float attackRange = m_attackRange;
		List<HitPoint> list = new List<HitPoint>();
		HashSet<Skills.SkillType> hashSet = new HashSet<Skills.SkillType>();
		int layerMask = (m_hitTerrain ? m_attackMaskTerrain : m_attackMask);
		for (float num3 = 0f - num; num3 <= num; num3 += num2)
		{
			Quaternion quaternion2 = Quaternion.identity;
			if (m_attackType == AttackType.Horizontal)
			{
				quaternion2 = Quaternion.Euler(0f, 0f - num3, 0f);
			}
			else if (m_attackType == AttackType.Vertical)
			{
				quaternion2 = Quaternion.Euler(num3, 0f, 0f);
			}
			Vector3 vector3 = m_character.transform.TransformDirection(quaternion2 * vector);
			Debug.DrawLine(vector2, vector2 + vector3 * attackRange);
			RaycastHit[] array = ((!(m_attackRayWidth > 0f)) ? Physics.RaycastAll(vector2, vector3, attackRange, layerMask, QueryTriggerInteraction.Ignore) : Physics.SphereCastAll(vector2, m_attackRayWidth, vector3, Mathf.Max(0f, attackRange - m_attackRayWidth), layerMask, QueryTriggerInteraction.Ignore));
			Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
			RaycastHit[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit raycastHit = array2[i];
				if (raycastHit.collider.gameObject == m_character.gameObject)
				{
					continue;
				}
				Vector3 vector4 = raycastHit.point;
				if (raycastHit.distance < float.Epsilon)
				{
					vector4 = ((!(raycastHit.collider is MeshCollider)) ? raycastHit.collider.ClosestPoint(vector2) : (vector2 + vector3 * attackRange));
				}
				if (m_attackAngle < 180f && Vector3.Dot(vector4 - vector2, attackDir) <= 0f)
				{
					continue;
				}
				GameObject gameObject = Projectile.FindHitObject(raycastHit.collider);
				if (gameObject == m_character.gameObject)
				{
					continue;
				}
				Vagon component = gameObject.GetComponent<Vagon>();
				if ((bool)component && component.IsAttached(m_character))
				{
					continue;
				}
				Character component2 = gameObject.GetComponent<Character>();
				if (component2 != null)
				{
					bool flag = BaseAI.IsEnemy(m_character, component2);
					if ((!m_character.IsPlayer() && !flag) || (!m_weapon.m_shared.m_tamedOnly && m_character.IsPlayer() && !m_character.IsPVPEnabled() && !flag) || (m_weapon.m_shared.m_tamedOnly && !component2.IsTamed()) || (m_weapon.m_shared.m_dodgeable && component2.IsDodgeInvincible()))
					{
						continue;
					}
				}
				else if (m_weapon.m_shared.m_tamedOnly)
				{
					continue;
				}
				bool multiCollider = m_pickaxeSpecial && ((bool)gameObject.GetComponent<MineRock5>() || (bool)gameObject.GetComponent<MineRock>());
				AddHitPoint(list, gameObject, raycastHit.collider, vector4, raycastHit.distance, multiCollider);
				if (!m_hitThroughWalls)
				{
					break;
				}
			}
		}
		int num4 = 0;
		Vector3 zero = Vector3.zero;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (HitPoint item in list)
		{
			GameObject go = item.go;
			Vector3 vector5 = item.avgPoint / item.count;
			Vector3 vector6 = vector5;
			switch (m_hitPointtype)
			{
			case HitPointType.Average:
				vector6 = vector5;
				break;
			case HitPointType.First:
				vector6 = item.firstPoint;
				break;
			case HitPointType.Closest:
				vector6 = item.closestPoint;
				break;
			}
			num4++;
			zero += vector5;
			m_weapon.m_shared.m_hitEffect.Create(vector6, Quaternion.identity);
			m_hitEffect.Create(vector6, Quaternion.identity);
			IDestructible component3 = go.GetComponent<IDestructible>();
			if (component3 != null)
			{
				DestructibleType destructibleType = component3.GetDestructibleType();
				Skills.SkillType skillType = m_weapon.m_shared.m_skillType;
				if (m_specialHitSkill != 0 && (destructibleType & m_specialHitType) != 0)
				{
					skillType = m_specialHitSkill;
					hashSet.Add(m_specialHitSkill);
				}
				else if ((destructibleType & m_skillHitType) != 0)
				{
					hashSet.Add(skillType);
				}
				float num5 = m_character.GetRandomSkillFactor(skillType);
				if (m_multiHit && m_lowerDamagePerHit && list.Count > 1)
				{
					num5 /= (float)list.Count * 0.75f;
				}
				HitData hitData = new HitData();
				hitData.m_toolTier = m_weapon.m_shared.m_toolTier;
				hitData.m_statusEffect = (m_weapon.m_shared.m_attackStatusEffect ? m_weapon.m_shared.m_attackStatusEffect.name : "");
				hitData.m_pushForce = m_weapon.m_shared.m_attackForce * num5 * m_forceMultiplier;
				hitData.m_backstabBonus = m_weapon.m_shared.m_backstabBonus;
				hitData.m_staggerMultiplier = m_staggerMultiplier;
				hitData.m_dodgeable = m_weapon.m_shared.m_dodgeable;
				hitData.m_blockable = m_weapon.m_shared.m_blockable;
				hitData.m_skill = skillType;
				hitData.m_damage = m_weapon.GetDamage();
				hitData.m_point = vector6;
				hitData.m_dir = (vector6 - vector2).normalized;
				hitData.m_hitCollider = item.collider;
				hitData.SetAttacker(m_character);
				hitData.m_damage.Modify(m_damageMultiplier);
				hitData.m_damage.Modify(num5);
				hitData.m_damage.Modify(GetLevelDamageFactor());
				if (m_attackChainLevels > 1 && m_currentAttackCainLevel == m_attackChainLevels - 1)
				{
					hitData.m_damage.Modify(2f);
					hitData.m_pushForce *= 1.2f;
				}
				m_character.GetSEMan().ModifyAttack(skillType, ref hitData);
				if (component3 is Character)
				{
					flag3 = true;
				}
				component3.Damage(hitData);
				if ((destructibleType & m_resetChainIfHit) != 0)
				{
					m_nextAttackChainLevel = 0;
				}
				if (!m_multiHit)
				{
					break;
				}
			}
			if (go.GetComponent<Heightmap>() != null && !flag2 && (!m_pickaxeSpecial || !flag4))
			{
				flag2 = true;
				m_weapon.m_shared.m_hitTerrainEffect.Create(vector6, quaternion);
				m_hitTerrainEffect.Create(vector6, quaternion);
				if ((bool)m_weapon.m_shared.m_spawnOnHitTerrain)
				{
					SpawnOnHitTerrain(vector6, m_weapon.m_shared.m_spawnOnHitTerrain);
				}
				if (!m_multiHit || m_pickaxeSpecial)
				{
					break;
				}
			}
			else
			{
				flag4 = true;
			}
		}
		if (num4 > 0)
		{
			zero /= (float)num4;
			if (m_weapon.m_shared.m_useDurability && m_character.IsPlayer())
			{
				m_weapon.m_durability -= m_weapon.m_shared.m_useDurabilityDrain;
			}
			m_character.AddNoise(m_attackHitNoise);
			m_animEvent.FreezeFrame(0.15f);
			if ((bool)m_weapon.m_shared.m_spawnOnHit)
			{
				UnityEngine.Object.Instantiate(m_weapon.m_shared.m_spawnOnHit, zero, quaternion).GetComponent<IProjectile>()?.Setup(m_character, Vector3.zero, m_attackHitNoise, null, m_weapon);
			}
			foreach (Skills.SkillType item2 in hashSet)
			{
				m_character.RaiseSkill(item2, flag3 ? 1.5f : 1f);
			}
		}
		if ((bool)m_spawnOnTrigger)
		{
			UnityEngine.Object.Instantiate(m_spawnOnTrigger, vector2, Quaternion.identity).GetComponent<IProjectile>()?.Setup(m_character, m_character.transform.forward, -1f, null, m_weapon);
		}
	}

	private void SpawnOnHitTerrain(Vector3 hitPoint, GameObject prefab)
	{
		TerrainModifier componentInChildren = prefab.GetComponentInChildren<TerrainModifier>();
		if (!componentInChildren || (PrivateArea.CheckAccess(hitPoint, componentInChildren.GetRadius()) && !Location.IsInsideNoBuildLocation(hitPoint)))
		{
			TerrainOp componentInChildren2 = prefab.GetComponentInChildren<TerrainOp>();
			if (!componentInChildren2 || (PrivateArea.CheckAccess(hitPoint, componentInChildren2.GetRadius()) && !Location.IsInsideNoBuildLocation(hitPoint)))
			{
				TerrainModifier.SetTriggerOnPlaced(trigger: true);
				GameObject gameObject = UnityEngine.Object.Instantiate(prefab, hitPoint, Quaternion.LookRotation(m_character.transform.forward));
				TerrainModifier.SetTriggerOnPlaced(trigger: false);
				gameObject.GetComponent<IProjectile>()?.Setup(m_character, Vector3.zero, m_attackHitNoise, null, m_weapon);
			}
		}
	}

	public Attack Clone()
	{
		return MemberwiseClone() as Attack;
	}

	public ItemDrop.ItemData GetWeapon()
	{
		return m_weapon;
	}

	public bool CanStartChainAttack()
	{
		if (m_nextAttackChainLevel > 0)
		{
			return m_animEvent.CanChain();
		}
		return false;
	}

	public void OnTrailStart()
	{
		if (m_attackType == AttackType.Projectile)
		{
			Transform attackOrigin = GetAttackOrigin();
			m_weapon.m_shared.m_trailStartEffect.Create(attackOrigin.position, m_character.transform.rotation);
			m_trailStartEffect.Create(attackOrigin.position, m_character.transform.rotation);
		}
		else
		{
			GetMeleeAttackDir(out var originJoint, out var attackDir);
			Quaternion baseRot = Quaternion.LookRotation(attackDir, Vector3.up);
			m_weapon.m_shared.m_trailStartEffect.Create(originJoint.position, baseRot);
			m_trailStartEffect.Create(originJoint.position, baseRot);
		}
	}
}
