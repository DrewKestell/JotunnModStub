using System;
using System.Collections;
using UnityEngine;

public class SpawnAbility : MonoBehaviour, IProjectile
{
	public enum TargetType
	{
		ClosestEnemy,
		RandomEnemy,
		Caster,
		Position
	}

	[Header("Spawn")]
	public GameObject[] m_spawnPrefab;

	public bool m_alertSpawnedCreature = true;

	public bool m_spawnAtTarget = true;

	public int m_minToSpawn = 1;

	public int m_maxToSpawn = 1;

	public int m_maxSpawned = 3;

	public float m_spawnRadius = 3f;

	public bool m_snapToTerrain = true;

	public float m_spawnGroundOffset;

	public float m_spawnDelay;

	public TargetType m_targetType;

	public float m_maxTargetRange = 40f;

	public EffectList m_spawnEffects = new EffectList();

	[Header("Projectile")]
	public float m_projectileVelocity = 10f;

	public float m_projectileAccuracy = 10f;

	private Character m_owner;

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
	{
		m_owner = owner;
		StartCoroutine("Spawn");
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private IEnumerator Spawn()
	{
		int toSpawn = UnityEngine.Random.Range(m_minToSpawn, m_maxToSpawn);
		int i = 0;
		while (i < toSpawn)
		{
			if (FindTarget(out var point))
			{
				Vector3 vector = (m_spawnAtTarget ? point : base.transform.position);
				Vector2 vector2 = UnityEngine.Random.insideUnitCircle * m_spawnRadius;
				Vector3 vector3 = vector + new Vector3(vector2.x, 0f, vector2.y);
				if (m_snapToTerrain)
				{
					float num = (vector3.y = ZoneSystem.instance.GetSolidHeight(vector3));
				}
				vector3.y += m_spawnGroundOffset;
				if (!(Mathf.Abs(vector3.y - vector.y) > 100f))
				{
					GameObject gameObject = m_spawnPrefab[UnityEngine.Random.Range(0, m_spawnPrefab.Length)];
					if (m_maxSpawned <= 0 || SpawnSystem.GetNrOfInstances(gameObject) < m_maxSpawned)
					{
						GameObject obj = UnityEngine.Object.Instantiate(gameObject, vector3, Quaternion.Euler(0f, UnityEngine.Random.value * (float)Math.PI * 2f, 0f));
						Projectile component = obj.GetComponent<Projectile>();
						if ((bool)component)
						{
							SetupProjectile(component, point);
						}
						BaseAI component2 = obj.GetComponent<BaseAI>();
						if (component2 != null && m_alertSpawnedCreature)
						{
							component2.Alert();
						}
						m_spawnEffects.Create(vector3, Quaternion.identity);
						if (m_spawnDelay > 0f)
						{
							yield return new WaitForSeconds(m_spawnDelay);
						}
					}
				}
			}
			int num2 = i + 1;
			i = num2;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SetupProjectile(Projectile projectile, Vector3 targetPoint)
	{
		Vector3 normalized = (targetPoint - projectile.transform.position).normalized;
		Vector3 axis = Vector3.Cross(normalized, Vector3.up);
		Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - m_projectileAccuracy, m_projectileAccuracy), Vector3.up);
		normalized = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - m_projectileAccuracy, m_projectileAccuracy), axis) * normalized;
		normalized = quaternion * normalized;
		projectile.Setup(m_owner, normalized * m_projectileVelocity, -1f, null, null);
	}

	private bool FindTarget(out Vector3 point)
	{
		point = Vector3.zero;
		switch (m_targetType)
		{
		case TargetType.ClosestEnemy:
		{
			if (m_owner == null)
			{
				return false;
			}
			Character character2 = BaseAI.FindClosestEnemy(m_owner, base.transform.position, m_maxTargetRange);
			if (character2 != null)
			{
				point = character2.transform.position;
				return true;
			}
			return false;
		}
		case TargetType.RandomEnemy:
		{
			if (m_owner == null)
			{
				return false;
			}
			Character character = BaseAI.FindRandomEnemy(m_owner, base.transform.position, m_maxTargetRange);
			if (character != null)
			{
				point = character.transform.position;
				return true;
			}
			return false;
		}
		case TargetType.Position:
			point = base.transform.position;
			return true;
		case TargetType.Caster:
			if (m_owner == null)
			{
				return false;
			}
			point = m_owner.transform.position;
			return true;
		default:
			return false;
		}
	}
}
