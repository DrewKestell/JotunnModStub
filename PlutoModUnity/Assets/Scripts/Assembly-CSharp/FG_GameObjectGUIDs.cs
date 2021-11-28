using System;
using UnityEngine;

public class FG_GameObjectGUIDs : MonoBehaviour
{
	[NonSerialized]
	public static bool _dirty = true;

	[HideInInspector]
	public string[] guids = new string[0];

	[HideInInspector]
	public UnityEngine.Object[] objects = new UnityEngine.Object[0];

	private FG_GameObjectGUIDs()
	{
		_dirty = true;
	}

	private void Awake()
	{
		_dirty = true;
	}

	private void OnDisable()
	{
		_dirty = true;
	}

	private void OnDestroy()
	{
		_dirty = true;
	}
}
