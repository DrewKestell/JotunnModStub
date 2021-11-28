using UnityEngine;

public class EnvZone : MonoBehaviour
{
	public string m_environment = "";

	public bool m_force = true;

	private static EnvZone m_triggered;

	private void OnTriggerStay(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			if (m_force)
			{
				EnvMan.instance.SetForceEnvironment(m_environment);
			}
			m_triggered = this;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (m_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			if (m_force)
			{
				EnvMan.instance.SetForceEnvironment("");
			}
			m_triggered = null;
		}
	}

	public static string GetEnvironment()
	{
		if ((bool)m_triggered && !m_triggered.m_force)
		{
			return m_triggered.m_environment;
		}
		return null;
	}
}
