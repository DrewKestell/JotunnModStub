using UnityEngine;

public class TeleportWorldTrigger : MonoBehaviour
{
	private TeleportWorld m_tp;

	private void Awake()
	{
		m_tp = GetComponentInParent<TeleportWorld>();
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			ZLog.Log("TRIGGER");
			m_tp.Teleport(component);
		}
	}
}
