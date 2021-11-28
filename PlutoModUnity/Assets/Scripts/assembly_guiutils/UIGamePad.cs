using UnityEngine;
using UnityEngine.UI;

public class UIGamePad : MonoBehaviour
{
	public KeyCode m_keyCode;

	public string m_zinputKey;

	public GameObject m_hint;

	private Button m_button;

	private Toggle m_toggle;

	private UIGroupHandler m_group;

	private static int m_lastInteractFrame;

	private void Start()
	{
		m_group = GetComponentInParent<UIGroupHandler>();
		m_button = GetComponent<Button>();
		m_toggle = GetComponent<Toggle>();
		if ((bool)m_hint)
		{
			m_hint.SetActive(value: false);
		}
	}

	private bool IsInteractive()
	{
		if (m_button != null && !m_button.IsInteractable())
		{
			return false;
		}
		if ((bool)m_toggle)
		{
			if (!m_toggle.IsInteractable())
			{
				return false;
			}
			if ((bool)m_toggle.group && !m_toggle.group.allowSwitchOff && m_toggle.isOn)
			{
				return false;
			}
		}
		if ((bool)m_group && !m_group.IsActive())
		{
			return false;
		}
		return true;
	}

	private void Update()
	{
		bool flag = IsInteractive();
		if ((bool)m_hint)
		{
			m_hint.SetActive(flag && ZInput.IsGamepadActive());
		}
		if (flag && Time.frameCount - m_lastInteractFrame >= 2 && ButtonPressed())
		{
			m_lastInteractFrame = Time.frameCount;
			ZLog.Log("Button pressed " + base.gameObject.name + "  frame:" + Time.frameCount);
			if (m_button != null)
			{
				m_button.OnSubmit(null);
			}
			if (m_toggle != null)
			{
				m_toggle.OnSubmit(null);
			}
		}
	}

	private bool ButtonPressed()
	{
		if (!string.IsNullOrEmpty(m_zinputKey) && ZInput.GetButtonDown(m_zinputKey))
		{
			return true;
		}
		if (m_keyCode != 0 && Input.GetKeyDown(m_keyCode))
		{
			return true;
		}
		return false;
	}
}
