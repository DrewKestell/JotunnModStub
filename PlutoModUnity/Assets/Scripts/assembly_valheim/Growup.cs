using UnityEngine;

public class Growup : MonoBehaviour
{
	public float m_growTime = 60f;

	public GameObject m_grownPrefab;

	private BaseAI m_baseAI;

	private ZNetView m_nview;

	private void Start()
	{
		m_baseAI = GetComponent<BaseAI>();
		m_nview = GetComponent<ZNetView>();
		InvokeRepeating("GrowUpdate", Random.Range(10f, 15f), 10f);
	}

	private void GrowUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && m_baseAI.GetTimeSinceSpawned().TotalSeconds > (double)m_growTime)
		{
			Character component = GetComponent<Character>();
			Character component2 = Object.Instantiate(m_grownPrefab, base.transform.position, base.transform.rotation).GetComponent<Character>();
			if ((bool)component && (bool)component2)
			{
				component2.SetTamed(component.IsTamed());
				component2.SetLevel(component.GetLevel());
			}
			m_nview.Destroy();
		}
	}
}
