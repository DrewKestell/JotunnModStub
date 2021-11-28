using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomEvent
{
	public string m_name = "";

	public bool m_enabled = true;

	public bool m_random = true;

	public float m_duration = 60f;

	public bool m_nearBaseOnly = true;

	public bool m_pauseIfNoPlayerInArea = true;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	[Header("( Keys required to be TRUE )")]
	public List<string> m_requiredGlobalKeys = new List<string>();

	[Header("( Keys required to be FALSE )")]
	public List<string> m_notRequiredGlobalKeys = new List<string>();

	[Space(20f)]
	public string m_startMessage = "";

	public string m_endMessage = "";

	public string m_forceMusic = "";

	public string m_forceEnvironment = "";

	public List<SpawnSystem.SpawnData> m_spawn = new List<SpawnSystem.SpawnData>();

	private bool m_firstActivation = true;

	private bool m_active;

	[NonSerialized]
	public float m_time;

	[NonSerialized]
	public Vector3 m_pos = Vector3.zero;

	public RandomEvent Clone()
	{
		RandomEvent randomEvent = MemberwiseClone() as RandomEvent;
		randomEvent.m_spawn = new List<SpawnSystem.SpawnData>();
		foreach (SpawnSystem.SpawnData item in m_spawn)
		{
			randomEvent.m_spawn.Add(item.Clone());
		}
		return randomEvent;
	}

	public bool Update(bool server, bool active, bool playerInArea, float dt)
	{
		if (m_pauseIfNoPlayerInArea && !playerInArea)
		{
			return false;
		}
		m_time += dt;
		if (m_duration > 0f && m_time > m_duration)
		{
			return true;
		}
		return false;
	}

	public void OnActivate()
	{
		m_active = true;
		if (m_firstActivation)
		{
			m_firstActivation = false;
			if (m_startMessage != "")
			{
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, m_startMessage);
			}
		}
	}

	public void OnDeactivate(bool end)
	{
		m_active = false;
		if (end && m_endMessage != "")
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, m_endMessage);
		}
	}

	public string GetHudText()
	{
		return m_startMessage;
	}

	public void OnStart()
	{
	}

	public void OnStop()
	{
	}

	public bool InEventBiome()
	{
		return (EnvMan.instance.GetCurrentBiome() & m_biome) != 0;
	}

	public float GetTime()
	{
		return m_time;
	}
}
