using UnityEngine;

public class LightFlicker : MonoBehaviour
{
	public float m_flickerIntensity = 0.1f;

	public float m_flickerSpeed = 10f;

	public float m_movement = 0.1f;

	public float m_ttl;

	public float m_fadeDuration = 0.2f;

	public float m_fadeInDuration;

	private Light m_light;

	private float m_baseIntensity = 1f;

	private Vector3 m_basePosition = Vector3.zero;

	private float m_time;

	private float m_flickerOffset;

	private void Awake()
	{
		m_light = GetComponent<Light>();
		m_baseIntensity = m_light.intensity;
		m_basePosition = base.transform.localPosition;
		m_flickerOffset = Random.Range(0f, 10f);
	}

	private void OnEnable()
	{
		m_time = 0f;
		if ((bool)m_light)
		{
			m_light.intensity = 0f;
		}
	}

	private void Update()
	{
		if (!m_light)
		{
			return;
		}
		m_time += Time.deltaTime;
		float num = m_flickerOffset + Time.time * m_flickerSpeed;
		float num2 = 1f + Mathf.Sin(num) * Mathf.Sin(num * 0.56436f) * Mathf.Cos(num * 0.758348f) * m_flickerIntensity;
		if (m_fadeInDuration > 0f)
		{
			num2 *= Utils.LerpStep(0f, m_fadeInDuration, m_time);
		}
		if (m_ttl > 0f)
		{
			if (m_time > m_ttl)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			float l = m_ttl - m_fadeDuration;
			num2 *= 1f - Utils.LerpStep(l, m_ttl, m_time);
		}
		m_light.intensity = m_baseIntensity * num2;
		Vector3 vector = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * m_movement;
		base.transform.localPosition = m_basePosition + vector;
	}
}
