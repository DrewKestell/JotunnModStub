using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ItemDrop : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class ItemData
	{
		public enum ItemType
		{
			None = 0,
			Material = 1,
			Consumable = 2,
			OneHandedWeapon = 3,
			Bow = 4,
			Shield = 5,
			Helmet = 6,
			Chest = 7,
			Ammo = 9,
			Customization = 10,
			Legs = 11,
			Hands = 12,
			Trophie = 13,
			TwoHandedWeapon = 14,
			Torch = 0xF,
			Misc = 0x10,
			Shoulder = 17,
			Utility = 18,
			Tool = 19,
			Attach_Atgeir = 20
		}

		public enum AnimationState
		{
			Unarmed,
			OneHanded,
			TwoHandedClub,
			Bow,
			Shield,
			Torch,
			LeftTorch,
			Atgeir,
			TwoHandedAxe,
			FishingRod
		}

		public enum AiTarget
		{
			Enemy,
			FriendHurt,
			Friend
		}

		[Serializable]
		public class SharedData
		{
			public string m_name = "";

			public string m_dlc = "";

			public ItemType m_itemType = ItemType.Misc;

			public Sprite[] m_icons = new Sprite[0];

			public ItemType m_attachOverride;

			[TextArea]
			public string m_description = "";

			public int m_maxStackSize = 1;

			public int m_maxQuality = 1;

			public float m_weight = 1f;

			public int m_value;

			public bool m_teleportable = true;

			public bool m_questItem;

			public float m_equipDuration = 1f;

			public int m_variants;

			public Vector2Int m_trophyPos = Vector2Int.zero;

			public PieceTable m_buildPieces;

			public bool m_centerCamera;

			public string m_setName = "";

			public int m_setSize;

			public StatusEffect m_setStatusEffect;

			public StatusEffect m_equipStatusEffect;

			public float m_movementModifier;

			[Header("Food settings")]
			public float m_food;

			public float m_foodStamina;

			public float m_foodBurnTime;

			public float m_foodRegen;

			public Color m_foodColor = Color.white;

			[Header("Armor settings")]
			public Material m_armorMaterial;

			public bool m_helmetHideHair = true;

			public float m_armor = 10f;

			public float m_armorPerLevel = 1f;

			public List<HitData.DamageModPair> m_damageModifiers = new List<HitData.DamageModPair>();

			[Header("Shield settings")]
			public float m_blockPower = 10f;

			public float m_blockPowerPerLevel;

			public float m_deflectionForce;

			public float m_deflectionForcePerLevel;

			public float m_timedBlockBonus = 1.5f;

			[Header("Weapon")]
			public AnimationState m_animationState = AnimationState.OneHanded;

			public Skills.SkillType m_skillType = Skills.SkillType.Swords;

			public int m_toolTier;

			public HitData.DamageTypes m_damages;

			public HitData.DamageTypes m_damagesPerLevel;

			public float m_attackForce = 30f;

			public float m_backstabBonus = 4f;

			public bool m_dodgeable;

			public bool m_blockable;

			public bool m_tamedOnly;

			public StatusEffect m_attackStatusEffect;

			public GameObject m_spawnOnHit;

			public GameObject m_spawnOnHitTerrain;

			[Header("Attacks")]
			public Attack m_attack;

			public Attack m_secondaryAttack;

			[Header("Durability")]
			public bool m_useDurability;

			public bool m_destroyBroken = true;

			public bool m_canBeReparied = true;

			public float m_maxDurability = 100f;

			public float m_durabilityPerLevel = 50f;

			public float m_useDurabilityDrain = 1f;

			public float m_durabilityDrain;

			[Header("Hold")]
			public float m_holdDurationMin;

			public float m_holdStaminaDrain;

			public string m_holdAnimationState = "";

			[Header("Ammo")]
			public string m_ammoType = "";

			[Header("AI")]
			public float m_aiAttackRange = 2f;

			public float m_aiAttackRangeMin;

			public float m_aiAttackInterval = 2f;

			public float m_aiAttackMaxAngle = 5f;

			public bool m_aiWhenFlying = true;

			public bool m_aiWhenWalking = true;

			public bool m_aiWhenSwiming = true;

			public bool m_aiPrioritized;

			public AiTarget m_aiTargetType;

			[Header("Effects")]
			public EffectList m_hitEffect = new EffectList();

			public EffectList m_hitTerrainEffect = new EffectList();

			public EffectList m_blockEffect = new EffectList();

			public EffectList m_startEffect = new EffectList();

			public EffectList m_holdStartEffect = new EffectList();

			public EffectList m_triggerEffect = new EffectList();

			public EffectList m_trailStartEffect = new EffectList();

			[Header("Consumable")]
			public StatusEffect m_consumeStatusEffect;
		}

		public int m_stack = 1;

		public float m_durability = 100f;

		public int m_quality = 1;

		public int m_variant;

		public SharedData m_shared;

		[NonSerialized]
		public long m_crafterID;

		[NonSerialized]
		public string m_crafterName = "";

		[NonSerialized]
		public Vector2i m_gridPos = Vector2i.zero;

		[NonSerialized]
		public bool m_equiped;

		[NonSerialized]
		public GameObject m_dropPrefab;

		[NonSerialized]
		public float m_lastAttackTime;

		[NonSerialized]
		public GameObject m_lastProjectile;

		public ItemData Clone()
		{
			return MemberwiseClone() as ItemData;
		}

		public bool IsEquipable()
		{
			if (m_shared.m_itemType != ItemType.Tool && m_shared.m_itemType != ItemType.OneHandedWeapon && m_shared.m_itemType != ItemType.TwoHandedWeapon && m_shared.m_itemType != ItemType.Bow && m_shared.m_itemType != ItemType.Shield && m_shared.m_itemType != ItemType.Helmet && m_shared.m_itemType != ItemType.Chest && m_shared.m_itemType != ItemType.Legs && m_shared.m_itemType != ItemType.Shoulder && m_shared.m_itemType != ItemType.Ammo && m_shared.m_itemType != ItemType.Torch)
			{
				return m_shared.m_itemType == ItemType.Utility;
			}
			return true;
		}

		public bool IsWeapon()
		{
			if (m_shared.m_itemType != ItemType.OneHandedWeapon && m_shared.m_itemType != ItemType.Bow && m_shared.m_itemType != ItemType.TwoHandedWeapon)
			{
				return m_shared.m_itemType == ItemType.Torch;
			}
			return true;
		}

		public bool HavePrimaryAttack()
		{
			return !string.IsNullOrEmpty(m_shared.m_attack.m_attackAnimation);
		}

		public bool HaveSecondaryAttack()
		{
			return !string.IsNullOrEmpty(m_shared.m_secondaryAttack.m_attackAnimation);
		}

		public float GetArmor()
		{
			return GetArmor(m_quality);
		}

		public float GetArmor(int quality)
		{
			return m_shared.m_armor + (float)Mathf.Max(0, quality - 1) * m_shared.m_armorPerLevel;
		}

		public int GetValue()
		{
			return m_shared.m_value * m_stack;
		}

		public float GetWeight()
		{
			return m_shared.m_weight * (float)m_stack;
		}

		public HitData.DamageTypes GetDamage()
		{
			return GetDamage(m_quality);
		}

		public float GetDurabilityPercentage()
		{
			float maxDurability = GetMaxDurability();
			if (maxDurability == 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(m_durability / maxDurability);
		}

		public float GetMaxDurability()
		{
			return GetMaxDurability(m_quality);
		}

		public float GetMaxDurability(int quality)
		{
			return m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * m_shared.m_durabilityPerLevel;
		}

		public HitData.DamageTypes GetDamage(int quality)
		{
			HitData.DamageTypes damages = m_shared.m_damages;
			if (quality > 1)
			{
				damages.Add(m_shared.m_damagesPerLevel, quality - 1);
			}
			return damages;
		}

		public float GetBaseBlockPower()
		{
			return GetBaseBlockPower(m_quality);
		}

		public float GetBaseBlockPower(int quality)
		{
			return m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * m_shared.m_blockPowerPerLevel;
		}

		public float GetBlockPower(float skillFactor)
		{
			return GetBlockPower(m_quality, skillFactor);
		}

		public float GetBlockPower(int quality, float skillFactor)
		{
			float baseBlockPower = GetBaseBlockPower(quality);
			return baseBlockPower + baseBlockPower * skillFactor * 0.5f;
		}

		public float GetBlockPowerTooltip(int quality)
		{
			if (Player.m_localPlayer == null)
			{
				return 0f;
			}
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking);
			return GetBlockPower(quality, skillFactor);
		}

		public float GetHoldStaminaDrain()
		{
			if (m_shared.m_holdStaminaDrain <= 0f)
			{
				return 0f;
			}
			float holdStaminaDrain = m_shared.m_holdStaminaDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(m_shared.m_skillType);
			return holdStaminaDrain - holdStaminaDrain * 0.33f * skillFactor;
		}

		public float GetDeflectionForce()
		{
			return GetDeflectionForce(m_quality);
		}

		public float GetDeflectionForce(int quality)
		{
			return m_shared.m_deflectionForce + (float)Mathf.Max(0, quality - 1) * m_shared.m_deflectionForcePerLevel;
		}

		public string GetTooltip()
		{
			return GetTooltip(this, m_quality, crafting: false);
		}

		public Sprite GetIcon()
		{
			return m_shared.m_icons[m_variant];
		}

		private static void AddHandedTip(ItemData item, StringBuilder text)
		{
			switch (item.m_shared.m_itemType)
			{
			case ItemType.OneHandedWeapon:
			case ItemType.Shield:
			case ItemType.Torch:
				text.Append("\n$item_onehanded");
				break;
			case ItemType.Bow:
			case ItemType.TwoHandedWeapon:
			case ItemType.Tool:
				text.Append("\n$item_twohanded");
				break;
			}
		}

		private static void AddBlockTooltip(ItemData item, int qualityLevel, StringBuilder text)
		{
			text.AppendFormat("\n$item_blockarmor: <color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(qualityLevel), item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
			text.AppendFormat("\n$item_blockforce: <color=orange>{0}</color>", item.GetDeflectionForce(qualityLevel));
			if (item.m_shared.m_timedBlockBonus > 1f)
			{
				text.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
			}
			string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
			if (damageModifiersTooltipString.Length > 0)
			{
				text.Append(damageModifiersTooltipString);
			}
		}

		public static string GetTooltip(ItemData item, int qualityLevel, bool crafting)
		{
			Player localPlayer = Player.m_localPlayer;
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.Append(item.m_shared.m_description);
			stringBuilder.Append("\n\n");
			if (item.m_shared.m_dlc.Length > 0)
			{
				stringBuilder.Append("\n<color=aqua>$item_dlc</color>");
			}
			AddHandedTip(item, stringBuilder);
			if (item.m_crafterID != 0L)
			{
				stringBuilder.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.m_crafterName);
			}
			if (!item.m_shared.m_teleportable)
			{
				stringBuilder.Append("\n<color=orange>$item_noteleport</color>");
			}
			if (item.m_shared.m_value > 0)
			{
				stringBuilder.AppendFormat("\n$item_value: <color=orange>{0}  ({1})</color>", item.GetValue(), item.m_shared.m_value);
			}
			stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0}</color>", item.GetWeight().ToString("0.0"));
			if (item.m_shared.m_maxQuality > 1)
			{
				stringBuilder.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
			}
			if (item.m_shared.m_useDurability)
			{
				if (crafting)
				{
					float maxDurability = item.GetMaxDurability(qualityLevel);
					stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}</color>", maxDurability);
				}
				else
				{
					float maxDurability2 = item.GetMaxDurability(qualityLevel);
					float durability = item.m_durability;
					stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}%</color> <color=yellow>({1}/{2})</color>", (item.GetDurabilityPercentage() * 100f).ToString("0"), durability.ToString("0"), maxDurability2.ToString("0"));
				}
				if (item.m_shared.m_canBeReparied)
				{
					Recipe recipe = ObjectDB.instance.GetRecipe(item);
					if (recipe != null)
					{
						int minStationLevel = recipe.m_minStationLevel;
						stringBuilder.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
					}
				}
			}
			switch (item.m_shared.m_itemType)
			{
			case ItemType.Ammo:
				stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				break;
			case ItemType.OneHandedWeapon:
			case ItemType.Bow:
			case ItemType.TwoHandedWeapon:
			case ItemType.Torch:
			{
				stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				if (item.m_shared.m_attack.m_attackStamina > 0f)
				{
					stringBuilder.AppendFormat("\n$item_staminause: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackStamina);
				}
				if (item.m_shared.m_holdStaminaDrain > 0f)
				{
					stringBuilder.AppendFormat("\n$item_staminahold: <color=orange>{0}</color>/s", item.m_shared.m_holdStaminaDrain);
				}
				AddBlockTooltip(item, qualityLevel, stringBuilder);
				stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", item.m_shared.m_backstabBonus);
				if (item.m_shared.m_tamedOnly)
				{
					stringBuilder.AppendFormat("\n<color=orange>$item_tamedonly</color>");
				}
				string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
				if (projectileTooltip.Length > 0)
				{
					stringBuilder.Append("\n\n");
					stringBuilder.Append(projectileTooltip);
				}
				break;
			}
			case ItemType.Helmet:
			case ItemType.Chest:
			case ItemType.Legs:
			case ItemType.Shoulder:
			{
				stringBuilder.AppendFormat("\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel));
				string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
				if (damageModifiersTooltipString.Length > 0)
				{
					stringBuilder.Append(damageModifiersTooltipString);
				}
				break;
			}
			case ItemType.Shield:
				AddBlockTooltip(item, qualityLevel, stringBuilder);
				break;
			case ItemType.Consumable:
				if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f)
				{
					float maxHealth = localPlayer.GetMaxHealth();
					float maxStamina = localPlayer.GetMaxStamina();
					stringBuilder.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_food, maxHealth.ToString("0"));
					stringBuilder.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodStamina, maxStamina.ToString("0"));
					stringBuilder.AppendFormat("\n$item_food_duration: <color=orange>{0}</color>", GetDurationString(item.m_shared.m_foodBurnTime));
					stringBuilder.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
				}
				break;
			}
			string statusEffectTooltip = item.GetStatusEffectTooltip();
			if (statusEffectTooltip.Length > 0)
			{
				stringBuilder.Append("\n\n");
				stringBuilder.Append(statusEffectTooltip);
			}
			if (item.m_shared.m_movementModifier != 0f && localPlayer != null)
			{
				float equipmentMovementModifier = localPlayer.GetEquipmentMovementModifier();
				stringBuilder.AppendFormat("\n$item_movement_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_movementModifier * 100f).ToString("+0;-0"), (equipmentMovementModifier * 100f).ToString("+0;-0"));
			}
			string setStatusEffectTooltip = item.GetSetStatusEffectTooltip();
			if (setStatusEffectTooltip.Length > 0)
			{
				stringBuilder.AppendFormat("\n\n$item_seteffect (<color=orange>{0}</color> $item_parts):<color=orange>{1}</color>", item.m_shared.m_setSize, setStatusEffectTooltip);
			}
			return stringBuilder.ToString();
		}

		public static string GetDurationString(float time)
		{
			int num = Mathf.CeilToInt(time);
			int num2 = (int)((float)num / 60f);
			int num3 = Mathf.Max(0, num - num2 * 60);
			if (num2 > 0 && num3 > 0)
			{
				return num2 + "m " + num3 + "s";
			}
			if (num2 > 0)
			{
				return num2 + "m ";
			}
			return num3 + "s";
		}

		private string GetStatusEffectTooltip()
		{
			if ((bool)m_shared.m_attackStatusEffect)
			{
				return m_shared.m_attackStatusEffect.GetTooltipString();
			}
			if ((bool)m_shared.m_consumeStatusEffect)
			{
				return "<color=orange>" + m_shared.m_consumeStatusEffect.m_name + "</color>\n" + m_shared.m_consumeStatusEffect.GetTooltipString();
			}
			return "";
		}

		private string GetSetStatusEffectTooltip()
		{
			if ((bool)m_shared.m_setStatusEffect)
			{
				StatusEffect setStatusEffect = m_shared.m_setStatusEffect;
				if (setStatusEffect != null)
				{
					return setStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetProjectileTooltip(int itemQuality)
		{
			string text = "";
			if ((bool)m_shared.m_attack.m_attackProjectile)
			{
				IProjectile component = m_shared.m_attack.m_attackProjectile.GetComponent<IProjectile>();
				if (component != null)
				{
					text += component.GetTooltipString(itemQuality);
				}
			}
			if ((bool)m_shared.m_spawnOnHit)
			{
				IProjectile component2 = m_shared.m_spawnOnHit.GetComponent<IProjectile>();
				if (component2 != null)
				{
					text += component2.GetTooltipString(itemQuality);
				}
			}
			return text;
		}
	}

	private static List<ItemDrop> m_instances = new List<ItemDrop>();

	private int m_myIndex = -1;

	public bool m_autoPickup = true;

	public bool m_autoDestroy = true;

	public ItemData m_itemData = new ItemData();

	private Floating m_floating;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private Character m_pickupRequester;

	private float m_lastOwnerRequest;

	private float m_spawnTime;

	private uint m_loadedRevision;

	private const double m_autoDestroyTimeout = 3600.0;

	private const double m_autoPickupDelay = 0.5;

	private const float m_autoDespawnBaseMinAltitude = -2f;

	private const int m_autoStackTreshold = 200;

	private const float m_autoStackRange = 4f;

	private static int m_itemMask = 0;

	private bool m_haveAutoStacked;

	private void Awake()
	{
		m_myIndex = m_instances.Count;
		m_instances.Add(this);
		string prefabName = GetPrefabName(base.gameObject.name);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
		m_itemData.m_dropPrefab = itemPrefab;
		if (Application.isEditor)
		{
			m_itemData.m_shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		m_floating = GetComponent<Floating>();
		m_body = GetComponent<Rigidbody>();
		if ((bool)m_body)
		{
			m_body.maxDepenetrationVelocity = 1f;
		}
		m_spawnTime = Time.time;
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview && m_nview.IsValid())
		{
			if (m_nview.IsOwner() && new DateTime(m_nview.GetZDO().GetLong("SpawnTime", 0L)).Ticks == 0L)
			{
				m_nview.GetZDO().Set("SpawnTime", ZNet.instance.GetTime().Ticks);
			}
			m_nview.Register("RequestOwn", RPC_RequestOwn);
			Load();
			InvokeRepeating("SlowUpdate", UnityEngine.Random.Range(1f, 2f), 10f);
		}
	}

	private void OnDestroy()
	{
		m_instances[m_myIndex] = m_instances[m_instances.Count - 1];
		m_instances[m_myIndex].m_myIndex = m_myIndex;
		m_instances.RemoveAt(m_instances.Count - 1);
	}

	private void Start()
	{
		Save();
		base.gameObject.GetComponentInChildren<IEquipmentVisual>()?.Setup(m_itemData.m_variant);
	}

	private double GetTimeSinceSpawned()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong("SpawnTime", 0L));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	private void SlowUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			TerrainCheck();
			if (m_autoDestroy)
			{
				TimedDestruction();
			}
			if (m_instances.Count > 200)
			{
				AutoStackItems();
			}
		}
	}

	private void TerrainCheck()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -0.5f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			Rigidbody component = GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.velocity = Vector3.zero;
			}
		}
	}

	private void TimedDestruction()
	{
		if (!(GetTimeSinceSpawned() < 3600.0) && !IsInsideBase() && !Player.IsPlayerInRange(base.transform.position, 25f) && !InTar())
		{
			m_nview.Destroy();
		}
	}

	private bool IsInsideBase()
	{
		if (base.transform.position.y > ZoneSystem.instance.m_waterLevel + -2f && (bool)EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase))
		{
			return true;
		}
		return false;
	}

	private void AutoStackItems()
	{
		if (m_itemData.m_shared.m_maxStackSize <= 1 || m_itemData.m_stack >= m_itemData.m_shared.m_maxStackSize || m_haveAutoStacked)
		{
			return;
		}
		m_haveAutoStacked = true;
		if (m_itemMask == 0)
		{
			m_itemMask = LayerMask.GetMask("item");
		}
		bool flag = false;
		Collider[] array = Physics.OverlapSphere(base.transform.position, 4f, m_itemMask);
		foreach (Collider collider in array)
		{
			if (!collider.attachedRigidbody)
			{
				continue;
			}
			ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
			if (!(component == null) && !(component == this) && !(component.m_nview == null) && component.m_nview.IsValid() && component.m_nview.IsOwner() && !(component.m_itemData.m_shared.m_name != m_itemData.m_shared.m_name) && component.m_itemData.m_quality == m_itemData.m_quality)
			{
				int num = m_itemData.m_shared.m_maxStackSize - m_itemData.m_stack;
				if (num == 0)
				{
					break;
				}
				if (component.m_itemData.m_stack <= num)
				{
					m_itemData.m_stack += component.m_itemData.m_stack;
					flag = true;
					component.m_nview.Destroy();
				}
			}
		}
		if (flag)
		{
			Save();
		}
	}

	public string GetHoverText()
	{
		Load();
		string text = m_itemData.m_shared.m_name;
		if (m_itemData.m_quality > 1)
		{
			text = text + "[" + m_itemData.m_quality + "] ";
		}
		if (m_itemData.m_stack > 1)
		{
			text = text + " x" + m_itemData.m_stack;
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		return m_itemData.m_shared.m_name;
	}

	private string GetPrefabName(string name)
	{
		char[] anyOf = new char[2] { '(', ' ' };
		int num = name.IndexOfAny(anyOf);
		if (num >= 0)
		{
			return name.Substring(0, num);
		}
		return name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (InTar())
		{
			character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar");
			return true;
		}
		Pickup(character);
		return true;
	}

	public bool InTar()
	{
		if (m_body == null)
		{
			return false;
		}
		if (m_floating != null)
		{
			return m_floating.IsInTar();
		}
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.Tar);
		return worldCenterOfMass.y < liquidLevel;
	}

	public bool UseItem(Humanoid user, ItemData item)
	{
		return false;
	}

	public void SetStack(int stack)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_itemData.m_stack = stack;
			if (m_itemData.m_stack > m_itemData.m_shared.m_maxStackSize)
			{
				m_itemData.m_stack = m_itemData.m_shared.m_maxStackSize;
			}
			Save();
		}
	}

	public void Pickup(Humanoid character)
	{
		if (m_nview.IsValid())
		{
			if (CanPickup())
			{
				Load();
				character.Pickup(base.gameObject);
				Save();
			}
			else
			{
				m_pickupRequester = character;
				CancelInvoke("PickupUpdate");
				float num = 0.05f;
				InvokeRepeating("PickupUpdate", num, num);
				RequestOwn();
			}
		}
	}

	public void RequestOwn()
	{
		if (!(Time.time - m_lastOwnerRequest < 0.2f) && !m_nview.IsOwner())
		{
			m_lastOwnerRequest = Time.time;
			m_nview.InvokeRPC("RequestOwn");
		}
	}

	public bool RemoveOne()
	{
		if (!CanPickup())
		{
			RequestOwn();
			return false;
		}
		if (m_itemData.m_stack <= 1)
		{
			m_nview.Destroy();
			return true;
		}
		m_itemData.m_stack--;
		Save();
		return true;
	}

	public void OnPlayerDrop()
	{
		m_autoPickup = false;
	}

	public bool CanPickup()
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return true;
		}
		if ((double)(Time.time - m_spawnTime) < 0.5)
		{
			return false;
		}
		return m_nview.IsOwner();
	}

	private void RPC_RequestOwn(long uid)
	{
		ZLog.Log("Player " + uid + " wants to pickup " + base.gameObject.name + "   im: " + ZDOMan.instance.GetMyID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else
		{
			m_nview.GetZDO().SetOwner(uid);
		}
	}

	private void PickupUpdate()
	{
		if (m_nview.IsValid())
		{
			if (CanPickup())
			{
				ZLog.Log("Im finally the owner");
				CancelInvoke("PickupUpdate");
				Load();
				(m_pickupRequester as Player).Pickup(base.gameObject);
				Save();
			}
			else
			{
				ZLog.Log("Im still nto the owner");
			}
		}
	}

	private void Save()
	{
		if (!(m_nview == null) && m_nview.IsValid() && m_nview.IsOwner())
		{
			SaveToZDO(m_itemData, m_nview.GetZDO());
		}
	}

	public void Load()
	{
		if (!(m_nview == null) && m_nview.IsValid())
		{
			ZDO zDO = m_nview.GetZDO();
			if (zDO.m_dataRevision != m_loadedRevision)
			{
				m_loadedRevision = zDO.m_dataRevision;
				LoadFromZDO(m_itemData, zDO);
			}
		}
	}

	public void LoadFromExternalZDO(ZDO zdo)
	{
		LoadFromZDO(m_itemData, zdo);
		SaveToZDO(m_itemData, m_nview.GetZDO());
	}

	public static void SaveToZDO(ItemData itemData, ZDO zdo)
	{
		zdo.Set("durability", itemData.m_durability);
		zdo.Set("stack", itemData.m_stack);
		zdo.Set("quality", itemData.m_quality);
		zdo.Set("variant", itemData.m_variant);
		zdo.Set("crafterID", itemData.m_crafterID);
		zdo.Set("crafterName", itemData.m_crafterName);
	}

	public static void LoadFromZDO(ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat("durability", itemData.m_durability);
		itemData.m_stack = zdo.GetInt("stack", itemData.m_stack);
		itemData.m_quality = zdo.GetInt("quality", itemData.m_quality);
		itemData.m_variant = zdo.GetInt("variant", itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong("crafterID", itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString("crafterName", itemData.m_crafterName);
	}

	public static ItemDrop DropItem(ItemData item, int amount, Vector3 position, Quaternion rotation)
	{
		ItemDrop component = UnityEngine.Object.Instantiate(item.m_dropPrefab, position, rotation).GetComponent<ItemDrop>();
		component.m_itemData = item.Clone();
		if (amount > 0)
		{
			component.m_itemData.m_stack = amount;
		}
		component.Save();
		return component;
	}

	private void OnDrawGizmos()
	{
	}
}
