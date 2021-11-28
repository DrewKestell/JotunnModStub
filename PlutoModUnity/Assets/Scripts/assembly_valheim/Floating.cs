using UnityEngine;

public class Floating : MonoBehaviour, IWaterInteractable
{
	private static int m_waterVolumeMask = 0;

	private static Collider[] tempColliderArray = new Collider[256];

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_balanceForceFraction = 0.02f;

	public float m_damping = 0.05f;

	private static float m_minImpactEffectVelocity = 0.5f;

	public EffectList m_impactEffects = new EffectList();

	public GameObject m_surfaceEffects;

	private float m_waterLevel = -10000f;

	private float m_tarLevel = -10000f;

	private bool m_beenFloating;

	private bool m_wasInWater = true;

	private Rigidbody m_body;

	private Collider m_collider;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_collider = GetComponentInChildren<Collider>();
		SetSurfaceEffect(enabled: false);
		InvokeRepeating("TerrainCheck", Random.Range(10f, 30f), 30f);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public bool IsOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	private void TerrainCheck()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.velocity = Vector3.zero;
			}
			ZLog.Log("Moved up item " + base.gameObject.name);
		}
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (!HaveLiquidLevel())
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		UpdateImpactEffect();
		float floatDepth = GetFloatDepth();
		if (floatDepth > 0f)
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		SetSurfaceEffect(enabled: true);
		Vector3 position = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float num = Mathf.Clamp01(Mathf.Abs(floatDepth) / m_forceDistance);
		Vector3 vector = Vector3.up * m_force * num * (Time.fixedDeltaTime * 50f);
		m_body.WakeUp();
		m_body.AddForceAtPosition(vector * m_balanceForceFraction, position, ForceMode.VelocityChange);
		m_body.AddForceAtPosition(vector, worldCenterOfMass, ForceMode.VelocityChange);
		m_body.velocity -= m_body.velocity * m_damping * num;
		m_body.angularVelocity -= m_body.angularVelocity * m_damping * num;
	}

	public bool HaveLiquidLevel()
	{
		if (!(m_waterLevel > -10000f))
		{
			return m_tarLevel > -10000f;
		}
		return true;
	}

	private void SetSurfaceEffect(bool enabled)
	{
		if (m_surfaceEffects != null)
		{
			m_surfaceEffects.SetActive(enabled);
		}
	}

	private void UpdateImpactEffect()
	{
		if (m_body.IsSleeping() || !m_impactEffects.HasEffects())
		{
			return;
		}
		Vector3 vector = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		float num = Mathf.Max(m_waterLevel, m_tarLevel);
		if (vector.y < num)
		{
			if (!m_wasInWater)
			{
				m_wasInWater = true;
				Vector3 basePos = vector;
				basePos.y = num;
				if (m_body.GetPointVelocity(vector).magnitude > m_minImpactEffectVelocity)
				{
					m_impactEffects.Create(basePos, Quaternion.identity);
				}
			}
		}
		else
		{
			m_wasInWater = false;
		}
	}

	private float GetFloatDepth()
	{
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float num = Mathf.Max(m_waterLevel, m_tarLevel);
		return worldCenterOfMass.y - num - m_waterLevelOffset;
	}

	public bool IsInTar()
	{
		if (m_tarLevel <= -10000f)
		{
			return false;
		}
		return m_body.worldCenterOfMass.y - m_tarLevel - m_waterLevelOffset < -0.2f;
	}

	public void SetLiquidLevel(float level, LiquidType type)
	{
		if (type == LiquidType.Water || type == LiquidType.Tar)
		{
			switch (type)
			{
			case LiquidType.Water:
				m_waterLevel = level;
				break;
			case LiquidType.Tar:
				m_tarLevel = level;
				break;
			}
			if (!m_beenFloating && level > -10000f && GetFloatDepth() < 0f)
			{
				m_beenFloating = true;
			}
		}
	}

	public bool BeenFloating()
	{
		return m_beenFloating;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}

	public static float GetLiquidLevel(Vector3 p, float waveFactor = 1f, LiquidType type = LiquidType.All)
	{
		if (m_waterVolumeMask == 0)
		{
			m_waterVolumeMask = LayerMask.GetMask("WaterVolume");
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, tempColliderArray, m_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = tempColliderArray[i];
			WaterVolume component = collider.GetComponent<WaterVolume>();
			if ((bool)component)
			{
				if (type == LiquidType.All || component.GetLiquidType() == type)
				{
					num = Mathf.Max(num, component.GetWaterSurface(p, waveFactor));
				}
				continue;
			}
			LiquidSurface component2 = collider.GetComponent<LiquidSurface>();
			if ((bool)component2 && (type == LiquidType.All || component2.GetLiquidType() == type))
			{
				num = Mathf.Max(num, component2.GetSurface(p));
			}
		}
		return num;
	}
}
