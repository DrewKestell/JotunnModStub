using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectList
{
	[Serializable]
	public class EffectData
	{
		public GameObject m_prefab;

		public bool m_enabled = true;

		public int m_variant = -1;

		public bool m_attach;

		public bool m_inheritParentRotation;

		public bool m_inheritParentScale;

		public bool m_randomRotation;

		public bool m_scale;

		public string m_childTransform;
	}

	public EffectData[] m_effectPrefabs = new EffectData[0];

	public GameObject[] Create(Vector3 basePos, Quaternion baseRot, Transform baseParent = null, float scale = 1f, int variant = -1)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < m_effectPrefabs.Length; i++)
		{
			EffectData effectData = m_effectPrefabs[i];
			if (!effectData.m_enabled || (variant >= 0 && effectData.m_variant >= 0 && variant != effectData.m_variant))
			{
				continue;
			}
			Transform transform = baseParent;
			Vector3 position = basePos;
			Quaternion rotation = baseRot;
			if (!string.IsNullOrEmpty(effectData.m_childTransform) && baseParent != null)
			{
				Transform transform2 = Utils.FindChild(transform, effectData.m_childTransform);
				if ((bool)transform2)
				{
					transform = transform2;
					position = transform.position;
				}
			}
			if ((bool)transform && effectData.m_inheritParentRotation)
			{
				rotation = transform.rotation;
			}
			if (effectData.m_randomRotation)
			{
				rotation = UnityEngine.Random.rotation;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(effectData.m_prefab, position, rotation);
			if (effectData.m_scale)
			{
				if ((bool)baseParent && effectData.m_inheritParentScale)
				{
					Vector3 localScale = baseParent.localScale * scale;
					gameObject.transform.localScale = localScale;
				}
				else
				{
					gameObject.transform.localScale = new Vector3(scale, scale, scale);
				}
			}
			else if ((bool)baseParent && effectData.m_inheritParentScale)
			{
				gameObject.transform.localScale = baseParent.localScale;
			}
			if (effectData.m_attach && transform != null)
			{
				gameObject.transform.SetParent(transform);
			}
			list.Add(gameObject);
		}
		return list.ToArray();
	}

	public bool HasEffects()
	{
		if (m_effectPrefabs == null || m_effectPrefabs.Length == 0)
		{
			return false;
		}
		EffectData[] effectPrefabs = m_effectPrefabs;
		for (int i = 0; i < effectPrefabs.Length; i++)
		{
			if (effectPrefabs[i].m_enabled)
			{
				return true;
			}
		}
		return false;
	}
}
