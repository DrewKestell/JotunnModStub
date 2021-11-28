using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class AudioMan : MonoBehaviour
{
	[Serializable]
	public class BiomeAmbients
	{
		public string m_name = "";

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		public List<AudioClip> m_randomAmbientClips = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsDay = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsNight = new List<AudioClip>();
	}

	private enum Snapshot
	{
		Default,
		Menu,
		Indoor
	}

	private static AudioMan m_instance;

	[Header("Mixers")]
	public AudioMixerGroup m_ambientMixer;

	public AudioMixer m_masterMixer;

	public float m_snapshotTransitionTime = 2f;

	[Header("Wind")]
	public AudioClip m_windAudio;

	public float m_windMinVol;

	public float m_windMaxVol = 1f;

	public float m_windMinPitch = 0.5f;

	public float m_windMaxPitch = 1.5f;

	public float m_windVariation = 0.2f;

	public float m_windIntensityPower = 1.5f;

	[Header("Ocean")]
	public AudioClip m_oceanAudio;

	public float m_oceanVolumeMax = 1f;

	public float m_oceanVolumeMin = 1f;

	public float m_oceanFadeSpeed = 0.1f;

	public float m_oceanMoveSpeed = 0.1f;

	public float m_oceanDepthTreshold = 10f;

	[Header("Random ambients")]
	public float m_ambientFadeTime = 2f;

	public float m_randomAmbientInterval = 5f;

	public float m_randomAmbientChance = 0.5f;

	public float m_randomMinPitch = 0.9f;

	public float m_randomMaxPitch = 1.1f;

	public float m_randomMinVol = 0.2f;

	public float m_randomMaxVol = 0.4f;

	public float m_randomPan = 0.2f;

	public float m_randomFadeIn = 0.2f;

	public float m_randomFadeOut = 2f;

	public float m_randomMinDistance = 5f;

	public float m_randomMaxDistance = 20f;

	public List<BiomeAmbients> m_randomAmbients = new List<BiomeAmbients>();

	public GameObject m_randomAmbientPrefab;

	private AudioSource m_oceanAmbientSource;

	private AudioSource m_ambientLoopSource;

	private AudioSource m_windLoopSource;

	private AudioClip m_queuedAmbientLoop;

	private float m_queuedAmbientVol;

	private float m_ambientVol;

	private float m_randomAmbientTimer;

	private bool m_stopAmbientLoop;

	private bool m_indoor;

	private float m_oceanUpdateTimer;

	private bool m_haveOcean;

	private Vector3 m_avgOceanPoint = Vector3.zero;

	private Snapshot m_currentSnapshot;

	public static AudioMan instance => m_instance;

	private void Awake()
	{
		if (m_instance != null)
		{
			ZLog.Log("Audioman already exist, destroying self");
			UnityEngine.Object.DestroyImmediate(base.gameObject);
			return;
		}
		m_instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		GameObject gameObject = new GameObject("ocean_ambient_loop");
		gameObject.transform.SetParent(base.transform);
		m_oceanAmbientSource = gameObject.AddComponent<AudioSource>();
		m_oceanAmbientSource.loop = true;
		m_oceanAmbientSource.spatialBlend = 0.75f;
		m_oceanAmbientSource.outputAudioMixerGroup = m_ambientMixer;
		m_oceanAmbientSource.maxDistance = 128f;
		m_oceanAmbientSource.minDistance = 40f;
		m_oceanAmbientSource.spread = 90f;
		m_oceanAmbientSource.rolloffMode = AudioRolloffMode.Linear;
		m_oceanAmbientSource.clip = m_oceanAudio;
		m_oceanAmbientSource.bypassReverbZones = true;
		m_oceanAmbientSource.dopplerLevel = 0f;
		m_oceanAmbientSource.volume = 0f;
		m_oceanAmbientSource.Play();
		GameObject gameObject2 = new GameObject("ambient_loop");
		gameObject2.transform.SetParent(base.transform);
		m_ambientLoopSource = gameObject2.AddComponent<AudioSource>();
		m_ambientLoopSource.loop = true;
		m_ambientLoopSource.spatialBlend = 0f;
		m_ambientLoopSource.outputAudioMixerGroup = m_ambientMixer;
		m_ambientLoopSource.bypassReverbZones = true;
		m_ambientLoopSource.volume = 0f;
		GameObject gameObject3 = new GameObject("wind_loop");
		gameObject3.transform.SetParent(base.transform);
		m_windLoopSource = gameObject3.AddComponent<AudioSource>();
		m_windLoopSource.loop = true;
		m_windLoopSource.spatialBlend = 0f;
		m_windLoopSource.outputAudioMixerGroup = m_ambientMixer;
		m_windLoopSource.bypassReverbZones = true;
		m_windLoopSource.clip = m_windAudio;
		m_windLoopSource.volume = 0f;
		m_windLoopSource.Play();
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			AudioListener.volume = 0f;
			return;
		}
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", GetSFXVolume()));
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		UpdateAmbientLoop(deltaTime);
		UpdateRandomAmbient(deltaTime);
		UpdateSnapshots(deltaTime);
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		UpdateOceanAmbiance(fixedDeltaTime);
		UpdateWindAmbience(fixedDeltaTime);
	}

	public static float GetSFXVolume()
	{
		if (m_instance == null)
		{
			return 1f;
		}
		m_instance.m_masterMixer.GetFloat("SfxVol", out var value);
		return Mathf.Pow(10f, value / 10f);
	}

	public static void SetSFXVolume(float vol)
	{
		if (!(m_instance == null))
		{
			float value = Mathf.Log10(Mathf.Clamp(vol, 0.001f, 1f)) * 10f;
			m_instance.m_masterMixer.SetFloat("SfxVol", value);
		}
	}

	private void UpdateRandomAmbient(float dt)
	{
		if (InMenu())
		{
			return;
		}
		m_randomAmbientTimer += dt;
		if (!(m_randomAmbientTimer > m_randomAmbientInterval))
		{
			return;
		}
		m_randomAmbientTimer = 0f;
		if (UnityEngine.Random.value <= m_randomAmbientChance)
		{
			AudioClip audioClip = SelectRandomAmbientClip();
			if ((bool)audioClip)
			{
				Vector3 randomAmbiencePoint = GetRandomAmbiencePoint();
				GameObject obj = UnityEngine.Object.Instantiate(m_randomAmbientPrefab, randomAmbiencePoint, Quaternion.identity, base.transform);
				obj.GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(m_randomMinPitch, m_randomMaxPitch);
				ZSFX component = obj.GetComponent<ZSFX>();
				component.m_audioClips = new AudioClip[1] { audioClip };
				component.Play();
				component.FadeOut();
			}
		}
	}

	private Vector3 GetRandomAmbiencePoint()
	{
		Vector3 vector = Vector3.zero;
		Camera mainCamera = Utils.GetMainCamera();
		if ((bool)Player.m_localPlayer)
		{
			vector = Player.m_localPlayer.transform.position;
		}
		else if ((bool)mainCamera)
		{
			vector = mainCamera.transform.position;
		}
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		float num = UnityEngine.Random.Range(m_randomMinDistance, m_randomMaxDistance);
		return vector + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private AudioClip SelectRandomAmbientClip()
	{
		if (EnvMan.instance == null)
		{
			return null;
		}
		EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
		BiomeAmbients biomeAmbients = null;
		biomeAmbients = ((currentEnvironment == null || string.IsNullOrEmpty(currentEnvironment.m_ambientList)) ? GetBiomeAmbients(EnvMan.instance.GetCurrentBiome()) : GetAmbients(currentEnvironment.m_ambientList));
		if (biomeAmbients == null)
		{
			return null;
		}
		List<AudioClip> list = new List<AudioClip>(biomeAmbients.m_randomAmbientClips);
		List<AudioClip> collection = (EnvMan.instance.IsDaylight() ? biomeAmbients.m_randomAmbientClipsDay : biomeAmbients.m_randomAmbientClipsNight);
		list.AddRange(collection);
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void UpdateAmbientLoop(float dt)
	{
		if (EnvMan.instance == null)
		{
			m_ambientLoopSource.Stop();
		}
		else if ((bool)m_queuedAmbientLoop || m_stopAmbientLoop)
		{
			if (!m_ambientLoopSource.isPlaying || m_ambientLoopSource.volume <= 0f)
			{
				m_ambientLoopSource.Stop();
				m_stopAmbientLoop = false;
				if ((bool)m_queuedAmbientLoop)
				{
					m_ambientLoopSource.clip = m_queuedAmbientLoop;
					m_ambientLoopSource.volume = 0f;
					m_ambientLoopSource.Play();
					m_ambientVol = m_queuedAmbientVol;
					m_queuedAmbientLoop = null;
				}
			}
			else
			{
				m_ambientLoopSource.volume = Mathf.MoveTowards(m_ambientLoopSource.volume, 0f, dt / m_ambientFadeTime);
			}
		}
		else if (m_ambientLoopSource.isPlaying)
		{
			m_ambientLoopSource.volume = Mathf.MoveTowards(m_ambientLoopSource.volume, m_ambientVol, dt / m_ambientFadeTime);
		}
	}

	public void SetIndoor(bool indoor)
	{
		m_indoor = indoor;
	}

	private bool InMenu()
	{
		if (!(FejdStartup.instance != null) && !Menu.IsVisible() && (!Game.instance || !Game.instance.WaitingForRespawn()))
		{
			return TextViewer.IsShowingIntro();
		}
		return true;
	}

	private void UpdateSnapshots(float dt)
	{
		if (InMenu())
		{
			SetSnapshot(Snapshot.Menu);
		}
		else if (m_indoor)
		{
			SetSnapshot(Snapshot.Indoor);
		}
		else
		{
			SetSnapshot(Snapshot.Default);
		}
	}

	private void SetSnapshot(Snapshot snapshot)
	{
		if (m_currentSnapshot != snapshot)
		{
			m_currentSnapshot = snapshot;
			switch (snapshot)
			{
			case Snapshot.Default:
				m_masterMixer.FindSnapshot("Default").TransitionTo(m_snapshotTransitionTime);
				break;
			case Snapshot.Indoor:
				m_masterMixer.FindSnapshot("Indoor").TransitionTo(m_snapshotTransitionTime);
				break;
			case Snapshot.Menu:
				m_masterMixer.FindSnapshot("Menu").TransitionTo(m_snapshotTransitionTime);
				break;
			}
		}
	}

	public void StopAmbientLoop()
	{
		m_queuedAmbientLoop = null;
		m_stopAmbientLoop = true;
	}

	public void QueueAmbientLoop(AudioClip clip, float vol)
	{
		if ((!(m_queuedAmbientLoop == clip) || m_queuedAmbientVol != vol) && (!(m_queuedAmbientLoop == null) || !(m_ambientLoopSource.clip == clip) || m_ambientVol != vol))
		{
			m_queuedAmbientLoop = clip;
			m_queuedAmbientVol = vol;
			m_stopAmbientLoop = false;
		}
	}

	private void UpdateWindAmbience(float dt)
	{
		if (ZoneSystem.instance == null)
		{
			m_windLoopSource.volume = 0f;
			return;
		}
		float windIntensity = EnvMan.instance.GetWindIntensity();
		windIntensity = Mathf.Pow(windIntensity, m_windIntensityPower);
		windIntensity += windIntensity * Mathf.Sin(Time.time) * Mathf.Sin(Time.time * 1.54323f) * Mathf.Sin(Time.time * 2.31237f) * m_windVariation;
		m_windLoopSource.volume = Mathf.Lerp(m_windMinVol, m_windMaxVol, windIntensity);
		m_windLoopSource.pitch = Mathf.Lerp(m_windMinPitch, m_windMaxPitch, windIntensity);
	}

	private void UpdateOceanAmbiance(float dt)
	{
		if (ZoneSystem.instance == null)
		{
			m_oceanAmbientSource.volume = 0f;
			return;
		}
		m_oceanUpdateTimer += dt;
		if (m_oceanUpdateTimer > 2f)
		{
			m_oceanUpdateTimer = 0f;
			m_haveOcean = FindAverageOceanPoint(out m_avgOceanPoint);
		}
		if (m_haveOcean)
		{
			float windIntensity = EnvMan.instance.GetWindIntensity();
			float target = Mathf.Lerp(m_oceanVolumeMin, m_oceanVolumeMax, windIntensity);
			m_oceanAmbientSource.volume = Mathf.MoveTowards(m_oceanAmbientSource.volume, target, m_oceanFadeSpeed * dt);
			m_oceanAmbientSource.transform.position = Vector3.Lerp(m_oceanAmbientSource.transform.position, m_avgOceanPoint, m_oceanMoveSpeed);
		}
		else
		{
			m_oceanAmbientSource.volume = Mathf.MoveTowards(m_oceanAmbientSource.volume, 0f, m_oceanFadeSpeed * dt);
		}
	}

	private bool FindAverageOceanPoint(out Vector3 point)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			point = Vector3.zero;
			return false;
		}
		Vector3 zero = Vector3.zero;
		int num = 0;
		Vector3 position = mainCamera.transform.position;
		Vector2i zone = ZoneSystem.instance.GetZone(position);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Vector2i id = zone;
				id.x += j;
				id.y += i;
				Vector3 zonePos = ZoneSystem.instance.GetZonePos(id);
				if (IsOceanZone(zonePos))
				{
					num++;
					zero += zonePos;
				}
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
			point = zero;
			point.y = ZoneSystem.instance.m_waterLevel;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	private bool IsOceanZone(Vector3 centerPos)
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(centerPos);
		if (ZoneSystem.instance.m_waterLevel - groundHeight > m_oceanDepthTreshold)
		{
			return true;
		}
		return false;
	}

	private BiomeAmbients GetAmbients(string name)
	{
		foreach (BiomeAmbients randomAmbient in m_randomAmbients)
		{
			if (randomAmbient.m_name == name)
			{
				return randomAmbient;
			}
		}
		return null;
	}

	private BiomeAmbients GetBiomeAmbients(Heightmap.Biome biome)
	{
		foreach (BiomeAmbients randomAmbient in m_randomAmbients)
		{
			if ((randomAmbient.m_biome & biome) != 0)
			{
				return randomAmbient;
			}
		}
		return null;
	}
}
