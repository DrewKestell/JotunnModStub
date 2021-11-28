using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIGroupHandler : MonoBehaviour
{
	public GameObject m_defaultElement;

	public GameObject m_enableWhenActiveAndGamepad;

	public int m_groupPriority;

	private CanvasGroup m_canvasGroup;

	private bool m_userActive = true;

	private bool m_active = true;

	private static List<UIGroupHandler> m_groups = new List<UIGroupHandler>();

	private void Awake()
	{
		m_groups.Add(this);
		m_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void OnDestroy()
	{
		m_groups.Remove(this);
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		if (m_active)
		{
			m_active = false;
			ResetActiveElement();
		}
	}

	private Selectable FindSelectable(GameObject root)
	{
		return root.GetComponentInChildren<Selectable>(includeInactive: false);
	}

	private bool IsHighestPriority()
	{
		if (!m_userActive)
		{
			return false;
		}
		foreach (UIGroupHandler group in m_groups)
		{
			if (!(group == this) && group.gameObject.activeInHierarchy && group.m_groupPriority > m_groupPriority)
			{
				return false;
			}
		}
		return true;
	}

	private void ResetActiveElement()
	{
		if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
		{
			return;
		}
		Selectable[] componentsInChildren = base.gameObject.GetComponentsInChildren<Selectable>(includeInactive: false);
		foreach (Selectable selectable in componentsInChildren)
		{
			if (EventSystem.current.currentSelectedGameObject == selectable.gameObject)
			{
				ZLog.Log("FOund selected " + selectable.gameObject.name);
				EventSystem.current.SetSelectedGameObject(null);
				break;
			}
		}
	}

	private void Update()
	{
		bool flag = IsHighestPriority();
		if (flag != m_active)
		{
			ZLog.Log("UI Group status changed " + base.gameObject.name + " = " + flag);
			ResetActiveElement();
		}
		m_active = flag;
		if ((bool)m_canvasGroup)
		{
			m_canvasGroup.interactable = flag;
		}
		if ((bool)m_enableWhenActiveAndGamepad)
		{
			m_enableWhenActiveAndGamepad.SetActive(m_active && ZInput.IsGamepadActive());
		}
		if (m_active && m_defaultElement != null && ZInput.IsGamepadActive() && !HaveSelectedObject())
		{
			Selectable selectable = FindSelectable(m_defaultElement);
			if ((bool)selectable)
			{
				ZLog.Log("Activating default element " + m_defaultElement.name);
				EventSystem.current.SetSelectedGameObject(selectable.gameObject);
				selectable.OnSelect(null);
			}
		}
	}

	private bool HaveSelectedObject()
	{
		if (EventSystem.current.currentSelectedGameObject == null)
		{
			return false;
		}
		if (!EventSystem.current.currentSelectedGameObject.activeInHierarchy)
		{
			return false;
		}
		Selectable component = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
		if ((bool)component && !component.IsInteractable())
		{
			return false;
		}
		return true;
	}

	public void SetActive(bool active)
	{
		m_userActive = active;
		if (!m_userActive && (bool)m_enableWhenActiveAndGamepad)
		{
			m_enableWhenActiveAndGamepad.SetActive(value: false);
		}
	}

	public bool IsActive()
	{
		return m_active;
	}
}
