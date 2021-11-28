using System;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
	private const float m_radius = 0.75f;

	public GameObject m_creaturePrefab;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	[Header("Spawn settings")]
	public float m_respawnTimeMinuts = 20f;

	public float m_triggerDistance = 60f;

	public float m_triggerNoise;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_requireSpawnArea;

	public bool m_spawnInPlayerBase;

	public bool m_setPatrolSpawnPoint;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			InvokeRepeating("UpdateSpawner", UnityEngine.Random.Range(3f, 5f), 5f);
		}
	}

	private void UpdateSpawner()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		ZDOID zDOID = m_nview.GetZDO().GetZDOID("spawn_id");
		if (m_respawnTimeMinuts <= 0f && !zDOID.IsNone())
		{
			return;
		}
		if (!zDOID.IsNone() && ZDOMan.instance.GetZDO(zDOID) != null)
		{
			m_nview.GetZDO().Set("alive_time", ZNet.instance.GetTime().Ticks);
			return;
		}
		if (m_respawnTimeMinuts > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong("alive_time", 0L));
			if ((time - dateTime).TotalMinutes < (double)m_respawnTimeMinuts)
			{
				return;
			}
		}
		if ((!m_spawnAtDay && EnvMan.instance.IsDay()) || (!m_spawnAtNight && EnvMan.instance.IsNight()))
		{
			return;
		}
		_ = m_requireSpawnArea;
		if (!m_spawnInPlayerBase && (bool)EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase))
		{
			return;
		}
		if (m_triggerNoise > 0f)
		{
			if (!Player.IsPlayerInRange(base.transform.position, m_triggerDistance, m_triggerNoise))
			{
				return;
			}
		}
		else if (!Player.IsPlayerInRange(base.transform.position, m_triggerDistance))
		{
			return;
		}
		Spawn();
	}

	private bool HasSpawned()
	{
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			return false;
		}
		return !m_nview.GetZDO().GetZDOID("spawn_id").IsNone();
	}

	private ZNetView Spawn()
	{
		Vector3 position = base.transform.position;
		if (ZoneSystem.instance.FindFloor(position, out var height))
		{
			position.y = height + 0.25f;
		}
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate(m_creaturePrefab, position, rotation);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		BaseAI component2 = gameObject.GetComponent<BaseAI>();
		if (component2 != null && m_setPatrolSpawnPoint)
		{
			component2.SetPatrolPoint();
		}
		if (m_maxLevel > 1)
		{
			Character component3 = gameObject.GetComponent<Character>();
			if ((bool)component3)
			{
				int i;
				for (i = m_minLevel; i < m_maxLevel; i++)
				{
					if (!(UnityEngine.Random.Range(0f, 100f) <= 10f))
					{
						break;
					}
				}
				if (i > 1)
				{
					component3.SetLevel(i);
				}
			}
		}
		component.GetZDO().SetPGWVersion(m_nview.GetZDO().GetPGWVersion());
		m_nview.GetZDO().Set("spawn_id", component.GetZDO().m_uid);
		m_nview.GetZDO().Set("alive_time", ZNet.instance.GetTime().Ticks);
		SpawnEffect(gameObject);
		return component;
	}

	private void SpawnEffect(GameObject spawnedObject)
	{
		Character component = spawnedObject.GetComponent<Character>();
		Vector3 basePos = (component ? component.GetCenterPoint() : (base.transform.position + Vector3.up * 0.75f));
		m_spawnEffects.Create(basePos, Quaternion.identity);
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}
}
