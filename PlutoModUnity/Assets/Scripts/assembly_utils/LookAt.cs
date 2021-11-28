using UnityEngine;

public class LookAt : MonoBehaviour
{
	private Vector3 m_lookAtTarget = Vector3.zero;

	private Vector3 m_lookAtPoint = Vector3.zero;

	private float m_weight;

	private float m_targetWeight;

	private Animator m_animator;

	private Vector3 m_turnVel = Vector3.zero;

	private void Awake()
	{
		m_animator = GetComponent<Animator>();
	}

	private void OnAnimatorIK(int layerIndex)
	{
		m_weight = Mathf.MoveTowards(m_weight, m_targetWeight, Time.deltaTime);
		m_lookAtPoint = Vector3.SmoothDamp(m_lookAtPoint, m_lookAtTarget, ref m_turnVel, 1f, 99f, Time.deltaTime);
		m_animator.SetLookAtPosition(m_lookAtPoint);
		m_animator.SetLookAtWeight(m_weight, 0.5f, 1f, 0f);
	}

	public void SetLoockAtTarget(Vector3 target)
	{
		m_lookAtTarget = target;
		m_targetWeight = 1f;
	}

	public void ResetTarget()
	{
		m_targetWeight = 0f;
	}
}
