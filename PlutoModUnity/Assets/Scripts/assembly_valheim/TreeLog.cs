using System.Collections.Generic;
using UnityEngine;

public class TreeLog : MonoBehaviour, IDestructible
{
	public float m_health = 60f;

	public HitData.DamageModifiers m_damages;

	public int m_minToolTier;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public DropTable m_dropWhenDestroyed = new DropTable();

	public GameObject m_subLogPrefab;

	public Transform[] m_subLogPoints = new Transform[0];

	public float m_spawnDistance = 2f;

	public float m_hitNoise = 100f;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private bool m_firstFrame = true;

	private void Awake()
	{
		m_body = GetComponent<Rigidbody>();
		m_body.maxDepenetrationVelocity = 1f;
		m_nview = GetComponent<ZNetView>();
		m_nview.Register<HitData>("Damage", RPC_Damage);
		if (m_nview.IsOwner())
		{
			float @float = m_nview.GetZDO().GetFloat("health", -1f);
			if (@float == -1f)
			{
				m_nview.GetZDO().Set("health", m_health);
			}
			else if (@float <= 0f)
			{
				m_nview.Destroy();
			}
		}
		Invoke("EnableDamage", 0.2f);
	}

	private void EnableDamage()
	{
		m_firstFrame = false;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Tree;
	}

	public void Damage(HitData hit)
	{
		if (!m_firstFrame && m_nview.IsValid())
		{
			m_nview.InvokeRPC("Damage", hit);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float @float = m_nview.GetZDO().GetFloat("health");
		if (@float <= 0f)
		{
			return;
		}
		hit.ApplyResistance(m_damages, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		if (hit.m_toolTier < m_minToolTier)
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
			return;
		}
		if ((bool)m_body)
		{
			m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce * 2f, hit.m_point, ForceMode.Impulse);
		}
		DamageText.instance.ShowText(significantModifier, hit.m_point, totalDamage);
		if (totalDamage <= 0f)
		{
			return;
		}
		@float -= totalDamage;
		if (@float < 0f)
		{
			@float = 0f;
		}
		m_nview.GetZDO().Set("health", @float);
		m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform);
		if (m_hitNoise > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_hitNoise);
			}
		}
		if (@float <= 0f)
		{
			Destroy();
		}
	}

	private void Destroy()
	{
		ZNetScene.instance.Destroy(base.gameObject);
		m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform);
		List<GameObject> dropList = m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector3 position = base.transform.position + base.transform.up * Random.Range(0f - m_spawnDistance, m_spawnDistance) + Vector3.up * 0.3f * i;
			Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
			Object.Instantiate(dropList[i], position, rotation);
		}
		if (m_subLogPrefab != null)
		{
			Transform[] subLogPoints = m_subLogPoints;
			foreach (Transform transform in subLogPoints)
			{
				Object.Instantiate(m_subLogPrefab, transform.position, base.transform.rotation).GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			}
		}
	}
}
