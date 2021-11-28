using System;
using UnityEngine;

public class Gibber : MonoBehaviour
{
	[Serializable]
	public class GibbData
	{
		public GameObject m_object;

		public float m_chanceToSpawn = 1f;
	}

	public EffectList m_punchEffector = new EffectList();

	public GameObject m_gibHitEffect;

	public GameObject m_gibDestroyEffect;

	public float m_gibHitDestroyChance;

	public GibbData[] m_gibbs = new GibbData[0];

	public float m_minVel = 10f;

	public float m_maxVel = 20f;

	public float m_maxRotVel = 20f;

	public float m_impactDirectionMix = 0.5f;

	public float m_timeout = 5f;

	private bool m_done;

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		if (!m_done)
		{
			Explode(base.transform.position, Vector3.zero);
		}
	}

	public void Setup(Vector3 hitPoint, Vector3 hitDirection)
	{
		Explode(hitPoint, hitDirection);
	}

	private void DestroyAll()
	{
		if ((bool)m_nview)
		{
			if (m_nview.GetZDO().m_owner == 0L)
			{
				m_nview.ClaimOwnership();
			}
			if (m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void CreateBodies()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (!gameObject.GetComponent<Rigidbody>())
			{
				gameObject.AddComponent<BoxCollider>();
				gameObject.AddComponent<Rigidbody>();
			}
		}
	}

	private void Explode(Vector3 hitPoint, Vector3 hitDirection)
	{
		m_done = true;
		InvokeRepeating("DestroyAll", m_timeout, 1f);
		Vector3 position = base.transform.position;
		float t = (((double)hitDirection.magnitude > 0.01) ? m_impactDirectionMix : 0f);
		CreateBodies();
		Rigidbody[] componentsInChildren = base.gameObject.GetComponentsInChildren<Rigidbody>();
		foreach (Rigidbody obj in componentsInChildren)
		{
			float num = UnityEngine.Random.Range(m_minVel, m_maxVel);
			Vector3 vector = Vector3.Lerp(Vector3.Normalize(obj.worldCenterOfMass - position), hitDirection, t);
			obj.velocity = vector * num;
			obj.angularVelocity = new Vector3(UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel), UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel), UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel));
		}
		GibbData[] gibbs = m_gibbs;
		foreach (GibbData gibbData in gibbs)
		{
			if ((bool)gibbData.m_object && gibbData.m_chanceToSpawn < 1f && UnityEngine.Random.value > gibbData.m_chanceToSpawn)
			{
				UnityEngine.Object.Destroy(gibbData.m_object);
			}
		}
		if ((double)hitDirection.magnitude > 0.01)
		{
			Quaternion baseRot = Quaternion.LookRotation(hitDirection);
			m_punchEffector.Create(hitPoint, baseRot);
		}
	}
}
