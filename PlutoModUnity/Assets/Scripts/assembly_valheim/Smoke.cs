using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour
{
	public Vector3 m_vel = Vector3.up;

	public float m_randomVel = 0.1f;

	public float m_force = 0.1f;

	public float m_ttl = 10f;

	public float m_fadetime = 3f;

	private Rigidbody m_body;

	private float m_time;

	private float m_fadeTimer = -1f;

	private bool m_added;

	private MeshRenderer m_mr;

	private static List<Smoke> m_smoke = new List<Smoke>();

	private void Awake()
	{
		m_body = GetComponent<Rigidbody>();
		m_smoke.Add(this);
		m_added = true;
		m_mr = GetComponent<MeshRenderer>();
		m_body.maxDepenetrationVelocity = 1f;
		m_vel += Quaternion.Euler(0f, Random.Range(0, 360), 0f) * Vector3.forward * m_randomVel;
	}

	private void OnDestroy()
	{
		if (m_added)
		{
			m_smoke.Remove(this);
			m_added = false;
		}
	}

	public void StartFadeOut()
	{
		if (!(m_fadeTimer >= 0f))
		{
			if (m_added)
			{
				m_smoke.Remove(this);
				m_added = false;
			}
			m_fadeTimer = 0f;
		}
	}

	public static int GetTotalSmoke()
	{
		return m_smoke.Count;
	}

	public static void FadeOldest()
	{
		if (m_smoke.Count != 0)
		{
			m_smoke[0].StartFadeOut();
		}
	}

	public static void FadeMostDistant()
	{
		if (m_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < m_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(m_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			m_smoke[num].StartFadeOut();
		}
	}

	private void Update()
	{
		m_time += Time.deltaTime;
		if (m_time > m_ttl && m_fadeTimer < 0f)
		{
			StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(m_time / m_ttl);
		m_body.mass = num * num;
		Vector3 velocity = m_body.velocity;
		Vector3 vel = m_vel;
		vel.y *= num;
		Vector3 vector = vel - velocity;
		m_body.AddForce(vector * m_force * Time.deltaTime, ForceMode.VelocityChange);
		if (m_fadeTimer >= 0f)
		{
			m_fadeTimer += Time.deltaTime;
			float a = 1f - Mathf.Clamp01(m_fadeTimer / m_fadetime);
			Color color = m_mr.material.color;
			color.a = a;
			m_mr.material.color = color;
			if (m_fadeTimer >= m_fadetime)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
