using UnityEngine;

public class TerrainLod : MonoBehaviour
{
	public float m_updateStepDistance = 256f;

	private Heightmap m_hmap;

	private Vector3 m_lastPoint = new Vector3(99999f, 0f, 99999f);

	private bool m_needRebuild = true;

	private void Awake()
	{
		m_hmap = GetComponent<Heightmap>();
	}

	private void Update()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null) && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			Vector3 position = mainCamera.transform.position;
			if (Utils.DistanceXZ(position, m_lastPoint) > m_updateStepDistance)
			{
				m_lastPoint = new Vector3(Mathf.Round(position.x / m_hmap.m_scale) * m_hmap.m_scale, 0f, Mathf.Round(position.z / m_hmap.m_scale) * m_hmap.m_scale);
				m_needRebuild = true;
			}
			if (m_needRebuild && HeightmapBuilder.instance.IsTerrainReady(m_lastPoint, m_hmap.m_width, m_hmap.m_scale, m_hmap.m_isDistantLod, WorldGenerator.instance))
			{
				base.transform.position = m_lastPoint;
				m_hmap.Regenerate();
				m_needRebuild = false;
			}
		}
	}
}
