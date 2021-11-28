using UnityEngine;

public class SmokeSpawner : MonoBehaviour
{
	private const float m_minPlayerDistance = 64f;

	private const int m_maxGlobalSmoke = 100;

	private const float m_blockedMinTime = 4f;

	public GameObject m_smokePrefab;

	public float m_interval = 0.5f;

	public LayerMask m_testMask;

	public float m_testRadius = 0.5f;

	private float m_lastSpawnTime;

	private float m_time;

	private void Start()
	{
		m_time = Random.Range(0f, m_interval);
	}

	private void Update()
	{
		m_time += Time.deltaTime;
		if (m_time > m_interval)
		{
			m_time = 0f;
			Spawn();
		}
	}

	private void Spawn()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || Vector3.Distance(localPlayer.transform.position, base.transform.position) > 64f)
		{
			m_lastSpawnTime = Time.time;
		}
		else if (!TestBlocked())
		{
			if (Smoke.GetTotalSmoke() > 100)
			{
				Smoke.FadeOldest();
			}
			Object.Instantiate(m_smokePrefab, base.transform.position, Random.rotation);
			m_lastSpawnTime = Time.time;
		}
	}

	private bool TestBlocked()
	{
		if (Physics.CheckSphere(base.transform.position, m_testRadius, m_testMask.value))
		{
			return true;
		}
		return false;
	}

	public bool IsBlocked()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return TestBlocked();
		}
		return Time.time - m_lastSpawnTime > 4f;
	}
}
