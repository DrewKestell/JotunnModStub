using UnityEngine;

public class ZSFX : MonoBehaviour
{
	public bool m_playOnAwake = true;

	[Header("Clips")]
	public AudioClip[] m_audioClips = new AudioClip[0];

	[Header("Random")]
	public float m_maxPitch = 1f;

	public float m_minPitch = 1f;

	public float m_maxVol = 1f;

	public float m_minVol = 1f;

	[Header("Fade")]
	public float m_fadeInDuration;

	public float m_fadeOutDuration;

	public float m_fadeOutDelay;

	public bool m_fadeOutOnAwake;

	[Header("Pan")]
	public bool m_randomPan;

	public float m_minPan = -1f;

	public float m_maxPan = 1f;

	[Header("Delay")]
	public float m_maxDelay;

	public float m_minDelay;

	[Header("Reverb")]
	public bool m_distanceReverb = true;

	public bool m_useCustomReverbDistance;

	public float m_customReverbDistance = 10f;

	private const float m_globalReverbDistance = 64f;

	private const float m_minReverbSpread = 45f;

	private const float m_maxReverbSpread = 120f;

	private float m_delay;

	private float m_time;

	private float m_fadeOutTimer = -1f;

	private float m_fadeInTimer = -1f;

	private float m_vol = 1f;

	private float m_baseSpread;

	private float m_updateReverbTimer;

	private AudioSource m_audioSource;

	public void Awake()
	{
		m_delay = Random.Range(m_minDelay, m_maxDelay);
		m_audioSource = GetComponent<AudioSource>();
		m_baseSpread = m_audioSource.spread;
	}

	private void OnDisable()
	{
		if (m_playOnAwake && m_audioSource.loop)
		{
			m_time = 0f;
			m_delay = Random.Range(m_minDelay, m_maxDelay);
			m_audioSource.Stop();
		}
	}

	public void Update()
	{
		if (m_audioSource == null)
		{
			return;
		}
		m_time += Time.deltaTime;
		if (m_delay >= 0f && m_time >= m_delay)
		{
			m_delay = -1f;
			if (m_playOnAwake)
			{
				Play();
			}
		}
		if (!m_audioSource.isPlaying)
		{
			return;
		}
		if (m_distanceReverb && m_audioSource.loop)
		{
			m_updateReverbTimer += Time.deltaTime;
			if (m_updateReverbTimer > 1f)
			{
				m_updateReverbTimer = 0f;
				UpdateReverb();
			}
		}
		if (m_fadeOutOnAwake && m_time > m_fadeOutDelay)
		{
			m_fadeOutOnAwake = false;
			FadeOut();
		}
		if (m_fadeOutTimer >= 0f)
		{
			m_fadeOutTimer += Time.deltaTime;
			if (m_fadeOutTimer >= m_fadeOutDuration)
			{
				m_audioSource.volume = 0f;
				Stop();
			}
			else
			{
				float num = Mathf.Clamp01(m_fadeOutTimer / m_fadeOutDuration);
				m_audioSource.volume = (1f - num) * m_vol;
			}
		}
		else if (m_fadeInTimer >= 0f)
		{
			m_fadeInTimer += Time.deltaTime;
			float num2 = Mathf.Clamp01(m_fadeInTimer / m_fadeInDuration);
			m_audioSource.volume = num2 * m_vol;
			if (m_fadeInTimer > m_fadeInDuration)
			{
				m_fadeInTimer = -1f;
			}
		}
	}

	public void FadeOut()
	{
		if (m_fadeOutTimer < 0f)
		{
			m_fadeOutTimer = 0f;
		}
	}

	public void Stop()
	{
		if (m_audioSource != null)
		{
			m_audioSource.Stop();
		}
	}

	public bool IsPlaying()
	{
		if (m_audioSource == null)
		{
			return false;
		}
		return m_audioSource.isPlaying;
	}

	private void UpdateReverb()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (m_distanceReverb && m_audioSource.spatialBlend != 0f && mainCamera != null)
		{
			float num = Vector3.Distance(mainCamera.transform.position, base.transform.position);
			float num2 = (m_useCustomReverbDistance ? m_customReverbDistance : 64f);
			float a = Mathf.Clamp01(num / num2);
			float b = Mathf.Clamp01(m_audioSource.maxDistance / num2) * Mathf.Clamp01(num / m_audioSource.maxDistance);
			float num3 = Mathf.Max(a, b);
			m_audioSource.bypassReverbZones = false;
			m_audioSource.reverbZoneMix = num3;
			if (m_baseSpread < 120f)
			{
				float a2 = Mathf.Max(m_baseSpread, 45f);
				m_audioSource.spread = Mathf.Lerp(a2, 120f, num3);
			}
		}
		else
		{
			m_audioSource.bypassReverbZones = true;
		}
	}

	public void Play()
	{
		if (!(m_audioSource == null) && m_audioClips.Length != 0 && m_audioSource.gameObject.activeInHierarchy)
		{
			int num = Random.Range(0, m_audioClips.Length);
			m_audioSource.clip = m_audioClips[num];
			m_audioSource.pitch = Random.Range(m_minPitch, m_maxPitch);
			if (m_randomPan)
			{
				m_audioSource.panStereo = Random.Range(m_minPan, m_maxPan);
			}
			m_vol = Random.Range(m_minVol, m_maxVol);
			if (m_fadeInDuration > 0f)
			{
				m_audioSource.volume = 0f;
				m_fadeInTimer = 0f;
			}
			else
			{
				m_audioSource.volume = m_vol;
			}
			UpdateReverb();
			m_audioSource.Play();
		}
	}
}
