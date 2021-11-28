using System.Collections.Generic;
using UnityEngine;

public class RandomSpawn : MonoBehaviour
{
	public GameObject m_OffObject;

	[Range(0f, 100f)]
	public float m_chanceToSpawn = 50f;

	private List<ZNetView> m_childNetViews;

	private ZNetView m_nview;

	public void Randomize()
	{
		bool spawned = Random.Range(0f, 100f) <= m_chanceToSpawn;
		SetSpawned(spawned);
	}

	public void Reset()
	{
		SetSpawned(doSpawn: true);
	}

	private void SetSpawned(bool doSpawn)
	{
		if (!doSpawn)
		{
			base.gameObject.SetActive(value: false);
			foreach (ZNetView childNetView in m_childNetViews)
			{
				childNetView.gameObject.SetActive(value: false);
			}
		}
		else if (m_nview == null)
		{
			base.gameObject.SetActive(value: true);
		}
		if (m_OffObject != null)
		{
			m_OffObject.SetActive(!doSpawn);
		}
	}

	public void Prepare()
	{
		m_nview = GetComponent<ZNetView>();
		m_childNetViews = new List<ZNetView>();
		ZNetView[] componentsInChildren = base.gameObject.GetComponentsInChildren<ZNetView>(includeInactive: true);
		foreach (ZNetView zNetView in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(zNetView.gameObject, base.gameObject))
			{
				m_childNetViews.Add(zNetView);
			}
		}
	}
}
