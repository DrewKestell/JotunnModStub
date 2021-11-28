using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WearNTearUpdater : MonoBehaviour
{
	private const int m_updatesPerFrame = 50;

	private void Awake()
	{
		StartCoroutine("UpdateWear");
	}

	private IEnumerator UpdateWear()
	{
		while (true)
		{
			List<WearNTear> instances = WearNTear.GetAllInstaces();
			int index = 0;
			while (index < instances.Count)
			{
				for (int i = 0; i < 50; i++)
				{
					if (instances.Count == 0)
					{
						break;
					}
					if (index >= instances.Count)
					{
						break;
					}
					instances[index].UpdateWear();
					int num = index + 1;
					index = num;
				}
				yield return null;
			}
			yield return new WaitForSeconds(0.5f);
		}
	}
}
