using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropTable
{
	[Serializable]
	public struct DropData
	{
		public GameObject m_item;

		public int m_stackMin;

		public int m_stackMax;

		public float m_weight;
	}

	public List<DropData> m_drops = new List<DropData>();

	public int m_dropMin = 1;

	public int m_dropMax = 1;

	[Range(0f, 1f)]
	public float m_dropChance = 1f;

	public bool m_oneOfEach;

	public DropTable Clone()
	{
		return MemberwiseClone() as DropTable;
	}

	public List<ItemDrop.ItemData> GetDropListItems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		if (m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > m_dropChance)
		{
			return list;
		}
		List<DropData> list2 = new List<DropData>(m_drops);
		float num = 0f;
		foreach (DropData item in list2)
		{
			num += item.m_weight;
		}
		int num2 = UnityEngine.Random.Range(m_dropMin, m_dropMax + 1);
		for (int i = 0; i < num2; i++)
		{
			float num3 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num4 = 0f;
			foreach (DropData item2 in list2)
			{
				num4 += item2.m_weight;
				if (num3 <= num4)
				{
					flag = true;
					AddItemToList(list, item2);
					if (m_oneOfEach)
					{
						list2.Remove(item2);
						num -= item2.m_weight;
					}
					break;
				}
			}
			if (!flag && list2.Count > 0)
			{
				AddItemToList(list, list2[0]);
			}
		}
		return list;
	}

	private void AddItemToList(List<ItemDrop.ItemData> toDrop, DropData data)
	{
		ItemDrop.ItemData itemData = data.m_item.GetComponent<ItemDrop>().m_itemData;
		ItemDrop.ItemData itemData2 = itemData.Clone();
		itemData2.m_dropPrefab = data.m_item;
		int min = Mathf.Max(1, data.m_stackMin);
		int num = Mathf.Min(itemData.m_shared.m_maxStackSize, data.m_stackMax);
		itemData2.m_stack = UnityEngine.Random.Range(min, num + 1);
		toDrop.Add(itemData2);
	}

	public List<GameObject> GetDropList()
	{
		int amount = UnityEngine.Random.Range(m_dropMin, m_dropMax + 1);
		return GetDropList(amount);
	}

	private List<GameObject> GetDropList(int amount)
	{
		List<GameObject> list = new List<GameObject>();
		if (m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > m_dropChance)
		{
			return list;
		}
		List<DropData> list2 = new List<DropData>(m_drops);
		float num = 0f;
		foreach (DropData item in list2)
		{
			num += item.m_weight;
		}
		for (int i = 0; i < amount; i++)
		{
			float num2 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num3 = 0f;
			foreach (DropData item2 in list2)
			{
				num3 += item2.m_weight;
				if (num2 <= num3)
				{
					flag = true;
					int num4 = UnityEngine.Random.Range(item2.m_stackMin, item2.m_stackMax);
					for (int j = 0; j < num4; j++)
					{
						list.Add(item2.m_item);
					}
					if (m_oneOfEach)
					{
						list2.Remove(item2);
						num -= item2.m_weight;
					}
					break;
				}
			}
			if (!flag && list2.Count > 0)
			{
				list.Add(list2[0].m_item);
			}
		}
		return list;
	}

	public bool IsEmpty()
	{
		return m_drops.Count == 0;
	}
}
