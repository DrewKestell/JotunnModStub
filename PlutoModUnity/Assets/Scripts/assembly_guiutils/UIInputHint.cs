using UnityEngine;
using UnityEngine.UI;

public class UIInputHint : MonoBehaviour
{
	public GameObject m_gamepadHint;

	public GameObject m_mouseKeyboardHint;

	private Button m_button;

	private UIGroupHandler m_group;

	private void Start()
	{
		m_group = GetComponentInParent<UIGroupHandler>();
		m_button = GetComponent<Button>();
		if ((bool)m_gamepadHint)
		{
			m_gamepadHint.SetActive(value: false);
		}
		if ((bool)m_mouseKeyboardHint)
		{
			m_mouseKeyboardHint.SetActive(value: false);
		}
	}

	private void Update()
	{
		bool flag = (m_button == null || m_button.IsInteractable()) && (m_group == null || m_group.IsActive());
		if (m_gamepadHint != null)
		{
			m_gamepadHint.SetActive(flag && ZInput.IsGamepadActive());
		}
		if (m_mouseKeyboardHint != null)
		{
			m_mouseKeyboardHint.SetActive(flag && ZInput.IsMouseActive());
		}
	}
}
