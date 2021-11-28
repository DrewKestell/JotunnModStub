using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FejdStartup : MonoBehaviour
{
	private float m_lastServerListRequesTime = -999f;

	private Vector3 camSpeed = Vector3.zero;

	private Vector3 camRotSpeed = Vector3.zero;

	private static FejdStartup m_instance;

	[Header("Start")]
	public Animator m_menuAnimator;

	public GameObject m_worldVersionPanel;

	public GameObject m_playerVersionPanel;

	public GameObject m_newGameVersionPanel;

	public GameObject m_connectionFailedPanel;

	public Text m_connectionFailedError;

	public Text m_newVersionName;

	public GameObject m_loading;

	public Text m_versionLabel;

	public GameObject m_mainMenu;

	public GameObject m_ndaPanel;

	public GameObject m_betaText;

	public Scrollbar m_patchLogScroll;

	public GameObject m_characterSelectScreen;

	public GameObject m_selectCharacterPanel;

	public GameObject m_newCharacterPanel;

	public GameObject m_creditsPanel;

	public GameObject m_startGamePanel;

	public GameObject m_createWorldPanel;

	public GameObject m_menuList;

	private Button[] m_menuButtons;

	private Button m_menuSelectedButton;

	public RectTransform m_creditsList;

	public float m_creditsSpeed = 100f;

	[Header("Camera")]
	public GameObject m_mainCamera;

	public Transform m_cameraMarkerStart;

	public Transform m_cameraMarkerMain;

	public Transform m_cameraMarkerCharacter;

	public Transform m_cameraMarkerCredits;

	public Transform m_cameraMarkerGame;

	public float m_cameraMoveSpeed = 1.5f;

	public float m_cameraMoveSpeedStart = 1.5f;

	[Header("Join")]
	public GameObject m_serverListPanel;

	public Toggle m_publicServerToggle;

	public Toggle m_openServerToggle;

	public InputField m_serverPassword;

	public RectTransform m_serverListRoot;

	public GameObject m_serverListElement;

	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	public float m_serverListElementStep = 28f;

	public Text m_serverCount;

	public Button m_serverRefreshButton;

	public InputField m_filterInputField;

	public Text m_passwordError;

	public Button m_manualIPButton;

	public GameObject m_joinIPPanel;

	public Button m_joinIPJoinButton;

	public InputField m_joinIPAddress;

	public Button m_joinGameButton;

	public Toggle m_friendFilterSwitch;

	public Toggle m_publicFilterSwitch;

	public int m_minimumPasswordLength = 5;

	public float m_characterRotateSpeed = 4f;

	public float m_characterRotateSpeedGamepad = 200f;

	public int m_joinHostPort = 2456;

	public int m_serverPlayerLimit = 10;

	[Header("World")]
	public GameObject m_worldListPanel;

	public RectTransform m_worldListRoot;

	public GameObject m_worldListElement;

	public ScrollRectEnsureVisible m_worldListEnsureVisible;

	public float m_worldListElementStep = 28f;

	public InputField m_newWorldName;

	public InputField m_newWorldSeed;

	public Button m_newWorldDone;

	public Button m_worldStart;

	public Button m_worldRemove;

	public GameObject m_removeWorldDialog;

	public Text m_removeWorldName;

	public GameObject m_removeCharacterDialog;

	public Text m_removeCharacterName;

	[Header("Character selectoin")]
	public Button m_csStartButton;

	public Button m_csNewBigButton;

	public Button m_csNewButton;

	public Button m_csRemoveButton;

	public Button m_csLeftButton;

	public Button m_csRightButton;

	public Button m_csNewCharacterDone;

	public GameObject m_newCharacterError;

	public Text m_csName;

	public InputField m_csNewCharacterName;

	[Header("Misc")]
	public Transform m_characterPreviewPoint;

	public GameObject m_playerPrefab;

	public GameObject m_gameMainPrefab;

	public GameObject m_settingsPrefab;

	public GameObject m_consolePrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_changeEffectPrefab;

	private string m_downloadUrl = "";

	[TextArea]
	public string m_versionXmlUrl = "https://dl.dropboxusercontent.com/s/5ibm05oelbqt8zq/fejdversion.xml?dl=0";

	private World m_world;

	private ServerData m_joinServer;

	private ServerData m_queuedJoinServer;

	private float m_serverListBaseSize;

	private float m_worldListBaseSize;

	private List<PlayerProfile> m_profiles;

	private int m_profileIndex;

	private string m_tempRemoveCharacterName = "";

	private int m_tempRemoveCharacterIndex = -1;

	private List<GameObject> m_serverListElements = new List<GameObject>();

	private List<ServerData> m_serverList = new List<ServerData>();

	private int m_serverListRevision = -1;

	private List<GameObject> m_worldListElements = new List<GameObject>();

	private List<World> m_worlds;

	private GameObject m_playerInstance;

	private bool m_doneInitialServerListRequest;

	private static bool m_firstStartup = true;

	public static FejdStartup instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		QualitySettings.maxQueuedFrames = 1;
		ZLog.Log("Valheim version:" + Version.GetVersionString());
		Settings.ApplyStartupSettings();
		WorldGenerator.Initialize(World.GetMenuWorld());
		if (!Console.instance)
		{
			UnityEngine.Object.Instantiate(m_consolePrefab);
		}
		m_mainCamera.transform.position = m_cameraMarkerMain.transform.position;
		m_mainCamera.transform.rotation = m_cameraMarkerMain.transform.rotation;
		ZLog.Log("Render threading mode:" + SystemInfo.renderingThreadingMode);
		Gogan.StartSession();
		Gogan.LogEvent("Game", "Version", Version.GetVersionString(), 0L);
		Gogan.LogEvent("Game", "SteamID", SteamManager.APP_ID.ToString(), 0L);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
		ParseArguments();
		if (InitializeSteam())
		{
			m_joinIPAddress.text = PlayerPrefs.GetString("LastIPJoined", "");
			m_menuButtons = m_menuList.GetComponentsInChildren<Button>();
		}
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = 60;
		SetupGui();
		SetupObjectDB();
		ZInput.Initialize();
		MusicMan.instance.Reset();
		MusicMan.instance.TriggerMusic("menu");
		ShowConnectError();
		ZSteamMatchmaking.Initialize();
		InvokeRepeating("UpdateServerList", 0.5f, 0.5f);
		if (m_firstStartup)
		{
			HandleStartupJoin();
		}
		m_menuAnimator.SetBool("FirstStartup", m_firstStartup);
		m_firstStartup = false;
		string @string = PlayerPrefs.GetString("profile");
		if (@string.Length > 0)
		{
			SetSelectedProfile(@string);
			return;
		}
		m_profiles = PlayerProfile.GetAllPlayerProfiles();
		if (m_profiles.Count > 0)
		{
			SetSelectedProfile(m_profiles[0].GetFilename());
		}
		else
		{
			UpdateCharacterList();
		}
	}

	private void SetupGui()
	{
		HideAll();
		m_mainMenu.SetActive(value: true);
		if (SteamManager.APP_ID == 1223920)
		{
			m_betaText.SetActive(value: true);
			if (!Debug.isDebugBuild && !AcceptedNDA())
			{
				m_ndaPanel.SetActive(value: true);
				m_mainMenu.SetActive(value: false);
			}
		}
		m_manualIPButton.gameObject.SetActive(value: true);
		m_serverListBaseSize = m_serverListRoot.rect.height;
		m_worldListBaseSize = m_worldListRoot.rect.height;
		m_versionLabel.text = "version " + Version.GetVersionString();
		Localization.instance.Localize(base.transform);
	}

	private void HideAll()
	{
		m_worldVersionPanel.SetActive(value: false);
		m_playerVersionPanel.SetActive(value: false);
		m_newGameVersionPanel.SetActive(value: false);
		m_loading.SetActive(value: false);
		m_characterSelectScreen.SetActive(value: false);
		m_creditsPanel.SetActive(value: false);
		m_startGamePanel.SetActive(value: false);
		m_joinIPPanel.SetActive(value: false);
		m_createWorldPanel.SetActive(value: false);
		m_mainMenu.SetActive(value: false);
		m_ndaPanel.SetActive(value: false);
		m_betaText.SetActive(value: false);
	}

	private bool InitializeSteam()
	{
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			return true;
		}
		ZLog.LogError("Steam is not initialized");
		Application.Quit();
		return false;
	}

	private void HandleStartupJoin()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i];
			if (text == "+connect" && i < commandLineArgs.Length - 1)
			{
				string text2 = commandLineArgs[i + 1];
				ZLog.Log("JOIN " + text2);
				ZSteamMatchmaking.instance.QueueServerJoin(text2);
			}
			else if (text == "+connect_lobby" && i < commandLineArgs.Length - 1)
			{
				string s = commandLineArgs[i + 1];
				CSteamID lobbyID = new CSteamID(ulong.Parse(s));
				ZSteamMatchmaking.instance.QueueLobbyJoin(lobbyID);
			}
		}
	}

	private void ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == "-console")
			{
				Console.SetConsoleEnabled(enabled: true);
			}
		}
	}

	private bool ParseServerArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = "Dedicated";
		string password = "";
		string text2 = "";
		int num = 2456;
		bool flag = true;
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			switch (commandLineArgs[i])
			{
			case "-world":
			{
				string text5 = commandLineArgs[i + 1];
				if (text5 != "")
				{
					text = text5;
				}
				i++;
				break;
			}
			case "-name":
			{
				string text4 = commandLineArgs[i + 1];
				if (text4 != "")
				{
					text2 = text4;
				}
				i++;
				break;
			}
			case "-port":
			{
				string text6 = commandLineArgs[i + 1];
				if (text6 != "")
				{
					num = int.Parse(text6);
				}
				i++;
				break;
			}
			case "-password":
				password = commandLineArgs[i + 1];
				i++;
				break;
			case "-savedir":
				Utils.SetSaveDataPath(commandLineArgs[i + 1]);
				i++;
				break;
			case "-public":
			{
				string text3 = commandLineArgs[i + 1];
				if (text3 != "")
				{
					flag = text3 == "1";
				}
				i++;
				break;
			}
			}
		}
		if (text2 == "")
		{
			text2 = text;
		}
		World createWorld = World.GetCreateWorld(text);
		if (flag && !IsPublicPasswordValid(password, createWorld))
		{
			string publicPasswordError = GetPublicPasswordError(password, createWorld);
			ZLog.LogError("Error bad password:" + publicPasswordError);
			Application.Quit();
			return false;
		}
		ZNet.SetServer(server: true, openServer: true, flag, text2, password, createWorld);
		ZNet.ResetServerHost();
		SteamManager.SetServerPort(num);
		ZSteamSocket.SetDataPort(num);
		return true;
	}

	private void SetupObjectDB()
	{
		ObjectDB objectDB = base.gameObject.AddComponent<ObjectDB>();
		ObjectDB component = m_gameMainPrefab.GetComponent<ObjectDB>();
		objectDB.CopyOtherDB(component);
	}

	private void ShowConnectError()
	{
		ZNet.ConnectionStatus connectionStatus = ZNet.GetConnectionStatus();
		if (connectionStatus != ZNet.ConnectionStatus.Connected && connectionStatus != ZNet.ConnectionStatus.Connecting && connectionStatus != 0)
		{
			m_connectionFailedPanel.SetActive(value: true);
			switch (connectionStatus)
			{
			case ZNet.ConnectionStatus.ErrorVersion:
				m_connectionFailedError.text = Localization.instance.Localize("$error_incompatibleversion");
				break;
			case ZNet.ConnectionStatus.ErrorConnectFailed:
				m_connectionFailedError.text = Localization.instance.Localize("$error_failedconnect");
				break;
			case ZNet.ConnectionStatus.ErrorDisconnected:
				m_connectionFailedError.text = Localization.instance.Localize("$error_disconnected");
				break;
			case ZNet.ConnectionStatus.ErrorPassword:
				m_connectionFailedError.text = Localization.instance.Localize("$error_password");
				break;
			case ZNet.ConnectionStatus.ErrorAlreadyConnected:
				m_connectionFailedError.text = Localization.instance.Localize("$error_alreadyconnected");
				break;
			case ZNet.ConnectionStatus.ErrorBanned:
				m_connectionFailedError.text = Localization.instance.Localize("$error_banned");
				break;
			case ZNet.ConnectionStatus.ErrorFull:
				m_connectionFailedError.text = Localization.instance.Localize("$error_serverfull");
				break;
			}
		}
	}

	public void OnNewVersionButtonDownload()
	{
		Application.OpenURL(m_downloadUrl);
		Application.Quit();
	}

	public void OnNewVersionButtonContinue()
	{
		m_newGameVersionPanel.SetActive(value: false);
	}

	public void OnStartGame()
	{
		Gogan.LogEvent("Screen", "Enter", "StartGame", 0L);
		m_mainMenu.SetActive(value: false);
		ShowCharacterSelection();
	}

	private void ShowStartGame()
	{
		m_mainMenu.SetActive(value: false);
		m_startGamePanel.SetActive(value: true);
		m_createWorldPanel.SetActive(value: false);
	}

	public void OnSelectWorldTab()
	{
		UpdateWorldList(centerSelection: true);
		if (m_world == null)
		{
			string @string = PlayerPrefs.GetString("world");
			if (@string.Length > 0)
			{
				m_world = FindWorld(@string);
			}
			if (m_world == null)
			{
				m_world = ((m_worlds.Count > 0) ? m_worlds[0] : null);
			}
			if (m_world != null)
			{
				UpdateWorldList(centerSelection: true);
			}
		}
	}

	private World FindWorld(string name)
	{
		foreach (World world in m_worlds)
		{
			if (world.m_name == name)
			{
				return world;
			}
		}
		return null;
	}

	private void UpdateWorldList(bool centerSelection)
	{
		m_worlds = World.GetWorldList();
		foreach (GameObject worldListElement in m_worldListElements)
		{
			UnityEngine.Object.Destroy(worldListElement);
		}
		m_worldListElements.Clear();
		float b = (float)m_worlds.Count * m_worldListElementStep;
		b = Mathf.Max(m_worldListBaseSize, b);
		m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
		for (int i = 0; i < m_worlds.Count; i++)
		{
			World world = m_worlds[i];
			GameObject gameObject = UnityEngine.Object.Instantiate(m_worldListElement, m_worldListRoot);
			gameObject.SetActive(value: true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * (0f - m_worldListElementStep));
			gameObject.GetComponent<Button>().onClick.AddListener(OnSelectWorld);
			Text component = gameObject.transform.Find("seed").GetComponent<Text>();
			component.text = "Seed:" + world.m_seedName;
			gameObject.transform.Find("name").GetComponent<Text>().text = world.m_name;
			if (world.m_loadError)
			{
				component.text = " [LOAD ERROR]";
			}
			else if (world.m_versionError)
			{
				component.text = " [BAD VERSION]";
			}
			RectTransform rectTransform = gameObject.transform.Find("selected") as RectTransform;
			bool flag = m_world != null && world.m_name == m_world.m_name;
			rectTransform.gameObject.SetActive(flag);
			if (flag && centerSelection)
			{
				m_worldListEnsureVisible.CenterOnItem(rectTransform);
			}
			m_worldListElements.Add(gameObject);
		}
	}

	public void OnWorldRemove()
	{
		if (m_world != null)
		{
			m_removeWorldName.text = m_world.m_name;
			m_removeWorldDialog.SetActive(value: true);
		}
	}

	public void OnButtonRemoveWorldYes()
	{
		World.RemoveWorld(m_world.m_name);
		m_world = null;
		SetSelectedWorld(0, centerSelection: true);
		m_removeWorldDialog.SetActive(value: false);
	}

	public void OnButtonRemoveWorldNo()
	{
		m_removeWorldDialog.SetActive(value: false);
	}

	private void OnSelectWorld()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		int index = FindSelectedWorld(currentSelectedGameObject);
		SetSelectedWorld(index, centerSelection: false);
	}

	private void SetSelectedWorld(int index, bool centerSelection)
	{
		if (m_worlds.Count != 0)
		{
			index = Mathf.Clamp(index, 0, m_worlds.Count - 1);
			m_world = m_worlds[index];
			UpdateWorldList(centerSelection);
		}
	}

	private int GetSelectedWorld()
	{
		if (m_world == null)
		{
			return -1;
		}
		for (int i = 0; i < m_worlds.Count; i++)
		{
			if (m_worlds[i].m_name == m_world.m_name)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedWorld(GameObject button)
	{
		for (int i = 0; i < m_worldListElements.Count; i++)
		{
			if (m_worldListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnWorldNew()
	{
		m_createWorldPanel.SetActive(value: true);
		m_newWorldName.text = "";
		m_newWorldSeed.text = World.GenerateSeed();
	}

	public void OnNewWorldDone()
	{
		string text = m_newWorldName.text;
		string text2 = m_newWorldSeed.text;
		if (!World.HaveWorld(text))
		{
			m_world = new World(text, text2);
			m_world.SaveWorldMetaData();
			UpdateWorldList(centerSelection: true);
			ShowStartGame();
			Gogan.LogEvent("Menu", "NewWorld", text, 0L);
		}
	}

	public void OnNewWorldBack()
	{
		ShowStartGame();
	}

	public void OnWorldStart()
	{
		if (m_world != null && !m_world.m_versionError && !m_world.m_loadError)
		{
			ZSteamMatchmaking.instance.StopServerListing();
			PlayerPrefs.SetString("world", m_world.m_name);
			bool isOn = m_publicServerToggle.isOn;
			bool isOn2 = m_openServerToggle.isOn;
			string text = m_serverPassword.text;
			ZNet.SetServer(server: true, isOn2, isOn, m_world.m_name, text, m_world);
			ZNet.ResetServerHost();
			string eventLabel = "open:" + isOn2 + ",public:" + isOn;
			Gogan.LogEvent("Menu", "WorldStart", eventLabel, 0L);
			TransitionToMainScene();
		}
	}

	private void ShowCharacterSelection()
	{
		Gogan.LogEvent("Screen", "Enter", "CharacterSelection", 0L);
		ZLog.Log("show character selection");
		m_characterSelectScreen.SetActive(value: true);
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
	}

	public void OnServerFilterChanged()
	{
		ZSteamMatchmaking.instance.SetNameFilter(m_filterInputField.text);
		ZSteamMatchmaking.instance.SetFriendFilter(m_friendFilterSwitch.isOn);
		PlayerPrefs.SetInt("publicfilter", m_publicFilterSwitch.isOn ? 1 : 0);
	}

	public void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
		if (!m_serverRefreshButton.interactable)
		{
			ZLog.DevLog("Server queue already running");
			return;
		}
		m_serverRefreshButton.interactable = false;
		m_lastServerListRequesTime = Time.time;
		ZSteamMatchmaking.instance.RequestServerlist();
	}

	private void UpdateServerList()
	{
		m_serverRefreshButton.interactable = Time.time - m_lastServerListRequesTime > 1f;
		m_serverCount.text = m_serverListElements.Count.ToString() + " / " + ZSteamMatchmaking.instance.GetTotalNrOfServers();
		if (m_serverListRevision == ZSteamMatchmaking.instance.GetServerListRevision())
		{
			return;
		}
		m_serverListRevision = ZSteamMatchmaking.instance.GetServerListRevision();
		m_serverList.Clear();
		ZSteamMatchmaking.instance.GetServers(m_serverList);
		m_serverList.Sort((ServerData a, ServerData b) => a.m_name.CompareTo(b.m_name));
		if (m_joinServer != null && !m_serverList.Contains(m_joinServer))
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (m_serverList.Count > 0)
			{
				m_joinServer = m_serverList[0];
			}
			else
			{
				m_joinServer = null;
			}
		}
		UpdateServerListGui(centerSelection: false);
	}

	private void UpdateServerListGui(bool centerSelection)
	{
		if (m_serverList.Count != m_serverListElements.Count)
		{
			foreach (GameObject serverListElement in m_serverListElements)
			{
				UnityEngine.Object.Destroy(serverListElement);
			}
			m_serverListElements.Clear();
			float b = (float)m_serverList.Count * m_serverListElementStep;
			b = Mathf.Max(m_serverListBaseSize, b);
			m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
			for (int i = 0; i < m_serverList.Count; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(m_serverListElement, m_serverListRoot);
				gameObject.SetActive(value: true);
				(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * (0f - m_serverListElementStep));
				gameObject.GetComponent<Button>().onClick.AddListener(OnSelectedServer);
				m_serverListElements.Add(gameObject);
			}
		}
		for (int j = 0; j < m_serverList.Count; j++)
		{
			ServerData serverData = m_serverList[j];
			GameObject gameObject2 = m_serverListElements[j];
			gameObject2.GetComponentInChildren<Text>().text = j + ". " + serverData.m_name;
			gameObject2.GetComponentInChildren<UITooltip>().m_text = serverData.ToString();
			gameObject2.transform.Find("version").GetComponent<Text>().text = serverData.m_version;
			gameObject2.transform.Find("players").GetComponent<Text>().text = "Players:" + serverData.m_players + " / " + m_serverPlayerLimit;
			gameObject2.transform.Find("Private").gameObject.SetActive(serverData.m_password);
			Transform transform = gameObject2.transform.Find("selected");
			bool flag = m_joinServer != null && m_joinServer.Equals(serverData);
			transform.gameObject.SetActive(flag);
			if (centerSelection && flag)
			{
				m_serverListEnsureVisible.CenterOnItem(transform as RectTransform);
			}
		}
	}

	private void OnSelectedServer()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		int index = FindSelectedServer(currentSelectedGameObject);
		m_joinServer = m_serverList[index];
		UpdateServerListGui(centerSelection: false);
	}

	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (m_serverList.Count != 0)
		{
			index = Mathf.Clamp(index, 0, m_serverList.Count - 1);
			m_joinServer = m_serverList[index];
			UpdateServerListGui(centerSelection);
		}
	}

	private int GetSelectedServer()
	{
		if (m_joinServer == null)
		{
			return -1;
		}
		for (int i = 0; i < m_serverList.Count; i++)
		{
			if (m_joinServer.Equals(m_serverList[i]))
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < m_serverListElements.Count; i++)
		{
			if (m_serverListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnJoinStart()
	{
		JoinServer();
	}

	private void JoinServer()
	{
		ZNet.SetServer(server: false, openServer: false, publicServer: false, "", "", null);
		ZSteamMatchmaking.instance.StopServerListing();
		if (m_joinServer.m_steamHostID != 0L)
		{
			ZNet.SetServerHost(m_joinServer.m_steamHostID);
		}
		else
		{
			ZNet.SetServerHost(m_joinServer.m_steamHostAddr);
		}
		Gogan.LogEvent("Menu", "JoinServer", "", 0L);
		TransitionToMainScene();
	}

	public void OnJoinIPOpen()
	{
		m_joinIPPanel.SetActive(value: true);
		m_joinIPAddress.ActivateInputField();
	}

	public void OnJoinIPConnect()
	{
		m_joinIPPanel.SetActive(value: true);
		PlayerPrefs.SetString("LastIPJoined", m_joinIPAddress.text);
		string[] array = m_joinIPAddress.text.Split(':');
		if (array.Length != 0)
		{
			string text = array[0];
			int num = m_joinHostPort;
			if (array.Length > 1 && int.TryParse(array[1], out var result))
			{
				num = result;
			}
			if (text.Length != 0)
			{
				ZSteamMatchmaking.instance.QueueServerJoin(text + ":" + num);
			}
		}
	}

	public void OnJoinIPBack()
	{
		m_joinIPPanel.SetActive(value: false);
	}

	public void OnServerListTab()
	{
		bool publicFilter = PlayerPrefs.GetInt("publicfilter", 0) == 1;
		SetPublicFilter(publicFilter);
		if (!m_doneInitialServerListRequest)
		{
			m_doneInitialServerListRequest = true;
			RequestServerList();
		}
		UpdateServerListGui(centerSelection: true);
		m_filterInputField.ActivateInputField();
	}

	private void SetPublicFilter(bool enabled)
	{
		m_friendFilterSwitch.isOn = !enabled;
		m_publicFilterSwitch.isOn = enabled;
	}

	public void OnStartGameBack()
	{
		m_startGamePanel.SetActive(value: false);
		ShowCharacterSelection();
	}

	public void OnCredits()
	{
		m_creditsPanel.SetActive(value: true);
		m_mainMenu.SetActive(value: false);
		Gogan.LogEvent("Screen", "Enter", "Credits", 0L);
		m_creditsList.anchoredPosition = new Vector2(0f, 0f);
	}

	public void OnCreditsBack()
	{
		m_mainMenu.SetActive(value: true);
		m_creditsPanel.SetActive(value: false);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnSelelectCharacterBack()
	{
		m_characterSelectScreen.SetActive(value: false);
		m_mainMenu.SetActive(value: true);
		m_queuedJoinServer = null;
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnAbort()
	{
		Application.Quit();
	}

	public void OnWorldVersionYes()
	{
		m_worldVersionPanel.SetActive(value: false);
	}

	public void OnPlayerVersionOk()
	{
		m_playerVersionPanel.SetActive(value: false);
	}

	private void FixedUpdate()
	{
		ZInput.FixedUpdate(Time.fixedDeltaTime);
	}

	private void UpdateCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = ZInput.IsMouseActive();
	}

	private void Update()
	{
		ZInput.Update(Time.deltaTime);
		UpdateCursor();
		UpdateGamepad();
		UpdateKeyboard();
		CheckPendingSteamJoinRequest();
		if (MasterClient.instance != null)
		{
			MasterClient.instance.Update(Time.deltaTime);
		}
		if (ZBroastcast.instance != null)
		{
			ZBroastcast.instance.Update(Time.deltaTime);
		}
		UpdateCharacterRotation(Time.deltaTime);
		UpdateCamera(Time.deltaTime);
		if (m_newCharacterPanel.activeInHierarchy)
		{
			m_csNewCharacterDone.interactable = m_csNewCharacterName.text.Length >= 3;
		}
		if (m_serverListPanel.activeInHierarchy)
		{
			m_joinGameButton.interactable = m_joinServer != null;
		}
		if (m_createWorldPanel.activeInHierarchy)
		{
			m_newWorldDone.interactable = m_newWorldName.text.Length >= 5;
		}
		if (m_startGamePanel.activeInHierarchy)
		{
			m_worldStart.interactable = CanStartServer();
			m_worldRemove.interactable = m_world != null;
			UpdatePasswordError();
		}
		if (m_joinIPPanel.activeInHierarchy)
		{
			m_joinIPJoinButton.interactable = m_joinIPAddress.text.Length > 0;
		}
		if (m_startGamePanel.activeInHierarchy)
		{
			m_publicServerToggle.interactable = m_openServerToggle.isOn;
			m_serverPassword.interactable = m_openServerToggle.isOn;
		}
		if (m_creditsPanel.activeInHierarchy)
		{
			RectTransform obj = m_creditsList.parent as RectTransform;
			Vector3[] array = new Vector3[4];
			m_creditsList.GetWorldCorners(array);
			Vector3[] array2 = new Vector3[4];
			obj.GetWorldCorners(array2);
			float num = array2[1].y - array2[0].y;
			if ((double)array[3].y < (double)num * 0.5)
			{
				Vector3 position = m_creditsList.position;
				position.y += Time.deltaTime * m_creditsSpeed * num;
				m_creditsList.position = position;
			}
		}
	}

	private void LateUpdate()
	{
		if (Input.GetKeyDown(KeyCode.F11))
		{
			GameCamera.ScreenShot();
		}
	}

	private void UpdateKeyboard()
	{
		if (Input.GetKeyDown(KeyCode.Return) && m_menuList.activeInHierarchy && !m_passwordError.gameObject.activeInHierarchy)
		{
			if (m_menuSelectedButton != null)
			{
				m_menuSelectedButton.OnSubmit(null);
			}
			else
			{
				OnStartGame();
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (m_joinIPPanel.activeInHierarchy)
			{
				OnJoinIPBack();
			}
			else if (m_worldListPanel.activeInHierarchy || m_serverListPanel.activeInHierarchy)
			{
				OnStartGameBack();
			}
			if (m_creditsList.gameObject.activeInHierarchy)
			{
				OnCreditsBack();
			}
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			if (m_worldListPanel.activeInHierarchy)
			{
				SetSelectedWorld(GetSelectedWorld() - 1, centerSelection: true);
			}
			else if (m_serverListPanel.activeInHierarchy)
			{
				SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
			}
			if (m_menuList.activeInHierarchy)
			{
				if (m_menuSelectedButton == null)
				{
					m_menuSelectedButton = m_menuButtons[0];
					m_menuSelectedButton.Select();
				}
				else
				{
					for (int i = 1; i < m_menuButtons.Length; i++)
					{
						if (m_menuButtons[i] == m_menuSelectedButton)
						{
							m_menuSelectedButton = m_menuButtons[i - 1];
							m_menuSelectedButton.Select();
							break;
						}
					}
				}
			}
		}
		if (!Input.GetKeyDown(KeyCode.DownArrow))
		{
			return;
		}
		if (m_worldListPanel.activeInHierarchy)
		{
			SetSelectedWorld(GetSelectedWorld() + 1, centerSelection: true);
		}
		else if (m_serverListPanel.activeInHierarchy)
		{
			SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
		}
		if (!m_menuList.activeInHierarchy)
		{
			return;
		}
		if (m_menuSelectedButton == null)
		{
			m_menuSelectedButton = m_menuButtons[0];
			m_menuSelectedButton.Select();
			return;
		}
		for (int j = 0; j < m_menuButtons.Length - 1; j++)
		{
			if (m_menuButtons[j] == m_menuSelectedButton)
			{
				m_menuSelectedButton = m_menuButtons[j + 1];
				m_menuSelectedButton.Select();
				break;
			}
		}
	}

	private void UpdateGamepad()
	{
		if (!ZInput.IsGamepadActive())
		{
			return;
		}
		if (m_worldListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				SetSelectedWorld(GetSelectedWorld() + 1, centerSelection: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				SetSelectedWorld(GetSelectedWorld() - 1, centerSelection: true);
			}
		}
		else if (m_serverListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown"))
			{
				SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp"))
			{
				SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
			}
		}
		if (m_patchLogScroll.gameObject.activeInHierarchy)
		{
			m_patchLogScroll.value -= ZInput.GetJoyRightStickY() * 0.02f;
		}
	}

	private void CheckPendingSteamJoinRequest()
	{
		if (ZSteamMatchmaking.instance != null && ZSteamMatchmaking.instance.GetJoinHost(out var steamID, out var addr))
		{
			m_queuedJoinServer = new ServerData();
			if (steamID.IsValid())
			{
				m_queuedJoinServer.m_steamHostID = (ulong)steamID;
			}
			else
			{
				m_queuedJoinServer.m_steamHostAddr = addr;
			}
			if (m_serverListPanel.activeInHierarchy)
			{
				m_joinServer = m_queuedJoinServer;
				m_queuedJoinServer = null;
				JoinServer();
			}
			else
			{
				HideAll();
				ShowCharacterSelection();
			}
		}
	}

	private void UpdateCharacterRotation(float dt)
	{
		if (!(m_playerInstance == null) && m_characterSelectScreen.activeInHierarchy)
		{
			if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				float axis = Input.GetAxis("Mouse X");
				m_playerInstance.transform.Rotate(0f, (0f - axis) * m_characterRotateSpeed, 0f);
			}
			float joyRightStickX = ZInput.GetJoyRightStickX();
			if (joyRightStickX != 0f)
			{
				m_playerInstance.transform.Rotate(0f, (0f - joyRightStickX) * m_characterRotateSpeedGamepad * dt, 0f);
			}
		}
	}

	private void UpdatePasswordError()
	{
		string text = "";
		if (m_publicServerToggle.isOn)
		{
			text = GetPublicPasswordError(m_serverPassword.text, m_world);
		}
		m_passwordError.text = text;
	}

	private string GetPublicPasswordError(string password, World world)
	{
		if (password.Length < m_minimumPasswordLength)
		{
			return Localization.instance.Localize("$menu_passwordshort");
		}
		if (world != null && (world.m_name.Contains(password) || world.m_seedName.Contains(password)))
		{
			return Localization.instance.Localize("$menu_passwordinvalid");
		}
		return "";
	}

	private bool IsPublicPasswordValid(string password, World world)
	{
		if (password.Length < m_minimumPasswordLength)
		{
			return false;
		}
		if (world.m_name.Contains(password))
		{
			return false;
		}
		if (world.m_seedName.Contains(password))
		{
			return false;
		}
		return true;
	}

	private bool CanStartServer()
	{
		if (m_world == null || m_world.m_loadError || m_world.m_versionError)
		{
			return false;
		}
		if (m_publicServerToggle.isOn && !IsPublicPasswordValid(m_serverPassword.text, m_world))
		{
			return false;
		}
		return true;
	}

	private void UpdateCamera(float dt)
	{
		Transform transform = m_cameraMarkerMain;
		if (m_characterSelectScreen.activeSelf)
		{
			transform = m_cameraMarkerCharacter;
		}
		else if (m_creditsPanel.activeSelf)
		{
			transform = m_cameraMarkerCredits;
		}
		else if (m_startGamePanel.activeSelf || m_joinIPPanel.activeSelf)
		{
			transform = m_cameraMarkerGame;
		}
		m_mainCamera.transform.position = Vector3.SmoothDamp(m_mainCamera.transform.position, transform.position, ref camSpeed, 1.5f, 1000f, dt);
		Vector3 forward = Vector3.SmoothDamp(m_mainCamera.transform.forward, transform.forward, ref camRotSpeed, 1.5f, 1000f, dt);
		forward.Normalize();
		m_mainCamera.transform.rotation = Quaternion.LookRotation(forward);
	}

	private void UpdateCharacterList()
	{
		if (m_profiles == null)
		{
			m_profiles = PlayerProfile.GetAllPlayerProfiles();
		}
		if (m_profileIndex >= m_profiles.Count)
		{
			m_profileIndex = m_profiles.Count - 1;
		}
		m_csRemoveButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csStartButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csNewButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csNewBigButton.gameObject.SetActive(m_profiles.Count == 0);
		m_csLeftButton.interactable = m_profileIndex > 0;
		m_csRightButton.interactable = m_profileIndex < m_profiles.Count - 1;
		if (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count)
		{
			PlayerProfile playerProfile = m_profiles[m_profileIndex];
			m_csName.text = playerProfile.GetName();
			m_csName.gameObject.SetActive(value: true);
			SetupCharacterPreview(playerProfile);
		}
		else
		{
			m_csName.gameObject.SetActive(value: false);
			ClearCharacterPreview();
		}
	}

	private void SetSelectedProfile(string filename)
	{
		if (m_profiles == null)
		{
			m_profiles = PlayerProfile.GetAllPlayerProfiles();
		}
		m_profileIndex = 0;
		for (int i = 0; i < m_profiles.Count; i++)
		{
			if (m_profiles[i].GetFilename() == filename)
			{
				m_profileIndex = i;
				break;
			}
		}
		UpdateCharacterList();
	}

	public void OnNewCharacterDone()
	{
		string text = m_csNewCharacterName.text;
		string text2 = text.ToLower();
		if (PlayerProfile.HaveProfile(text2))
		{
			m_newCharacterError.SetActive(value: true);
			return;
		}
		Player component = m_playerInstance.GetComponent<Player>();
		component.GiveDefaultItems();
		PlayerProfile playerProfile = new PlayerProfile(text2);
		playerProfile.SetName(text);
		playerProfile.SavePlayerData(component);
		playerProfile.Save();
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
		m_profiles = null;
		SetSelectedProfile(text2);
		Gogan.LogEvent("Menu", "NewCharacter", text, 0L);
	}

	public void OnNewCharacterCancel()
	{
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
		UpdateCharacterList();
	}

	public void OnCharacterNew()
	{
		m_newCharacterPanel.SetActive(value: true);
		m_selectCharacterPanel.SetActive(value: false);
		m_csNewCharacterName.text = "";
		m_newCharacterError.SetActive(value: false);
		SetupCharacterPreview(null);
		Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
	}

	public void OnCharacterRemove()
	{
		if (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count)
		{
			PlayerProfile playerProfile = m_profiles[m_profileIndex];
			m_removeCharacterName.text = playerProfile.GetName();
			m_tempRemoveCharacterName = playerProfile.GetFilename();
			m_tempRemoveCharacterIndex = m_profileIndex;
			m_removeCharacterDialog.SetActive(value: true);
		}
	}

	public void OnButtonRemoveCharacterYes()
	{
		ZLog.Log("Remove character");
		PlayerProfile.RemoveProfile(m_tempRemoveCharacterName);
		m_profiles.RemoveAt(m_tempRemoveCharacterIndex);
		UpdateCharacterList();
		m_removeCharacterDialog.SetActive(value: false);
	}

	public void OnButtonRemoveCharacterNo()
	{
		m_removeCharacterDialog.SetActive(value: false);
	}

	public void OnCharacterLeft()
	{
		if (m_profileIndex > 0)
		{
			m_profileIndex--;
		}
		UpdateCharacterList();
	}

	public void OnCharacterRight()
	{
		if (m_profileIndex < m_profiles.Count - 1)
		{
			m_profileIndex++;
		}
		UpdateCharacterList();
	}

	public void OnCharacterStart()
	{
		ZLog.Log("OnCharacterStart");
		if (m_profileIndex < 0 || m_profileIndex >= m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = m_profiles[m_profileIndex];
		PlayerPrefs.SetString("profile", playerProfile.GetFilename());
		Game.SetProfile(playerProfile.GetFilename());
		m_characterSelectScreen.SetActive(value: false);
		if (m_queuedJoinServer != null)
		{
			m_joinServer = m_queuedJoinServer;
			m_queuedJoinServer = null;
			JoinServer();
			return;
		}
		ShowStartGame();
		if (m_worlds.Count == 0)
		{
			OnWorldNew();
		}
	}

	private void TransitionToMainScene()
	{
		m_menuAnimator.SetTrigger("FadeOut");
		Invoke("LoadMainScene", 1.5f);
	}

	private void LoadMainScene()
	{
		m_loading.SetActive(value: true);
		SceneManager.LoadScene("main");
	}

	public void OnButtonSettings()
	{
		UnityEngine.Object.Instantiate(m_settingsPrefab, base.transform);
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate(m_feedbackPrefab, base.transform);
	}

	public void OnButtonTwitter()
	{
		Application.OpenURL("https://twitter.com/valheimgame");
	}

	public void OnButtonWebPage()
	{
		Application.OpenURL("http://valheimgame.com/");
	}

	public void OnButtonDiscord()
	{
		Application.OpenURL("https://discord.gg/44qXMJH");
	}

	public void OnButtonFacebook()
	{
		Application.OpenURL("https://www.facebook.com/valheimgame/");
	}

	public void OnButtonShowLog()
	{
		Application.OpenURL(Application.persistentDataPath + "/");
	}

	private bool AcceptedNDA()
	{
		return PlayerPrefs.GetInt("accepted_nda", 0) == 1;
	}

	public void OnButtonNDAAccept()
	{
		PlayerPrefs.SetInt("accepted_nda", 1);
		m_ndaPanel.SetActive(value: false);
		m_mainMenu.SetActive(value: true);
	}

	public void OnButtonNDADecline()
	{
		Application.Quit();
	}

	public void OnConnectionFailedOk()
	{
		m_connectionFailedPanel.SetActive(value: false);
	}

	public Player GetPreviewPlayer()
	{
		if (m_playerInstance != null)
		{
			return m_playerInstance.GetComponent<Player>();
		}
		return null;
	}

	private void ClearCharacterPreview()
	{
		if ((bool)m_playerInstance)
		{
			UnityEngine.Object.Instantiate(m_changeEffectPrefab, m_characterPreviewPoint.position, m_characterPreviewPoint.rotation);
			UnityEngine.Object.Destroy(m_playerInstance);
			m_playerInstance = null;
		}
	}

	private void SetupCharacterPreview(PlayerProfile profile)
	{
		ClearCharacterPreview();
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate(m_playerPrefab, m_characterPreviewPoint.position, m_characterPreviewPoint.rotation);
		ZNetView.m_forceDisableInit = false;
		UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
		Animator[] componentsInChildren = gameObject.GetComponentsInChildren<Animator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].updateMode = AnimatorUpdateMode.Normal;
		}
		Player component = gameObject.GetComponent<Player>();
		profile?.LoadPlayerData(component);
		m_playerInstance = gameObject;
	}
}
