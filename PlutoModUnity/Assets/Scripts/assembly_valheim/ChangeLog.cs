using UnityEngine;
using UnityEngine.UI;

public class ChangeLog : MonoBehaviour
{
	private bool m_hasSetScroll;

	public Text m_textField;

	public TextAsset m_changeLog;

	public Scrollbar m_scrollbar;

	private void Start()
	{
		string text = m_changeLog.text;
		m_textField.text = text;
	}

	private void LateUpdate()
	{
		if (!m_hasSetScroll)
		{
			m_hasSetScroll = true;
			if (m_scrollbar != null)
			{
				m_scrollbar.value = 1f;
			}
		}
	}
}
