using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextInput : MonoBehaviour
{
	private static TextInput m_instance;

	public GameObject m_panel;

	public InputField m_textField;

	public Text m_topic;

	private TextReceiver m_queuedSign;

	private bool m_visibleFrame;

	public static TextInput instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_panel.SetActive(value: false);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_visibleFrame;
		}
		return false;
	}

	private void Update()
	{
		m_visibleFrame = m_instance.m_panel.gameObject.activeSelf;
		if (!m_visibleFrame || Console.IsVisible() || Chat.instance.HasFocus())
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Hide();
			return;
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			OnEnter();
		}
		if (!m_textField.isFocused)
		{
			EventSystem.current.SetSelectedGameObject(m_textField.gameObject);
		}
	}

	public void OnCancel()
	{
		Hide();
	}

	public void OnEnter()
	{
		string text = m_textField.text;
		if (m_queuedSign != null)
		{
			m_queuedSign.SetText(text);
			m_queuedSign = null;
		}
		Hide();
	}

	public void RequestText(TextReceiver sign, string topic, int charLimit)
	{
		m_queuedSign = sign;
		Show(topic, sign.GetText(), charLimit);
	}

	private void Show(string topic, string text, int charLimit)
	{
		m_panel.SetActive(value: true);
		m_textField.text = text;
		m_topic.text = Localization.instance.Localize(topic);
		m_textField.ActivateInputField();
		m_textField.characterLimit = charLimit;
	}

	public void Hide()
	{
		m_panel.SetActive(value: false);
	}
}
