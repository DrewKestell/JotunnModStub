using System.Collections.Generic;
using UnityEngine;

public class WaterVolume : MonoBehaviour
{
	private float[] m_normalizedDepth = new float[4];

	private Collider m_collider;

	public MeshRenderer m_waterSurface;

	public Heightmap m_heightmap;

	public float m_forceDepth = -1f;

	public float m_surfaceOffset;

	public bool m_menuWater;

	public bool m_useGlobalWind = true;

	private static float m_waterTime = 0f;

	private static int m_waterUpdateFrame = 0;

	private static int _WaterTime = Shader.PropertyToID("_WaterTime");

	private static int _depth = Shader.PropertyToID("_depth");

	private static int _UseGlobalWind = Shader.PropertyToID("_UseGlobalWind");

	private List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	private void Awake()
	{
		m_collider = GetComponent<Collider>();
	}

	private void Start()
	{
		DetectWaterDepth();
		SetupMaterial();
	}

	private void DetectWaterDepth()
	{
		if ((bool)m_heightmap)
		{
			float[] oceanDepth = m_heightmap.GetOceanDepth();
			m_normalizedDepth[0] = Mathf.Clamp01(oceanDepth[0] / 10f);
			m_normalizedDepth[1] = Mathf.Clamp01(oceanDepth[1] / 10f);
			m_normalizedDepth[2] = Mathf.Clamp01(oceanDepth[2] / 10f);
			m_normalizedDepth[3] = Mathf.Clamp01(oceanDepth[3] / 10f);
		}
		else
		{
			m_normalizedDepth[0] = m_forceDepth;
			m_normalizedDepth[1] = m_forceDepth;
			m_normalizedDepth[2] = m_forceDepth;
			m_normalizedDepth[3] = m_forceDepth;
		}
	}

	private void Update()
	{
		if (m_waterUpdateFrame != Time.frameCount)
		{
			m_waterUpdateFrame = Time.frameCount;
			UpdateWaterTime(Time.deltaTime);
		}
		UpdateFloaters();
		m_waterSurface.material.SetFloat(_WaterTime, m_waterTime);
	}

	private void UpdateWaterTime(float dt)
	{
		float num = (m_menuWater ? Time.time : ZNet.instance.GetWrappedDayTimeSeconds());
		m_waterTime += dt;
		if (Mathf.Abs(num - m_waterTime) > 10f)
		{
			m_waterTime = num;
		}
		m_waterTime = Mathf.Lerp(m_waterTime, num, 0.05f);
	}

	private void SetupMaterial()
	{
		if (m_forceDepth >= 0f)
		{
			m_waterSurface.material.SetFloatArray(_depth, new float[4] { m_forceDepth, m_forceDepth, m_forceDepth, m_forceDepth });
		}
		else
		{
			m_waterSurface.material.SetFloatArray(_depth, m_normalizedDepth);
		}
		m_waterSurface.material.SetFloat(_UseGlobalWind, m_useGlobalWind ? 1f : 0f);
	}

	public LiquidType GetLiquidType()
	{
		return LiquidType.Water;
	}

	public float GetWaterSurface(Vector3 point, float waveFactor = 1f)
	{
		float wrappedDayTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
		float depth = Depth(point);
		float num = CalcWave(point, depth, wrappedDayTimeSeconds, waveFactor);
		float num2 = base.transform.position.y + num + m_surfaceOffset;
		if (Utils.LengthXZ(point) > 10500f && m_forceDepth < 0f)
		{
			num2 -= 100f;
		}
		return num2;
	}

	private float TrochSin(float x, float k)
	{
		return Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5f + 0.5f;
	}

	private float CreateWave(Vector3 worldPos, float time, float waveSpeed, float waveLength, float waveHeight, Vector2 dir2d, float sharpness)
	{
		Vector3 normalized = new Vector3(dir2d.x, 0f, dir2d.y).normalized;
		Vector3 vector = Vector3.Cross(normalized, Vector3.up);
		Vector3 vector2 = -(worldPos.z * normalized + worldPos.x * vector);
		return (TrochSin(time * waveSpeed + vector2.z * waveLength, sharpness) * TrochSin(time * waveSpeed * 0.123f + vector2.x * 0.13123f * waveLength, sharpness) - 0.2f) * waveHeight;
	}

