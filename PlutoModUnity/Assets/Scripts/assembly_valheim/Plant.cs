using System;
using UnityEngine;

public class Plant : SlowUpdate, Hoverable
{
	private enum Status
	{
		Healthy,
		NoSun,
		NoSpace,
		WrongBiome,
		NotCultivated
	}

	public string m_name = "Plant";

	public float m_growTime = 10f;

	public float m_growTimeMax = 2000f;

	public GameObject[] m_grownPrefabs = new GameObject[0];

	public float m_minScale = 1f;

	public float m_maxScale = 1f;

	public float m_growRadius = 1f;

	public bool m_needCultivatedGround;

	public bool m_destroyIfCantGrow;

	[SerializeField]
	private GameObject m_healthy;

	[SerializeField]
	private GameObject m_unhealthy;

	[SerializeField]
	private GameObject m_healthyGrown;

	[SerializeField]
	private GameObject m_unhealthyGrown;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public EffectList m_growEffect = new EffectList();

	private Status m_status;

	private ZNetView m_nview;

	private float m_updateTime;

	private float m_spawnTime;

	private static int m_spaceMask;

	private static int m_roofMask;

	public override void Awake()
	{
		base.Awake();
		m_nview = base.gameObject.GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			if (m_nview.IsOwner() && m_nview.GetZDO().GetLong("plantTime", 0L) == 0L)
			{
				m_nview.GetZDO().Set("plantTime", ZNet.instance.GetTime().Ticks);
			}
			m_spawnTime = Time.time;
		}
	}

	public string GetHoverText()
	{
		switch (m_status)
		{
		case Status.Healthy:
			return Localization.instance.Localize(m_name + " ( $piece_plant_healthy )");
		case Status.NoSpace:
			return Localization.instance.Localize(m_name + " ( $piece_plant_nospace )");
		case Status.NoSun:
			return Localization.instance.Localize(m_name + " ( $piece_plant_nosun )");
		case Status.WrongBiome:
			return Localization.instance.Localize(m_name + " ( $piece_plant_wrongbiome )");
		case Status.NotCultivated:
			return Localization.instance.Localize(m_name + " ( $piece_plant_notcultivated )");
		default:
			return "";
		}
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_name);
	}

	private double TimeSincePlanted()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong("plantTime", ZNet.instance.GetTime().Ticks));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	public override void SUpdate()
	{
		if (m_nview.IsValid() && !(Time.time - m_updateTime < 10f))
		{
			m_updateTime = Time.time;
			double num = TimeSincePlanted();
			UpdateHealth(num);
			float growTime = GetGrowTime();
			if ((bool)m_healthyGrown)
			{
				bool flag = num > (double)(growTime * 0.5f);
				m_healthy.SetActive(!flag && m_status == Status.Healthy);
				m_unhealthy.SetActive(!flag && m_status != Status.Healthy);
				m_healthyGrown.SetActive(flag && m_status == Status.Healthy);
				m_unhealthyGrown.SetActive(flag && m_status != Status.Healthy);
			}
			else
			{
				m_healthy.SetActive(m_status == Status.Healthy);
				m_unhealthy.SetActive(m_status != Status.Healthy);
			}
			if (m_nview.IsOwner() && Time.time - m_spawnTime > 10f && num > (double)growTime)
			{
				Grow();
			}
		}
	}

	private float GetGrowTime()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState((int)(m_nview.GetZDO().m_uid.id + m_nview.GetZDO().m_uid.userID));
		float value = UnityEngine.Random.value;
		UnityEngine.Random.state = state;
		return Mathf.Lerp(m_growTime, m_growTimeMax, value);
	}

	private void Grow()
	{
		if (m_status != 0)
		{
			if (m_destroyIfCantGrow)
			{
				Destroy();
			}
			return;
		}
		GameObject original = m_grownPrefabs[UnityEngine.Random.Range(0, m_grownPrefabs.Length)];
		Quaternion quaternion = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject obj = UnityEngine.Object.Instantiate(original, base.transform.position, quaternion);
		ZNetView component = obj.GetComponent<ZNetView>();
		float num = UnityEngine.Random.Range(m_minScale, m_maxScale);
		component.SetLocalScale(new Vector3(num, num, num));
		TreeBase component2 = obj.GetComponent<TreeBase>();
		if ((bool)component2)
		{
			component2.Grow();
		}
		m_nview.Destroy();
		m_growEffect.Create(base.transform.position, quaternion, null, num);
	}

	private void UpdateHealth(double timeSincePlanted)
	{
		if (timeSincePlanted < 10.0)
		{
			m_status = Status.Healthy;
			return;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(base.transform.position);
		if ((bool)heightmap)
		{
			if ((heightmap.GetBiome(base.transform.position) & m_biome) == 0)
			{
				m_status = Status.WrongBiome;
				return;
			}
			if (m_needCultivatedGround && !heightmap.IsCultivated(base.transform.position))
			{
				m_status = Status.NotCultivated;
				return;
			}
		}
		if (HaveRoof())
		{
			m_status = Status.NoSun;
		}
		else if (!HaveGrowSpace())
		{
			m_status = Status.NoSpace;
		}
		else
		{
			m_status = Status.Healthy;
		}
	}

	private void Destroy()
	{
		IDestructible component = GetComponent<IDestructible>();
		if (component != null)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 9999f;
			component.Damage(hitData);
		}
	}

	private bool HaveRoof()
	{
		if (m_roofMask == 0)
		{
			m_roofMask = LayerMask.GetMask("Default", "static_solid", "piece");
		}
		if (Physics.Raycast(base.transform.position, Vector3.up, 100f, m_roofMask))
		{
			return true;
		}
		return false;
	}

	private bool HaveGrowSpace()
	{
		if (m_spaceMask == 0)
		{
			m_spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, m_growRadius, m_spaceMask);
		for (int i = 0; i < array.Length; i++)
		{
			Plant component = array[i].GetComponent<Plant>();
			if (!component || (!(component == this) && component.GetStatus() == Status.Healthy))
			{
				return false;
			}
		}
		return true;
	}

	private Status GetStatus()
	{
		return m_status;
	}
}
