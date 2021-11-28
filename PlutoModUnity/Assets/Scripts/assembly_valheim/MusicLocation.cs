using System;
using UnityEngine;

public class MusicLocation : MonoBehaviour
{
	public bool m_takeRadiusFromLocation = true;

	public float m_radius = 20f;

	public float m_exitRadiusMultiplier = 1f;

	[Header("Music")]
	public string m_musicName = "";

	public float m_musicChance = 0.7f;

	[Tooltip("If the music can play again before playing a different location music first.")]
	public bool m_musicCanRepeat = true;

	public bool m_loopMusic;

	public bool m_stopMusicOnExit;

	public int m_maxPlaysPerActivation;

	[HideInInspector]
	public int m_PlayCount;

	private DateTime m_lastEnterCheck;

	private bool m_lastWasInside;

	private bool m_lastWasInsideWide;

	private bool m_isLooping;

	private void Start()
	{
		Location component;
		if (m_takeRadiusFromLocation && (object)(component = GetComponent<Location>()) != null)
		{
			m_radius = component.GetMaxRadius();
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		if (DateTime.Now > m_lastEnterCheck + TimeSpan.FromSeconds(1.0))
		{
			m_lastEnterCheck = DateTime.Now;
			if (IsInside(Player.m_localPlayer.transform.position))
			{
				if (!m_lastWasInside)
				{
					m_lastWasInside = (m_lastWasInsideWide = true);
					OnEnter();
				}
			}
			else
			{
				if (m_lastWasInside)
				{
					m_lastWasInside = false;
					OnExit();
				}
				if (m_lastWasInsideWide && !IsInside(Player.m_localPlayer.transform.position, m_radius * m_exitRadiusMultiplier))
				{
					m_lastWasInsideWide = false;
					OnExitWide();
				}
			}
		}
		if (m_isLooping && m_lastWasInside && !string.IsNullOrEmpty(m_musicName))
		{
			MusicMan.instance.LocationMusic(m_musicName);
		}
	}

	private void OnEnter()
	{
		ZLog.Log("MusicZone.OnEnter: " + base.name);
		if (!string.IsNullOrEmpty(m_musicName) && (m_maxPlaysPerActivation == 0 || m_PlayCount < m_maxPlaysPerActivation) && UnityEngine.Random.Range(0f, 1f) <= m_musicChance && (m_musicCanRepeat || MusicMan.instance.m_lastLocationMusic != m_musicName))
		{
			ZLog.Log("MusicZone '" + base.name + "' Playing Music: " + m_musicName);
			m_PlayCount++;
			MusicMan.instance.LocationMusic(m_musicName);
			if (m_loopMusic)
			{
				m_isLooping = true;
			}
		}
	}

	private void OnExit()
	{
		ZLog.Log("MusicZone.OnExit: " + base.name);
	}

	private void OnExitWide()
	{
		ZLog.Log("MusicZone.OnExitWide: " + base.name);
		if (MusicMan.instance.m_lastLocationMusic == m_musicName && (m_stopMusicOnExit || m_loopMusic))
		{
			MusicMan.instance.LocationMusic(null);
		}
		m_isLooping = false;
	}

	public bool IsInside(Vector3 point, float additionalRadius = 0f)
	{
		float radius = m_radius;
		return Utils.DistanceXZ(base.transform.position, point) < radius + additionalRadius;
	}

	private void OnDrawGizmos()
	{
		float num = m_radius;
		Location component;
		if (m_takeRadiusFromLocation && (object)(component = GetComponent<Location>()) != null)
		{
			num = component.GetMaxRadius();
		}
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Utils.DrawGizmoCircle(base.transform.position, num, 32);
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
		Utils.DrawGizmoCircle(base.transform.position, num + num * m_exitRadiusMultiplier, 32);
	}
}
