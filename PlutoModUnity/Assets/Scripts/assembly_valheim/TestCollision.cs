using UnityEngine;

public class TestCollision : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnCollisionEnter(Collision info)
	{
		ZLog.Log("Hit by " + info.rigidbody.gameObject.name);
		ZLog.Log(string.Concat("rel vel ", info.relativeVelocity, " ", info.relativeVelocity));
		ZLog.Log(string.Concat("Vel ", info.rigidbody.velocity, "  ", info.rigidbody.angularVelocity));
	}
}