	private float CalcWave(Vector3 worldPos, float depth, Vector4 wind, float _WaterTime, float waveFactor)
	{
		Vector3 vector = new Vector3(wind.x, wind.y, wind.z);
		float w = wind.w;
		float num = Mathf.Lerp(0f, w, depth);
		float time = _WaterTime / 20f;
		float num2 = CreateWave(worldPos, time, 10f, 0.04f, 8f, new Vector2(vector.x, vector.z), 0.5f);
		float num3 = CreateWave(worldPos, time, 14.123f, 0.08f, 6f, new Vector2(1.0312f, 0.312f), 0.5f);
		float num4 = CreateWave(worldPos, time, 22.312f, 0.1f, 4f, new Vector2(-0.123f, 1.12f), 0.5f);
		float num5 = CreateWave(worldPos, time, 31.42f, 0.2f, 2f, new Vector2(0.423f, 0.124f), 0.5f);
		float num6 = CreateWave(worldPos, time, 35.42f, 0.4f, 1f, new Vector2(0.123f, -0.64f), 0.5f);
		float num7 = CreateWave(worldPos, time, 38.1223f, 1f, 0.8f, new Vector2(-0.523f, -0.64f), 0.7f);
		float num8 = CreateWave(worldPos, time, 41.1223f, 1.2f, 0.6f * waveFactor, new Vector2(0.223f, 0.74f), 0.8f);
		float num9 = CreateWave(worldPos, time, 51.5123f, 1.3f, 0.4f * waveFactor, new Vector2(0.923f, -0.24f), 0.9f);
		float num10 = CreateWave(worldPos, time, 54.2f, 1.3f, 0.3f * waveFactor, new Vector2(-0.323f, 0.44f), 0.9f);
		float num11 = CreateWave(worldPos, time, 56.123f, 1.5f, 0.2f * waveFactor, new Vector2(0.5312f, -0.812f), 0.9f);
		return (num2 + num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10 + num11) * num;
	}

	private float CalcWave(Vector3 worldPos, float depth, float _WaterTime, float waveFactor)
	{
		Vector4 wind = new Vector4(1f, 0f, 0f, 0f);
		Vector4 wind2 = new Vector4(1f, 0f, 0f, 0f);
		float alpha = 0f;
		if (m_useGlobalWind)
		{
			EnvMan.instance.GetWindData(out wind, out wind2, out alpha);
		}
		float a = CalcWave(worldPos, depth, wind, _WaterTime, waveFactor);
		float b = CalcWave(worldPos, depth, wind2, _WaterTime, waveFactor);
		return Mathf.Lerp(a, b, alpha);
	}

	private float Depth(Vector3 point)
	{
		Vector3 vector = base.transform.InverseTransformPoint(point);
		float value = (vector.x + m_collider.bounds.size.x / 2f) / m_collider.bounds.size.x;
		float value2 = (vector.z + m_collider.bounds.size.z / 2f) / m_collider.bounds.size.z;
		value = Mathf.Clamp01(value);
		value2 = Mathf.Clamp01(value2);
		float a = Mathf.Lerp(m_normalizedDepth[3], m_normalizedDepth[2], value);
		float b = Mathf.Lerp(m_normalizedDepth[0], m_normalizedDepth[1], value);
		return Mathf.Lerp(a, b, value2);
	}

	private void OnTriggerEnter(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null && !m_inWater.Contains(component))
		{
			m_inWater.Add(component);
		}
	}

	private void UpdateFloaters()
	{
		if (m_inWater.Count == 0)
		{
			return;
		}
		IWaterInteractable waterInteractable = null;
		foreach (IWaterInteractable item in m_inWater)
		{
			Transform transform = item.GetTransform();
			if ((bool)transform)
			{
				float waterSurface = GetWaterSurface(transform.position);
				item.SetLiquidLevel(waterSurface, LiquidType.Water);
			}
			else
			{
				waterInteractable = item;
			}
		}
		if (waterInteractable != null)
		{
			m_inWater.Remove(waterInteractable);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.SetLiquidLevel(-10000f, LiquidType.Water);
			m_inWater.Remove(component);
		}
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable item in m_inWater)
		{
			item?.SetLiquidLevel(-10000f, LiquidType.Water);
		}
		m_inWater.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_surfaceOffset, new Vector3(2f, 0.05f, 2f));
	}
}
