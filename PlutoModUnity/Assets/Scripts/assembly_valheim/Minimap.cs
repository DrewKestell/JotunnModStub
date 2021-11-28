using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
	public enum MapMode
	{
		None,
		Small,
		Large
	}

	public enum PinType
	{
		Icon0,
		Icon1,
		Icon2,
		Icon3,
		Death,
		Bed,
		Icon4,
		Shout,
		None,
		Boss,
		Player,
		RandomEvent,
		Ping,
		EventArea
	}

	public class PinData
	{
		public string m_name;

		public PinType m_type;

		public Sprite m_icon;

		public Vector3 m_pos;

		public bool m_save;

		public long m_ownerID;

		public bool m_checked;

		public bool m_doubleSize;

		public bool m_animate;

		public float m_worldSize;

		public RectTransform m_uiElement;

		public GameObject m_checkedElement;

		public Text m_nameElement;

		public Image m_iconElement;
	}

	[Serializable]
	public struct SpriteData
	{
		public PinType m_name;

		public Sprite m_icon;
	}

	[Serializable]
	public struct LocationSpriteData
	{
		public string m_name;

		public Sprite m_icon;
	}

	private Color forest = new Color(1f, 0f, 0f, 0f);

	private Color noForest = new Color(0f, 0f, 0f, 0f);

	private static int MAPVERSION = 7;

	private const int sharedMapDataVersion = 2;

	private static Minimap m_instance;

	public GameObject m_smallRoot;

	public GameObject m_largeRoot;

	public RawImage m_mapImageSmall;

	public RawImage m_mapImageLarge;

	public RectTransform m_pinRootSmall;

	public RectTransform m_pinRootLarge;

	public Text m_biomeNameSmall;

	public Text m_biomeNameLarge;

	public RectTransform m_smallShipMarker;

	public RectTransform m_largeShipMarker;

	public RectTransform m_smallMarker;

	public RectTransform m_largeMarker;

	public RectTransform m_windMarker;

	public RectTransform m_gamepadCrosshair;

	public Toggle m_publicPosition;

	public Image m_selectedIcon0;

	public Image m_selectedIcon1;

	public Image m_selectedIcon2;

	public Image m_selectedIcon3;

	public Image m_selectedIcon4;

	public Image m_selectedIconDeath;

	public Image m_selectedIconBoss;

	private Dictionary<PinType, Image> m_selectedIcons = new Dictionary<PinType, Image>();

	private bool[] m_visibleIconTypes;

	private bool m_showSharedMapData = true;

	private float m_sharedMapDataFadeRate = 0.5f;

	private float m_sharedMapDataFade;

	public GameObject m_mapSmall;

	public GameObject m_mapLarge;

	private Material m_mapSmallShader;

	private Material m_mapLargeShader;

	public GameObject m_pinPrefab;

	public InputField m_nameInput;

	public int m_textureSize = 256;

	public float m_pixelSize = 64f;

	public float m_minZoom = 0.01f;

	public float m_maxZoom = 1f;

	public float m_showNamesZoom = 0.5f;

	public float m_exploreInterval = 2f;

	public float m_exploreRadius = 100f;

	public float m_removeRadius = 128f;

	public float m_pinSizeSmall = 32f;

	public float m_pinSizeLarge = 48f;

	public float m_clickDuration = 0.25f;

	public List<SpriteData> m_icons = new List<SpriteData>();

	public List<LocationSpriteData> m_locationIcons = new List<LocationSpriteData>();

	public Color m_meadowsColor = new Color(0.45f, 1f, 0.43f);

	public Color m_ashlandsColor = new Color(1f, 0.2f, 0.2f);

	public Color m_blackforestColor = new Color(0f, 0.7f, 0f);

	public Color m_deepnorthColor = new Color(1f, 1f, 1f);

	public Color m_heathColor = new Color(1f, 1f, 0.2f);

	public Color m_swampColor = new Color(0.6f, 0.5f, 0.5f);

	public Color m_mountainColor = new Color(1f, 1f, 1f);

	public Color m_mistlandsColor = new Color(0.5f, 0.5f, 0.5f);

	private PinData m_namePin;

	private PinType m_selectedType;

	private PinData m_deathPin;

	private PinData m_spawnPointPin;

	private Dictionary<Vector3, PinData> m_locationPins = new Dictionary<Vector3, PinData>();

	private float m_updateLocationsTimer;

	private List<PinData> m_pingPins = new List<PinData>();

	private List<PinData> m_shoutPins = new List<PinData>();

	private List<Chat.WorldTextInstance> m_tempShouts = new List<Chat.WorldTextInstance>();

	private List<PinData> m_playerPins = new List<PinData>();

	private List<ZNet.PlayerInfo> m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

	private PinData m_randEventPin;

	private PinData m_randEventAreaPin;

	private float m_updateEventTime;

	private bool[] m_explored;

	private bool[] m_exploredOthers;

	public GameObject m_sharedMapHint;

	public List<GameObject> m_hints;

	private List<PinData> m_pins = new List<PinData>();

	private Texture2D m_forestMaskTexture;

	private Texture2D m_mapTexture;

	private Texture2D m_heightTexture;

	private Texture2D m_fogTexture;

	private float m_largeZoom = 0.1f;

	private float m_smallZoom = 0.01f;

	private Heightmap.Biome m_biome;

	private MapMode m_mode;

	private float m_exploreTimer;

	private bool m_hasGenerated;

	private bool m_dragView = true;

	private Vector3 m_mapOffset = Vector3.zero;

	private float m_leftDownTime;

	private float m_leftClickTime;

	private Vector3 m_dragWorldPos = Vector3.zero;

	private bool m_wasFocused;

	private const bool m_enableLastDeathAutoPin = false;

	public static Minimap instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_largeRoot.SetActive(value: false);
		m_smallRoot.SetActive(value: true);
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	public static bool IsOpen()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_largeRoot.activeSelf;
		}
		return false;
	}

	public static bool InTextInput()
	{
		if ((bool)m_instance && m_instance.m_mode == MapMode.Large)
		{
			return m_instance.m_wasFocused;
		}
		return false;
	}

	private void Start()
	{
		m_mapTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGBA32, mipChain: false);
		m_mapTexture.wrapMode = TextureWrapMode.Clamp;
		m_forestMaskTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGBA32, mipChain: false);
		m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
		m_heightTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RFloat, mipChain: false);
		m_heightTexture.wrapMode = TextureWrapMode.Clamp;
		m_fogTexture = new Texture2D(m_textureSize, m_textureSize, TextureFormat.RGBA32, mipChain: false);
		m_fogTexture.wrapMode = TextureWrapMode.Clamp;
		m_explored = new bool[m_textureSize * m_textureSize];
		m_exploredOthers = new bool[m_textureSize * m_textureSize];
		m_mapImageLarge.material = UnityEngine.Object.Instantiate(m_mapImageLarge.material);
		m_mapImageSmall.material = UnityEngine.Object.Instantiate(m_mapImageSmall.material);
		m_mapImageLarge.material.SetTexture("_MainTex", m_mapTexture);
		m_mapImageLarge.material.SetTexture("_MaskTex", m_forestMaskTexture);
		m_mapImageLarge.material.SetTexture("_HeightTex", m_heightTexture);
		m_mapImageLarge.material.SetTexture("_FogTex", m_fogTexture);
		m_mapImageSmall.material.SetTexture("_MainTex", m_mapTexture);
		m_mapImageSmall.material.SetTexture("_MaskTex", m_forestMaskTexture);
		m_mapImageSmall.material.SetTexture("_HeightTex", m_heightTexture);
		m_mapImageSmall.material.SetTexture("_FogTex", m_fogTexture);
		m_nameInput.gameObject.SetActive(value: false);
		UIInputHandler component = m_mapImageLarge.GetComponent<UIInputHandler>();
		component.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightClick, new Action<UIInputHandler>(OnMapRightClick));
		component.m_onMiddleClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onMiddleClick, new Action<UIInputHandler>(OnMapMiddleClick));
		component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(OnMapLeftDown));
		component.m_onLeftUp = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftUp, new Action<UIInputHandler>(OnMapLeftUp));
		m_visibleIconTypes = new bool[Enum.GetValues(typeof(PinType)).Length];
		for (int i = 0; i < m_visibleIconTypes.Length; i++)
		{
			m_visibleIconTypes[i] = true;
		}
		m_mapSmallShader = m_mapSmall.GetComponent<RawImage>().material;
		m_mapLargeShader = m_mapLarge.GetComponent<RawImage>().material;
		m_selectedIcons[PinType.Death] = m_selectedIconDeath;
		m_selectedIcons[PinType.Boss] = m_selectedIconBoss;
		m_selectedIcons[PinType.Icon0] = m_selectedIcon0;
		m_selectedIcons[PinType.Icon1] = m_selectedIcon1;
		m_selectedIcons[PinType.Icon2] = m_selectedIcon2;
		m_selectedIcons[PinType.Icon3] = m_selectedIcon3;
		m_selectedIcons[PinType.Icon4] = m_selectedIcon4;
		SelectIcon(PinType.Icon0);
		Reset();
	}

	public void Reset()
	{
		Color32[] array = new Color32[m_textureSize * m_textureSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		m_fogTexture.SetPixels32(array);
		m_fogTexture.Apply();
		for (int j = 0; j < m_explored.Length; j++)
		{
			m_explored[j] = false;
			m_exploredOthers[j] = false;
		}
		m_sharedMapHint.gameObject.SetActive(value: false);
	}

	public void ForceRegen()
	{
		if (WorldGenerator.instance != null)
		{
			GenerateWorldMap();
		}
	}

	private void Update()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Utils.GetMainCamera() == null)
		{
			return;
		}
		if (!m_hasGenerated)
		{
			if (WorldGenerator.instance == null)
			{
				return;
			}
			GenerateWorldMap();
			LoadMapData();
			m_hasGenerated = true;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		UpdateExplore(deltaTime, localPlayer);
		if (localPlayer.IsDead())
		{
			SetMapMode(MapMode.None);
			return;
		}
		if (m_mode == MapMode.None)
		{
			SetMapMode(MapMode.Small);
		}
		bool flag = (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !Menu.IsVisible() && !InventoryGui.IsVisible();
		if (flag)
		{
			if (InTextInput())
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					m_namePin = null;
				}
			}
			else if (ZInput.GetButtonDown("Map") || ZInput.GetButtonDown("JoyMap") || (m_mode == MapMode.Large && (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMap"))))
			{
				switch (m_mode)
				{
				case MapMode.None:
					SetMapMode(MapMode.Small);
					break;
				case MapMode.Small:
					SetMapMode(MapMode.Large);
					break;
				case MapMode.Large:
					SetMapMode(MapMode.Small);
					break;
				}
			}
		}
		if (m_mode == MapMode.Large)
		{
			m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
			m_gamepadCrosshair.gameObject.SetActive(ZInput.IsGamepadActive());
		}
		if (m_showSharedMapData && m_sharedMapDataFade < 1f)
		{
			m_sharedMapDataFade = Mathf.Min(1f, m_sharedMapDataFade + m_sharedMapDataFadeRate * deltaTime);
			m_mapSmallShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			m_mapLargeShader.SetFloat("_SharedFade", m_sharedMapDataFade);
		}
		else if (!m_showSharedMapData && m_sharedMapDataFade > 0f)
		{
			m_sharedMapDataFade = Mathf.Max(0f, m_sharedMapDataFade - m_sharedMapDataFadeRate * deltaTime);
			m_mapSmallShader.SetFloat("_SharedFade", m_sharedMapDataFade);
			m_mapLargeShader.SetFloat("_SharedFade", m_sharedMapDataFade);
		}
		UpdateMap(localPlayer, deltaTime, flag);
		UpdateDynamicPins(deltaTime);
		UpdatePins();
		UpdateBiome(localPlayer);
		UpdateNameInput();
	}

	private void ShowPinNameInput(PinData pin)
	{
		m_namePin = pin;
		m_nameInput.text = "";
	}

	private void UpdateNameInput()
	{
		if (m_namePin == null)
		{
			m_wasFocused = false;
		}
		if (m_namePin != null && m_mode == MapMode.Large)
		{
			m_nameInput.gameObject.SetActive(value: true);
			if (!m_nameInput.isFocused)
			{
				EventSystem.current.SetSelectedGameObject(m_nameInput.gameObject);
			}
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				string text = m_nameInput.text;
				text = text.Replace('$', ' ');
				text = text.Replace('<', ' ');
				text = text.Replace('>', ' ');
				m_namePin.m_name = text;
				m_namePin = null;
			}
			m_wasFocused = true;
		}
		else
		{
			m_nameInput.gameObject.SetActive(value: false);
		}
	}

	private void UpdateMap(Player player, float dt, bool takeInput)
	{
		if (takeInput)
		{
			if (m_mode == MapMode.Large)
			{
				float num = 0f;
				num += Input.GetAxis("Mouse ScrollWheel") * m_largeZoom * 2f;
				if (ZInput.GetButton("JoyButtonX"))
				{
					Vector3 viewCenterWorldPoint = GetViewCenterWorldPoint();
					Chat.instance.SendPing(viewCenterWorldPoint);
				}
				if (ZInput.GetButton("JoyLTrigger"))
				{
					num -= m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButton("JoyRTrigger"))
				{
					num += m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButtonDown("JoyDPadUp"))
				{
					PinType pinType = PinType.None;
					foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
					{
						if (selectedIcon.Key == m_selectedType && pinType != PinType.None)
						{
							SelectIcon(pinType);
							break;
						}
						pinType = selectedIcon.Key;
					}
				}
				else if (ZInput.GetButtonDown("JoyDPadDown"))
				{
					bool flag = false;
					foreach (KeyValuePair<PinType, Image> selectedIcon2 in m_selectedIcons)
					{
						if (flag)
						{
							SelectIcon(selectedIcon2.Key);
							break;
						}
						if (selectedIcon2.Key == m_selectedType)
						{
							flag = true;
						}
					}
				}
				if (ZInput.GetButtonDown("JoyDPadRight"))
				{
					ToggleIconFilter(m_selectedType);
				}
				if (ZInput.GetButtonDown("JoyButtonA"))
				{
					Vector3 pos = ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
					AddPin(pos, (m_selectedType != PinType.Death && m_selectedType != PinType.Boss) ? m_selectedType : PinType.Icon0, "", save: true, isChecked: false, 0L);
				}
				if (ZInput.GetButtonDown("JoyButtonB"))
				{
					Vector3 pos2 = ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
					RemovePin(pos2, m_removeRadius * (m_largeZoom * 2f));
					m_namePin = null;
				}
				if (ZInput.GetButtonDown("MapZoomOut") && !InTextInput())
				{
					num -= m_largeZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !InTextInput())
				{
					num += m_largeZoom * 0.5f;
				}
				m_largeZoom = Mathf.Clamp(m_largeZoom - num, m_minZoom, m_maxZoom);
			}
			else
			{
				float num2 = 0f;
				if (ZInput.GetButtonDown("MapZoomOut"))
				{
					num2 -= m_smallZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn"))
				{
					num2 += m_smallZoom * 0.5f;
				}
				m_smallZoom = Mathf.Clamp(m_smallZoom - num2, m_minZoom, m_maxZoom);
			}
		}
		if (m_mode == MapMode.Large)
		{
			if (m_leftDownTime != 0f && m_leftDownTime > m_clickDuration && !m_dragView)
			{
				m_dragWorldPos = ScreenToWorldPoint(Input.mousePosition);
				m_dragView = true;
				m_namePin = null;
			}
			m_mapOffset.x += ZInput.GetJoyLeftStickX(smooth: true) * dt * 50000f * m_largeZoom;
			m_mapOffset.z -= ZInput.GetJoyLeftStickY() * dt * 50000f * m_largeZoom;
			if (m_dragView)
			{
				Vector3 vector = ScreenToWorldPoint(Input.mousePosition) - m_dragWorldPos;
				m_mapOffset -= vector;
				CenterMap(player.transform.position + m_mapOffset);
				m_dragWorldPos = ScreenToWorldPoint(Input.mousePosition);
			}
			else
			{
				CenterMap(player.transform.position + m_mapOffset);
			}
		}
		else
		{
			CenterMap(player.transform.position);
		}
		UpdateWindMarker();
		UpdatePlayerMarker(player, Utils.GetMainCamera().transform.rotation);
	}

	public void SetMapMode(MapMode mode)
	{
		if (mode == m_mode)
		{
			return;
		}
		if (Player.m_localPlayer != null && PlayerPrefs.GetFloat("mapenabled_" + Player.m_localPlayer.GetPlayerName(), 1f) == 0f)
		{
			mode = MapMode.None;
		}
		m_mode = mode;
		switch (mode)
		{
		case MapMode.None:
			m_largeRoot.SetActive(value: false);
			m_smallRoot.SetActive(value: false);
			break;
		case MapMode.Small:
			m_largeRoot.SetActive(value: false);
			m_smallRoot.SetActive(value: true);
			break;
		case MapMode.Large:
		{
			m_largeRoot.SetActive(value: true);
			m_smallRoot.SetActive(value: false);
			bool active = PlayerPrefs.GetInt("KeyHints", 1) == 1;
			foreach (GameObject hint in m_hints)
			{
				hint.SetActive(active);
			}
			m_dragView = false;
			m_mapOffset = Vector3.zero;
			m_namePin = null;
			break;
		}
		}
	}

	private void CenterMap(Vector3 centerPoint)
	{
		WorldToMapPoint(centerPoint, out var mx, out var my);
		Rect uvRect = m_mapImageSmall.uvRect;
		uvRect.width = m_smallZoom;
		uvRect.height = m_smallZoom;
		uvRect.center = new Vector2(mx, my);
		m_mapImageSmall.uvRect = uvRect;
		RectTransform rectTransform = m_mapImageLarge.transform as RectTransform;
		float num = rectTransform.rect.width / rectTransform.rect.height;
		Rect uvRect2 = m_mapImageSmall.uvRect;
		uvRect2.width = m_largeZoom * num;
		uvRect2.height = m_largeZoom;
		uvRect2.center = new Vector2(mx, my);
		m_mapImageLarge.uvRect = uvRect2;
		if (m_mode == MapMode.Large)
		{
			m_mapImageLarge.material.SetFloat("_zoom", m_largeZoom);
			m_mapImageLarge.material.SetFloat("_pixelSize", 200f / m_largeZoom);
			m_mapImageLarge.material.SetVector("_mapCenter", centerPoint);
		}
		else
		{
			m_mapImageSmall.material.SetFloat("_zoom", m_smallZoom);
			m_mapImageSmall.material.SetFloat("_pixelSize", 200f / m_smallZoom);
			m_mapImageSmall.material.SetVector("_mapCenter", centerPoint);
		}
	}

	private void UpdateDynamicPins(float dt)
	{
		UpdateProfilePins();
		UpdateShoutPins();
		UpdatePingPins();
		UpdatePlayerPins(dt);
		UpdateLocationPins(dt);
		UpdateEventPin(dt);
	}

	private void UpdateProfilePins()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.HaveDeathPoint();
		if (m_deathPin != null)
		{
			RemovePin(m_deathPin);
			m_deathPin = null;
		}
		if (playerProfile.HaveCustomSpawnPoint())
		{
			if (m_spawnPointPin == null)
			{
				m_spawnPointPin = AddPin(playerProfile.GetCustomSpawnPoint(), PinType.Bed, "", save: false, isChecked: false, 0L);
			}
			m_spawnPointPin.m_pos = playerProfile.GetCustomSpawnPoint();
		}
		else if (m_spawnPointPin != null)
		{
			RemovePin(m_spawnPointPin);
			m_spawnPointPin = null;
		}
	}

	private void UpdateEventPin(float dt)
	{
		if (Time.time - m_updateEventTime < 1f)
		{
			return;
		}
		m_updateEventTime = Time.time;
		RandomEvent currentRandomEvent = RandEventSystem.instance.GetCurrentRandomEvent();
		if (currentRandomEvent != null)
		{
			if (m_randEventAreaPin == null)
			{
				m_randEventAreaPin = AddPin(currentRandomEvent.m_pos, PinType.EventArea, "", save: false, isChecked: false, 0L);
				m_randEventAreaPin.m_worldSize = RandEventSystem.instance.m_randomEventRange * 2f;
				m_randEventAreaPin.m_worldSize *= 0.9f;
			}
			if (m_randEventPin == null)
			{
				m_randEventPin = AddPin(currentRandomEvent.m_pos, PinType.RandomEvent, "", save: false, isChecked: false, 0L);
				m_randEventPin.m_animate = true;
				m_randEventPin.m_doubleSize = true;
			}
			m_randEventAreaPin.m_pos = currentRandomEvent.m_pos;
			m_randEventPin.m_pos = currentRandomEvent.m_pos;
			m_randEventPin.m_name = Localization.instance.Localize(currentRandomEvent.GetHudText());
		}
		else
		{
			if (m_randEventPin != null)
			{
				RemovePin(m_randEventPin);
				m_randEventPin = null;
			}
			if (m_randEventAreaPin != null)
			{
				RemovePin(m_randEventAreaPin);
				m_randEventAreaPin = null;
			}
		}
	}

	private void UpdateLocationPins(float dt)
	{
		m_updateLocationsTimer -= dt;
		if (!(m_updateLocationsTimer <= 0f))
		{
			return;
		}
		m_updateLocationsTimer = 5f;
		Dictionary<Vector3, string> dictionary = new Dictionary<Vector3, string>();
		ZoneSystem.instance.GetLocationIcons(dictionary);
		bool flag = false;
		while (!flag)
		{
			flag = true;
			foreach (KeyValuePair<Vector3, PinData> locationPin in m_locationPins)
			{
				if (!dictionary.ContainsKey(locationPin.Key))
				{
					ZLog.DevLog("Minimap: Removing location " + locationPin.Value.m_name);
					RemovePin(locationPin.Value);
					m_locationPins.Remove(locationPin.Key);
					flag = false;
					break;
				}
			}
		}
		foreach (KeyValuePair<Vector3, string> item in dictionary)
		{
			if (!m_locationPins.ContainsKey(item.Key))
			{
				Sprite locationIcon = GetLocationIcon(item.Value);
				if ((bool)locationIcon)
				{
					PinData pinData = AddPin(item.Key, PinType.None, "", save: false, isChecked: false, 0L);
					pinData.m_icon = locationIcon;
					pinData.m_doubleSize = true;
					m_locationPins.Add(item.Key, pinData);
					ZLog.Log("Minimap: Adding unique location " + item.Key);
				}
			}
		}
	}

	private Sprite GetLocationIcon(string name)
	{
		foreach (LocationSpriteData locationIcon in m_locationIcons)
		{
			if (locationIcon.m_name == name)
			{
				return locationIcon.m_icon;
			}
		}
		return null;
	}

	private void UpdatePlayerPins(float dt)
	{
		m_tempPlayerInfo.Clear();
		ZNet.instance.GetOtherPublicPlayers(m_tempPlayerInfo);
		if (m_playerPins.Count != m_tempPlayerInfo.Count)
		{
			foreach (PinData playerPin in m_playerPins)
			{
				RemovePin(playerPin);
			}
			m_playerPins.Clear();
			foreach (ZNet.PlayerInfo item2 in m_tempPlayerInfo)
			{
				_ = item2;
				PinData item = AddPin(Vector3.zero, PinType.Player, "", save: false, isChecked: false, 0L);
				m_playerPins.Add(item);
			}
		}
		for (int i = 0; i < m_tempPlayerInfo.Count; i++)
		{
			PinData pinData = m_playerPins[i];
			ZNet.PlayerInfo playerInfo = m_tempPlayerInfo[i];
			if (pinData.m_name == playerInfo.m_name)
			{
				pinData.m_pos = Vector3.MoveTowards(pinData.m_pos, playerInfo.m_position, 200f * dt);
				continue;
			}
			pinData.m_name = playerInfo.m_name;
			pinData.m_pos = playerInfo.m_position;
		}
	}

	private void UpdatePingPins()
	{
		m_tempShouts.Clear();
		Chat.instance.GetPingWorldTexts(m_tempShouts);
		if (m_pingPins.Count != m_tempShouts.Count)
		{
			foreach (PinData pingPin in m_pingPins)
			{
				RemovePin(pingPin);
			}
			m_pingPins.Clear();
			foreach (Chat.WorldTextInstance tempShout in m_tempShouts)
			{
				_ = tempShout;
				PinData pinData = AddPin(Vector3.zero, PinType.Ping, "", save: false, isChecked: false, 0L);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				m_pingPins.Add(pinData);
			}
		}
		for (int i = 0; i < m_tempShouts.Count; i++)
		{
			PinData pinData2 = m_pingPins[i];
			Chat.WorldTextInstance worldTextInstance = m_tempShouts[i];
			pinData2.m_pos = worldTextInstance.m_position;
			pinData2.m_name = worldTextInstance.m_name + ": " + worldTextInstance.m_text;
		}
	}

	private void UpdateShoutPins()
	{
		m_tempShouts.Clear();
		Chat.instance.GetShoutWorldTexts(m_tempShouts);
		if (m_shoutPins.Count != m_tempShouts.Count)
		{
			foreach (PinData shoutPin in m_shoutPins)
			{
				RemovePin(shoutPin);
			}
			m_shoutPins.Clear();
			foreach (Chat.WorldTextInstance tempShout in m_tempShouts)
			{
				_ = tempShout;
				PinData pinData = AddPin(Vector3.zero, PinType.Shout, "", save: false, isChecked: false, 0L);
				pinData.m_doubleSize = true;
				pinData.m_animate = true;
				m_shoutPins.Add(pinData);
			}
		}
		for (int i = 0; i < m_tempShouts.Count; i++)
		{
			PinData pinData2 = m_shoutPins[i];
			Chat.WorldTextInstance worldTextInstance = m_tempShouts[i];
			pinData2.m_pos = worldTextInstance.m_position;
			pinData2.m_name = worldTextInstance.m_name + ": " + worldTextInstance.m_text;
		}
	}

	private void UpdatePins()
	{
		RawImage rawImage = ((m_mode == MapMode.Large) ? m_mapImageLarge : m_mapImageSmall);
		float num = ((m_mode == MapMode.Large) ? m_pinSizeLarge : m_pinSizeSmall);
		RectTransform rectTransform = ((m_mode == MapMode.Large) ? m_pinRootLarge : m_pinRootSmall);
		if (m_mode != MapMode.Large)
		{
			_ = m_smallZoom;
		}
		else
		{
			_ = m_largeZoom;
		}
		Color color = new Color(0.7f, 0.7f, 0.7f, 0.8f * m_sharedMapDataFade);
		foreach (PinData pin in m_pins)
		{
			if (IsPointVisible(pin.m_pos, rawImage) && m_visibleIconTypes[(int)pin.m_type] && (m_sharedMapDataFade > 0f || pin.m_ownerID == 0L))
			{
				if (pin.m_uiElement == null || pin.m_uiElement.parent != rectTransform)
				{
					if (pin.m_uiElement != null)
					{
						UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
					}
					GameObject gameObject = UnityEngine.Object.Instantiate(m_pinPrefab);
					pin.m_iconElement = gameObject.GetComponent<Image>();
					pin.m_iconElement.sprite = pin.m_icon;
					pin.m_uiElement = gameObject.transform as RectTransform;
					pin.m_uiElement.SetParent(rectTransform);
					float size = (pin.m_doubleSize ? (num * 2f) : num);
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
					pin.m_checkedElement = gameObject.transform.Find("Checked").gameObject;
					pin.m_nameElement = gameObject.transform.Find("Name").GetComponent<Text>();
				}
				if (pin.m_ownerID != 0L && m_sharedMapHint != null)
				{
					m_sharedMapHint.gameObject.SetActive(value: true);
				}
				pin.m_iconElement.color = ((pin.m_ownerID != 0L) ? color : Color.white);
				pin.m_nameElement.color = ((pin.m_ownerID != 0L) ? color : Color.white);
				WorldToMapPoint(pin.m_pos, out var mx, out var my);
				Vector2 anchoredPosition = MapPointToLocalGuiPos(mx, my, rawImage);
				pin.m_uiElement.anchoredPosition = anchoredPosition;
				if (pin.m_animate)
				{
					float num2 = (pin.m_doubleSize ? (num * 2f) : num);
					num2 *= 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
				}
				if (pin.m_worldSize > 0f)
				{
					Vector2 size2 = new Vector2(pin.m_worldSize / m_pixelSize / (float)m_textureSize, pin.m_worldSize / m_pixelSize / (float)m_textureSize);
					Vector2 vector = MapSizeToLocalGuiSize(size2, rawImage);
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector.x);
					pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector.y);
				}
				pin.m_checkedElement.SetActive(pin.m_checked);
				if (pin.m_name.Length > 0 && m_mode == MapMode.Large && m_largeZoom < m_showNamesZoom)
				{
					pin.m_nameElement.gameObject.SetActive(value: true);
					pin.m_nameElement.text = Localization.instance.Localize(pin.m_name);
				}
				else
				{
					pin.m_nameElement.gameObject.SetActive(value: false);
				}
			}
			else if (pin.m_uiElement != null)
			{
				UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
				pin.m_uiElement = null;
			}
		}
	}

	private void UpdateWindMarker()
	{
		Quaternion quaternion = Quaternion.LookRotation(EnvMan.instance.GetWindDir());
		m_windMarker.rotation = Quaternion.Euler(0f, 0f, 0f - quaternion.eulerAngles.y);
	}

	private void UpdatePlayerMarker(Player player, Quaternion playerRot)
	{
		Vector3 position = player.transform.position;
		Vector3 eulerAngles = playerRot.eulerAngles;
		m_smallMarker.rotation = Quaternion.Euler(0f, 0f, 0f - eulerAngles.y);
		if (m_mode == MapMode.Large && IsPointVisible(position, m_mapImageLarge))
		{
			m_largeMarker.gameObject.SetActive(value: true);
			m_largeMarker.rotation = m_smallMarker.rotation;
			WorldToMapPoint(position, out var mx, out var my);
			Vector2 anchoredPosition = MapPointToLocalGuiPos(mx, my, m_mapImageLarge);
			m_largeMarker.anchoredPosition = anchoredPosition;
		}
		else
		{
			m_largeMarker.gameObject.SetActive(value: false);
		}
		Ship controlledShip = player.GetControlledShip();
		if ((bool)controlledShip)
		{
			m_smallShipMarker.gameObject.SetActive(value: true);
			Vector3 eulerAngles2 = controlledShip.transform.rotation.eulerAngles;
			m_smallShipMarker.rotation = Quaternion.Euler(0f, 0f, 0f - eulerAngles2.y);
			if (m_mode == MapMode.Large)
			{
				m_largeShipMarker.gameObject.SetActive(value: true);
				Vector3 position2 = controlledShip.transform.position;
				WorldToMapPoint(position2, out var mx2, out var my2);
				Vector2 anchoredPosition2 = MapPointToLocalGuiPos(mx2, my2, m_mapImageLarge);
				m_largeShipMarker.anchoredPosition = anchoredPosition2;
				m_largeShipMarker.rotation = m_smallShipMarker.rotation;
			}
		}
		else
		{
			m_smallShipMarker.gameObject.SetActive(value: false);
			m_largeShipMarker.gameObject.SetActive(value: false);
		}
	}

	private Vector2 MapPointToLocalGuiPos(float mx, float my, RawImage img)
	{
		Vector2 result = default(Vector2);
		result.x = (mx - img.uvRect.xMin) / img.uvRect.width;
		result.y = (my - img.uvRect.yMin) / img.uvRect.height;
		result.x *= img.rectTransform.rect.width;
		result.y *= img.rectTransform.rect.height;
		return result;
	}

	private Vector2 MapSizeToLocalGuiSize(Vector2 size, RawImage img)
	{
		size.x /= img.uvRect.width;
		size.y /= img.uvRect.height;
		return new Vector2(size.x * img.rectTransform.rect.width, size.y * img.rectTransform.rect.height);
	}

	private bool IsPointVisible(Vector3 p, RawImage map)
	{
		WorldToMapPoint(p, out var mx, out var my);
		if (mx > map.uvRect.xMin && mx < map.uvRect.xMax && my > map.uvRect.yMin)
		{
			return my < map.uvRect.yMax;
		}
		return false;
	}

	public void ExploreAll()
	{
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				Explore(j, i);
			}
		}
		m_fogTexture.Apply();
	}

	private void WorldToMapPoint(Vector3 p, out float mx, out float my)
	{
		int num = m_textureSize / 2;
		mx = p.x / m_pixelSize + (float)num;
		my = p.z / m_pixelSize + (float)num;
		mx /= m_textureSize;
		my /= m_textureSize;
	}

	private Vector3 MapPointToWorld(float mx, float my)
	{
		int num = m_textureSize / 2;
		mx *= (float)m_textureSize;
		my *= (float)m_textureSize;
		mx -= (float)num;
		my -= (float)num;
		mx *= m_pixelSize;
		my *= m_pixelSize;
		return new Vector3(mx, 0f, my);
	}

	private void WorldToPixel(Vector3 p, out int px, out int py)
	{
		int num = m_textureSize / 2;
		px = Mathf.RoundToInt(p.x / m_pixelSize + (float)num);
		py = Mathf.RoundToInt(p.z / m_pixelSize + (float)num);
	}

	private void UpdateExplore(float dt, Player player)
	{
		m_exploreTimer += Time.deltaTime;
		if (m_exploreTimer > m_exploreInterval)
		{
			m_exploreTimer = 0f;
			Explore(player.transform.position, m_exploreRadius);
		}
	}

	private void Explore(Vector3 p, float radius)
	{
		int num = (int)Mathf.Ceil(radius / m_pixelSize);
		bool flag = false;
		WorldToPixel(p, out var px, out var py);
		for (int i = py - num; i <= py + num; i++)
		{
			for (int j = px - num; j <= px + num; j++)
			{
				if (j >= 0 && i >= 0 && j < m_textureSize && i < m_textureSize && !(new Vector2(j - px, i - py).magnitude > (float)num) && Explore(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			m_fogTexture.Apply();
		}
	}

	private bool Explore(int x, int y)
	{
		if (m_explored[y * m_textureSize + x])
		{
			return false;
		}
		Color pixel = m_fogTexture.GetPixel(x, y);
		pixel.r = 0f;
		m_fogTexture.SetPixel(x, y, pixel);
		m_explored[y * m_textureSize + x] = true;
		return true;
	}

	private bool ExploreOthers(int x, int y)
	{
		if (m_exploredOthers[y * m_textureSize + x])
		{
			return false;
		}
		Color pixel = m_fogTexture.GetPixel(x, y);
		pixel.g = 0f;
		m_fogTexture.SetPixel(x, y, pixel);
		m_exploredOthers[y * m_textureSize + x] = true;
		if (m_sharedMapHint != null)
		{
			m_sharedMapHint.gameObject.SetActive(value: true);
		}
		return true;
	}

	private bool IsExplored(Vector3 worldPos)
	{
		WorldToPixel(worldPos, out var px, out var py);
		if (px < 0 || px >= m_textureSize || py < 0 || py >= m_textureSize)
		{
			return false;
		}
		if (!m_explored[py * m_textureSize + px])
		{
			return m_exploredOthers[py * m_textureSize + px];
		}
		return true;
	}

	private float GetHeight(int x, int y)
	{
		return m_heightTexture.GetPixel(x, y).r;
	}

	private void GenerateWorldMap()
	{
		int num = m_textureSize / 2;
		float num2 = m_pixelSize / 2f;
		Color32[] array = new Color32[m_textureSize * m_textureSize];
		Color32[] array2 = new Color32[m_textureSize * m_textureSize];
		Color[] array3 = new Color[m_textureSize * m_textureSize];
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				float wx = (float)(j - num) * m_pixelSize + num2;
				float wy = (float)(i - num) * m_pixelSize + num2;
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(wx, wy);
				float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy);
				array[i * m_textureSize + j] = GetPixelColor(biome);
				array2[i * m_textureSize + j] = GetMaskColor(wx, wy, biomeHeight, biome);
				array3[i * m_textureSize + j] = new Color(biomeHeight, 0f, 0f);
			}
		}
		m_forestMaskTexture.SetPixels32(array2);
		m_forestMaskTexture.Apply();
		m_mapTexture.SetPixels32(array);
		m_mapTexture.Apply();
		m_heightTexture.SetPixels(array3);
		m_heightTexture.Apply();
	}

	private Color GetMaskColor(float wx, float wy, float height, Heightmap.Biome biome)
	{
		if (height < ZoneSystem.instance.m_waterLevel)
		{
			return noForest;
		}
		switch (biome)
		{
		case Heightmap.Biome.Meadows:
			if (!WorldGenerator.InForest(new Vector3(wx, 0f, wy)))
			{
				return noForest;
			}
			return forest;
		case Heightmap.Biome.Plains:
			if (!(WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy)) < 0.8f))
			{
				return noForest;
			}
			return forest;
		case Heightmap.Biome.BlackForest:
		case Heightmap.Biome.Mistlands:
			return forest;
		default:
			return noForest;
		}
	}

	private Color GetPixelColor(Heightmap.Biome biome)
	{
		switch (biome)
		{
		case Heightmap.Biome.Meadows:
			return m_meadowsColor;
		case Heightmap.Biome.AshLands:
			return m_ashlandsColor;
		case Heightmap.Biome.BlackForest:
			return m_blackforestColor;
		case Heightmap.Biome.DeepNorth:
			return m_deepnorthColor;
		case Heightmap.Biome.Plains:
			return m_heathColor;
		case Heightmap.Biome.Swamp:
			return m_swampColor;
		case Heightmap.Biome.Mountain:
			return m_mountainColor;
		case Heightmap.Biome.Mistlands:
			return m_mistlandsColor;
		case Heightmap.Biome.Ocean:
			return Color.white;
		default:
			return Color.white;
		}
	}

	private void LoadMapData()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.GetMapData() != null)
		{
			SetMapData(playerProfile.GetMapData());
		}
	}

	public void SaveMapData()
	{
		Game.instance.GetPlayerProfile().SetMapData(GetMapData());
	}

	private byte[] GetMapData()
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(MAPVERSION);
		ZPackage zPackage2 = new ZPackage();
		zPackage2.Write(m_textureSize);
		for (int i = 0; i < m_explored.Length; i++)
		{
			zPackage2.Write(m_explored[i]);
		}
		for (int j = 0; j < m_explored.Length; j++)
		{
			zPackage2.Write(m_exploredOthers[j]);
		}
		int num = 0;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save)
			{
				num++;
			}
		}
		zPackage2.Write(num);
		foreach (PinData pin2 in m_pins)
		{
			if (pin2.m_save)
			{
				zPackage2.Write(pin2.m_name);
				zPackage2.Write(pin2.m_pos);
				zPackage2.Write((int)pin2.m_type);
				zPackage2.Write(pin2.m_checked);
				zPackage2.Write(pin2.m_ownerID);
			}
		}
		zPackage2.Write(ZNet.instance.IsReferencePositionPublic());
		ZLog.Log("Uncompressed size " + zPackage2.Size());
		zPackage.WriteCompressed(zPackage2);
		ZLog.Log("Compressed size " + zPackage.Size());
		return zPackage.GetArray();
	}

	private void SetMapData(byte[] data)
	{
		ZPackage zPackage = new ZPackage(data);
		int num = zPackage.ReadInt();
		if (num >= 7)
		{
			ZLog.Log("Unpacking compressed mapdata " + zPackage.Size());
			zPackage = zPackage.ReadCompressedPackage();
		}
		int num2 = zPackage.ReadInt();
		if (m_textureSize != num2)
		{
			ZLog.LogWarning(string.Concat("Missmatching mapsize ", m_mapTexture, " vs ", num2));
			return;
		}
		Reset();
		for (int i = 0; i < m_explored.Length; i++)
		{
			if (zPackage.ReadBool())
			{
				int x = i % num2;
				int y = i / num2;
				Explore(x, y);
			}
		}
		if (num >= 5)
		{
			for (int j = 0; j < m_exploredOthers.Length; j++)
			{
				if (zPackage.ReadBool())
				{
					int x2 = j % num2;
					int y2 = j / num2;
					ExploreOthers(x2, y2);
				}
			}
		}
		if (num >= 2)
		{
			int num3 = zPackage.ReadInt();
			ClearPins();
			for (int k = 0; k < num3; k++)
			{
				string text = zPackage.ReadString();
				Vector3 pos = zPackage.ReadVector3();
				PinType type = (PinType)zPackage.ReadInt();
				bool isChecked = num >= 3 && zPackage.ReadBool();
				long ownerID = ((num >= 6) ? zPackage.ReadLong() : 0);
				AddPin(pos, type, text, save: true, isChecked, ownerID);
			}
		}
		if (num >= 4)
		{
			bool publicReferencePosition = zPackage.ReadBool();
			ZNet.instance.SetPublicReferencePosition(publicReferencePosition);
		}
		m_fogTexture.Apply();
	}

	public bool RemovePin(Vector3 pos, float radius)
	{
		PinData closestPin = GetClosestPin(pos, radius);
		if (closestPin != null)
		{
			RemovePin(closestPin);
			return true;
		}
		return false;
	}

	private bool HavePinInRange(Vector3 pos, float radius)
	{
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save && Utils.DistanceXZ(pos, pin.m_pos) < radius)
			{
				return true;
			}
		}
		return false;
	}

	private PinData GetClosestPin(Vector3 pos, float radius)
	{
		PinData pinData = null;
		float num = 999999f;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save)
			{
				float num2 = Utils.DistanceXZ(pos, pin.m_pos);
				if (num2 < radius && (num2 < num || pinData == null))
				{
					pinData = pin;
					num = num2;
				}
			}
		}
		return pinData;
	}

	public void RemovePin(PinData pin)
	{
		if ((bool)pin.m_uiElement)
		{
			UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
		}
		m_pins.Remove(pin);
	}

	public void ShowPointOnMap(Vector3 point)
	{
		if (!(Player.m_localPlayer == null))
		{
			SetMapMode(MapMode.Large);
			m_mapOffset = point - Player.m_localPlayer.transform.position;
		}
	}

	public bool DiscoverLocation(Vector3 pos, PinType type, string name, bool showMap)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (HaveSimilarPin(pos, type, name, save: true))
		{
			if (showMap)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_pin_exist");
				ShowPointOnMap(pos);
			}
			return false;
		}
		Sprite sprite = GetSprite(type);
		AddPin(pos, type, name, save: true, isChecked: false, 0L);
		if (showMap)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
			ShowPointOnMap(pos);
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
		}
		return true;
	}

	private bool HaveSimilarPin(Vector3 pos, PinType type, string name, bool save)
	{
		foreach (PinData pin in m_pins)
		{
			if (pin.m_name == name && pin.m_type == type && pin.m_save == save && Utils.DistanceXZ(pos, pin.m_pos) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	public PinData AddPin(Vector3 pos, PinType type, string name, bool save, bool isChecked, long ownerID = 0L)
	{
		if ((int)type >= m_visibleIconTypes.Length || type < PinType.Icon0)
		{
			ZLog.LogWarning($"Trying to add invalid pin type: {type}");
			type = PinType.Icon3;
		}
		if (name == null)
		{
			name = "";
		}
		PinData pinData = new PinData();
		pinData.m_type = type;
		pinData.m_name = name;
		pinData.m_pos = pos;
		pinData.m_icon = GetSprite(type);
		pinData.m_save = save;
		pinData.m_checked = isChecked;
		pinData.m_ownerID = ownerID;
		m_pins.Add(pinData);
		if ((int)type < m_visibleIconTypes.Length && !m_visibleIconTypes[(int)type])
		{
			ToggleIconFilter(type);
		}
		return pinData;
	}

	private Sprite GetSprite(PinType type)
	{
		if (type == PinType.None)
		{
			return null;
		}
		return m_icons.Find((SpriteData x) => x.m_name == type).m_icon;
	}

	private Vector3 GetViewCenterWorldPoint()
	{
		Rect uvRect = m_mapImageLarge.uvRect;
		float mx = uvRect.xMin + 0.5f * uvRect.width;
		float my = uvRect.yMin + 0.5f * uvRect.height;
		return MapPointToWorld(mx, my);
	}

	private Vector3 ScreenToWorldPoint(Vector3 mousePos)
	{
		Vector2 screenPoint = mousePos;
		RectTransform rectTransform = m_mapImageLarge.transform as RectTransform;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out var localPoint))
		{
			Vector2 vector = Rect.PointToNormalized(rectTransform.rect, localPoint);
			Rect uvRect = m_mapImageLarge.uvRect;
			float mx = uvRect.xMin + vector.x * uvRect.width;
			float my = uvRect.yMin + vector.y * uvRect.height;
			return MapPointToWorld(mx, my);
		}
		return Vector3.zero;
	}

	private void OnMapLeftDown(UIInputHandler handler)
	{
		if (Time.time - m_leftClickTime < 0.3f)
		{
			OnMapDblClick();
			m_leftClickTime = 0f;
			m_leftDownTime = 0f;
		}
		else
		{
			m_leftClickTime = Time.time;
			m_leftDownTime = Time.time;
		}
	}

	private void OnMapLeftUp(UIInputHandler handler)
	{
		if (m_leftDownTime != 0f)
		{
			if (Time.time - m_leftDownTime < m_clickDuration)
			{
				OnMapLeftClick();
			}
			m_leftDownTime = 0f;
		}
		m_dragView = false;
	}

	public void OnMapDblClick()
	{
		if (m_selectedType != PinType.Death)
		{
			Vector3 pos = ScreenToWorldPoint(Input.mousePosition);
			PinData pin = AddPin(pos, m_selectedType, "", save: true, isChecked: false, 0L);
			ShowPinNameInput(pin);
		}
	}

	public void OnMapLeftClick()
	{
		ZLog.Log("Left click");
		Vector3 pos = ScreenToWorldPoint(Input.mousePosition);
		PinData closestPin = GetClosestPin(pos, m_removeRadius * (m_largeZoom * 2f));
		if (closestPin != null)
		{
			if (closestPin.m_ownerID != 0L)
			{
				closestPin.m_ownerID = 0L;
			}
			else
			{
				closestPin.m_checked = !closestPin.m_checked;
			}
		}
	}

	public void OnMapMiddleClick(UIInputHandler handler)
	{
		Vector3 position = ScreenToWorldPoint(Input.mousePosition);
		Chat.instance.SendPing(position);
	}

	public void OnMapRightClick(UIInputHandler handler)
	{
		ZLog.Log("Right click");
		Vector3 pos = ScreenToWorldPoint(Input.mousePosition);
		RemovePin(pos, m_removeRadius * (m_largeZoom * 2f));
		m_namePin = null;
	}

	public void OnPressedIcon0()
	{
		SelectIcon(PinType.Icon0);
	}

	public void OnPressedIcon1()
	{
		SelectIcon(PinType.Icon1);
	}

	public void OnPressedIcon2()
	{
		SelectIcon(PinType.Icon2);
	}

	public void OnPressedIcon3()
	{
		SelectIcon(PinType.Icon3);
	}

	public void OnPressedIcon4()
	{
		SelectIcon(PinType.Icon4);
	}

	public void OnPressedIconDeath()
	{
	}

	public void OnPressedIconBoss()
	{
	}

	public void OnAltPressedIcon0()
	{
		ToggleIconFilter(PinType.Icon0);
	}

	public void OnAltPressedIcon1()
	{
		ToggleIconFilter(PinType.Icon1);
	}

	public void OnAltPressedIcon2()
	{
		ToggleIconFilter(PinType.Icon2);
	}

	public void OnAltPressedIcon3()
	{
		ToggleIconFilter(PinType.Icon3);
	}

	public void OnAltPressedIcon4()
	{
		ToggleIconFilter(PinType.Icon4);
	}

	public void OnAltPressedIconDeath()
	{
		ToggleIconFilter(PinType.Death);
	}

	public void OnAltPressedIconBoss()
	{
		ToggleIconFilter(PinType.Boss);
	}

	public void OnTogglePublicPosition()
	{
		ZNet.instance.SetPublicReferencePosition(m_publicPosition.isOn);
	}

	public void OnToggleSharedMapData()
	{
		m_showSharedMapData = !m_showSharedMapData;
	}

	private void SelectIcon(PinType type)
	{
		m_selectedType = type;
		foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
		{
			selectedIcon.Value.enabled = selectedIcon.Key == type;
		}
	}

	private void ToggleIconFilter(PinType type)
	{
		m_visibleIconTypes[(int)type] = !m_visibleIconTypes[(int)type];
		foreach (KeyValuePair<PinType, Image> selectedIcon in m_selectedIcons)
		{
			selectedIcon.Value.transform.parent.GetComponent<Image>().color = (m_visibleIconTypes[(int)selectedIcon.Key] ? Color.white : Color.gray);
		}
	}

	private void ClearPins()
	{
		foreach (PinData pin in m_pins)
		{
			if (pin.m_uiElement != null)
			{
				UnityEngine.Object.Destroy(pin.m_uiElement);
			}
		}
		m_pins.Clear();
		m_deathPin = null;
	}

	private void UpdateBiome(Player player)
	{
		if (m_mode == MapMode.Large)
		{
			Vector3 vector = ScreenToWorldPoint(ZInput.IsMouseActive() ? Input.mousePosition : new Vector3(Screen.width / 2, Screen.height / 2));
			if (IsExplored(vector))
			{
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(vector);
				string text = Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
				m_biomeNameLarge.text = text;
			}
			else
			{
				m_biomeNameLarge.text = "";
			}
			return;
		}
		Heightmap.Biome currentBiome = player.GetCurrentBiome();
		if (currentBiome != m_biome)
		{
			m_biome = currentBiome;
			string text2 = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
			m_biomeNameSmall.text = text2;
			m_biomeNameLarge.text = text2;
			m_biomeNameSmall.GetComponent<Animator>().SetTrigger("pulse");
		}
	}

	public byte[] GetSharedMapData(byte[] oldMapData)
	{
		List<bool> list = null;
		if (oldMapData != null)
		{
			ZPackage zPackage = new ZPackage(oldMapData);
			int version = zPackage.ReadInt();
			list = ReadExploredArray(zPackage, version);
		}
		ZPackage zPackage2 = new ZPackage();
		zPackage2.Write(2);
		zPackage2.Write(m_explored.Length);
		for (int i = 0; i < m_explored.Length; i++)
		{
			bool flag = m_exploredOthers[i] || m_explored[i];
			if (list != null)
			{
				flag |= list[i];
			}
			zPackage2.Write(flag);
		}
		int num = 0;
		foreach (PinData pin in m_pins)
		{
			if (pin.m_save && pin.m_type != PinType.Death)
			{
				num++;
			}
		}
		long playerID = Player.m_localPlayer.GetPlayerID();
		zPackage2.Write(num);
		foreach (PinData pin2 in m_pins)
		{
			if (pin2.m_save && pin2.m_type != PinType.Death)
			{
				long data = ((pin2.m_ownerID != 0L) ? pin2.m_ownerID : playerID);
				zPackage2.Write(data);
				zPackage2.Write(pin2.m_name);
				zPackage2.Write(pin2.m_pos);
				zPackage2.Write((int)pin2.m_type);
				zPackage2.Write(pin2.m_checked);
			}
		}
		return zPackage2.GetArray();
	}

	private List<bool> ReadExploredArray(ZPackage pkg, int version)
	{
		int num = pkg.ReadInt();
		if (num != m_explored.Length)
		{
			ZLog.LogWarning("Map exploration array size missmatch:" + num + " VS " + m_explored.Length);
			return null;
		}
		List<bool> list = new List<bool>();
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				bool item = pkg.ReadBool();
				list.Add(item);
			}
		}
		return list;
	}

	public bool AddSharedMapData(byte[] dataArray)
	{
		ZPackage zPackage = new ZPackage(dataArray);
		int num = zPackage.ReadInt();
		List<bool> list = ReadExploredArray(zPackage, num);
		if (list == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < m_textureSize; i++)
		{
			for (int j = 0; j < m_textureSize; j++)
			{
				int num2 = i * m_textureSize + j;
				bool flag2 = list[num2];
				bool flag3 = m_exploredOthers[num2] || m_explored[num2];
				if (flag2 != flag3 && flag2 && ExploreOthers(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			m_fogTexture.Apply();
		}
		bool flag4 = false;
		if (num >= 2)
		{
			long playerID = Player.m_localPlayer.GetPlayerID();
			int num3 = zPackage.ReadInt();
			for (int k = 0; k < num3; k++)
			{
				long num4 = zPackage.ReadLong();
				string text = zPackage.ReadString();
				Vector3 pos = zPackage.ReadVector3();
				PinType type = (PinType)zPackage.ReadInt();
				bool isChecked = zPackage.ReadBool();
				if (num4 == playerID)
				{
					num4 = 0L;
				}
				if (!HavePinInRange(pos, 1f))
				{
					AddPin(pos, type, text, save: true, isChecked, num4);
					flag4 = true;
				}
			}
		}
		return flag || flag4;
	}
}
