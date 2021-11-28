using System;
using System.Collections.Generic;
using UnityEngine;

public class Humanoid : Character
{
	[Serializable]
	public class ItemSet
	{
		public string m_name = "";

		public GameObject[] m_items = new GameObject[0];
	}

	private static List<ItemDrop.ItemData> optimalWeapons = new List<ItemDrop.ItemData>();

	private static List<ItemDrop.ItemData> outofRangeWeapons = new List<ItemDrop.ItemData>();

	private static List<ItemDrop.ItemData> allWeapons = new List<ItemDrop.ItemData>();

	[Header("Humanoid")]
	public float m_equipStaminaDrain = 10f;

	public float m_blockStaminaDrain = 25f;

	[Header("Default items")]
	public GameObject[] m_defaultItems;

	public GameObject[] m_randomWeapon;

	public GameObject[] m_randomArmor;

	public GameObject[] m_randomShield;

	public ItemSet[] m_randomSets;

	public ItemDrop m_unarmedWeapon;

	[Header("Effects")]
	public EffectList m_pickupEffects = new EffectList();

	public EffectList m_dropEffects = new EffectList();

	public EffectList m_consumeItemEffects = new EffectList();

	public EffectList m_equipEffects = new EffectList();

	public EffectList m_perfectBlockEffect = new EffectList();

	protected Inventory m_inventory = new Inventory("Inventory", null, 8, 4);

	protected ItemDrop.ItemData m_rightItem;

	protected ItemDrop.ItemData m_leftItem;

	protected ItemDrop.ItemData m_chestItem;

	protected ItemDrop.ItemData m_legItem;

	protected ItemDrop.ItemData m_ammoItem;

	protected ItemDrop.ItemData m_helmetItem;

	protected ItemDrop.ItemData m_shoulderItem;

	protected ItemDrop.ItemData m_utilityItem;

	protected string m_beardItem = "";

	protected string m_hairItem = "";

	private int m_lastEquipEffectFrame;

	protected ItemDrop.ItemData m_hiddenLeftItem;

	protected ItemDrop.ItemData m_hiddenRightItem;

	protected Attack m_currentAttack;

	protected Attack m_previousAttack;

	private float m_timeSinceLastAttack;

	private bool m_internalBlockingState;

	private float m_blockTimer = 9999f;

	private const float m_perfectBlockInterval = 0.25f;

	protected float m_attackHoldTime;

	protected float m_lastCombatTimer = 999f;

	protected VisEquipment m_visEquipment;

	private static int statef = 0;

	private static int statei = 0;

	private static int blocking = 0;

	private static int isBlockingHash = 0;

	private HashSet<StatusEffect> m_eqipmentStatusEffects = new HashSet<StatusEffect>();

	protected static int m_animatorTagAttack = Animator.StringToHash("attack");

	protected override void Awake()
	{
		base.Awake();
		m_visEquipment = GetComponent<VisEquipment>();
		if (statef == 0)
		{
			statef = ZSyncAnimation.GetHash("statef");
			statei = ZSyncAnimation.GetHash("statei");
			blocking = ZSyncAnimation.GetHash("blocking");
		}
		if (isBlockingHash == 0)
		{
			isBlockingHash = "IsBlocking".GetStableHashCode();
		}
	}

	protected override void Start()
	{
		base.Start();
		if (!IsPlayer())
		{
			GiveDefaultItems();
		}
	}

