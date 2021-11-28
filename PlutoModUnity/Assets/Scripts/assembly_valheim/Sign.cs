using UnityEngine;
using UnityEngine.UI;

public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	public Text m_textWidget;

	public string m_name = "Sign";

	public string m_defaultText = "Sign";

	public int m_characterLimit = 50;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			UpdateText();
			InvokeRepeating("UpdateText", 2f, 2f);
		}
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return "\"" + GetText() + "\"";
		}
		return "\"" + GetText() + "\"\n" + Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", m_characterLimit);
		return true;
	}

	private void UpdateText()
	{
		string text = GetText();
		if (!(m_textWidget.text == text))
		{
			m_textWidget.text = text;
		}
	}

	public string GetText()
	{
		return m_nview.GetZDO().GetString("text", m_defaultText);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetText(string text)
	{
		if (PrivateArea.CheckAccess(base.transform.position))
		{
			m_nview.ClaimOwnership();
			m_textWidget.text = text;
			m_nview.GetZDO().Set("text", text);
		}
	}
}
