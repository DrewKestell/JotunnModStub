using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGui : MonoBehaviour
{
	private List<ItemDrop.ItemData> m_tempItemList = new List<ItemDrop.ItemData>();

	private List<ItemDrop.ItemData> m_tempWornItems = new List<ItemDrop.ItemData>();

	private static InventoryGui m_instance;

	[Header("Gamepad")]
	public UIGroupHandler m_inventoryGroup;

	public UIGroupHandler[] m_uiGroups = new UIGroupHandler[0];

	private int m_activeGroup = 1;

	[Header("Other")]
	public Transform m_inventoryRoot;

	public RectTransform m_player;

	public RectTransform m_container;

	public GameObject m_dragItemPrefab;

	public Text m_containerName;

	public Button m_dropButton;

	public Button m_takeAllButton;

	public float m_autoCloseDistance = 4f;

	[Header("Crafting dialog")]
	public Button m_tabCraft;

	public Button m_tabUpgrade;

	public GameObject m_recipeElementPrefab;

	public RectTransform m_recipeListRoot;

	public Scrollbar m_recipeListScroll;

	public float m_recipeListSpace = 30f;

	public float m_craftDuration = 2f;

	public Text m_craftingStationName;

	public Image m_craftingStationIcon;

	public RectTransform m_craftingStationLevelRoot;

	public Text m_craftingStationLevel;

	public Text m_recipeName;

	public Text m_recipeDecription;

	public Image m_recipeIcon;

	public GameObject[] m_recipeRequirementList = new GameObject[0];

	public Button m_variantButton;

	public Button m_craftButton;

	public Button m_craftCancelButton;

	public Transform m_craftProgressPanel;

	public GuiBar m_craftProgressBar;

	[Header("Repair")]
	public Button m_repairButton;

	public Transform m_repairPanel;

	public Image m_repairButtonGlow;

	public Transform m_repairPanelSelection;

	[Header("Upgrade")]
	public Image m_upgradeItemIcon;

	public GuiBar m_upgradeItemDurability;

	public Text m_upgradeItemName;

	public Text m_upgradeItemQuality;

	public GameObject m_upgradeItemQualityArrow;

	public Text m_upgradeItemNextQuality;

	public Text m_upgradeItemIndex;

	public Text m_itemCraftType;

	public RectTransform m_qualityPanel;

	public Button m_qualityLevelDown;

	public Button m_qualityLevelUp;

	public Text m_qualityLevel;

	public Image m_minStationLevelIcon;

	private Color m_minStationLevelBasecolor;

	public Text m_minStationLevelText;

	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	[Header("Variants dialog")]
	public VariantDialog m_variantDialog;

	[Header("Skills dialog")]
	public SkillsDialog m_skillsDialog;

	[Header("Texts dialog")]
	public TextsDialog m_textsDialog;

	[Header("Split dialog")]
	public Transform m_splitPanel;

	public Slider m_splitSlider;

	public Text m_splitAmount;

	public Button m_splitCancelButton;

	public Button m_splitOkButton;

	public Image m_splitIcon;

	public Text m_splitIconName;

	[Header("Character stats")]
	public Transform m_infoPanel;

	public Text m_playerName;

	public Text m_armor;

	public Text m_weight;

	public Text m_containerWeight;

	public Toggle m_pvp;

	[Header("Trophies")]
	public GameObject m_trophiesPanel;

	public RectTransform m_trophieListRoot;

	public float m_trophieListSpace = 30f;

	public GameObject m_trophieElementPrefab;

	public Scrollbar m_trophyListScroll;

	[Header("Effects")]
	public EffectList m_moveItemEffects = new EffectList();

	public EffectList m_craftItemEffects = new EffectList();

	public EffectList m_craftItemDoneEffects = new EffectList();

	public EffectList m_openInventoryEffects = new EffectList();

	public EffectList m_closeInventoryEffects = new EffectList();

	[HideInInspector]
	public InventoryGrid m_playerGrid;

	private InventoryGrid m_containerGrid;

	private Animator m_animator;

	private Container m_currentContainer;

	private bool m_firstContainerUpdate = true;

	private KeyValuePair<Recipe, ItemDrop.ItemData> m_selectedRecipe;

	private List<ItemDrop.ItemData> m_upgradeItems = new List<ItemDrop.ItemData>();

	private int m_selectedVariant;

	private Recipe m_craftRecipe;

	private ItemDrop.ItemData m_craftUpgradeItem;

	private int m_craftVariant;

	private List<GameObject> m_recipeList = new List<GameObject>();

	private List<KeyValuePair<Recipe, ItemDrop.ItemData>> m_availableRecipes = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();

	private GameObject m_dragGo;

	private ItemDrop.ItemData m_dragItem;

	private Inventory m_dragInventory;

	private int m_dragAmount = 1;

	private ItemDrop.ItemData m_splitItem;

	private Inventory m_splitInventory;

	private float m_craftTimer = -1f;

	private float m_recipeListBaseSize;

	private int m_hiddenFrames = 9999;

	private List<GameObject> m_trophyList = new List<GameObject>();

	private float m_trophieListBaseSize;

	public static InventoryGui instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_animator = GetComponent<Animator>();
		m_inventoryRoot.gameObject.SetActive(value: true);
		m_container.gameObject.SetActive(value: false);
		m_splitPanel.gameObject.SetActive(value: false);
		m_trophiesPanel.SetActive(value: false);
		m_variantDialog.gameObject.SetActive(value: false);
		m_skillsDialog.gameObject.SetActive(value: false);
		m_textsDialog.gameObject.SetActive(value: false);
		m_playerGrid = m_player.GetComponentInChildren<InventoryGrid>();
		m_containerGrid = m_container.GetComponentInChildren<InventoryGrid>();
		InventoryGrid playerGrid = m_playerGrid;
		playerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(playerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(OnSelectedItem));
		InventoryGrid playerGrid2 = m_playerGrid;
		playerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(playerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(OnRightClickItem));
		InventoryGrid containerGrid = m_containerGrid;
		containerGrid.m_onSelected = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>)Delegate.Combine(containerGrid.m_onSelected, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier>(OnSelectedItem));
		InventoryGrid containerGrid2 = m_containerGrid;
		containerGrid2.m_onRightClick = (Action<InventoryGrid, ItemDrop.ItemData, Vector2i>)Delegate.Combine(containerGrid2.m_onRightClick, new Action<InventoryGrid, ItemDrop.ItemData, Vector2i>(OnRightClickItem));
		m_craftButton.onClick.AddListener(OnCraftPressed);
		m_craftCancelButton.onClick.AddListener(OnCraftCancelPressed);
		m_dropButton.onClick.AddListener(OnDropOutside);
		m_takeAllButton.onClick.AddListener(OnTakeAll);
		m_repairButton.onClick.AddListener(OnRepairPressed);
		m_splitSlider.onValueChanged.AddListener(OnSplitSliderChanged);
		m_splitCancelButton.onClick.AddListener(OnSplitCancel);
		m_splitOkButton.onClick.AddListener(OnSplitOk);
		VariantDialog variantDialog = m_variantDialog;
		variantDialog.m_selected = (Action<int>)Delegate.Combine(variantDialog.m_selected, new Action<int>(OnVariantSelected));
		m_recipeListBaseSize = m_recipeListRoot.rect.height;
		m_trophieListBaseSize = m_trophieListRoot.rect.height;
		m_minStationLevelBasecolor = m_minStationLevelText.color;
		m_tabCraft.interactable = false;
		m_tabUpgrade.interactable = true;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Update()
	{
		bool @bool = m_animator.GetBool("visible");
		if (!@bool)
		{
			m_hiddenFrames++;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
		{
			Hide();
			return;
		}
		if (m_craftTimer < 0f && (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && (bool)TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && !GameCamera.InFreeFly() && !Minimap.IsOpen())
		{
			if (m_trophiesPanel.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				m_trophiesPanel.SetActive(value: false);
			}
			else if (m_skillsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				m_skillsDialog.gameObject.SetActive(value: false);
			}
			else if (m_textsDialog.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				m_textsDialog.gameObject.SetActive(value: false);
			}
			else if (m_splitPanel.gameObject.activeSelf && (ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape)))
			{
				m_splitPanel.gameObject.SetActive(value: false);
			}
			else if (@bool)
			{
				if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use"))
				{
					ZInput.ResetButtonStatus("Inventory");
					ZInput.ResetButtonStatus("JoyButtonB");
					ZInput.ResetButtonStatus("JoyButtonY");
					ZInput.ResetButtonStatus("Use");
					Hide();
				}
			}
			else if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonY"))
			{
				ZInput.ResetButtonStatus("Inventory");
				ZInput.ResetButtonStatus("JoyButtonY");
				localPlayer.ShowTutorial("inventory", force: true);
				Show(null);
			}
		}
		if (@bool)
		{
			m_hiddenFrames = 0;
			UpdateGamepad();
			UpdateInventory(localPlayer);
			UpdateContainer(localPlayer);
			UpdateItemDrag();
			UpdateCharacterStats(localPlayer);
			UpdateInventoryWeight(localPlayer);
			UpdateContainerWeight();
			UpdateRecipe(localPlayer, Time.deltaTime);
			UpdateRepair();
		}
	}

	private void UpdateGamepad()
	{
		if (m_inventoryGroup.IsActive())
		{
			if (ZInput.GetButtonDown("JoyTabLeft"))
			{
				SetActiveGroup(m_activeGroup - 1);
			}
			if (ZInput.GetButtonDown("JoyTabRight"))
			{
				SetActiveGroup(m_activeGroup + 1);
			}
			if (m_activeGroup == 0 && !IsContainerOpen())
			{
				SetActiveGroup(1);
			}
			if (m_activeGroup == 3)
			{
				UpdateRecipeGamepadInput();
			}
		}
	}

	private void SetActiveGroup(int index)
	{
		index = Mathf.Clamp(index, 0, m_uiGroups.Length - 1);
		m_activeGroup = index;
		for (int i = 0; i < m_uiGroups.Length; i++)
		{
			m_uiGroups[i].SetActive(i == m_activeGroup);
		}
	}

	private void UpdateCharacterStats(Player player)
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		m_playerName.text = playerProfile.GetName();
		float bodyArmor = player.GetBodyArmor();
		m_armor.text = bodyArmor.ToString();
		m_pvp.interactable = player.CanSwitchPVP();
		player.SetPVP(m_pvp.isOn);
	}

	private void UpdateInventoryWeight(Player player)
	{
		int num = Mathf.CeilToInt(player.GetInventory().GetTotalWeight());
		int num2 = Mathf.CeilToInt(player.GetMaxCarryWeight());
		if (num > num2)
		{
			if (Mathf.Sin(Time.time * 10f) > 0f)
			{
				m_weight.text = "<color=red>" + num + "</color>/" + num2;
			}
			else
			{
				m_weight.text = num + "/" + num2;
			}
		}
		else
		{
			m_weight.text = num + "/" + num2;
		}
	}

	private void UpdateContainerWeight()
	{
		if (!(m_currentContainer == null))
		{
			int num = Mathf.CeilToInt(m_currentContainer.GetInventory().GetTotalWeight());
			m_containerWeight.text = num.ToString();
		}
	}

	private void UpdateInventory(Player player)
	{
		Inventory inventory = player.GetInventory();
		m_playerGrid.UpdateInventory(inventory, player, m_dragItem);
	}

	private void UpdateContainer(Player player)
	{
		if (!m_animator.GetBool("visible"))
		{
			return;
		}
		if ((bool)m_currentContainer && m_currentContainer.IsOwner())
		{
			m_currentContainer.SetInUse(inUse: true);
			m_container.gameObject.SetActive(value: true);
			m_containerGrid.UpdateInventory(m_currentContainer.GetInventory(), null, m_dragItem);
			m_containerName.text = Localization.instance.Localize(m_currentContainer.GetInventory().GetName());
			if (m_firstContainerUpdate)
			{
				m_containerGrid.ResetView();
				m_firstContainerUpdate = false;
			}
			if (Vector3.Distance(m_currentContainer.transform.position, player.transform.position) > m_autoCloseDistance)
			{
				CloseContainer();
			}
		}
		else
		{
			m_container.gameObject.SetActive(value: false);
			if (m_dragInventory != null && m_dragInventory != Player.m_localPlayer.GetInventory())
			{
				SetupDragItem(null, null, 1);
			}
		}
	}

	private RectTransform GetSelectedGamepadElement()
	{
		RectTransform gamepadSelectedElement = m_playerGrid.GetGamepadSelectedElement();
		if ((bool)gamepadSelectedElement)
		{
			return gamepadSelectedElement;
		}
		if (m_container.gameObject.activeSelf)
		{
			return m_containerGrid.GetGamepadSelectedElement();
		}
		return null;
	}

	private void UpdateItemDrag()
	{
		if (!m_dragGo)
		{
			return;
		}
		if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
		{
			RectTransform selectedGamepadElement = GetSelectedGamepadElement();
			if ((bool)selectedGamepadElement)
			{
				Vector3[] array = new Vector3[4];
				selectedGamepadElement.GetWorldCorners(array);
				m_dragGo.transform.position = array[2] + new Vector3(0f, 32f, 0f);
			}
			else
			{
				m_dragGo.transform.position = new Vector3(-99999f, 0f, 0f);
			}
		}
		else
		{
			m_dragGo.transform.position = Input.mousePosition;
		}
		Image component = m_dragGo.transform.Find("icon").GetComponent<Image>();
		Text component2 = m_dragGo.transform.Find("name").GetComponent<Text>();
		Text component3 = m_dragGo.transform.Find("amount").GetComponent<Text>();
		component.sprite = m_dragItem.GetIcon();
		component2.text = m_dragItem.m_shared.m_name;
		component3.text = ((m_dragAmount > 1) ? m_dragAmount.ToString() : "");
		if (Input.GetMouseButton(1))
		{
			SetupDragItem(null, null, 1);
		}
	}

	private void OnTakeAll()
	{
		if (!Player.m_localPlayer.IsTeleporting() && (bool)m_currentContainer)
		{
			SetupDragItem(null, null, 1);
			Inventory inventory = m_currentContainer.GetInventory();
			Player.m_localPlayer.GetInventory().MoveAll(inventory);
		}
	}

	private void OnDropOutside()
	{
		if ((bool)m_dragGo)
		{
			ZLog.Log("Drop item " + m_dragItem.m_shared.m_name);
			if (!m_dragInventory.ContainsItem(m_dragItem))
			{
				SetupDragItem(null, null, 1);
			}
			else if (Player.m_localPlayer.DropItem(m_dragInventory, m_dragItem, m_dragAmount))
			{
				m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
				SetupDragItem(null, null, 1);
				UpdateCraftingPanel();
			}
		}
	}

	private void OnRightClickItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
	{
		if (item != null && (bool)Player.m_localPlayer)
		{
			Player.m_localPlayer.UseItem(grid.GetInventory(), item, fromInventoryGui: true);
		}
	}

	private void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer.IsTeleporting())
		{
			return;
		}
		if ((bool)m_dragGo)
		{
			m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
			bool flag = localPlayer.IsItemEquiped(m_dragItem);
			bool flag2 = item != null && localPlayer.IsItemEquiped(item);
			Vector2i gridPos = m_dragItem.m_gridPos;
			if ((m_dragItem.m_shared.m_questItem || (item != null && item.m_shared.m_questItem)) && m_dragInventory != grid.GetInventory())
			{
				return;
			}
			if (!m_dragInventory.ContainsItem(m_dragItem))
			{
				SetupDragItem(null, null, 1);
				return;
			}
			localPlayer.RemoveFromEquipQueue(item);
			localPlayer.RemoveFromEquipQueue(m_dragItem);
			localPlayer.UnequipItem(m_dragItem, triggerEquipEffects: false);
			localPlayer.UnequipItem(item, triggerEquipEffects: false);
			bool num = grid.DropItem(m_dragInventory, m_dragItem, m_dragAmount, pos);
			if (m_dragItem.m_stack < m_dragAmount)
			{
				m_dragAmount = m_dragItem.m_stack;
			}
			if (flag)
			{
				ItemDrop.ItemData itemAt = grid.GetInventory().GetItemAt(pos.x, pos.y);
				if (itemAt != null)
				{
					localPlayer.EquipItem(itemAt, triggerEquipEffects: false);
				}
				if (localPlayer.GetInventory().ContainsItem(m_dragItem))
				{
					localPlayer.EquipItem(m_dragItem, triggerEquipEffects: false);
				}
			}
			if (flag2)
			{
				ItemDrop.ItemData itemAt2 = m_dragInventory.GetItemAt(gridPos.x, gridPos.y);
				if (itemAt2 != null)
				{
					localPlayer.EquipItem(itemAt2, triggerEquipEffects: false);
				}
				if (localPlayer.GetInventory().ContainsItem(item))
				{
					localPlayer.EquipItem(item, triggerEquipEffects: false);
				}
			}
			if (num)
			{
				SetupDragItem(null, null, 1);
				UpdateCraftingPanel();
			}
		}
		else
		{
			if (item == null)
			{
				return;
			}
			switch (mod)
			{
			case InventoryGrid.Modifier.Move:
				if (item.m_shared.m_questItem)
				{
					return;
				}
				if (m_currentContainer != null)
				{
					localPlayer.RemoveFromEquipQueue(item);
					localPlayer.UnequipItem(item);
					if (grid.GetInventory() == m_currentContainer.GetInventory())
					{
						localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
					}
					else
					{
						m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), item);
					}
					m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
				}
				else if (Player.m_localPlayer.DropItem(localPlayer.GetInventory(), item, item.m_stack))
				{
					m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
				}
				return;
			case InventoryGrid.Modifier.Split:
				if (item.m_stack > 1)
				{
					ShowSplitDialog(item, grid.GetInventory());
					return;
				}
				break;
			}
			SetupDragItem(item, grid.GetInventory(), item.m_stack);
		}
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_hiddenFrames <= 1;
		}
		return false;
	}

	public bool IsContainerOpen()
	{
		return m_currentContainer != null;
	}

	public void Show(Container container)
	{
		Hud.HidePieceSelection();
		m_animator.SetBool("visible", value: true);
		SetActiveGroup(1);
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			SetupCrafting();
		}
		m_currentContainer = container;
		m_hiddenFrames = 0;
		if ((bool)localPlayer)
		{
			m_openInventoryEffects.Create(localPlayer.transform.position, Quaternion.identity);
		}
		Gogan.LogEvent("Screen", "Enter", "Inventory", 0L);
	}

	public void Hide()
	{
		if (m_animator.GetBool("visible"))
		{
			m_craftTimer = -1f;
			m_animator.SetBool("visible", value: false);
			m_trophiesPanel.SetActive(value: false);
			m_variantDialog.gameObject.SetActive(value: false);
			m_skillsDialog.gameObject.SetActive(value: false);
			m_textsDialog.gameObject.SetActive(value: false);
			m_splitPanel.gameObject.SetActive(value: false);
			SetupDragItem(null, null, 1);
			if ((bool)m_currentContainer)
			{
				m_currentContainer.SetInUse(inUse: false);
				m_currentContainer = null;
			}
			if ((bool)Player.m_localPlayer)
			{
				m_closeInventoryEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
			}
			Gogan.LogEvent("Screen", "Exit", "Inventory", 0L);
		}
	}

	private void CloseContainer()
	{
		if (m_dragInventory != null && m_dragInventory != Player.m_localPlayer.GetInventory())
		{
			SetupDragItem(null, null, 1);
		}
		if ((bool)m_currentContainer)
		{
			m_currentContainer.SetInUse(inUse: false);
			m_currentContainer = null;
		}
		m_splitPanel.gameObject.SetActive(value: false);
		m_firstContainerUpdate = true;
		m_container.gameObject.SetActive(value: false);
	}

	private void SetupCrafting()
	{
		UpdateCraftingPanel(focusView: true);
	}

	private void UpdateCraftingPanel(bool focusView = false)
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer.GetCurrentCraftingStation() && !localPlayer.NoCostCheat())
		{
			m_tabCraft.interactable = false;
			m_tabUpgrade.interactable = true;
			m_tabUpgrade.gameObject.SetActive(value: false);
		}
		else
		{
			m_tabUpgrade.gameObject.SetActive(value: true);
		}
		List<Recipe> available = new List<Recipe>();
		localPlayer.GetAvailableRecipes(ref available);
		UpdateRecipeList(available);
		if (m_availableRecipes.Count > 0)
		{
			if (m_selectedRecipe.Key != null)
			{
				int selectedRecipeIndex = GetSelectedRecipeIndex();
				SetRecipe(selectedRecipeIndex, focusView);
			}
			else
			{
				SetRecipe(0, focusView);
			}
		}
		else
		{
			SetRecipe(-1, focusView);
		}
	}

	private void UpdateRecipeList(List<Recipe> recipes)
	{
		Player localPlayer = Player.m_localPlayer;
		m_availableRecipes.Clear();
		foreach (GameObject recipe3 in m_recipeList)
		{
			UnityEngine.Object.Destroy(recipe3);
		}
		m_recipeList.Clear();
		if (InCraftTab())
		{
			bool[] array = new bool[recipes.Count];
			for (int i = 0; i < recipes.Count; i++)
			{
				Recipe recipe = recipes[i];
				array[i] = localPlayer.HaveRequirements(recipe, discover: false, 1);
			}
			for (int j = 0; j < recipes.Count; j++)
			{
				if (array[j])
				{
					AddRecipeToList(localPlayer, recipes[j], null, canCraft: true);
				}
			}
			for (int k = 0; k < recipes.Count; k++)
			{
				if (!array[k])
				{
					AddRecipeToList(localPlayer, recipes[k], null, canCraft: false);
				}
			}
		}
		else
		{
			List<KeyValuePair<Recipe, ItemDrop.ItemData>> list = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();
			List<KeyValuePair<Recipe, ItemDrop.ItemData>> list2 = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();
			for (int l = 0; l < recipes.Count; l++)
			{
				Recipe recipe2 = recipes[l];
				if (recipe2.m_item.m_itemData.m_shared.m_maxQuality <= 1)
				{
					continue;
				}
				m_tempItemList.Clear();
				localPlayer.GetInventory().GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, m_tempItemList);
				foreach (ItemDrop.ItemData tempItem in m_tempItemList)
				{
					if (tempItem.m_quality < tempItem.m_shared.m_maxQuality && localPlayer.HaveRequirements(recipe2, discover: false, tempItem.m_quality + 1))
					{
						list.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, tempItem));
					}
					else
					{
						list2.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, tempItem));
					}
				}
			}
			foreach (KeyValuePair<Recipe, ItemDrop.ItemData> item in list)
			{
				AddRecipeToList(localPlayer, item.Key, item.Value, canCraft: true);
			}
			foreach (KeyValuePair<Recipe, ItemDrop.ItemData> item2 in list2)
			{
				AddRecipeToList(localPlayer, item2.Key, item2.Value, canCraft: false);
			}
		}
		float b = (float)m_recipeList.Count * m_recipeListSpace;
		b = Mathf.Max(m_recipeListBaseSize, b);
		m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
	}

	private void AddRecipeToList(Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
	{
		int count = m_recipeList.Count;
		GameObject element = UnityEngine.Object.Instantiate(m_recipeElementPrefab, m_recipeListRoot);
		element.SetActive(value: true);
		(element.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)count * (0f - m_recipeListSpace));
		Image component = element.transform.Find("icon").GetComponent<Image>();
		component.sprite = recipe.m_item.m_itemData.GetIcon();
		component.color = (canCraft ? Color.white : new Color(1f, 0f, 1f, 0f));
		Text component2 = element.transform.Find("name").GetComponent<Text>();
		string text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
		if (recipe.m_amount > 1)
		{
			text = text + " x" + recipe.m_amount;
		}
		component2.text = text;
		component2.color = (canCraft ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f));
		GuiBar component3 = element.transform.Find("Durability").GetComponent<GuiBar>();
		if (item != null && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
		{
			component3.gameObject.SetActive(value: true);
			component3.SetValue(item.GetDurabilityPercentage());
		}
		else
		{
			component3.gameObject.SetActive(value: false);
		}
		Text component4 = element.transform.Find("QualityLevel").GetComponent<Text>();
		if (item != null)
		{
			component4.gameObject.SetActive(value: true);
			component4.text = item.m_quality.ToString();
		}
		else
		{
			component4.gameObject.SetActive(value: false);
		}
		element.GetComponent<Button>().onClick.AddListener(delegate
		{
			OnSelectedRecipe(element);
		});
		m_recipeList.Add(element);
		m_availableRecipes.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe, item));
	}

	private void OnSelectedRecipe(GameObject button)
	{
		int index = FindSelectedRecipe(button);
		SetRecipe(index, center: false);
	}

	private void UpdateRecipeGamepadInput()
	{
		if (m_availableRecipes.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				SetRecipe(Mathf.Min(m_availableRecipes.Count - 1, GetSelectedRecipeIndex() + 1), center: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				SetRecipe(Mathf.Max(0, GetSelectedRecipeIndex() - 1), center: true);
			}
		}
	}

	private int GetSelectedRecipeIndex()
	{
		int result = 0;
		for (int i = 0; i < m_availableRecipes.Count; i++)
		{
			if (m_availableRecipes[i].Key == m_selectedRecipe.Key && m_availableRecipes[i].Value == m_selectedRecipe.Value)
			{
				result = i;
			}
		}
		return result;
	}

	private void SetRecipe(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index);
		for (int i = 0; i < m_recipeList.Count; i++)
		{
			bool active = i == index;
			m_recipeList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			m_recipeEnsureVisible.CenterOnItem(m_recipeList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			m_selectedRecipe = new KeyValuePair<Recipe, ItemDrop.ItemData>(null, null);
			m_selectedVariant = 0;
			return;
		}
		KeyValuePair<Recipe, ItemDrop.ItemData> selectedRecipe = m_availableRecipes[index];
		if (selectedRecipe.Key != m_selectedRecipe.Key || selectedRecipe.Value != m_selectedRecipe.Value)
		{
			m_selectedRecipe = selectedRecipe;
			m_selectedVariant = 0;
		}
	}

	private void UpdateRecipe(Player player, float dt)
	{
		CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
		if ((bool)currentCraftingStation)
		{
			m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
			m_craftingStationIcon.gameObject.SetActive(value: true);
			m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
			int level = currentCraftingStation.GetLevel();
			m_craftingStationLevel.text = level.ToString();
			m_craftingStationLevelRoot.gameObject.SetActive(value: true);
		}
		else
		{
			m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
			m_craftingStationIcon.gameObject.SetActive(value: false);
			m_craftingStationLevelRoot.gameObject.SetActive(value: false);
		}
		if ((bool)m_selectedRecipe.Key)
		{
			m_recipeIcon.enabled = true;
			m_recipeName.enabled = true;
			m_recipeDecription.enabled = true;
			ItemDrop.ItemData value = m_selectedRecipe.Value;
			int num = ((value == null) ? 1 : (value.m_quality + 1));
			bool flag = num <= m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality;
			int num2 = value?.m_variant ?? m_selectedVariant;
			m_recipeIcon.sprite = m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_icons[num2];
			string text = Localization.instance.Localize(m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name);
			if (m_selectedRecipe.Key.m_amount > 1)
			{
				text = text + " x" + m_selectedRecipe.Key.m_amount;
			}
			m_recipeName.text = text;
			m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(m_selectedRecipe.Key.m_item.m_itemData, num, crafting: true));
			if (value != null)
			{
				m_itemCraftType.gameObject.SetActive(value: true);
				if (value.m_quality >= value.m_shared.m_maxQuality)
				{
					m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
				}
				else
				{
					string text2 = Localization.instance.Localize(value.m_shared.m_name);
					m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade", text2, (value.m_quality + 1).ToString());
				}
			}
			else
			{
				m_itemCraftType.gameObject.SetActive(value: false);
			}
			m_variantButton.gameObject.SetActive(m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_variants > 1 && m_selectedRecipe.Value == null);
			SetupRequirementList(num, player, flag);
			int requiredStationLevel = m_selectedRecipe.Key.GetRequiredStationLevel(num);
			CraftingStation requiredStation = m_selectedRecipe.Key.GetRequiredStation(num);
			if (requiredStation != null && flag)
			{
				m_minStationLevelIcon.gameObject.SetActive(value: true);
				m_minStationLevelText.text = requiredStationLevel.ToString();
				if (currentCraftingStation == null || currentCraftingStation.GetLevel() < requiredStationLevel)
				{
					m_minStationLevelText.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : m_minStationLevelBasecolor);
				}
				else
				{
					m_minStationLevelText.color = m_minStationLevelBasecolor;
				}
			}
			else
			{
				m_minStationLevelIcon.gameObject.SetActive(value: false);
			}
			bool flag2 = player.HaveRequirements(m_selectedRecipe.Key, discover: false, num);
			bool flag3 = m_selectedRecipe.Value != null || player.GetInventory().HaveEmptySlot();
			bool flag4 = !requiredStation || ((bool)currentCraftingStation && currentCraftingStation.CheckUsable(player, showMessage: false));
			m_craftButton.interactable = ((flag2 && flag4) || player.NoCostCheat()) && flag3 && flag;
			Text componentInChildren = m_craftButton.GetComponentInChildren<Text>();
			if (num > 1)
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_upgradebutton");
			}
			else
			{
				componentInChildren.text = Localization.instance.Localize("$inventory_craftbutton");
			}
			UITooltip component = m_craftButton.GetComponent<UITooltip>();
			if (!flag3)
			{
				component.m_text = Localization.instance.Localize("$inventory_full");
			}
			else if (!flag2)
			{
				component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			}
			else if (!flag4)
			{
				component.m_text = Localization.instance.Localize("$msg_missingstation");
			}
			else
			{
				component.m_text = "";
			}
		}
		else
		{
			m_recipeIcon.enabled = false;
			m_recipeName.enabled = false;
			m_recipeDecription.enabled = false;
			m_qualityPanel.gameObject.SetActive(value: false);
			m_minStationLevelIcon.gameObject.SetActive(value: false);
			m_craftButton.GetComponent<UITooltip>().m_text = "";
			m_variantButton.gameObject.SetActive(value: false);
			m_itemCraftType.gameObject.SetActive(value: false);
			for (int i = 0; i < m_recipeRequirementList.Length; i++)
			{
				HideRequirement(m_recipeRequirementList[i].transform);
			}
			m_craftButton.interactable = false;
		}
		if (m_craftTimer < 0f)
		{
			m_craftProgressPanel.gameObject.SetActive(value: false);
			m_craftButton.gameObject.SetActive(value: true);
			return;
		}
		m_craftButton.gameObject.SetActive(value: false);
		m_craftProgressPanel.gameObject.SetActive(value: true);
		m_craftProgressBar.SetMaxValue(m_craftDuration);
		m_craftProgressBar.SetValue(m_craftTimer);
		m_craftTimer += dt;
		if (m_craftTimer >= m_craftDuration)
		{
			DoCrafting(player);
			m_craftTimer = -1f;
		}
	}

	private void SetupRequirementList(int quality, Player player, bool allowedQuality)
	{
		int i = 0;
		if (allowedQuality)
		{
			Piece.Requirement[] resources = m_selectedRecipe.Key.m_resources;
			foreach (Piece.Requirement req in resources)
			{
				if (SetupRequirement(m_recipeRequirementList[i].transform, req, player, craft: true, quality))
				{
					i++;
				}
			}
		}
		for (; i < m_recipeRequirementList.Length; i++)
		{
			HideRequirement(m_recipeRequirementList[i].transform);
		}
	}

	private void SetupUpgradeItem(Recipe recipe, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			m_upgradeItemIcon.sprite = recipe.m_item.m_itemData.m_shared.m_icons[m_selectedVariant];
			m_upgradeItemName.text = Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name);
			m_upgradeItemNextQuality.text = ((recipe.m_item.m_itemData.m_shared.m_maxQuality > 1) ? "1" : "");
			m_itemCraftType.text = Localization.instance.Localize("$inventory_new");
			m_upgradeItemDurability.gameObject.SetActive(recipe.m_item.m_itemData.m_shared.m_useDurability);
			if (recipe.m_item.m_itemData.m_shared.m_useDurability)
			{
				m_upgradeItemDurability.SetValue(1f);
			}
			return;
		}
		m_upgradeItemIcon.sprite = item.GetIcon();
		m_upgradeItemName.text = Localization.instance.Localize(item.m_shared.m_name);
		m_upgradeItemNextQuality.text = item.m_quality.ToString();
		m_upgradeItemDurability.gameObject.SetActive(item.m_shared.m_useDurability);
		if (item.m_shared.m_useDurability)
		{
			m_upgradeItemDurability.SetValue(item.GetDurabilityPercentage());
		}
		if (item.m_quality >= item.m_shared.m_maxQuality)
		{
			m_itemCraftType.text = Localization.instance.Localize("$inventory_maxquality");
		}
		else
		{
			m_itemCraftType.text = Localization.instance.Localize("$inventory_upgrade");
		}
	}

	public static bool SetupRequirement(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		Text component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
		Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
		UITooltip component4 = elementRoot.GetComponent<UITooltip>();
		if (req.m_resItem != null)
		{
			component.gameObject.SetActive(value: true);
			component2.gameObject.SetActive(value: true);
			component3.gameObject.SetActive(value: true);
			component.sprite = req.m_resItem.m_itemData.GetIcon();
			component.color = Color.white;
			component4.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			component2.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
			int num = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
			int amount = req.GetAmount(quality);
			if (amount <= 0)
			{
				HideRequirement(elementRoot);
				return false;
			}
			component3.text = amount.ToString();
			if (num < amount)
			{
				component3.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : Color.white);
			}
			else
			{
				component3.color = Color.white;
			}
		}
		return true;
	}

	public static void HideRequirement(Transform elementRoot)
	{
		Image component = elementRoot.transform.Find("res_icon").GetComponent<Image>();
		Text component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
		Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
		elementRoot.GetComponent<UITooltip>().m_text = "";
		component.gameObject.SetActive(value: false);
		component2.gameObject.SetActive(value: false);
		component3.gameObject.SetActive(value: false);
	}

	private void DoCrafting(Player player)
	{
		if (m_craftRecipe == null)
		{
			return;
		}
		int num = ((m_craftUpgradeItem == null) ? 1 : (m_craftUpgradeItem.m_quality + 1));
		if (num > m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality || (!player.HaveRequirements(m_craftRecipe, discover: false, num) && !player.NoCostCheat()) || (m_craftUpgradeItem != null && !player.GetInventory().ContainsItem(m_craftUpgradeItem)) || (m_craftUpgradeItem == null && !player.GetInventory().HaveEmptySlot()))
		{
			return;
		}
		if (m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_dlcrequired");
			return;
		}
		int variant = m_craftVariant;
		if (m_craftUpgradeItem != null)
		{
			variant = m_craftUpgradeItem.m_variant;
			player.UnequipItem(m_craftUpgradeItem);
			player.GetInventory().RemoveItem(m_craftUpgradeItem);
		}
		long playerID = player.GetPlayerID();
		string playerName = player.GetPlayerName();
		if (player.GetInventory().AddItem(m_craftRecipe.m_item.gameObject.name, m_craftRecipe.m_amount, num, variant, playerID, playerName) != null)
		{
			if (!player.NoCostCheat())
			{
				player.ConsumeResources(m_craftRecipe.m_resources, num);
			}
			UpdateCraftingPanel();
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if ((bool)currentCraftingStation)
		{
			currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
		}
		else
		{
			m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
		}
		Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
		Gogan.LogEvent("Game", "Crafted", m_craftRecipe.m_item.m_itemData.m_shared.m_name, num);
	}

	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < m_recipeList.Count; i++)
		{
			if (m_recipeList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnCraftCancelPressed()
	{
		if (m_craftTimer >= 0f)
		{
			m_craftTimer = -1f;
		}
	}

	private void OnCraftPressed()
	{
		if (!m_selectedRecipe.Key)
		{
			return;
		}
		m_craftRecipe = m_selectedRecipe.Key;
		m_craftUpgradeItem = m_selectedRecipe.Value;
		m_craftVariant = m_selectedVariant;
		m_craftTimer = 0f;
		if ((bool)m_craftRecipe.m_craftingStation)
		{
			CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
			if ((bool)currentCraftingStation)
			{
				currentCraftingStation.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
			}
		}
		else
		{
			m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
		}
	}

	private void OnRepairPressed()
	{
		RepairOneItem();
		UpdateRepair();
	}

	private void UpdateRepair()
	{
		if (Player.m_localPlayer.GetCurrentCraftingStation() == null && !Player.m_localPlayer.NoCostCheat())
		{
			m_repairPanel.gameObject.SetActive(value: false);
			m_repairPanelSelection.gameObject.SetActive(value: false);
			m_repairButton.gameObject.SetActive(value: false);
			return;
		}
		m_repairButton.gameObject.SetActive(value: true);
		m_repairPanel.gameObject.SetActive(value: true);
		m_repairPanelSelection.gameObject.SetActive(value: true);
		if (HaveRepairableItems())
		{
			m_repairButton.interactable = true;
			m_repairButtonGlow.gameObject.SetActive(value: true);
			Color color = m_repairButtonGlow.color;
			color.a = 0.5f + Mathf.Sin(Time.time * 5f) * 0.5f;
			m_repairButtonGlow.color = color;
		}
		else
		{
			m_repairButton.interactable = false;
			m_repairButtonGlow.gameObject.SetActive(value: false);
		}
	}

	private void RepairOneItem()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if ((currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat()) || ((bool)currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, showMessage: false)))
		{
			return;
		}
		m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(m_tempWornItems);
		foreach (ItemDrop.ItemData tempWornItem in m_tempWornItems)
		{
			if (CanRepair(tempWornItem))
			{
				tempWornItem.m_durability = tempWornItem.GetMaxDurability();
				if ((bool)currentCraftingStation)
				{
					currentCraftingStation.m_repairItemDoneEffects.Create(currentCraftingStation.transform.position, Quaternion.identity);
				}
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", tempWornItem.m_shared.m_name));
				return;
			}
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No more item to repair");
	}

	private bool HaveRepairableItems()
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null && !Player.m_localPlayer.NoCostCheat())
		{
			return false;
		}
		if ((bool)currentCraftingStation && !currentCraftingStation.CheckUsable(Player.m_localPlayer, showMessage: false))
		{
			return false;
		}
		m_tempWornItems.Clear();
		Player.m_localPlayer.GetInventory().GetWornItems(m_tempWornItems);
		foreach (ItemDrop.ItemData tempWornItem in m_tempWornItems)
		{
			if (CanRepair(tempWornItem))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanRepair(ItemDrop.ItemData item)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (!item.m_shared.m_canBeReparied)
		{
			return false;
		}
		if (Player.m_localPlayer.NoCostCheat())
		{
			return true;
		}
		CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
		if (currentCraftingStation == null)
		{
			return false;
		}
		Recipe recipe = ObjectDB.instance.GetRecipe(item);
		if (recipe == null)
		{
			return false;
		}
		if (recipe.m_craftingStation == null && recipe.m_repairStation == null)
		{
			return false;
		}
		if ((recipe.m_repairStation != null && recipe.m_repairStation.m_name == currentCraftingStation.m_name) || (recipe.m_craftingStation != null && recipe.m_craftingStation.m_name == currentCraftingStation.m_name))
		{
			if (currentCraftingStation.GetLevel() < recipe.m_minStationLevel)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private void SetupDragItem(ItemDrop.ItemData item, Inventory inventory, int amount)
	{
		if ((bool)m_dragGo)
		{
			UnityEngine.Object.Destroy(m_dragGo);
			m_dragGo = null;
			m_dragItem = null;
			m_dragInventory = null;
			m_dragAmount = 0;
		}
		if (item != null)
		{
			m_dragGo = UnityEngine.Object.Instantiate(m_dragItemPrefab, base.transform);
			m_dragItem = item;
			m_dragInventory = inventory;
			m_dragAmount = amount;
			m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
			UITooltip.HideTooltip();
		}
	}

	private void ShowSplitDialog(ItemDrop.ItemData item, Inventory fromIventory)
	{
		bool num = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		m_splitSlider.minValue = 1f;
		m_splitSlider.maxValue = item.m_stack;
		if (!num)
		{
			m_splitSlider.value = Mathf.CeilToInt((float)item.m_stack / 2f);
		}
		else if (m_splitSlider.value / (float)item.m_stack > 0.5f)
		{
			m_splitSlider.value = Mathf.Min(m_splitSlider.value, item.m_stack);
		}
		m_splitIcon.sprite = item.GetIcon();
		m_splitIconName.text = Localization.instance.Localize(item.m_shared.m_name);
		m_splitPanel.gameObject.SetActive(value: true);
		m_splitItem = item;
		m_splitInventory = fromIventory;
		OnSplitSliderChanged(m_splitSlider.value);
	}

	private void OnSplitSliderChanged(float value)
	{
		m_splitAmount.text = (int)value + "/" + (int)m_splitSlider.maxValue;
	}

	private void OnSplitCancel()
	{
		m_splitItem = null;
		m_splitInventory = null;
		m_splitPanel.gameObject.SetActive(value: false);
	}

	private void OnSplitOk()
	{
		SetupDragItem(m_splitItem, m_splitInventory, (int)m_splitSlider.value);
		m_splitItem = null;
		m_splitInventory = null;
		m_splitPanel.gameObject.SetActive(value: false);
	}

	public void OnOpenSkills()
	{
		if ((bool)Player.m_localPlayer)
		{
			m_skillsDialog.Setup(Player.m_localPlayer);
			Gogan.LogEvent("Screen", "Enter", "Skills", 0L);
		}
	}

	public void OnOpenTexts()
	{
		if ((bool)Player.m_localPlayer)
		{
			m_textsDialog.Setup(Player.m_localPlayer);
			Gogan.LogEvent("Screen", "Enter", "Texts", 0L);
		}
	}

	public void OnOpenTrophies()
	{
		m_trophiesPanel.SetActive(value: true);
		UpdateTrophyList();
		Gogan.LogEvent("Screen", "Enter", "Trophies", 0L);
	}

	public void OnCloseTrophies()
	{
		m_trophiesPanel.SetActive(value: false);
	}

	private void UpdateTrophyList()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		foreach (GameObject trophy in m_trophyList)
		{
			UnityEngine.Object.Destroy(trophy);
		}
		m_trophyList.Clear();
		List<string> trophies = Player.m_localPlayer.GetTrophies();
		float num = 0f;
		for (int i = 0; i < trophies.Count; i++)
		{
			string text = trophies[i];
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(text);
			if (itemPrefab == null)
			{
				ZLog.LogWarning("Missing trophy prefab:" + text);
				continue;
			}
			ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
			GameObject gameObject = UnityEngine.Object.Instantiate(m_trophieElementPrefab, m_trophieListRoot);
			gameObject.SetActive(value: true);
			RectTransform rectTransform = gameObject.transform as RectTransform;
			rectTransform.anchoredPosition = new Vector2((float)component.m_itemData.m_shared.m_trophyPos.x * m_trophieListSpace, (float)component.m_itemData.m_shared.m_trophyPos.y * (0f - m_trophieListSpace));
			num = Mathf.Min(num, rectTransform.anchoredPosition.y - m_trophieListSpace);
			string text2 = Localization.instance.Localize(component.m_itemData.m_shared.m_name);
			if (text2.EndsWith(" trophy"))
			{
				text2 = text2.Remove(text2.Length - 7);
			}
			rectTransform.Find("icon_bkg/icon").GetComponent<Image>().sprite = component.m_itemData.GetIcon();
			rectTransform.Find("name").GetComponent<Text>().text = text2;
			rectTransform.Find("description").GetComponent<Text>().text = Localization.instance.Localize(component.m_itemData.m_shared.m_name + "_lore");
			m_trophyList.Add(gameObject);
		}
		ZLog.Log("SIZE " + num);
		float size = Mathf.Max(m_trophieListBaseSize, 0f - num);
		m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		m_trophyListScroll.value = 1f;
	}

	public void OnShowVariantSelection()
	{
		m_variantDialog.Setup(m_selectedRecipe.Key.m_item.m_itemData);
		Gogan.LogEvent("Screen", "Enter", "VariantSelection", 0L);
	}

	private void OnVariantSelected(int index)
	{
		ZLog.Log("Item variant selected " + index);
		m_selectedVariant = index;
	}

	public bool InUpradeTab()
	{
		return !m_tabUpgrade.interactable;
	}

	public bool InCraftTab()
	{
		return !m_tabCraft.interactable;
	}

	public void OnTabCraftPressed()
	{
		m_tabCraft.interactable = false;
		m_tabUpgrade.interactable = true;
		UpdateCraftingPanel();
	}

	public void OnTabUpgradePressed()
	{
		m_tabCraft.interactable = true;
		m_tabUpgrade.interactable = false;
		UpdateCraftingPanel();
	}
}
