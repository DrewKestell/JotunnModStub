using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LineAttach : MonoBehaviour
{
	public List<Transform> m_attachments = new List<Transform>();

	private LineRenderer m_lineRenderer;

	private void Start()
	{
		m_lineRenderer = GetComponent<LineRenderer>();
	}

	private void LateUpdate()
	{
		for (int i = 0; i < m_attachments.Count; i++)
		{
			Transform transform = m_attachments[i];
			if ((bool)transform)
			{
				m_lineRenderer.SetPosition(i, base.transform.InverseTransformPoint(transform.position));
			}
		}
	}
}
