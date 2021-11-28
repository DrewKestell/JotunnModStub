using UnityEngine;

public class Console : Terminal
{
	private static Console m_instance;

	private static bool m_consoleEnabled;

	public static Console instance => m_instance;

	protected override Terminal m_terminalInstance => m_instance;

	public override void Awake()
	{
		base.Awake();
		m_instance = this;
		AddString("Valheim " + Version.GetVersionString());
		AddString("");
		AddString("type \"help\" - for commands");
		AddString("");
		m_chatWindow.gameObject.SetActive(value: false);
	}

	public override void Update()
	{
		m_focused = false;
		if ((bool)ZNet.instance && ZNet.instance.InPasswordDialog())
		{
			m_chatWindow.gameObject.SetActive(value: false);
		}
		else if (IsConsoleEnabled())
		{
			if (Input.GetKeyDown(KeyCode.F5) || (IsVisible() && Input.GetKeyDown(KeyCode.Escape)))
			{
				m_chatWindow.gameObject.SetActive(!m_chatWindow.gameObject.activeSelf);
			}
			if (m_chatWindow.gameObject.activeInHierarchy)
			{
				m_focused = true;
			}
			base.Update();
		}
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_chatWindow.gameObject.activeInHierarchy;
		}
		return false;
	}

	public void Print(string text)
	{
		AddString(text);
	}

	public bool IsConsoleEnabled()
	{
		return m_consoleEnabled;
	}

	public static void SetConsoleEnabled(bool enabled)
	{
		m_consoleEnabled = enabled;
	}
}
