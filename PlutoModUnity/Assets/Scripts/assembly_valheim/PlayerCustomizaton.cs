using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustomizaton : MonoBehaviour
{
	public Color m_skinColor0 = Color.white;

	public Color m_skinColor1 = Color.white;

	public Color m_hairColor0 = Color.white;

	public Color m_hairColor1 = Color.white;

	public float m_hairMaxLevel = 1f;

	public float m_hairMinLevel = 0.1f;

	public Text m_selectedBeard;

	public Text m_selectedHair;

	public Slider m_skinHue;

	public Slider m_hairLevel;

	public Slider m_hairTone;

	public RectTransform m_beardPanel;

	public Toggle m_maleToggle;

	public Toggle m_femaleToggle;

	public ItemDrop m_noHair;

	public ItemDrop m_noBeard;

	private List<ItemDrop> m_beards;

	private List<ItemDrop> m_hairs;

	private void OnEnable()
	{
		m_maleToggle.isOn = true;
		m_femaleToggle.isOn = false;
		m_beardPanel.gameObject.SetActive(value: true);
		m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
		m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
		m_beards.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
		m_hairs.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
		m_beards.Remove(m_noBeard);
		m_beards.Insert(0, m_noBeard);
		m_hairs.Remove(m_noHair);
		m_hairs.Insert(0, m_noHair);
	}

	private void Update()
	{
		if (!(GetPlayer() == null))
		{
			m_selectedHair.text = Localization.instance.Localize(GetHair());
			m_selectedBeard.text = Localization.instance.Localize(GetBeard());
			Color c = Color.Lerp(m_skinColor0, m_skinColor1, m_skinHue.value);
			GetPlayer().SetSkinColor(Utils.ColorToVec3(c));
			Color c2 = Color.Lerp(m_hairColor0, m_hairColor1, m_hairTone.value) * Mathf.Lerp(m_hairMinLevel, m_hairMaxLevel, m_hairLevel.value);
			GetPlayer().SetHairColor(Utils.ColorToVec3(c2));
		}
	}

	private Player GetPlayer()
	{
		return GetComponentInParent<FejdStartup>().GetPreviewPlayer();
	}

	public void OnHairHueChange(float v)
	{
	}

	public void OnSkinHueChange(float v)
	{
	}

	public void SetPlayerModel(int index)
	{
		GetPlayer().SetPlayerModel(index);
		if (index == 1)
		{
			ResetBeard();
		}
	}

	public void OnHairLeft()
	{
		SetHair(GetHairIndex() - 1);
	}

	public void OnHairRight()
	{
		SetHair(GetHairIndex() + 1);
	}

	public void OnBeardLeft()
	{
		if (GetPlayer().GetPlayerModel() != 1)
		{
			SetBeard(GetBeardIndex() - 1);
		}
	}

	public void OnBeardRight()
	{
		if (GetPlayer().GetPlayerModel() != 1)
		{
			SetBeard(GetBeardIndex() + 1);
		}
	}

	private void ResetBeard()
	{
		GetPlayer().SetBeard(m_noBeard.gameObject.name);
	}

	private void SetBeard(int index)
	{
		if (index >= 0 && index < m_beards.Count)
		{
			GetPlayer().SetBeard(m_beards[index].gameObject.name);
		}
	}

	private void SetHair(int index)
	{
		ZLog.Log("Set hair " + index);
		if (index >= 0 && index < m_hairs.Count)
		{
			GetPlayer().SetHair(m_hairs[index].gameObject.name);
		}
	}

	private int GetBeardIndex()
	{
		string beard = GetPlayer().GetBeard();
		for (int i = 0; i < m_beards.Count; i++)
		{
			if (m_beards[i].gameObject.name == beard)
			{
				return i;
			}
		}
		return 0;
	}

	private int GetHairIndex()
	{
		string hair = GetPlayer().GetHair();
		for (int i = 0; i < m_hairs.Count; i++)
		{
			if (m_hairs[i].gameObject.name == hair)
			{
				return i;
			}
		}
		return 0;
	}

	private string GetHair()
	{
		return m_hairs[GetHairIndex()].m_itemData.m_shared.m_name;
	}

	private string GetBeard()
	{
		return m_beards[GetBeardIndex()].m_itemData.m_shared.m_name;
	}
}
