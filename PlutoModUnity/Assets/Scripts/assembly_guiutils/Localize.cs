using UnityEngine;

public class Localize : MonoBehaviour
{
	private void Awake()
	{
		Localization.instance.Localize(base.transform);
	}
}
