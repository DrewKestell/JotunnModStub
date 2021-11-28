using UnityEngine;
using UnityEngine.UI;

public class ButtonTextColor : MonoBehaviour
{
	private Color m_defaultColor = Color.white;

	public Color m_disabledColor = Color.grey;

	private Button m_button;

	private Text m_text;

	private Sprite m_sprite;

	private void Awake()
	{
		m_button = GetComponent<Button>();
		m_text = GetComponentInChildren<Text>();
		m_defaultColor = m_text.color;
	}

	private void Update()
	{
		if (m_button.IsInteractable())
		{
			m_text.color = m_defaultColor;
		}
		else
		{
			m_text.color = m_disabledColor;
		}
	}
}
