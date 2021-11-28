using UnityEngine;
using UnityEngine.UI;

public class SleepText : MonoBehaviour
{
	public Text m_textField;

	public Text m_dreamField;

	public DreamTexts m_dreamTexts;

	private void OnEnable()
	{
		m_textField.canvasRenderer.SetAlpha(0f);
		m_textField.CrossFadeAlpha(1f, 1f, ignoreTimeScale: true);
		m_dreamField.enabled = false;
		Invoke("HideZZZ", 2f);
		Invoke("ShowDreamText", 4f);
	}

	private void HideZZZ()
	{
		m_textField.CrossFadeAlpha(0f, 2f, ignoreTimeScale: true);
	}

	private void ShowDreamText()
	{
		DreamTexts.DreamText randomDreamText = m_dreamTexts.GetRandomDreamText();
		if (randomDreamText != null)
		{
			m_dreamField.enabled = true;
			m_dreamField.canvasRenderer.SetAlpha(0f);
			m_dreamField.CrossFadeAlpha(1f, 1.5f, ignoreTimeScale: true);
			m_dreamField.text = Localization.instance.Localize(randomDreamText.m_text);
			Invoke("HideDreamText", 6.5f);
		}
	}

	private void HideDreamText()
	{
		m_dreamField.CrossFadeAlpha(0f, 1.5f, ignoreTimeScale: true);
	}
}
