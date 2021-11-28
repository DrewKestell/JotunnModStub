using UnityEngine;

public class Menu : MonoBehaviour
{
	private GameObject m_settingsInstance;

	private static Menu m_instance;

	public Transform m_root;

	public Transform m_menuDialog;

	public Transform m_quitDialog;

	public Transform m_logoutDialog;

	public GameObject m_settingsPrefab;

	public GameObject m_feedbackPrefab;

	private int m_hiddenFrames;

	public static Menu instance => m_instance;

	private void Start()
	{
		m_instance = this;
		m_root.gameObject.SetActive(value: false);
	}

	public static bool IsVisible()
	{
		if (m_instance == null)
		{
			return false;
		}
		return m_instance.m_hiddenFrames <= 2;
	}

	private void Update()
	{
		if (Game.instance.IsShuttingDown())
		{
			m_root.gameObject.SetActive(value: false);
			return;
		}
		if (m_root.gameObject.activeSelf)
		{
			m_hiddenFrames = 0;
			if ((Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")) && !m_settingsInstance && !Feedback.IsVisible())
			{
				if (m_quitDialog.gameObject.activeSelf)
				{
					OnQuitNo();
				}
				else if (m_logoutDialog.gameObject.activeSelf)
				{
					OnLogoutNo();
				}
				else
				{
					m_root.gameObject.SetActive(value: false);
				}
			}
			return;
		}
		m_hiddenFrames++;
		bool flag = !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Console.IsVisible() && !TextInput.IsVisible() && !ZNet.instance.InPasswordDialog() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible();
		if ((Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu")) && flag)
		{
			Gogan.LogEvent("Screen", "Enter", "Menu", 0L);
			m_root.gameObject.SetActive(value: true);
			m_menuDialog.gameObject.SetActive(value: true);
			m_logoutDialog.gameObject.SetActive(value: false);
			m_quitDialog.gameObject.SetActive(value: false);
		}
	}

	public void OnSettings()
	{
		Gogan.LogEvent("Screen", "Enter", "Settings", 0L);
		m_settingsInstance = Object.Instantiate(m_settingsPrefab, base.transform);
	}

	public void OnQuit()
	{
		m_quitDialog.gameObject.SetActive(value: true);
		m_menuDialog.gameObject.SetActive(value: false);
	}

	public void OnQuitYes()
	{
		Gogan.LogEvent("Game", "Quit", "", 0L);
		Application.Quit();
	}

	public void OnQuitNo()
	{
		m_quitDialog.gameObject.SetActive(value: false);
		m_menuDialog.gameObject.SetActive(value: true);
	}

	public void OnLogout()
	{
		m_menuDialog.gameObject.SetActive(value: false);
		m_logoutDialog.gameObject.SetActive(value: true);
	}

	public void OnLogoutYes()
	{
		Gogan.LogEvent("Game", "LogOut", "", 0L);
		Game.instance.Logout();
	}

	public void OnLogoutNo()
	{
		m_logoutDialog.gameObject.SetActive(value: false);
		m_menuDialog.gameObject.SetActive(value: true);
	}

	public void OnClose()
	{
		Gogan.LogEvent("Screen", "Exit", "Menu", 0L);
		m_root.gameObject.SetActive(value: false);
	}

	public void OnButtonFeedback()
	{
		Object.Instantiate(m_feedbackPrefab, base.transform);
	}
}
