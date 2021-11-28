using UnityEngine;

public class TimedDestruction : MonoBehaviour
{
	public float m_timeout = 1f;

	public bool m_triggerOnAwake;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_triggerOnAwake)
		{
			Trigger();
		}
	}

	public void Trigger()
	{
		InvokeRepeating("DestroyNow", m_timeout, 1f);
	}

	public void Trigger(float timeout)
	{
		InvokeRepeating("DestroyNow", timeout, 1f);
	}

	private void DestroyNow()
	{
		if ((bool)m_nview)
		{
			if (m_nview.IsValid() && m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
