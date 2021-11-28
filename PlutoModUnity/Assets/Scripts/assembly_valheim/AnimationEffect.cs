using System.Collections.Generic;
using UnityEngine;

public class AnimationEffect : MonoBehaviour
{
	public Transform m_effectRoot;

	private Animator m_animator;

	private List<GameObject> m_attachments;

	private int m_attachStateHash;

	private void Start()
	{
		m_animator = GetComponent<Animator>();
	}

	public void Effect(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject original = e.objectReferenceParameter as GameObject;
		Transform transform = null;
		if (stringParameter.Length > 0)
		{
			transform = Utils.FindChild(base.transform, stringParameter);
		}
		if (transform == null)
		{
			transform = (m_effectRoot ? m_effectRoot : base.transform);
		}
		Object.Instantiate(original, transform.position, transform.rotation);
	}

	public void Attach(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject original = e.objectReferenceParameter as GameObject;
		Transform transform = Utils.FindChild(base.transform, stringParameter);
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find attach joint " + stringParameter);
			return;
		}
		ClearAttachment(transform);
		GameObject gameObject = Object.Instantiate(original, transform.position, transform.rotation);
		gameObject.transform.SetParent(transform, worldPositionStays: true);
		if (m_attachments == null)
		{
			m_attachments = new List<GameObject>();
		}
		m_attachments.Add(gameObject);
		m_attachStateHash = e.animatorStateInfo.fullPathHash;
		CancelInvoke("UpdateAttachments");
		InvokeRepeating("UpdateAttachments", 0.1f, 0.1f);
	}

	private void ClearAttachment(Transform parent)
	{
		if (m_attachments == null)
		{
			return;
		}
		foreach (GameObject attachment in m_attachments)
		{
			if ((bool)attachment && attachment.transform.parent == parent)
			{
				m_attachments.Remove(attachment);
				Object.Destroy(attachment);
				break;
			}
		}
	}

	public void RemoveAttachments()
	{
		if (m_attachments == null)
		{
			return;
		}
		foreach (GameObject attachment in m_attachments)
		{
			Object.Destroy(attachment);
		}
		m_attachments.Clear();
	}

	private void UpdateAttachments()
	{
		if (m_attachments != null && m_attachments.Count > 0)
		{
			if (m_attachStateHash != m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash && m_attachStateHash != m_animator.GetNextAnimatorStateInfo(0).fullPathHash)
			{
				RemoveAttachments();
			}
		}
		else
		{
			CancelInvoke("UpdateAttachments");
		}
	}
}
