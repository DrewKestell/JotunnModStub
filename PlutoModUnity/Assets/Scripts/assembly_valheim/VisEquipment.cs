using System;
using System.Collections.Generic;
using UnityEngine;

public class VisEquipment : MonoBehaviour
{
	[Serializable]
	public class PlayerModel
	{
		public Mesh m_mesh;

		public Material m_baseMaterial;
	}

	public SkinnedMeshRenderer m_bodyModel;

	[Header("Attachment points")]
	public Transform m_leftHand;

	public Transform m_rightHand;

	public Transform m_helmet;

	public Transform m_backShield;

	public Transform m_backMelee;

	public Transform m_backTwohandedMelee;

	public Transform m_backBow;

	public Transform m_backTool;

	public Transform m_backAtgeir;

	public CapsuleCollider[] m_clothColliders = new CapsuleCollider[0];

	public PlayerModel[] m_models = new PlayerModel[0];

	public bool m_isPlayer;

	public bool m_useAllTrails;

	private string m_leftItem = "";

	private string m_rightItem = "";

	private string m_chestItem = "";

	private string m_legItem = "";

	private string m_helmetItem = "";

	private string m_shoulderItem = "";

	private string m_beardItem = "";

	private string m_hairItem = "";

	private string m_utilityItem = "";

	private string m_leftBackItem = "";

	private string m_rightBackItem = "";

	private int m_shoulderItemVariant;

	private int m_leftItemVariant;

	private int m_leftBackItemVariant;

	private GameObject m_leftItemInstance;

	private GameObject m_rightItemInstance;

	private GameObject m_helmetItemInstance;

	private List<GameObject> m_chestItemInstances;

	private List<GameObject> m_legItemInstances;

	private List<GameObject> m_shoulderItemInstances;

	private List<GameObject> m_utilityItemInstances;

	private GameObject m_beardItemInstance;

	private GameObject m_hairItemInstance;

	private GameObject m_leftBackItemInstance;

	private GameObject m_rightBackItemInstance;

	private int m_currentLeftItemHash;

	private int m_currentRightItemHash;

	private int m_currentChestItemHash;

	private int m_currentLegItemHash;

	private int m_currentHelmetItemHash;

	private int m_currentShoulderItemHash;

	private int m_currentBeardItemHash;

	private int m_currentHairItemHash;

	private int m_currentUtilityItemHash;

	private int m_currentLeftBackItemHash;

	private int m_currentRightBackItemHash;

	private int m_currenShoulderItemVariant;

	private int m_currentLeftItemVariant;

	private int m_currentLeftBackItemVariant;

	private bool m_helmetHideHair;

	private Texture m_emptyBodyTexture;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private int m_currentModelIndex;

	private ZNetView m_nview;

	private GameObject m_visual;

