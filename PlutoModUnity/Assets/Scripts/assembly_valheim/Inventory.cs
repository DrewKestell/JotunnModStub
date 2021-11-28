using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
	private int currentVersion = 103;

	public Action m_onChanged;

	private string m_name = "";

	private Sprite m_bkg;

	private List<ItemDrop.ItemData> m_inventory = new List<ItemDrop.ItemData>();

	private int m_width = 4;

	private int m_height = 4;

	private float m_totalWeight;

	public Inventory(string name, Sprite bkg, int w, int h)
	{
		m_bkg = bkg;
		m_name = name;
		m_width = w;
		m_height = h;
	}

	private bool AddItem(ItemDrop.ItemData item, int amount, int x, int y)
	{
		amount = Mathf.Min(amount, item.m_stack);
		if (x < 0 || y < 0 || x >= m_width || y >= m_height)
		{
			return false;
		}
		bool flag = false;
		ItemDrop.ItemData itemAt = GetItemAt(x, y);
		if (itemAt != null)
		{
			if (itemAt.m_shared.m_name != item.m_shared.m_name || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
			{
				return false;
			}
			int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
			if (num <= 0)
			{
				return false;
			}
			int num2 = Mathf.Min(num, amount);
			itemAt.m_stack += num2;
			item.m_stack -= num2;
			flag = num2 == amount;
			ZLog.Log("Added to stack" + itemAt.m_stack + " " + item.m_stack);
		}
		else
		{
			ItemDrop.ItemData itemData = item.Clone();
			itemData.m_stack = amount;
			itemData.m_gridPos = new Vector2i(x, y);
			m_inventory.Add(itemData);
			item.m_stack -= amount;
			flag = true;
		}
		Changed();
		return flag;
	}

	public bool CanAddItem(GameObject prefab, int stack = -1)
	{
		ItemDrop component = prefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			return false;
		}
		return CanAddItem(component.m_itemData, stack);
	}

	public bool CanAddItem(ItemDrop.ItemData item, int stack = -1)
	{
		if (HaveEmptySlot())
		{
			return true;
		}
		if (stack <= 0)
		{
			stack = item.m_stack;
		}
		return FindFreeStackSpace(item.m_shared.m_name) >= stack;
	}

	public bool AddItem(GameObject prefab, int amount)
	{
		ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
		itemData.m_dropPrefab = prefab;
		itemData.m_stack = Mathf.Min(amount, itemData.m_shared.m_maxStackSize);
		ZLog.Log("adding " + prefab.name + "  " + itemData.m_stack);
		return AddItem(itemData);
	}

	public bool AddItem(ItemDrop.ItemData item)
	{
		bool result = true;
		if (item.m_shared.m_maxStackSize > 1)
		{
			for (int i = 0; i < item.m_stack; i++)
			{
				ItemDrop.ItemData itemData = FindFreeStackItem(item.m_shared.m_name, item.m_quality);
				if (itemData != null)
				{
					itemData.m_stack++;
					continue;
				}
				item.m_stack -= i;
				Vector2i gridPos = FindEmptySlot(TopFirst(item));
				if (gridPos.x >= 0)
				{
					item.m_gridPos = gridPos;
					m_inventory.Add(item);
				}
				else
				{
					result = false;
				}
				break;
			}
		}
		else
		{
			Vector2i gridPos2 = FindEmptySlot(TopFirst(item));
			if (gridPos2.x >= 0)
			{
				item.m_gridPos = gridPos2;
				m_inventory.Add(item);
			}
			else
			{
				result = false;
			}
		}
		Changed();
		return result;
	}

	private bool TopFirst(ItemDrop.ItemData item)
	{
		if (item.IsWeapon())
		{
			return true;
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Misc)
		{
			return true;
		}
		return false;
	}

	public void MoveAll(Inventory fromInventory)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData item in list)
		{
			if (AddItem(item, item.m_stack, item.m_gridPos.x, item.m_gridPos.y))
			{
				fromInventory.RemoveItem(item);
			}
			else
			{
				list2.Add(item);
			}
		}
		foreach (ItemDrop.ItemData item2 in list2)
		{
			if (AddItem(item2))
			{
				fromInventory.RemoveItem(item2);
				continue;
			}
			break;
		}
		Changed();
		fromInventory.Changed();
	}

	public void MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item)
	{
		if (AddItem(item))
		{
			fromInventory.RemoveItem(item);
		}
		Changed();
		fromInventory.Changed();
	}

	public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
	{
		bool result = AddItem(item, amount, x, y);
		if (item.m_stack == 0)
		{
			fromInventory.RemoveItem(item);
			return result;
		}
		fromInventory.Changed();
		return result;
	}

	public bool RemoveItem(int index)
	{
		if (index < 0 || index >= m_inventory.Count)
		{
			return false;
		}
		m_inventory.RemoveAt(index);
		Changed();
		return true;
	}

	public bool ContainsItem(ItemDrop.ItemData item)
	{
		return m_inventory.Contains(item);
	}

	public bool RemoveOneItem(ItemDrop.ItemData item)
	{
		if (!m_inventory.Contains(item))
		{
			return false;
		}
		if (item.m_stack > 1)
		{
			item.m_stack--;
			Changed();
		}
		else
		{
			m_inventory.Remove(item);
			Changed();
		}
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item)
	{
		if (!m_inventory.Contains(item))
		{
			ZLog.Log("Item is not in this container");
			return false;
		}
		m_inventory.Remove(item);
		Changed();
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item, int amount)
	{
		amount = Mathf.Min(item.m_stack, amount);
		if (amount == item.m_stack)
		{
			return RemoveItem(item);
		}
		if (!m_inventory.Contains(item))
		{
			return false;
		}
		item.m_stack -= amount;
		Changed();
		return true;
	}

	public void RemoveItem(string name, int amount)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name)
			{
				int num = Mathf.Min(item.m_stack, amount);
				item.m_stack -= num;
				amount -= num;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		m_inventory.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
		Changed();
	}

	public bool HaveItem(string name)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name)
			{
				return true;
			}
		}
		return false;
	}

	public void GetAllPieceTables(List<PieceTable> tables)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_buildPieces != null && !tables.Contains(item.m_shared.m_buildPieces))
			{
				tables.Add(item.m_shared.m_buildPieces);
			}
		}
	}

	public int CountItems(string name)
	{
		int num = 0;
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name)
			{
				num += item.m_stack;
			}
		}
		return num;
	}

	public ItemDrop.ItemData GetItem(int index)
	{
		return m_inventory[index];
	}

	public ItemDrop.ItemData GetItem(string name)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name)
			{
				return item;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetAmmoItem(string ammoName)
	{
		int num = 0;
		ItemDrop.ItemData itemData = null;
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if ((item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) && item.m_shared.m_ammoType == ammoName)
			{
				int num2 = item.m_gridPos.y * m_width + item.m_gridPos.x;
				if (num2 < num || itemData == null)
				{
					num = num2;
					itemData = item;
				}
			}
		}
		return itemData;
	}

	private int FindFreeStackSpace(string name)
	{
		int num = 0;
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name && item.m_stack < item.m_shared.m_maxStackSize)
			{
				num += item.m_shared.m_maxStackSize - item.m_stack;
			}
		}
		return num;
	}

	private ItemDrop.ItemData FindFreeStackItem(string name, int quality)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name && item.m_quality == quality && item.m_stack < item.m_shared.m_maxStackSize)
			{
				return item;
			}
		}
		return null;
	}

	public int NrOfItems()
	{
		return m_inventory.Count;
	}

	public int NrOfItemsIncludingStacks()
	{
		int num = 0;
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			num += item.m_stack;
		}
		return num;
	}

	public float SlotsUsedPercentage()
	{
		return (float)m_inventory.Count / (float)(m_width * m_height) * 100f;
	}

	public void Print()
	{
		for (int i = 0; i < m_inventory.Count; i++)
		{
			ItemDrop.ItemData itemData = m_inventory[i];
			ZLog.Log(i.ToString() + ": " + itemData.m_shared.m_name + "  " + itemData.m_stack + " / " + itemData.m_shared.m_maxStackSize);
		}
	}

	public int GetEmptySlots()
	{
		return m_height * m_width - m_inventory.Count;
	}

	public bool HaveEmptySlot()
	{
		return m_inventory.Count < m_width * m_height;
	}

	private Vector2i FindEmptySlot(bool topFirst)
	{
		if (topFirst)
		{
			for (int i = 0; i < m_height; i++)
			{
				for (int j = 0; j < m_width; j++)
				{
					if (GetItemAt(j, i) == null)
					{
						return new Vector2i(j, i);
					}
				}
			}
		}
		else
		{
			for (int num = m_height - 1; num >= 0; num--)
			{
				for (int k = 0; k < m_width; k++)
				{
					if (GetItemAt(k, num) == null)
					{
						return new Vector2i(k, num);
					}
				}
			}
		}
		return new Vector2i(-1, -1);
	}

	public ItemDrop.ItemData GetOtherItemAt(int x, int y, ItemDrop.ItemData oldItem)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item != oldItem && item.m_gridPos.x == x && item.m_gridPos.y == y)
			{
				return item;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetItemAt(int x, int y)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_gridPos.x == x && item.m_gridPos.y == y)
			{
				return item;
			}
		}
		return null;
	}

	public List<ItemDrop.ItemData> GetEquipedtems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_equiped)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public void GetWornItems(List<ItemDrop.ItemData> worn)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
			{
				worn.Add(item);
			}
		}
	}

	public void GetValuableItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_value > 0)
			{
				items.Add(item);
			}
		}
	}

	public List<ItemDrop.ItemData> GetAllItems()
	{
		return m_inventory;
	}

	public void GetAllItems(string name, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_name == name)
			{
				items.Add(item);
			}
		}
	}

	public void GetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_shared.m_itemType == type)
			{
				items.Add(item);
			}
		}
	}

	public int GetWidth()
	{
		return m_width;
	}

	public int GetHeight()
	{
		return m_height;
	}

	public string GetName()
	{
		return m_name;
	}

	public Sprite GetBkg()
	{
		return m_bkg;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(currentVersion);
		pkg.Write(m_inventory.Count);
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_dropPrefab == null)
			{
				ZLog.Log("Item missing prefab " + item.m_shared.m_name);
				pkg.Write("");
			}
			else
			{
				pkg.Write(item.m_dropPrefab.name);
			}
			pkg.Write(item.m_stack);
			pkg.Write(item.m_durability);
			pkg.Write(item.m_gridPos);
			pkg.Write(item.m_equiped);
			pkg.Write(item.m_quality);
			pkg.Write(item.m_variant);
			pkg.Write(item.m_crafterID);
			pkg.Write(item.m_crafterName);
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		int num2 = pkg.ReadInt();
		m_inventory.Clear();
		for (int i = 0; i < num2; i++)
		{
			string text = pkg.ReadString();
			int stack = pkg.ReadInt();
			float durability = pkg.ReadSingle();
			Vector2i pos = pkg.ReadVector2i();
			bool equiped = pkg.ReadBool();
			int quality = 1;
			if (num >= 101)
			{
				quality = pkg.ReadInt();
			}
			int variant = 0;
			if (num >= 102)
			{
				variant = pkg.ReadInt();
			}
			long crafterID = 0L;
			string crafterName = "";
			if (num >= 103)
			{
				crafterID = pkg.ReadLong();
				crafterName = pkg.ReadString();
			}
			if (text != "")
			{
				AddItem(text, stack, durability, pos, equiped, quality, variant, crafterID, crafterName);
			}
		}
		Changed();
	}

	public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Invalid item " + name);
			return null;
		}
		if (FindEmptySlot(TopFirst(component.m_itemData)).x == -1)
		{
			return null;
		}
		ItemDrop.ItemData result = null;
		int num = stack;
		while (num > 0)
		{
			ZNetView.m_forceDisableInit = true;
			GameObject gameObject = UnityEngine.Object.Instantiate(itemPrefab);
			ZNetView.m_forceDisableInit = false;
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			if (component2 == null)
			{
				ZLog.Log("Missing itemdrop in " + name);
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			int num2 = Mathf.Min(num, component2.m_itemData.m_shared.m_maxStackSize);
			num -= num2;
			component2.m_itemData.m_stack = num2;
			component2.m_itemData.m_quality = quality;
			component2.m_itemData.m_variant = variant;
			component2.m_itemData.m_durability = component2.m_itemData.GetMaxDurability();
			component2.m_itemData.m_crafterID = crafterID;
			component2.m_itemData.m_crafterName = crafterName;
			AddItem(component2.m_itemData);
			result = component2.m_itemData;
			UnityEngine.Object.Destroy(gameObject);
		}
		return result;
	}

	private bool AddItem(string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return false;
		}
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate(itemPrefab);
		ZNetView.m_forceDisableInit = false;
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Missing itemdrop in " + name);
			UnityEngine.Object.Destroy(gameObject);
			return false;
		}
		component.m_itemData.m_stack = Mathf.Min(stack, component.m_itemData.m_shared.m_maxStackSize);
		component.m_itemData.m_durability = durability;
		component.m_itemData.m_equiped = equiped;
		component.m_itemData.m_quality = quality;
		component.m_itemData.m_variant = variant;
		component.m_itemData.m_crafterID = crafterID;
		component.m_itemData.m_crafterName = crafterName;
		AddItem(component.m_itemData, component.m_itemData.m_stack, pos.x, pos.y);
		UnityEngine.Object.Destroy(gameObject);
		return true;
	}

	public void MoveInventoryToGrave(Inventory original)
	{
		m_inventory.Clear();
		m_width = original.m_width;
		m_height = original.m_height;
		foreach (ItemDrop.ItemData item in original.m_inventory)
		{
			if (!item.m_shared.m_questItem && !item.m_equiped)
			{
				m_inventory.Add(item);
			}
		}
		original.m_inventory.RemoveAll((ItemDrop.ItemData x) => !x.m_shared.m_questItem && !x.m_equiped);
		original.Changed();
		Changed();
	}

	private void Changed()
	{
		UpdateTotalWeight();
		if (m_onChanged != null)
		{
			m_onChanged();
		}
	}

	public void RemoveAll()
	{
		m_inventory.Clear();
		Changed();
	}

	private void UpdateTotalWeight()
	{
		m_totalWeight = 0f;
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			m_totalWeight += item.GetWeight();
		}
	}

	public float GetTotalWeight()
	{
		return m_totalWeight;
	}

	public void GetBoundItems(List<ItemDrop.ItemData> bound)
	{
		bound.Clear();
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (item.m_gridPos.y == 0)
			{
				bound.Add(item);
			}
		}
	}

	public bool IsTeleportable()
	{
		foreach (ItemDrop.ItemData item in m_inventory)
		{
			if (!item.m_shared.m_teleportable)
			{
				return false;
			}
		}
		return true;
	}
}
