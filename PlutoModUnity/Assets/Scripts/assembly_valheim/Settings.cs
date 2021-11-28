using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
	[Serializable]
	public class KeySetting
	{
		public string m_keyName = "";

		public RectTransform m_keyTransform;
	}

	private static Settings m_instance;

	public GameObject m_settingsPanel;

	private List<Selectable> m_navigationObjects = new List<Selectable>();

	private bool m_navigationEnabled = true;

	[Header("Inout")]
	public Slider m_sensitivitySlider;

	public Slider m_gamepadSensitivitySlider;

	public Toggle m_invertMouse;

	public Toggle m_gamepadEnabled;

	public GameObject m_bindDialog;

	public List<KeySetting> m_keys = new List<KeySetting>();

	[Header("Misc")]
	public Toggle m_cameraShake;

	public Toggle m_shipCameraTilt;

	public Toggle m_quickPieceSelect;

	public Toggle m_showKeyHints;

	public Slider m_guiScaleSlider;

	public Text m_guiScaleText;

	public Text m_language;

	public Button m_resetTutorial;

	[Header("Audio")]
	public Slider m_volumeSlider;

	public Slider m_sfxVolumeSlider;

	public Slider m_musicVolumeSlider;

	public Toggle m_continousMusic;

	public AudioMixer m_masterMixer;

	[Header("Graphics")]
	public Toggle m_dofToggle;

	public Toggle m_vsyncToggle;

	public Toggle m_bloomToggle;

	public Toggle m_ssaoToggle;

	public Toggle m_sunshaftsToggle;

	public Toggle m_aaToggle;

	public Toggle m_caToggle;

	public Toggle m_motionblurToggle;

	public Toggle m_tesselationToggle;

	public Toggle m_softPartToggle;

	public Toggle m_fullscreenToggle;

	public Slider m_shadowQuality;

	public Text m_shadowQualityText;

	public Slider m_lod;

	public Text m_lodText;

	public Slider m_lights;

	public Text m_lightsText;

	public Slider m_vegetation;

	public Text m_vegetationText;

	public Slider m_pointLights;

	public Text m_pointLightsText;

	public Slider m_pointLightShadows;

	public Text m_pointLightShadowsText;

	public Text m_resButtonText;

	public GameObject m_resDialog;

	public GameObject m_resListElement;

	public RectTransform m_resListRoot;

	public Scrollbar m_resListScroll;

	public float m_resListSpace = 20f;

	public GameObject m_resSwitchDialog;

	public Text m_resSwitchCountdown;

	public int m_minResWidth = 1280;

	public int m_minResHeight = 720;

	private string m_languageKey = "";

	private bool m_oldFullscreen;

	private Resolution m_oldRes;

	private Resolution m_selectedRes;

	private List<GameObject> m_resObjects = new List<GameObject>();

	private List<Resolution> m_resolutions = new List<Resolution>();

	private float m_resListBaseSize;

	private int m_selectedResIndex;

	private bool m_modeApplied;

	private float m_resCountdownTimer = 1f;

	public static Settings instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_bindDialog.SetActive(value: false);
		m_resDialog.SetActive(value: false);
		m_resSwitchDialog.SetActive(value: false);
		m_resListBaseSize = m_resListRoot.rect.height;
		LoadSettings();
		SetupKeys();
		Selectable[] componentsInChildren = m_settingsPanel.GetComponentsInChildren<Selectable>();
		foreach (Selectable selectable in componentsInChildren)
		{
			if (selectable.enabled)
			{
				m_navigationObjects.Add(selectable);
			}
		}
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Update()
	{
		if (m_bindDialog.activeSelf)
		{
			UpdateBinding();
			return;
		}
		UpdateResSwitch(Time.deltaTime);
		AudioListener.volume = m_volumeSlider.value;
		MusicMan.m_masterMusicVolume = m_musicVolumeSlider.value;
		AudioMan.SetSFXVolume(m_sfxVolumeSlider.value);
		SetQualityText(m_shadowQualityText, GetQualityText((int)m_shadowQuality.value));
		SetQualityText(m_lodText, GetQualityText((int)m_lod.value));
		SetQualityText(m_lightsText, GetQualityText((int)m_lights.value));
		SetQualityText(m_vegetationText, GetQualityText((int)m_vegetation.value));
		int pointLightLimit = GetPointLightLimit((int)m_pointLights.value);
		int pointLightShadowLimit = GetPointLightShadowLimit((int)m_pointLightShadows.value);
		SetQualityText(m_pointLightsText, GetQualityText((int)m_pointLights.value) + " (" + ((pointLightLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightLimit.ToString()) + ")");
		SetQualityText(m_pointLightShadowsText, GetQualityText((int)m_pointLightShadows.value) + " (" + ((pointLightShadowLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightShadowLimit.ToString()) + ")");
		m_resButtonText.text = m_selectedRes.width + "x" + m_selectedRes.height + "  " + m_selectedRes.refreshRate + "hz";
		m_guiScaleText.text = m_guiScaleSlider.value + "%";
		GuiScaler.SetScale(m_guiScaleSlider.value / 100f);
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
		UpdateGamepad();
		if (!m_navigationEnabled && !m_resDialog.activeInHierarchy && !m_resSwitchDialog.activeInHierarchy)
		{
			ToggleNaviation(enabled: true);
		}
	}

	private void UpdateGamepad()
	{
		if (m_resDialog.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB"))
			{
				OnResCancel();
			}
			if (m_resObjects.Count > 1)
			{
				if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown") || Input.GetKeyDown(KeyCode.DownArrow))
				{
					if (m_selectedResIndex < m_resObjects.Count - 1)
					{
						m_selectedResIndex++;
					}
					updateResScroll();
				}
				else if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp") || Input.GetKeyDown(KeyCode.UpArrow))
				{
					if (m_selectedResIndex > 0)
					{
						m_selectedResIndex--;
					}
					updateResScroll();
				}
			}
		}
		if (m_resSwitchDialog.activeInHierarchy && (ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB")))
		{
			RevertMode();
			m_resSwitchDialog.SetActive(value: false);
			ToggleNaviation(enabled: true);
		}
		void updateResScroll()
		{
			Debug.Log("Res index " + m_selectedResIndex);
			if (m_selectedResIndex >= m_resObjects.Count)
			{
				m_selectedResIndex = m_resObjects.Count - 1;
			}
			m_resObjects[m_selectedResIndex].GetComponentInChildren<Button>().Select();
			m_resListScroll.value = 1f - (float)m_selectedResIndex / (float)(m_resObjects.Count - 1);
		}
	}

	private void SetQualityText(Text text, string str)
	{
		text.text = Localization.instance.Localize(str);
	}

	private string GetQualityText(int level)
	{
		switch (level)
		{
		default:
			return "[$settings_low]";
		case 1:
			return "[$settings_medium]";
		case 2:
			return "[$settings_high]";
		case 3:
			return "[$settings_veryhigh]";
		}
	}

	public void OnBack()
	{
		RevertMode();
		LoadSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		SaveSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetFloat("MasterVolume", m_volumeSlider.value);
		PlayerPrefs.SetFloat("MouseSensitivity", m_sensitivitySlider.value);
		PlayerPrefs.SetFloat("GamepadSensitivity", m_gamepadSensitivitySlider.value);
		PlayerPrefs.SetFloat("MusicVolume", m_musicVolumeSlider.value);
		PlayerPrefs.SetFloat("SfxVolume", m_sfxVolumeSlider.value);
		PlayerPrefs.SetInt("ContinousMusic", m_continousMusic.isOn ? 1 : 0);
		PlayerPrefs.SetInt("InvertMouse", m_invertMouse.isOn ? 1 : 0);
		PlayerPrefs.SetFloat("GuiScale", m_guiScaleSlider.value / 100f);
		PlayerPrefs.SetInt("CameraShake", m_cameraShake.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ShipCameraTilt", m_shipCameraTilt.isOn ? 1 : 0);
		PlayerPrefs.SetInt("QuickPieceSelect", m_quickPieceSelect.isOn ? 1 : 0);
		PlayerPrefs.SetInt("KeyHints", m_showKeyHints.isOn ? 1 : 0);
		PlayerPrefs.SetInt("DOF", m_dofToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("VSync", m_vsyncToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("Bloom", m_bloomToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SSAO", m_ssaoToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SunShafts", m_sunshaftsToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("AntiAliasing", m_aaToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ChromaticAberration", m_caToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("MotionBlur", m_motionblurToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("SoftPart", m_softPartToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("Tesselation", m_tesselationToggle.isOn ? 1 : 0);
		PlayerPrefs.SetInt("ShadowQuality", (int)m_shadowQuality.value);
		PlayerPrefs.SetInt("LodBias", (int)m_lod.value);
		PlayerPrefs.SetInt("Lights", (int)m_lights.value);
		PlayerPrefs.SetInt("ClutterQuality", (int)m_vegetation.value);
		PlayerPrefs.SetInt("PointLights", (int)m_pointLights.value);
		PlayerPrefs.SetInt("PointLightShadows", (int)m_pointLightShadows.value);
		ZInput.SetGamepadEnabled(m_gamepadEnabled.isOn);
		ZInput.instance.Save();
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if ((bool)MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		ApplyQualitySettings();
		ApplyMode();
		PlayerController.m_mouseSens = m_sensitivitySlider.value;
		PlayerController.m_gamepadSens = m_gamepadSensitivitySlider.value;
		PlayerController.m_invertMouse = m_invertMouse.isOn;
		Localization.instance.SetLanguage(m_languageKey);
		GuiScaler.LoadGuiScale();
		PlayerPrefs.Save();
	}

	public static void ApplyStartupSettings()
	{
		QualitySettings.vSyncCount = ((PlayerPrefs.GetInt("VSync", 0) == 1) ? 1 : 0);
		ApplyQualitySettings();
	}

	private static void ApplyQualitySettings()
	{
		QualitySettings.softParticles = PlayerPrefs.GetInt("SoftPart", 1) == 1;
		if (PlayerPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlayerPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlayerPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		LightLod.m_lightLimit = GetPointLightLimit(PlayerPrefs.GetInt("PointLights", 3));
		LightLod.m_shadowLimit = GetPointLightShadowLimit(PlayerPrefs.GetInt("PointLightShadows", 2));
		ApplyShadowQuality();
	}

	private static int GetPointLightLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 4;
		case 1:
			return 15;
		default:
			return 40;
		case 3:
			return -1;
		}
	}

	private static int GetPointLightShadowLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 0;
		case 1:
			return 1;
		default:
			return 3;
		case 3:
			return -1;
		}
	}

	private static void ApplyShadowQuality()
	{
		switch (PlayerPrefs.GetInt("ShadowQuality", 2))
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			break;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			break;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			break;
		}
	}

	private void LoadSettings()
	{
		ZInput.instance.Load();
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		MusicMan.m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		AudioMan.SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
		m_continousMusic.isOn = PlayerPrefs.GetInt("ContinousMusic", 1) == 1;
		PlayerController.m_mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_gamepadSens = PlayerPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertMouse = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
		float @float = PlayerPrefs.GetFloat("GuiScale", 1f);
		m_volumeSlider.value = AudioListener.volume;
		m_sensitivitySlider.value = PlayerController.m_mouseSens;
		m_gamepadSensitivitySlider.value = PlayerController.m_gamepadSens;
		m_sfxVolumeSlider.value = AudioMan.GetSFXVolume();
		m_musicVolumeSlider.value = MusicMan.m_masterMusicVolume;
		m_guiScaleSlider.value = @float * 100f;
		m_invertMouse.isOn = PlayerController.m_invertMouse;
		m_gamepadEnabled.isOn = ZInput.IsGamepadEnabled();
		m_languageKey = Localization.instance.GetSelectedLanguage();
		m_language.text = Localization.instance.Localize("$language_" + m_languageKey.ToLower());
		m_cameraShake.isOn = PlayerPrefs.GetInt("CameraShake", 1) == 1;
		m_shipCameraTilt.isOn = PlayerPrefs.GetInt("ShipCameraTilt", 1) == 1;
		m_quickPieceSelect.isOn = PlayerPrefs.GetInt("QuickPieceSelect", 0) == 1;
		m_showKeyHints.isOn = PlayerPrefs.GetInt("KeyHints", 1) == 1;
		m_dofToggle.isOn = PlayerPrefs.GetInt("DOF", 1) == 1;
		m_vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", 0) == 1;
		m_bloomToggle.isOn = PlayerPrefs.GetInt("Bloom", 1) == 1;
		m_ssaoToggle.isOn = PlayerPrefs.GetInt("SSAO", 1) == 1;
		m_sunshaftsToggle.isOn = PlayerPrefs.GetInt("SunShafts", 1) == 1;
		m_aaToggle.isOn = PlayerPrefs.GetInt("AntiAliasing", 1) == 1;
		m_caToggle.isOn = PlayerPrefs.GetInt("ChromaticAberration", 1) == 1;
		m_motionblurToggle.isOn = PlayerPrefs.GetInt("MotionBlur", 1) == 1;
		m_softPartToggle.isOn = PlayerPrefs.GetInt("SoftPart", 1) == 1;
		m_tesselationToggle.isOn = PlayerPrefs.GetInt("Tesselation", 1) == 1;
		m_shadowQuality.value = PlayerPrefs.GetInt("ShadowQuality", 2);
		m_lod.value = PlayerPrefs.GetInt("LodBias", 2);
		m_lights.value = PlayerPrefs.GetInt("Lights", 2);
		m_vegetation.value = PlayerPrefs.GetInt("ClutterQuality", 2);
		m_pointLights.value = PlayerPrefs.GetInt("PointLights", 3);
		m_pointLightShadows.value = PlayerPrefs.GetInt("PointLightShadows", 2);
		m_fullscreenToggle.isOn = Screen.fullScreen;
		m_oldFullscreen = m_fullscreenToggle.isOn;
		m_oldRes = Screen.currentResolution;
		m_oldRes.width = Screen.width;
		m_oldRes.height = Screen.height;
		m_selectedRes = m_oldRes;
		ZLog.Log("Current res " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + "     " + Screen.width + "x" + Screen.height);
	}

	private void SetupKeys()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<Button>().onClick.AddListener(OnKeySet);
		}
		UpdateBindings();
	}

	private void UpdateBindings()
	{
		foreach (KeySetting key in m_keys)
		{
			key.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = Localization.instance.GetBoundKeyString(key.m_keyName);
		}
	}

	private void OnKeySet()
	{
		foreach (KeySetting key in m_keys)
		{
			if (key.m_keyTransform.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject)
			{
				OpenBindDialog(key.m_keyName);
				return;
			}
		}
		ZLog.Log("NOT FOUND");
	}

	private void OpenBindDialog(string keyName)
	{
		ZLog.Log("BInding key " + keyName);
		ZInput.instance.StartBindKey(keyName);
		m_bindDialog.SetActive(value: true);
	}

	private void UpdateBinding()
	{
		if (m_bindDialog.activeSelf && ZInput.instance.EndBindKey())
		{
			m_bindDialog.SetActive(value: false);
			UpdateBindings();
		}
	}

	public void ResetBindings()
	{
		ZInput.instance.Reset();
		UpdateBindings();
	}

	public void OnLanguageLeft()
	{
		m_languageKey = Localization.instance.GetPrevLanguage(m_languageKey);
		m_language.text = Localization.instance.Localize("$language_" + m_languageKey.ToLower());
	}

	public void OnLanguageRight()
	{
		m_languageKey = Localization.instance.GetNextLanguage(m_languageKey);
		m_language.text = Localization.instance.Localize("$language_" + m_languageKey.ToLower());
	}

	public void OnShowResList()
	{
		m_resDialog.SetActive(value: true);
		ToggleNaviation(enabled: false);
		FillResList();
	}

	private void UpdateValidResolutions()
	{
		Resolution[] array = Screen.resolutions;
		if (array.Length == 0)
		{
			array = new Resolution[1] { m_oldRes };
		}
		m_resolutions.Clear();
		Resolution[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Resolution item = array2[i];
			if ((item.width >= m_minResWidth && item.height >= m_minResHeight) || item.width == m_oldRes.width || item.height == m_oldRes.height)
			{
				m_resolutions.Add(item);
			}
		}
		if (m_resolutions.Count == 0)
		{
			Resolution item2 = default(Resolution);
			item2.width = 1280;
			item2.height = 720;
			item2.refreshRate = 60;
			m_resolutions.Add(item2);
		}
	}

	private void FillResList()
	{
		foreach (GameObject resObject in m_resObjects)
		{
			UnityEngine.Object.Destroy(resObject);
		}
		m_resObjects.Clear();
		m_selectedResIndex = 0;
		UpdateValidResolutions();
		float num = 0f;
		foreach (Resolution res in m_resolutions)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_resListElement, m_resListRoot.transform);
			gameObject.SetActive(value: true);
			gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate
			{
				OnResClick(res);
			});
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, num * (0f - m_resListSpace));
			gameObject.GetComponentInChildren<Text>().text = res.width + "x" + res.height + "  " + res.refreshRate + "hz";
			m_resObjects.Add(gameObject);
			num += 1f;
		}
		float size = Mathf.Max(m_resListBaseSize, num * m_resListSpace);
		m_resListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		m_resListScroll.value = 1f;
	}

	private void ToggleNaviation(bool enabled)
	{
		m_navigationEnabled = enabled;
		foreach (Selectable navigationObject in m_navigationObjects)
		{
			navigationObject.enabled = enabled;
		}
	}

	public void OnResCancel()
	{
		m_resDialog.SetActive(value: false);
		ToggleNaviation(enabled: true);
	}

	private void OnResClick(Resolution res)
	{
		m_selectedRes = res;
		m_resDialog.SetActive(value: false);
		ToggleNaviation(enabled: true);
	}

	public void OnApplyMode()
	{
		ApplyMode();
		ShowResSwitchCountdown();
	}

	private void ApplyMode()
	{
		if (Screen.width != m_selectedRes.width || Screen.height != m_selectedRes.height || m_fullscreenToggle.isOn != Screen.fullScreen)
		{
			Screen.SetResolution(m_selectedRes.width, m_selectedRes.height, m_fullscreenToggle.isOn, m_selectedRes.refreshRate);
			m_modeApplied = true;
		}
	}

	private void RevertMode()
	{
		if (m_modeApplied)
		{
			m_modeApplied = false;
			m_selectedRes = m_oldRes;
			m_fullscreenToggle.isOn = m_oldFullscreen;
			Screen.SetResolution(m_oldRes.width, m_oldRes.height, m_oldFullscreen, m_oldRes.refreshRate);
		}
	}

	private void ShowResSwitchCountdown()
	{
		m_resSwitchDialog.SetActive(value: true);
		m_resCountdownTimer = 5f;
		m_resSwitchDialog.GetComponentInChildren<Button>().Select();
		ToggleNaviation(enabled: false);
	}

	public void OnResSwitchOK()
	{
		m_resSwitchDialog.SetActive(value: false);
		ToggleNaviation(enabled: true);
	}

	private void UpdateResSwitch(float dt)
	{
		if (m_resSwitchDialog.activeSelf)
		{
			m_resCountdownTimer -= dt;
			m_resSwitchCountdown.text = Mathf.CeilToInt(m_resCountdownTimer).ToString();
			if (m_resCountdownTimer <= 0f)
			{
				RevertMode();
				m_resSwitchDialog.SetActive(value: false);
				ToggleNaviation(enabled: true);
			}
		}
	}

	public void OnResetTutorial()
	{
		Player.ResetSeenTutorials();
	}
}