	private LODGroup m_lodGroup;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		Transform transform = base.transform.Find("Visual");
		if (transform == null)
		{
			transform = base.transform;
		}
		m_visual = transform.gameObject;
		m_lodGroup = m_visual.GetComponentInChildren<LODGroup>();
		if (m_bodyModel != null && m_bodyModel.material.HasProperty("_ChestTex"))
		{
			m_emptyBodyTexture = m_bodyModel.material.GetTexture("_ChestTex");
		}
	}

	private void Start()
	{
		UpdateVisuals();
	}

	public void SetWeaponTrails(bool enabled)
	{
		if (m_useAllTrails)
		{
			MeleeWeaponTrail[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
		else if ((bool)m_rightItemInstance)
		{
			MeleeWeaponTrail[] componentsInChildren = m_rightItemInstance.GetComponentsInChildren<MeleeWeaponTrail>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Emit = enabled;
			}
		}
	}

	public void SetModel(int index)
	{
		if (m_modelIndex != index && index >= 0 && index < m_models.Length)
		{
			ZLog.Log("Vis equip model set to " + index);
			m_modelIndex = index;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("ModelIndex", m_modelIndex);
			}
		}
	}

	public void SetSkinColor(Vector3 color)
	{
		if (!(color == m_skinColor))
		{
			m_skinColor = color;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("SkinColor", m_skinColor);
			}
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (!(m_hairColor == color))
		{
			m_hairColor = color;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("HairColor", m_hairColor);
			}
		}
	}

	public void SetLeftItem(string name, int variant)
	{
		if (!(m_leftItem == name) || m_leftItemVariant != variant)
		{
			m_leftItem = name;
			m_leftItemVariant = variant;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("LeftItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set("LeftItemVariant", variant);
			}
		}
	}

	public void SetRightItem(string name)
	{
		if (!(m_rightItem == name))
		{
			m_rightItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("RightItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetLeftBackItem(string name, int variant)
	{
		if (!(m_leftBackItem == name) || m_leftBackItemVariant != variant)
		{
			m_leftBackItem = name;
			m_leftBackItemVariant = variant;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("LeftBackItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set("LeftBackItemVariant", variant);
			}
		}
	}

	public void SetRightBackItem(string name)
	{
		if (!(m_rightBackItem == name))
		{
			m_rightBackItem = name;
			ZLog.Log("Right back item " + name);
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("RightBackItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetChestItem(string name)
	{
		if (!(m_chestItem == name))
		{
			m_chestItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("ChestItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetLegItem(string name)
	{
		if (!(m_legItem == name))
		{
			m_legItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("LegItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetHelmetItem(string name)
	{
		if (!(m_helmetItem == name))
		{
			m_helmetItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("HelmetItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetShoulderItem(string name, int variant)
	{
		if (!(m_shoulderItem == name) || m_shoulderItemVariant != variant)
		{
			m_shoulderItem = name;
			m_shoulderItemVariant = variant;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("ShoulderItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
				m_nview.GetZDO().Set("ShoulderItemVariant", variant);
			}
		}
	}

	public void SetBeardItem(string name)
	{
		if (!(m_beardItem == name))
		{
			m_beardItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("BeardItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetHairItem(string name)
	{
		if (!(m_hairItem == name))
		{
			m_hairItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("HairItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	public void SetUtilityItem(string name)
	{
		if (!(m_utilityItem == name))
		{
			m_utilityItem = name;
			if (m_nview.GetZDO() != null)
			{
				m_nview.GetZDO().Set("UtilityItem", (!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
			}
		}
	}

	private void Update()
	{
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		UpdateEquipmentVisuals();
		if (m_isPlayer)
		{
			UpdateBaseModel();
			UpdateColors();
		}
	}

	private void UpdateColors()
	{
		Color value = Utils.Vec3ToColor(m_skinColor);
		Color value2 = Utils.Vec3ToColor(m_hairColor);
		if (m_nview.GetZDO() != null)
		{
			value = Utils.Vec3ToColor(m_nview.GetZDO().GetVec3("SkinColor", Vector3.one));
			value2 = Utils.Vec3ToColor(m_nview.GetZDO().GetVec3("HairColor", Vector3.one));
		}
		m_bodyModel.materials[0].SetColor("_SkinColor", value);
		m_bodyModel.materials[1].SetColor("_SkinColor", value2);
		if ((bool)m_beardItemInstance)
		{
			Renderer[] componentsInChildren = m_beardItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
		if ((bool)m_hairItemInstance)
		{
			Renderer[] componentsInChildren = m_hairItemInstance.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.SetColor("_SkinColor", value2);
			}
		}
	}

	private void UpdateBaseModel()
	{
		if (m_models.Length != 0)
		{
			int num = m_modelIndex;
			if (m_nview.GetZDO() != null)
			{
				num = m_nview.GetZDO().GetInt("ModelIndex");
			}
			if (m_currentModelIndex != num || m_bodyModel.sharedMesh != m_models[num].m_mesh)
			{
				m_currentModelIndex = num;
				m_bodyModel.sharedMesh = m_models[num].m_mesh;
				m_bodyModel.materials[0].SetTexture("_MainTex", m_models[num].m_baseMaterial.GetTexture("_MainTex"));
				m_bodyModel.materials[0].SetTexture("_SkinBumpMap", m_models[num].m_baseMaterial.GetTexture("_SkinBumpMap"));
			}
		}
	}

	private void UpdateEquipmentVisuals()
	{
		int hash = 0;
		int rightHandEquiped = 0;
		int chestEquiped = 0;
		int legEquiped = 0;
		int hash2 = 0;
		int beardEquiped = 0;
		int num = 0;
		int hash3 = 0;
		int utilityEquiped = 0;
		int leftItem = 0;
		int rightItem = 0;
		int variant = m_shoulderItemVariant;
		int variant2 = m_leftItemVariant;
		int leftVariant = m_leftBackItemVariant;
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null)
		{
			hash = zDO.GetInt("LeftItem");
			rightHandEquiped = zDO.GetInt("RightItem");
			chestEquiped = zDO.GetInt("ChestItem");
			legEquiped = zDO.GetInt("LegItem");
			hash2 = zDO.GetInt("HelmetItem");
			hash3 = zDO.GetInt("ShoulderItem");
			utilityEquiped = zDO.GetInt("UtilityItem");
			if (m_isPlayer)
			{
				beardEquiped = zDO.GetInt("BeardItem");
				num = zDO.GetInt("HairItem");
				leftItem = zDO.GetInt("LeftBackItem");
				rightItem = zDO.GetInt("RightBackItem");
				variant = zDO.GetInt("ShoulderItemVariant");
				variant2 = zDO.GetInt("LeftItemVariant");
				leftVariant = zDO.GetInt("LeftBackItemVariant");
			}
		}
		else
		{
			if (!string.IsNullOrEmpty(m_leftItem))
			{
				hash = m_leftItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_rightItem))
			{
				rightHandEquiped = m_rightItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_chestItem))
			{
				chestEquiped = m_chestItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_legItem))
			{
				legEquiped = m_legItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_helmetItem))
			{
				hash2 = m_helmetItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_shoulderItem))
			{
				hash3 = m_shoulderItem.GetStableHashCode();
			}
			if (!string.IsNullOrEmpty(m_utilityItem))
			{
				utilityEquiped = m_utilityItem.GetStableHashCode();
			}
			if (m_isPlayer)
			{
				if (!string.IsNullOrEmpty(m_beardItem))
				{
					beardEquiped = m_beardItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_hairItem))
				{
					num = m_hairItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_leftBackItem))
				{
					leftItem = m_leftBackItem.GetStableHashCode();
				}
				if (!string.IsNullOrEmpty(m_rightBackItem))
				{
					rightItem = m_rightBackItem.GetStableHashCode();
				}
			}
		}
		bool flag = false;
		flag = SetRightHandEquiped(rightHandEquiped) || flag;
		flag = SetLeftHandEquiped(hash, variant2) || flag;
		flag = SetChestEquiped(chestEquiped) || flag;
		flag = SetLegEquiped(legEquiped) || flag;
		flag = SetHelmetEquiped(hash2, num) || flag;
		flag = SetShoulderEquiped(hash3, variant) || flag;
		flag = SetUtilityEquiped(utilityEquiped) || flag;
		if (m_isPlayer)
		{
			flag = SetBeardEquiped(beardEquiped) || flag;
			flag = SetBackEquiped(leftItem, rightItem, leftVariant) || flag;
			if (m_helmetHideHair)
			{
				num = 0;
			}
			flag = SetHairEquiped(num) || flag;
		}
		if (flag)
		{
			UpdateLodgroup();
		}
	}

	protected void UpdateLodgroup()
	{
		if (!(m_lodGroup == null))
		{
			Renderer[] componentsInChildren = m_visual.GetComponentsInChildren<Renderer>();
			LOD[] lODs = m_lodGroup.GetLODs();
			lODs[0].renderers = componentsInChildren;
			m_lodGroup.SetLODs(lODs);
		}
	}

	private bool SetRightHandEquiped(int hash)
	{
		if (m_currentRightItemHash == hash)
		{
			return false;
		}
		if ((bool)m_rightItemInstance)
		{
			UnityEngine.Object.Destroy(m_rightItemInstance);
			m_rightItemInstance = null;
		}
		m_currentRightItemHash = hash;
		if (hash != 0)
		{
			m_rightItemInstance = AttachItem(hash, 0, m_rightHand);
		}
		return true;
	}

	private bool SetLeftHandEquiped(int hash, int variant)
	{
		if (m_currentLeftItemHash == hash && m_currentLeftItemVariant == variant)
		{
			return false;
		}
		if ((bool)m_leftItemInstance)
		{
			UnityEngine.Object.Destroy(m_leftItemInstance);
			m_leftItemInstance = null;
		}
		m_currentLeftItemHash = hash;
		m_currentLeftItemVariant = variant;
		if (hash != 0)
		{
			m_leftItemInstance = AttachItem(hash, variant, m_leftHand);
		}
		return true;
	}

	private bool SetBackEquiped(int leftItem, int rightItem, int leftVariant)
	{
		if (m_currentLeftBackItemHash == leftItem && m_currentRightBackItemHash == rightItem && m_currentLeftBackItemVariant == leftVariant)
		{
			return false;
		}
		if ((bool)m_leftBackItemInstance)
		{
			UnityEngine.Object.Destroy(m_leftBackItemInstance);
			m_leftBackItemInstance = null;
		}
		if ((bool)m_rightBackItemInstance)
		{
			UnityEngine.Object.Destroy(m_rightBackItemInstance);
			m_rightBackItemInstance = null;
		}
		m_currentLeftBackItemHash = leftItem;
		m_currentRightBackItemHash = rightItem;
		m_currentLeftBackItemVariant = leftVariant;
		if (m_currentLeftBackItemHash != 0)
		{
			m_leftBackItemInstance = AttachBackItem(leftItem, leftVariant, rightHand: false);
		}
		if (m_currentRightBackItemHash != 0)
		{
			m_rightBackItemInstance = AttachBackItem(rightItem, 0, rightHand: true);
		}
		return true;
	}

	private GameObject AttachBackItem(int hash, int variant, bool rightHand)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing back attach item prefab: " + hash);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		switch ((component.m_itemData.m_shared.m_attachOverride != 0) ? component.m_itemData.m_shared.m_attachOverride : component.m_itemData.m_shared.m_itemType)
		{
		case ItemDrop.ItemData.ItemType.Torch:
			if (rightHand)
			{
				return AttachItem(hash, variant, m_backMelee, enableEquipEffects: false);
			}
			return AttachItem(hash, variant, m_backTool, enableEquipEffects: false);
		case ItemDrop.ItemData.ItemType.Bow:
			return AttachItem(hash, variant, m_backBow);
		case ItemDrop.ItemData.ItemType.Tool:
			return AttachItem(hash, variant, m_backTool);
		case ItemDrop.ItemData.ItemType.Attach_Atgeir:
			return AttachItem(hash, variant, m_backAtgeir);
		case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			return AttachItem(hash, variant, m_backMelee);
		case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
			return AttachItem(hash, variant, m_backTwohandedMelee);
		case ItemDrop.ItemData.ItemType.Shield:
			return AttachItem(hash, variant, m_backShield);
		default:
			return null;
		}
	}

	private bool SetChestEquiped(int hash)
	{
		if (m_currentChestItemHash == hash)
		{
			return false;
		}
		m_currentChestItemHash = hash;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_chestItemInstances != null)
		{
			foreach (GameObject chestItemInstance in m_chestItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, chestItemInstance);
				}
				UnityEngine.Object.Destroy(chestItemInstance);
			}
			m_chestItemInstances = null;
			m_bodyModel.material.SetTexture("_ChestTex", m_emptyBodyTexture);
			m_bodyModel.material.SetTexture("_ChestBumpMap", null);
			m_bodyModel.material.SetTexture("_ChestMetal", null);
		}
		if (m_currentChestItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing chest item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if ((bool)component.m_itemData.m_shared.m_armorMaterial)
		{
			m_bodyModel.material.SetTexture("_ChestTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestTex"));
			m_bodyModel.material.SetTexture("_ChestBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestBumpMap"));
			m_bodyModel.material.SetTexture("_ChestMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_ChestMetal"));
		}
		m_chestItemInstances = AttachArmor(hash);
		return true;
	}

	private bool SetShoulderEquiped(int hash, int variant)
	{
		if (m_currentShoulderItemHash == hash && m_currenShoulderItemVariant == variant)
		{
			return false;
		}
		m_currentShoulderItemHash = hash;
		m_currenShoulderItemVariant = variant;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_shoulderItemInstances != null)
		{
			foreach (GameObject shoulderItemInstance in m_shoulderItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, shoulderItemInstance);
				}
				UnityEngine.Object.Destroy(shoulderItemInstance);
			}
			m_shoulderItemInstances = null;
		}
		if (m_currentShoulderItemHash == 0)
		{
			return true;
		}
		if (ObjectDB.instance.GetItemPrefab(hash) == null)
		{
			ZLog.Log("Missing shoulder item " + hash);
			return true;
		}
		m_shoulderItemInstances = AttachArmor(hash, variant);
		return true;
	}

	private bool SetLegEquiped(int hash)
	{
		if (m_currentLegItemHash == hash)
		{
			return false;
		}
		m_currentLegItemHash = hash;
		if (m_bodyModel == null)
		{
			return true;
		}
		if (m_legItemInstances != null)
		{
			foreach (GameObject legItemInstance in m_legItemInstances)
			{
				UnityEngine.Object.Destroy(legItemInstance);
			}
			m_legItemInstances = null;
			m_bodyModel.material.SetTexture("_LegsTex", m_emptyBodyTexture);
			m_bodyModel.material.SetTexture("_LegsBumpMap", null);
			m_bodyModel.material.SetTexture("_LegsMetal", null);
		}
		if (m_currentLegItemHash == 0)
		{
			return true;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing legs item " + hash);
			return true;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if ((bool)component.m_itemData.m_shared.m_armorMaterial)
		{
			m_bodyModel.material.SetTexture("_LegsTex", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsTex"));
			m_bodyModel.material.SetTexture("_LegsBumpMap", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsBumpMap"));
			m_bodyModel.material.SetTexture("_LegsMetal", component.m_itemData.m_shared.m_armorMaterial.GetTexture("_LegsMetal"));
		}
		m_legItemInstances = AttachArmor(hash);
		return true;
	}

	private bool SetBeardEquiped(int hash)
	{
		if (m_currentBeardItemHash == hash)
		{
			return false;
		}
		if ((bool)m_beardItemInstance)
		{
			UnityEngine.Object.Destroy(m_beardItemInstance);
			m_beardItemInstance = null;
		}
		m_currentBeardItemHash = hash;
		if (hash != 0)
		{
			m_beardItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetHairEquiped(int hash)
	{
		if (m_currentHairItemHash == hash)
		{
			return false;
		}
		if ((bool)m_hairItemInstance)
		{
			UnityEngine.Object.Destroy(m_hairItemInstance);
			m_hairItemInstance = null;
		}
		m_currentHairItemHash = hash;
		if (hash != 0)
		{
			m_hairItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetHelmetEquiped(int hash, int hairHash)
	{
		if (m_currentHelmetItemHash == hash)
		{
			return false;
		}
		if ((bool)m_helmetItemInstance)
		{
			UnityEngine.Object.Destroy(m_helmetItemInstance);
			m_helmetItemInstance = null;
		}
		m_currentHelmetItemHash = hash;
		m_helmetHideHair = HelmetHidesHair(hash);
		if (hash != 0)
		{
			m_helmetItemInstance = AttachItem(hash, 0, m_helmet);
		}
		return true;
	}

	private bool SetUtilityEquiped(int hash)
	{
		if (m_currentUtilityItemHash == hash)
		{
			return false;
		}
		if (m_utilityItemInstances != null)
		{
			foreach (GameObject utilityItemInstance in m_utilityItemInstances)
			{
				if ((bool)m_lodGroup)
				{
					Utils.RemoveFromLodgroup(m_lodGroup, utilityItemInstance);
				}
				UnityEngine.Object.Destroy(utilityItemInstance);
			}
			m_utilityItemInstances = null;
		}
		m_currentUtilityItemHash = hash;
		if (hash != 0)
		{
			m_utilityItemInstances = AttachArmor(hash);
		}
		return true;
	}

	private bool HelmetHidesHair(int itemHash)
	{
		if (itemHash == 0)
		{
			return false;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			return false;
		}
		return itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_helmetHideHair;
	}

	private List<GameObject> AttachArmor(int itemHash, int variant = -1)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash + "  ob:" + base.gameObject.name);
			return null;
		}
		List<GameObject> list = new List<GameObject>();
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (!child.gameObject.name.StartsWith("attach_"))
			{
				continue;
			}
			string text = child.gameObject.name.Substring(7);
			GameObject gameObject;
			if (text == "skin")
			{
				gameObject = UnityEngine.Object.Instantiate(child.gameObject, m_bodyModel.transform.position, m_bodyModel.transform.parent.rotation, m_bodyModel.transform.parent);
				gameObject.SetActive(value: true);
				SkinnedMeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach (SkinnedMeshRenderer obj in componentsInChildren)
				{
					obj.rootBone = m_bodyModel.rootBone;
					obj.bones = m_bodyModel.bones;
				}
				Cloth[] componentsInChildren2 = gameObject.GetComponentsInChildren<Cloth>();
				foreach (Cloth cloth in componentsInChildren2)
				{
					if (m_clothColliders.Length != 0)
					{
						if (cloth.capsuleColliders.Length != 0)
						{
							List<CapsuleCollider> list2 = new List<CapsuleCollider>(m_clothColliders);
							list2.AddRange(cloth.capsuleColliders);
							cloth.capsuleColliders = list2.ToArray();
						}
						else
						{
							cloth.capsuleColliders = m_clothColliders;
						}
					}
				}
			}
			else
			{
				Transform transform = Utils.FindChild(m_visual.transform, text);
				if (transform == null)
				{
					ZLog.LogWarning("Missing joint " + text + " in item " + itemPrefab.name);
					continue;
				}
				gameObject = UnityEngine.Object.Instantiate(child.gameObject);
				gameObject.SetActive(value: true);
				gameObject.transform.SetParent(transform);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
			}
			if (variant >= 0)
			{
				gameObject.GetComponentInChildren<IEquipmentVisual>()?.Setup(variant);
			}
			CleanupInstance(gameObject);
			EnableEquipedEffects(gameObject);
			list.Add(gameObject);
		}
		return list;
	}

	protected GameObject AttachItem(int itemHash, int variant, Transform joint, bool enableEquipEffects = true)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
		if (itemPrefab == null)
		{
			ZLog.Log("Missing attach item: " + itemHash + "  ob:" + base.gameObject.name + "  joint:" + (joint ? joint.name : "none"));
			return null;
		}
		GameObject gameObject = null;
		int childCount = itemPrefab.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = itemPrefab.transform.GetChild(i);
			if (child.gameObject.name == "attach" || child.gameObject.name == "attach_skin")
			{
				gameObject = child.gameObject;
				break;
			}
		}
		if (gameObject == null)
		{
			return null;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.SetActive(value: true);
		CleanupInstance(gameObject2);
		if (enableEquipEffects)
		{
			EnableEquipedEffects(gameObject2);
		}
		if (gameObject.name == "attach_skin")
		{
			gameObject2.transform.SetParent(m_bodyModel.transform.parent);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			SkinnedMeshRenderer[] componentsInChildren = gameObject2.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer obj in componentsInChildren)
			{
				obj.rootBone = m_bodyModel.rootBone;
				obj.bones = m_bodyModel.bones;
			}
		}
		else
		{
			gameObject2.transform.SetParent(joint);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
		}
		gameObject2.GetComponentInChildren<IEquipmentVisual>()?.Setup(variant);
		return gameObject2;
	}

	private void CleanupInstance(GameObject instance)
	{
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private void EnableEquipedEffects(GameObject instance)
	{
		Transform transform = instance.transform.Find("equiped");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
	}

	public int GetModelIndex()
	{
		int result = m_modelIndex;
		if (m_nview.IsValid())
		{
			result = m_nview.GetZDO().GetInt("ModelIndex");
		}
		return result;
	}
}
