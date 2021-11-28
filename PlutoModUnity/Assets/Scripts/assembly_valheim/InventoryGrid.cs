using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
	private class Element
	{
		public Vector2i m_pos;

		public GameObject m_go;

		public Image m_icon;

		public Text m_amount;

		public Text m_quality;

		public Image m_equiped;

		public Image m_queued;

		public GameObject m_selected;

		public Image m_noteleport;

		public Image m_food;

		public UITooltip m_tooltip;

		public GuiBar m_durability;

		public bool m_used;
	}

	public enum Modifier
	{
		Select,
		Split,
		Move
	}

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i, Modifier> m_onSelected;

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i> m_onRightClick;

	public GameObject m_elementPrefab;

	public RectTransform m_gridRoot;

	public Scrollbar m_scrollbar;

	public UIGroupHandler m_uiGroup;

	public float m_elementSpace = 10f;

	private int m_width = 4;

	private int m_height = 4;

	private Vector2i m_selected = new Vector2i(0, 0);

	private Inventory m_inventory;

	private List<Element> m_elements = new List<Element>();

	protected void Awake()
	{
	}

	public void ResetView()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (m_gridRoot.rect.height > rectTransform.rect.height)
		{
			m_gridRoot.pivot = new Vector2(m_gridRoot.pivot.x, 1f);
		}
		else
		{
			m_gridRoot.pivot = new Vector2(m_gridRoot.pivot.x, 0.5f);
		}
		m_gridRoot.anchoredPosition = new Vector2(0f, 0f);
	}

	public void UpdateInventory(Inventory inventory, Player player, ItemDrop.ItemData dragItem)
	{
		m_inventory = inventory;
		UpdateGamepad();
		UpdateGui(player, dragItem);
	}

	private void UpdateGamepad()
	{
		if (!m_uiGroup.IsActive())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft"))
		{
			m_selected.x = Mathf.Max(0, m_selected.x - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight"))
		{
			m_selected.x = Mathf.Min(m_width - 1, m_selected.x + 1);
		}
		if (ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp"))
		{
			m_selected.y = Mathf.Max(0, m_selected.y - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown"))
		{
			m_selected.y = Mathf.Min(m_width - 1, m_selected.y + 1);
		}
		if (ZInput.GetButtonDown("JoyButtonA"))
		{
			Modifier arg = Modifier.Select;
			if (ZInput.GetButton("JoyLTrigger"))
			{
				arg = Modifier.Split;
			}
			if (ZInput.GetButton("JoyRTrigger"))
			{
				arg = Modifier.Move;
			}
			ItemDrop.ItemData gamepadSelectedItem = GetGamepadSelectedItem();
			m_onSelected(this, gamepadSelectedItem, m_selected, arg);
		}
		if (ZInput.GetButtonDown("JoyButtonX"))
		{
			ItemDrop.ItemData gamepadSelectedItem2 = GetGamepadSelectedItem();
			m_onRightClick(this, gamepadSelectedItem2, m_selected);
		}
	}

	private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		int width = m_inventory.GetWidth();
		int height = m_inventory.GetHeight();
		if (m_selected.x >= width - 1)
		{
			m_selected.x = width - 1;
		}
		if (m_selected.y >= height - 1)
		{
			m_selected.y = height - 1;
		}
		if (m_width != width || m_height != height)
		{
			m_width = width;
			m_height = height;
			foreach (Element element3 in m_elements)
			{
				UnityEngine.Object.Destroy(element3.m_go);
			}
			m_elements.Clear();
			Vector2 widgetSize = GetWidgetSize();
			Vector2 vector = new Vector2(rectTransform.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					Vector2 vector2 = new Vector3((float)j * m_elementSpace, (float)i * (0f - m_elementSpace));
					GameObject gameObject = UnityEngine.Object.Instantiate(m_elementPrefab, m_gridRoot);
					(gameObject.transform as RectTransform).anchoredPosition = vector + vector2;
					UIInputHandler componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
					componentInChildren.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onRightDown, new Action<UIInputHandler>(OnRightClick));
					componentInChildren.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onLeftDown, new Action<UIInputHandler>(OnLeftClick));
					Text component = gameObject.transform.Find("binding").GetComponent<Text>();
					if ((bool)player && i == 0)
					{
						component.text = (j + 1).ToString();
					}
					else
					{
						component.enabled = false;
					}
					Element element = new Element();
					element.m_pos = new Vector2i(j, i);
					element.m_go = gameObject;
					element.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					element.m_amount = gameObject.transform.Find("amount").GetComponent<Text>();
					element.m_quality = gameObject.transform.Find("quality").GetComponent<Text>();
					element.m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>();
					element.m_queued = gameObject.transform.Find("queued").GetComponent<Image>();
					element.m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>();
					element.m_food = gameObject.transform.Find("foodicon").GetComponent<Image>();
					element.m_selected = gameObject.transform.Find("selected").gameObject;
					element.m_tooltip = gameObject.GetComponent<UITooltip>();
					element.m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>();
					m_elements.Add(element);
				}
			}
		}
		foreach (Element element4 in m_elements)
		{
			element4.m_used = false;
		}
		bool flag = m_uiGroup.IsActive() && ZInput.IsGamepadActive();
		foreach (ItemDrop.ItemData allItem in m_inventory.GetAllItems())
		{
			Element element2 = GetElement(allItem.m_gridPos.x, allItem.m_gridPos.y, width);
			element2.m_used = true;
			element2.m_icon.enabled = true;
			element2.m_icon.sprite = allItem.GetIcon();
			element2.m_icon.color = ((allItem == dragItem) ? Color.grey : Color.white);
			element2.m_durability.gameObject.SetActive(allItem.m_shared.m_useDurability);
			if (allItem.m_shared.m_useDurability)
			{
				if (allItem.m_durability <= 0f)
				{
					element2.m_durability.SetValue(1f);
					element2.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					element2.m_durability.SetValue(allItem.GetDurabilityPercentage());
					element2.m_durability.ResetColor();
				}
			}
			element2.m_equiped.enabled = (bool)player && allItem.m_equiped;
			element2.m_queued.enabled = (bool)player && player.IsItemQueued(allItem);
			element2.m_noteleport.enabled = !allItem.m_shared.m_teleportable;
			if (allItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (allItem.m_shared.m_food > 0f || allItem.m_shared.m_foodStamina > 0f))
			{
				element2.m_food.enabled = true;
				if (allItem.m_shared.m_foodStamina < allItem.m_shared.m_food / 2f)
				{
					element2.m_food.color = new Color(1f, 0.5f, 0.5f, 1f);
				}
				else if (allItem.m_shared.m_food < allItem.m_shared.m_foodStamina / 2f)
				{
					element2.m_food.color = new Color(1f, 1f, 0.5f, 1f);
				}
				else
				{
					element2.m_food.color = Color.white;
				}
			}
			else
			{
				element2.m_food.enabled = false;
			}
			if (dragItem == null)
			{
				CreateItemTooltip(allItem, element2.m_tooltip);
			}
			element2.m_quality.enabled = allItem.m_shared.m_maxQuality > 1;
			if (allItem.m_shared.m_maxQuality > 1)
			{
				element2.m_quality.text = allItem.m_quality.ToString();
			}
			element2.m_amount.enabled = allItem.m_shared.m_maxStackSize > 1;
			if (allItem.m_shared.m_maxStackSize > 1)
			{
				element2.m_amount.text = allItem.m_stack + "/" + allItem.m_shared.m_maxStackSize;
			}
		}
		foreach (Element element5 in m_elements)
		{
			element5.m_selected.SetActive(flag && element5.m_pos == m_selected);
			if (!element5.m_used)
			{
				element5.m_durability.gameObject.SetActive(value: false);
				element5.m_icon.enabled = false;
				element5.m_amount.enabled = false;
				element5.m_quality.enabled = false;
				element5.m_equiped.enabled = false;
				element5.m_queued.enabled = false;
				element5.m_noteleport.enabled = false;
				element5.m_food.enabled = false;
				element5.m_tooltip.m_text = "";
				element5.m_tooltip.m_topic = "";
			}
		}
		float size = (float)height * m_elementSpace;
		m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
	}

	private void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip)
	{
		tooltip.Set(item.m_shared.m_name, item.GetTooltip());
	}

	public Vector2 GetWidgetSize()
	{
		return new Vector2((float)m_width * m_elementSpace, (float)m_height * m_elementSpace);
	}

	private void OnRightClick(UIInputHandler element)
	{
		GameObject go = element.gameObject;
		Vector2i buttonPos = GetButtonPos(go);
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		if (m_onRightClick != null)
		{
			m_onRightClick(this, itemAt, buttonPos);
		}
	}

	private void OnLeftClick(UIInputHandler clickHandler)
	{
		GameObject go = clickHandler.gameObject;
		Vector2i buttonPos = GetButtonPos(go);
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		Modifier arg = Modifier.Select;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			arg = Modifier.Split;
		}
		else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			arg = Modifier.Move;
		}
		if (m_onSelected != null)
		{
			m_onSelected(this, itemAt, buttonPos, arg);
		}
	}

	private Element GetElement(int x, int y, int width)
	{
		int index = y * width + x;
		return m_elements[index];
	}

	private Vector2i GetButtonPos(GameObject go)
	{
		for (int i = 0; i < m_elements.Count; i++)
		{
			if (m_elements[i].m_go == go)
			{
				int num = i / m_width;
				return new Vector2i(i - num * m_width, num);
			}
		}
		return new Vector2i(-1, -1);
	}

	public bool DropItem(Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos)
	{
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(pos.x, pos.y);
		if (itemAt == item)
		{
			return true;
		}
		if (itemAt != null && (itemAt.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1) && item.m_stack == amount)
		{
			fromInventory.RemoveItem(item);
			fromInventory.MoveItemToThis(m_inventory, itemAt, itemAt.m_stack, item.m_gridPos.x, item.m_gridPos.y);
			m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
			return true;
		}
		return m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
	}

	public ItemDrop.ItemData GetItem(Vector2i cursorPosition)
	{
		foreach (Element element in m_elements)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(element.m_go.transform as RectTransform, cursorPosition.ToVector2()))
			{
				Vector2i buttonPos = GetButtonPos(element.m_go);
				return m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
			}
		}
		return null;
	}

	public Inventory GetInventory()
	{
		return m_inventory;
	}

	public void SetSelection(Vector2i pos)
	{
		m_selected = pos;
	}

	public ItemDrop.ItemData GetGamepadSelectedItem()
	{
		if (!m_uiGroup.IsActive())
		{
			return null;
		}
		return m_inventory.GetItemAt(m_selected.x, m_selected.y);
	}

	public RectTransform GetGamepadSelectedElement()
	{
		if (!m_uiGroup.IsActive())
		{
			return null;
		}
		if (m_selected.x < 0 || m_selected.x >= m_width || m_selected.y < 0 || m_selected.y >= m_height)
		{
			return null;
		}
		return GetElement(m_selected.x, m_selected.y, m_width).m_go.transform as RectTransform;
	}
}