	public void GiveDefaultItems()
	{
		GameObject[] defaultItems = m_defaultItems;
		foreach (GameObject prefab in defaultItems)
		{
			GiveDefaultItem(prefab);
		}
		if (m_randomWeapon.Length == 0 && m_randomArmor.Length == 0 && m_randomShield.Length == 0 && m_randomSets.Length == 0)
		{
			return;
		}
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_nview.GetZDO().m_uid.GetHashCode());
		if (m_randomShield.Length != 0)
		{
			GameObject gameObject = m_randomShield[UnityEngine.Random.Range(0, m_randomShield.Length)];
			if ((bool)gameObject)
			{
				GiveDefaultItem(gameObject);
			}
		}
		if (m_randomWeapon.Length != 0)
		{
			GameObject gameObject2 = m_randomWeapon[UnityEngine.Random.Range(0, m_randomWeapon.Length)];
			if ((bool)gameObject2)
			{
				GiveDefaultItem(gameObject2);
			}
		}
		if (m_randomArmor.Length != 0)
		{
			GameObject gameObject3 = m_randomArmor[UnityEngine.Random.Range(0, m_randomArmor.Length)];
			if ((bool)gameObject3)
			{
				GiveDefaultItem(gameObject3);
			}
		}
		if (m_randomSets.Length != 0)
		{
			defaultItems = m_randomSets[UnityEngine.Random.Range(0, m_randomSets.Length)].m_items;
			foreach (GameObject prefab2 in defaultItems)
			{
				GiveDefaultItem(prefab2);
			}
		}
		UnityEngine.Random.state = state;
	}

	private void GiveDefaultItem(GameObject prefab)
	{
		ItemDrop.ItemData itemData = PickupPrefab(prefab, 0, autoequip: false);
		if (itemData != null && !itemData.IsWeapon())
		{
			EquipItem(itemData, triggerEquipEffects: false);
		}
	}

	protected override void FixedUpdate()
	{
		if (m_nview.IsValid())
		{
			if (m_nview == null || m_nview.IsOwner())
			{
				UpdateAttack(Time.fixedDeltaTime);
				UpdateEquipment(Time.fixedDeltaTime);
				UpdateBlock(Time.fixedDeltaTime);
			}
			base.FixedUpdate();
		}
	}

	public override bool InAttack()
	{
		if (m_animator.IsInTransition(0))
		{
			if (m_animator.GetCurrentAnimatorStateInfo(0).tagHash == m_animatorTagAttack)
			{
				return true;
			}
			if (m_animator.GetNextAnimatorStateInfo(0).tagHash == m_animatorTagAttack)
			{
				return true;
			}
			return false;
		}
		if (m_animator.GetCurrentAnimatorStateInfo(0).tagHash == m_animatorTagAttack)
		{
			return true;
		}
		return false;
	}

	public override bool StartAttack(Character target, bool secondaryAttack)
	{
		AbortEquipQueue();
		if ((InAttack() && !HaveQueuedChain()) || InDodge() || !CanMove() || IsKnockedBack() || IsStaggering() || InMinorAction())
		{
			return false;
		}
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (m_currentAttack != null)
		{
			m_currentAttack.Stop();
			m_previousAttack = m_currentAttack;
			m_currentAttack = null;
		}
		Attack attack;
		if (secondaryAttack)
		{
			if (!currentWeapon.HaveSecondaryAttack())
			{
				return false;
			}
			attack = currentWeapon.m_shared.m_secondaryAttack.Clone();
		}
		else
		{
			if (!currentWeapon.HavePrimaryAttack())
			{
				return false;
			}
			attack = currentWeapon.m_shared.m_attack.Clone();
		}
		if (attack.Start(this, m_body, m_zanim, m_animEvent, m_visEquipment, currentWeapon, m_previousAttack, m_timeSinceLastAttack, GetAttackDrawPercentage()))
		{
			m_currentAttack = attack;
			m_lastCombatTimer = 0f;
			return true;
		}
		return false;
	}

	public float GetAttackDrawPercentage()
	{
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if (currentWeapon != null && currentWeapon.m_shared.m_holdDurationMin > 0f && m_attackHoldTime > 0f)
		{
			float skillFactor = GetSkillFactor(currentWeapon.m_shared.m_skillType);
			float num = Mathf.Lerp(currentWeapon.m_shared.m_holdDurationMin, currentWeapon.m_shared.m_holdDurationMin * 0.2f, skillFactor);
			if (!(num > 0f))
			{
				return 1f;
			}
			return Mathf.Clamp01(m_attackHoldTime / num);
		}
		return 0f;
	}

	private void UpdateEquipment(float dt)
	{
		if (IsPlayer())
		{
			if (IsSwiming() && !IsOnGround())
			{
				HideHandItems();
			}
			if (m_rightItem != null && m_rightItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_rightItem, dt);
			}
			if (m_leftItem != null && m_leftItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_leftItem, dt);
			}
			if (m_chestItem != null && m_chestItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_chestItem, dt);
			}
			if (m_legItem != null && m_legItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_legItem, dt);
			}
			if (m_helmetItem != null && m_helmetItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_helmetItem, dt);
			}
			if (m_shoulderItem != null && m_shoulderItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_shoulderItem, dt);
			}
			if (m_utilityItem != null && m_utilityItem.m_shared.m_useDurability)
			{
				DrainEquipedItemDurability(m_utilityItem, dt);
			}
		}
	}

	private void DrainEquipedItemDurability(ItemDrop.ItemData item, float dt)
	{
		item.m_durability -= item.m_shared.m_durabilityDrain * dt;
		if (item.m_durability <= 0f)
		{
			Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_broke", item.m_shared.m_name), 0, item.GetIcon());
			UnequipItem(item, triggerEquipEffects: false);
			if (item.m_shared.m_destroyBroken)
			{
				m_inventory.RemoveItem(item);
			}
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		SetCrouch(crouch: false);
	}

	public ItemDrop.ItemData GetCurrentWeapon()
	{
		if (m_rightItem != null && m_rightItem.IsWeapon())
		{
			return m_rightItem;
		}
		if (m_leftItem != null && m_leftItem.IsWeapon() && m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
		{
			return m_leftItem;
		}
		if ((bool)m_unarmedWeapon)
		{
			return m_unarmedWeapon.m_itemData;
		}
		return null;
	}

	protected ItemDrop.ItemData GetCurrentBlocker()
	{
		if (m_leftItem != null)
		{
			return m_leftItem;
		}
		return GetCurrentWeapon();
	}

	private void UpdateAttack(float dt)
	{
		m_lastCombatTimer += dt;
		if (GetCurrentWeapon() != null && m_currentAttack != null)
		{
			m_currentAttack.Update(dt);
		}
		if (InAttack())
		{
			m_timeSinceLastAttack = 0f;
		}
		else
		{
			m_timeSinceLastAttack += dt;
		}
	}

	protected override float GetAttackSpeedFactorMovement()
	{
		if (InAttack() && m_currentAttack != null)
		{
			if (!IsFlying() && !IsOnGround())
			{
				return 1f;
			}
			return m_currentAttack.m_speedFactor;
		}
		return 1f;
	}

	protected override float GetAttackSpeedFactorRotation()
	{
		if (InAttack() && m_currentAttack != null)
		{
			return m_currentAttack.m_speedFactorRotation;
		}
		return 1f;
	}

	protected virtual bool HaveQueuedChain()
	{
		return false;
	}

	public override void OnWeaponTrailStart()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && GetCurrentWeapon() != null && m_currentAttack != null)
		{
			m_currentAttack.OnTrailStart();
		}
	}

	public override void OnAttackTrigger()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && GetCurrentWeapon() != null && m_currentAttack != null)
		{
			m_currentAttack.OnAttackTrigger();
		}
	}

	public override void OnStopMoving()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && InAttack() && GetCurrentWeapon() != null && m_currentAttack != null)
		{
			m_currentAttack.m_speedFactor = 0f;
			m_currentAttack.m_speedFactorRotation = 0f;
		}
	}

	public virtual Vector3 GetAimDir(Vector3 fromPoint)
	{
		return GetLookDir();
	}

	public ItemDrop.ItemData PickupPrefab(GameObject prefab, int stackSize = 0, bool autoequip = true)
	{
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
		ZNetView.m_forceDisableInit = false;
		if (stackSize > 0)
		{
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			component.m_itemData.m_stack = Mathf.Clamp(stackSize, 1, component.m_itemData.m_shared.m_maxStackSize);
		}
		if (Pickup(gameObject, autoequip))
		{
			return gameObject.GetComponent<ItemDrop>().m_itemData;
		}
		UnityEngine.Object.Destroy(gameObject);
		return null;
	}

	public virtual bool HaveUniqueKey(string name)
	{
		return false;
	}

	public virtual void AddUniqueKey(string name)
	{
	}

	public bool Pickup(GameObject go, bool autoequip = true)
	{
		if (IsTeleporting())
		{
			return false;
		}
		ItemDrop component = go.GetComponent<ItemDrop>();
		if (component == null)
		{
			return false;
		}
		component.Load();
		if (IsPlayer() && (component.m_itemData.m_shared.m_icons == null || component.m_itemData.m_shared.m_icons.Length == 0 || component.m_itemData.m_variant >= component.m_itemData.m_shared.m_icons.Length))
		{
			return false;
		}
		if (!component.CanPickup())
		{
			return false;
		}
		if (m_inventory.ContainsItem(component.m_itemData))
		{
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem && HaveUniqueKey(component.m_itemData.m_shared.m_name))
		{
			Message(MessageHud.MessageType.Center, "$msg_cantpickup");
			return false;
		}
		int stack = component.m_itemData.m_stack;
		bool flag = m_inventory.AddItem(component.m_itemData);
		if (m_nview.GetZDO() == null)
		{
			UnityEngine.Object.Destroy(go);
			return true;
		}
		if (!flag)
		{
			Message(MessageHud.MessageType.Center, "$msg_noroom");
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem)
		{
			AddUniqueKey(component.m_itemData.m_shared.m_name);
		}
		ZNetScene.instance.Destroy(go);
		if (autoequip && flag && IsPlayer() && m_rightItem == null && m_hiddenRightItem == null && component.m_itemData.IsWeapon())
		{
			EquipItem(component.m_itemData);
		}
		m_pickupEffects.Create(base.transform.position, Quaternion.identity);
		if (IsPlayer())
		{
			ShowPickupMessage(component.m_itemData, stack);
		}
		return flag;
	}

	public void EquipBestWeapon(Character targetCreature, StaticTarget targetStatic, Character hurtFriend, Character friend)
	{
		List<ItemDrop.ItemData> allItems = m_inventory.GetAllItems();
		if (allItems.Count == 0 || InAttack())
		{
			return;
		}
		float num = 0f;
		if ((bool)targetCreature)
		{
			float radius = targetCreature.GetRadius();
			num = Vector3.Distance(targetCreature.transform.position, base.transform.position) - radius;
		}
		else if ((bool)targetStatic)
		{
			num = Vector3.Distance(targetStatic.transform.position, base.transform.position);
		}
		float time = Time.time;
		IsFlying();
		IsSwiming();
		optimalWeapons.Clear();
		outofRangeWeapons.Clear();
		allWeapons.Clear();
		foreach (ItemDrop.ItemData item in allItems)
		{
			if (!item.IsWeapon() || !BaseAI.CanUseAttack(this, item))
			{
				continue;
			}
			if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
			{
				if (num < item.m_shared.m_aiAttackRangeMin)
				{
					continue;
				}
				allWeapons.Add(item);
				if ((targetCreature == null && targetStatic == null) || time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval)
				{
					continue;
				}
				if (num > item.m_shared.m_aiAttackRange)
				{
					outofRangeWeapons.Add(item);
					continue;
				}
				if (item.m_shared.m_aiPrioritized)
				{
					EquipItem(item);
					return;
				}
				optimalWeapons.Add(item);
			}
			else if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt)
			{
				if (!(hurtFriend == null) && !(time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval))
				{
					if (item.m_shared.m_aiPrioritized)
					{
						EquipItem(item);
						return;
					}
					optimalWeapons.Add(item);
				}
			}
			else if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend && !(friend == null) && !(time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval))
			{
				if (item.m_shared.m_aiPrioritized)
				{
					EquipItem(item);
					return;
				}
				optimalWeapons.Add(item);
			}
		}
		if (optimalWeapons.Count > 0)
		{
			EquipItem(optimalWeapons[UnityEngine.Random.Range(0, optimalWeapons.Count)]);
			return;
		}
		if (outofRangeWeapons.Count > 0)
		{
			EquipItem(outofRangeWeapons[UnityEngine.Random.Range(0, outofRangeWeapons.Count)]);
			return;
		}
		if (allWeapons.Count > 0)
		{
			EquipItem(allWeapons[UnityEngine.Random.Range(0, allWeapons.Count)]);
			return;
		}
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if (currentWeapon != null)
		{
			UnequipItem(currentWeapon, triggerEquipEffects: false);
		}
	}

	public bool DropItem(Inventory inventory, ItemDrop.ItemData item, int amount)
	{
		if (amount == 0)
		{
			return false;
		}
		if (item.m_shared.m_questItem)
		{
			Message(MessageHud.MessageType.Center, "$msg_cantdrop");
			return false;
		}
		if (amount > item.m_stack)
		{
			amount = item.m_stack;
		}
		RemoveFromEquipQueue(item);
		UnequipItem(item, triggerEquipEffects: false);
		if (m_hiddenLeftItem == item)
		{
			m_hiddenLeftItem = null;
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
		}
		if (m_hiddenRightItem == item)
		{
			m_hiddenRightItem = null;
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
		}
		if (amount == item.m_stack)
		{
			ZLog.Log("drop all " + amount + "  " + item.m_stack);
			if (!inventory.RemoveItem(item))
			{
				ZLog.Log("Was not removed");
				return false;
			}
		}
		else
		{
			ZLog.Log("drop some " + amount + "  " + item.m_stack);
			inventory.RemoveItem(item, amount);
		}
		ItemDrop itemDrop = ItemDrop.DropItem(item, amount, base.transform.position + base.transform.forward + base.transform.up, base.transform.rotation);
		if (IsPlayer())
		{
			itemDrop.OnPlayerDrop();
		}
		itemDrop.GetComponent<Rigidbody>().velocity = (base.transform.forward + Vector3.up) * 5f;
		m_zanim.SetTrigger("interact");
		m_dropEffects.Create(base.transform.position, Quaternion.identity);
		Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + itemDrop.m_itemData.m_shared.m_name, itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
		return true;
	}

	protected virtual void SetPlaceMode(PieceTable buildPieces)
	{
	}

	public Inventory GetInventory()
	{
		return m_inventory;
	}

	public void UseItem(Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
	{
		if (inventory == null)
		{
			inventory = m_inventory;
		}
		if (!inventory.ContainsItem(item))
		{
			return;
		}
		GameObject hoverObject = GetHoverObject();
		Hoverable hoverable = (hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null);
		if (hoverable != null && !fromInventoryGui)
		{
			Interactable componentInParent = hoverObject.GetComponentInParent<Interactable>();
			if (componentInParent != null && componentInParent.UseItem(this, item))
			{
				return;
			}
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
		{
			if (ConsumeItem(inventory, item))
			{
				m_consumeItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
				m_zanim.SetTrigger("eat");
			}
		}
		else if ((inventory != m_inventory || !ToggleEquiped(item)) && !fromInventoryGui)
		{
			if (hoverable != null)
			{
				Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantuseon", item.m_shared.m_name, hoverable.GetHoverName()));
			}
			else
			{
				Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_useonwhat", item.m_shared.m_name));
			}
		}
	}

	public virtual void AbortEquipQueue()
	{
	}

	public virtual void RemoveFromEquipQueue(ItemDrop.ItemData item)
	{
	}

	protected virtual bool ToggleEquiped(ItemDrop.ItemData item)
	{
		if (item.IsEquipable())
		{
			if (InAttack())
			{
				return true;
			}
			if (IsItemEquiped(item))
			{
				UnequipItem(item);
			}
			else
			{
				EquipItem(item);
			}
			return true;
		}
		return false;
	}

	public virtual bool CanConsumeItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		{
			return false;
		}
		return true;
	}

	public virtual bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
	{
		CanConsumeItem(item);
		return false;
	}

	public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (IsItemEquiped(item))
		{
			return false;
		}
		if (!m_inventory.ContainsItem(item))
		{
			return false;
		}
		if (InAttack() || InDodge())
		{
			return false;
		}
		if (IsPlayer() && !IsDead() && IsSwiming() && !IsOnGround())
		{
			return false;
		}
		if (item.m_shared.m_useDurability && item.m_durability <= 0f)
		{
			return false;
		}
		if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc))
		{
			Message(MessageHud.MessageType.Center, "$msg_dlcrequired");
			return false;
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
		{
			UnequipItem(m_rightItem, triggerEquipEffects);
			UnequipItem(m_leftItem, triggerEquipEffects);
			m_rightItem = item;
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
		{
			if (m_rightItem != null && m_leftItem == null && m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
			{
				m_leftItem = item;
			}
			else
			{
				UnequipItem(m_rightItem, triggerEquipEffects);
				if (m_leftItem != null && m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
				{
					UnequipItem(m_leftItem, triggerEquipEffects);
				}
				m_rightItem = item;
			}
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
		{
			if (m_rightItem != null && m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && m_leftItem == null)
			{
				ItemDrop.ItemData rightItem = m_rightItem;
				UnequipItem(m_rightItem, triggerEquipEffects);
				m_leftItem = rightItem;
				m_leftItem.m_equiped = true;
			}
			UnequipItem(m_rightItem, triggerEquipEffects);
			if (m_leftItem != null && m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield && m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				UnequipItem(m_leftItem, triggerEquipEffects);
			}
			m_rightItem = item;
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
		{
			UnequipItem(m_leftItem, triggerEquipEffects);
			if (m_rightItem != null && m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon && m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				UnequipItem(m_rightItem, triggerEquipEffects);
			}
			m_leftItem = item;
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
		{
			UnequipItem(m_leftItem, triggerEquipEffects);
			UnequipItem(m_rightItem, triggerEquipEffects);
			m_leftItem = item;
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
		{
			UnequipItem(m_leftItem, triggerEquipEffects);
			UnequipItem(m_rightItem, triggerEquipEffects);
			m_rightItem = item;
			m_hiddenRightItem = null;
			m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
		{
			UnequipItem(m_chestItem, triggerEquipEffects);
			m_chestItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
		{
			UnequipItem(m_legItem, triggerEquipEffects);
			m_legItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
		{
			UnequipItem(m_ammoItem, triggerEquipEffects);
			m_ammoItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
		{
			UnequipItem(m_helmetItem, triggerEquipEffects);
			m_helmetItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
		{
			UnequipItem(m_shoulderItem, triggerEquipEffects);
			m_shoulderItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
		{
			UnequipItem(m_utilityItem, triggerEquipEffects);
			m_utilityItem = item;
		}
		if (IsItemEquiped(item))
		{
			item.m_equiped = true;
		}
		SetupEquipment();
		if (triggerEquipEffects)
		{
			TriggerEquipEffect(item);
		}
		return true;
	}

	public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (item == null)
		{
			return;
		}
		if (m_hiddenLeftItem == item)
		{
			m_hiddenLeftItem = null;
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
		}
		if (m_hiddenRightItem == item)
		{
			m_hiddenRightItem = null;
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
		}
		if (!IsItemEquiped(item))
		{
			return;
		}
		if (item.IsWeapon())
		{
			if (m_currentAttack != null && m_currentAttack.GetWeapon() == item)
			{
				m_currentAttack.Stop();
				m_previousAttack = m_currentAttack;
				m_currentAttack = null;
			}
			if (!string.IsNullOrEmpty(item.m_shared.m_holdAnimationState))
			{
				m_zanim.SetBool(item.m_shared.m_holdAnimationState, value: false);
			}
			m_attackHoldTime = 0f;
		}
		if (m_rightItem == item)
		{
			m_rightItem = null;
		}
		else if (m_leftItem == item)
		{
			m_leftItem = null;
		}
		else if (m_chestItem == item)
		{
			m_chestItem = null;
		}
		else if (m_legItem == item)
		{
			m_legItem = null;
		}
		else if (m_ammoItem == item)
		{
			m_ammoItem = null;
		}
		else if (m_helmetItem == item)
		{
			m_helmetItem = null;
		}
		else if (m_shoulderItem == item)
		{
			m_shoulderItem = null;
		}
		else if (m_utilityItem == item)
		{
			m_utilityItem = null;
		}
		item.m_equiped = false;
		SetupEquipment();
		if (triggerEquipEffects)
		{
			TriggerEquipEffect(item);
		}
	}

	private void TriggerEquipEffect(ItemDrop.ItemData item)
	{
		if (m_nview.GetZDO() != null && Time.frameCount != m_lastEquipEffectFrame)
		{
			m_lastEquipEffectFrame = Time.frameCount;
			m_equipEffects.Create(base.transform.position, Quaternion.identity);
		}
	}

	public void UnequipAllItems()
	{
		if (m_rightItem != null)
		{
			UnequipItem(m_rightItem, triggerEquipEffects: false);
		}
		if (m_leftItem != null)
		{
			UnequipItem(m_leftItem, triggerEquipEffects: false);
		}
		if (m_chestItem != null)
		{
			UnequipItem(m_chestItem, triggerEquipEffects: false);
		}
		if (m_legItem != null)
		{
			UnequipItem(m_legItem, triggerEquipEffects: false);
		}
		if (m_helmetItem != null)
		{
			UnequipItem(m_helmetItem, triggerEquipEffects: false);
		}
		if (m_ammoItem != null)
		{
			UnequipItem(m_ammoItem, triggerEquipEffects: false);
		}
		if (m_shoulderItem != null)
		{
			UnequipItem(m_shoulderItem, triggerEquipEffects: false);
		}
		if (m_utilityItem != null)
		{
			UnequipItem(m_utilityItem, triggerEquipEffects: false);
		}
	}

	protected override void OnRagdollCreated(Ragdoll ragdoll)
	{
		VisEquipment component = ragdoll.GetComponent<VisEquipment>();
		if ((bool)component)
		{
			SetupVisEquipment(component, isRagdoll: true);
		}
	}

	protected virtual void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		if (!isRagdoll)
		{
			visEq.SetLeftItem((m_leftItem != null) ? m_leftItem.m_dropPrefab.name : "", (m_leftItem != null) ? m_leftItem.m_variant : 0);
			visEq.SetRightItem((m_rightItem != null) ? m_rightItem.m_dropPrefab.name : "");
			if (IsPlayer())
			{
				visEq.SetLeftBackItem((m_hiddenLeftItem != null) ? m_hiddenLeftItem.m_dropPrefab.name : "", (m_hiddenLeftItem != null) ? m_hiddenLeftItem.m_variant : 0);
				visEq.SetRightBackItem((m_hiddenRightItem != null) ? m_hiddenRightItem.m_dropPrefab.name : "");
			}
		}
		visEq.SetChestItem((m_chestItem != null) ? m_chestItem.m_dropPrefab.name : "");
		visEq.SetLegItem((m_legItem != null) ? m_legItem.m_dropPrefab.name : "");
		visEq.SetHelmetItem((m_helmetItem != null) ? m_helmetItem.m_dropPrefab.name : "");
		visEq.SetShoulderItem((m_shoulderItem != null) ? m_shoulderItem.m_dropPrefab.name : "", (m_shoulderItem != null) ? m_shoulderItem.m_variant : 0);
		visEq.SetUtilityItem((m_utilityItem != null) ? m_utilityItem.m_dropPrefab.name : "");
		if (IsPlayer())
		{
			visEq.SetBeardItem(m_beardItem);
			visEq.SetHairItem(m_hairItem);
		}
	}

	private void SetupEquipment()
	{
		if ((bool)m_visEquipment && (m_nview.GetZDO() == null || m_nview.IsOwner()))
		{
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
		}
		if (m_nview.GetZDO() != null)
		{
			UpdateEquipmentStatusEffects();
			if (m_rightItem != null && (bool)m_rightItem.m_shared.m_buildPieces)
			{
				SetPlaceMode(m_rightItem.m_shared.m_buildPieces);
			}
			else
			{
				SetPlaceMode(null);
			}
			SetupAnimationState();
		}
	}

	private void SetupAnimationState()
	{
		if (m_leftItem != null)
		{
			if (m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
			{
				SetAnimationState(ItemDrop.ItemData.AnimationState.LeftTorch);
			}
			else
			{
				SetAnimationState(m_leftItem.m_shared.m_animationState);
			}
		}
		else if (m_rightItem != null)
		{
			SetAnimationState(m_rightItem.m_shared.m_animationState);
		}
		else if (m_unarmedWeapon != null)
		{
			SetAnimationState(m_unarmedWeapon.m_itemData.m_shared.m_animationState);
		}
	}

	private void SetAnimationState(ItemDrop.ItemData.AnimationState state)
	{
		m_zanim.SetFloat(statef, (float)state);
		m_zanim.SetInt(statei, (int)state);
	}

	public override bool IsSitting()
	{
		return m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Character.m_animatorTagSitting;
	}

	private void UpdateEquipmentStatusEffects()
	{
		HashSet<StatusEffect> hashSet = new HashSet<StatusEffect>();
		if (m_leftItem != null && (bool)m_leftItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_leftItem.m_shared.m_equipStatusEffect);
		}
		if (m_rightItem != null && (bool)m_rightItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_rightItem.m_shared.m_equipStatusEffect);
		}
		if (m_chestItem != null && (bool)m_chestItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_chestItem.m_shared.m_equipStatusEffect);
		}
		if (m_legItem != null && (bool)m_legItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_legItem.m_shared.m_equipStatusEffect);
		}
		if (m_helmetItem != null && (bool)m_helmetItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_helmetItem.m_shared.m_equipStatusEffect);
		}
		if (m_shoulderItem != null && (bool)m_shoulderItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_shoulderItem.m_shared.m_equipStatusEffect);
		}
		if (m_utilityItem != null && (bool)m_utilityItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(m_utilityItem.m_shared.m_equipStatusEffect);
		}
		if (HaveSetEffect(m_leftItem))
		{
			hashSet.Add(m_leftItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_rightItem))
		{
			hashSet.Add(m_rightItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_chestItem))
		{
			hashSet.Add(m_chestItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_legItem))
		{
			hashSet.Add(m_legItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_helmetItem))
		{
			hashSet.Add(m_helmetItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_shoulderItem))
		{
			hashSet.Add(m_shoulderItem.m_shared.m_setStatusEffect);
		}
		if (HaveSetEffect(m_utilityItem))
		{
			hashSet.Add(m_utilityItem.m_shared.m_setStatusEffect);
		}
		foreach (StatusEffect eqipmentStatusEffect in m_eqipmentStatusEffects)
		{
			if (!hashSet.Contains(eqipmentStatusEffect))
			{
				m_seman.RemoveStatusEffect(eqipmentStatusEffect.name);
			}
		}
		foreach (StatusEffect item in hashSet)
		{
			if (!m_eqipmentStatusEffects.Contains(item))
			{
				m_seman.AddStatusEffect(item);
			}
		}
		m_eqipmentStatusEffects.Clear();
		m_eqipmentStatusEffects.UnionWith(hashSet);
	}

	private bool HaveSetEffect(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		if (item.m_shared.m_setStatusEffect == null || item.m_shared.m_setName.Length == 0 || item.m_shared.m_setSize <= 1)
		{
			return false;
		}
		if (GetSetCount(item.m_shared.m_setName) >= item.m_shared.m_setSize)
		{
			return true;
		}
		return false;
	}

	private int GetSetCount(string setName)
	{
		int num = 0;
		if (m_leftItem != null && m_leftItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_rightItem != null && m_rightItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_chestItem != null && m_chestItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_legItem != null && m_legItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_helmetItem != null && m_helmetItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_shoulderItem != null && m_shoulderItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (m_utilityItem != null && m_utilityItem.m_shared.m_setName == setName)
		{
			num++;
		}
		return num;
	}

	public void SetBeard(string name)
	{
		m_beardItem = name;
		SetupEquipment();
	}

	public string GetBeard()
	{
		return m_beardItem;
	}

	public void SetHair(string hair)
	{
		m_hairItem = hair;
		SetupEquipment();
	}

	public string GetHair()
	{
		return m_hairItem;
	}

	public bool IsItemEquiped(ItemDrop.ItemData item)
	{
		if (m_rightItem == item)
		{
			return true;
		}
		if (m_leftItem == item)
		{
			return true;
		}
		if (m_chestItem == item)
		{
			return true;
		}
		if (m_legItem == item)
		{
			return true;
		}
		if (m_ammoItem == item)
		{
			return true;
		}
		if (m_helmetItem == item)
		{
			return true;
		}
		if (m_shoulderItem == item)
		{
			return true;
		}
		if (m_utilityItem == item)
		{
			return true;
		}
		return false;
	}

	public ItemDrop.ItemData GetRightItem()
	{
		return m_rightItem;
	}

	public ItemDrop.ItemData GetLeftItem()
	{
		return m_leftItem;
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (IsHoldingAttack())
		{
			return false;
		}
		if (IsBlocking())
		{
			return false;
		}
		return base.CheckRun(moveDir, dt);
	}

	public override bool IsHoldingAttack()
	{
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if (currentWeapon != null && currentWeapon.m_shared.m_holdDurationMin > 0f && m_attackHoldTime > 0f)
		{
			return true;
		}
		return false;
	}

	protected override bool BlockAttack(HitData hit, Character attacker)
	{
		if (Vector3.Dot(hit.m_dir, base.transform.forward) > 0f)
		{
			return false;
		}
		ItemDrop.ItemData currentBlocker = GetCurrentBlocker();
		if (currentBlocker == null)
		{
			return false;
		}
		bool flag = currentBlocker.m_shared.m_timedBlockBonus > 1f && m_blockTimer != -1f && m_blockTimer < 0.25f;
		float skillFactor = GetSkillFactor(Skills.SkillType.Blocking);
		float num = currentBlocker.GetBlockPower(skillFactor);
		if (flag)
		{
			num *= currentBlocker.m_shared.m_timedBlockBonus;
		}
		if (currentBlocker.m_shared.m_damageModifiers.Count > 0)
		{
			HitData.DamageModifiers modifiers = default(HitData.DamageModifiers);
			modifiers.Apply(currentBlocker.m_shared.m_damageModifiers);
			hit.ApplyResistance(modifiers, out var _);
		}
		HitData.DamageTypes damageTypes = hit.m_damage.Clone();
		damageTypes.ApplyArmor(num);
		float totalBlockableDamage = hit.GetTotalBlockableDamage();
		float totalBlockableDamage2 = damageTypes.GetTotalBlockableDamage();
		float num2 = totalBlockableDamage - totalBlockableDamage2;
		float num3 = Mathf.Clamp01(num2 / num);
		float stamina = (flag ? m_blockStaminaDrain : (m_blockStaminaDrain * num3));
		UseStamina(stamina);
		float totalStaggerDamage = damageTypes.GetTotalStaggerDamage();
		bool flag2 = AddStaggerDamage(totalStaggerDamage, hit.m_dir);
		bool num4 = HaveStamina();
		bool flag3 = num4 && !flag2;
		if (num4 && !flag2)
		{
			hit.m_statusEffect = "";
			hit.BlockDamage(num2);
			DamageText.instance.ShowText(DamageText.TextType.Blocked, hit.m_point + Vector3.up * 0.5f, num2);
		}
		if (currentBlocker.m_shared.m_useDurability)
		{
			float num5 = currentBlocker.m_shared.m_useDurabilityDrain * (totalBlockableDamage / num);
			currentBlocker.m_durability -= num5;
		}
		RaiseSkill(Skills.SkillType.Blocking, flag ? 2f : 1f);
		currentBlocker.m_shared.m_blockEffect.Create(hit.m_point, Quaternion.identity);
		if ((bool)attacker && flag && flag3)
		{
			m_perfectBlockEffect.Create(hit.m_point, Quaternion.identity);
			if (attacker.m_staggerWhenBlocked)
			{
				attacker.Stagger(-hit.m_dir);
			}
			UseStamina(m_blockStaminaDrain);
		}
		if (flag3)
		{
			hit.m_pushForce *= num3;
			if ((bool)attacker && !hit.m_ranged)
			{
				float num6 = 1f - Mathf.Clamp01(num3 * 0.5f);
				HitData hitData = new HitData();
				hitData.m_pushForce = currentBlocker.GetDeflectionForce() * num6;
				hitData.m_dir = attacker.transform.position - base.transform.position;
				hitData.m_dir.y = 0f;
				hitData.m_dir.Normalize();
				attacker.Damage(hitData);
			}
		}
		return true;
	}

	public override bool IsBlocking()
	{
		if (m_nview.IsValid() && !m_nview.IsOwner())
		{
			return m_nview.GetZDO().GetBool(isBlockingHash);
		}
		if (m_blocking && !InAttack() && !InDodge() && !InPlaceMode() && !IsEncumbered() && !InMinorAction())
		{
			return !IsStaggering();
		}
		return false;
	}

	private void UpdateBlock(float dt)
	{
		if (IsBlocking())
		{
			if (!m_internalBlockingState)
			{
				m_internalBlockingState = true;
				m_nview.GetZDO().Set(isBlockingHash, value: true);
				m_zanim.SetBool(blocking, value: true);
			}
			if (m_blockTimer < 0f)
			{
				m_blockTimer = 0f;
			}
			else
			{
				m_blockTimer += dt;
			}
		}
		else
		{
			if (m_internalBlockingState)
			{
				m_internalBlockingState = false;
				m_nview.GetZDO().Set(isBlockingHash, value: false);
				m_zanim.SetBool(blocking, value: false);
			}
			m_blockTimer = -1f;
		}
	}

	public void HideHandItems()
	{
		if (m_leftItem != null || m_rightItem != null)
		{
			ItemDrop.ItemData leftItem = m_leftItem;
			ItemDrop.ItemData rightItem = m_rightItem;
			UnequipItem(m_leftItem);
			UnequipItem(m_rightItem);
			m_hiddenLeftItem = leftItem;
			m_hiddenRightItem = rightItem;
			SetupVisEquipment(m_visEquipment, isRagdoll: false);
			m_zanim.SetTrigger("equip_hip");
		}
	}

	public void ShowHandItems()
	{
		ItemDrop.ItemData hiddenLeftItem = m_hiddenLeftItem;
		ItemDrop.ItemData hiddenRightItem = m_hiddenRightItem;
		if (hiddenLeftItem != null || hiddenRightItem != null)
		{
			m_hiddenLeftItem = null;
			m_hiddenRightItem = null;
			if (hiddenLeftItem != null)
			{
				EquipItem(hiddenLeftItem);
			}
			if (hiddenRightItem != null)
			{
				EquipItem(hiddenRightItem);
			}
			m_zanim.SetTrigger("equip_hip");
		}
	}

	public ItemDrop.ItemData GetAmmoItem()
	{
		return m_ammoItem;
	}

	public virtual GameObject GetHoverObject()
	{
		return null;
	}

	public bool IsTeleportable()
	{
		return m_inventory.IsTeleportable();
	}

	public override bool UseMeleeCamera()
	{
		return GetCurrentWeapon()?.m_shared.m_centerCamera ?? false;
	}

	public float GetEquipmentWeight()
	{
		float num = 0f;
		if (m_rightItem != null)
		{
			num += m_rightItem.m_shared.m_weight;
		}
		if (m_leftItem != null)
		{
			num += m_leftItem.m_shared.m_weight;
		}
		if (m_chestItem != null)
		{
			num += m_chestItem.m_shared.m_weight;
		}
		if (m_legItem != null)
		{
			num += m_legItem.m_shared.m_weight;
		}
		if (m_helmetItem != null)
		{
			num += m_helmetItem.m_shared.m_weight;
		}
		if (m_shoulderItem != null)
		{
			num += m_shoulderItem.m_shared.m_weight;
		}
		if (m_utilityItem != null)
		{
			num += m_utilityItem.m_shared.m_weight;
		}
		return num;
	}
}
