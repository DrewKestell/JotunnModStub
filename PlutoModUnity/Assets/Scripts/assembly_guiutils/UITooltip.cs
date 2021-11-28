using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public GameObject m_tooltipPrefab;

	public string m_text = "";

	public string m_topic = "";

	public GameObject m_gamepadFocusObject;

	private static UITooltip m_current;

	private static GameObject m_tooltip;

	private static GameObject m_hovered;

	private const float m_showDelay = 0.5f;

	private float m_showTimer;

	private void LateUpdate()
	{
		if (m_current == this && !m_tooltip.activeSelf)
		{
			m_showTimer += Time.deltaTime;
			if (m_showTimer > 0.5f)
			{
				m_tooltip.SetActive(value: true);
			}
		}
		if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
		{
			if (m_gamepadFocusObject != null)
			{
				if (m_gamepadFocusObject.activeSelf && m_current != this)
				{
					OnHoverStart(m_gamepadFocusObject);
				}
				else if (!m_gamepadFocusObject.activeSelf && m_current == this)
				{
					HideTooltip();
				}
				if (m_current == this && m_tooltip != null)
				{
					RectTransform obj = base.gameObject.transform as RectTransform;
					Vector3[] array = new Vector3[4];
					obj.GetWorldCorners(array);
					m_tooltip.transform.position = array[2];
					Utils.ClampUIToScreen(m_tooltip.transform as RectTransform);
				}
			}
		}
		else if (m_current == this)
		{
			if (m_hovered == null)
			{
				HideTooltip();
				return;
			}
			if (!RectTransformUtility.RectangleContainsScreenPoint(m_hovered.transform as RectTransform, Input.mousePosition))
			{
				HideTooltip();
				return;
			}
			m_tooltip.transform.position = Input.mousePosition;
			Utils.ClampUIToScreen(m_tooltip.transform as RectTransform);
		}
	}

	private void OnDisable()
	{
		if (m_current == this)
		{
			HideTooltip();
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		OnHoverStart(eventData.pointerEnter);
	}

	private void OnHoverStart(GameObject go)
	{
		if ((bool)m_current)
		{
			HideTooltip();
		}
		if (m_tooltip == null && (m_text != "" || m_topic != ""))
		{
			m_tooltip = Object.Instantiate(m_tooltipPrefab, base.transform.GetComponentInParent<Canvas>().transform);
			UpdateTextElements();
			Utils.ClampUIToScreen(m_tooltip.transform as RectTransform);
			m_hovered = go;
			m_current = this;
			m_tooltip.SetActive(value: false);
			m_showTimer = 0f;
		}
	}

	private void UpdateTextElements()
	{
		if (m_tooltip != null)
		{
			Transform transform = Utils.FindChild(m_tooltip.transform, "Text");
			if (transform != null)
			{
				transform.GetComponent<Text>().text = Localization.instance.Localize(m_text);
			}
			Transform transform2 = Utils.FindChild(m_tooltip.transform, "Topic");
			if (transform2 != null)
			{
				transform2.GetComponent<Text>().text = Localization.instance.Localize(m_topic);
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_current == this)
		{
			HideTooltip();
		}
	}

	public static void HideTooltip()
	{
		if ((bool)m_tooltip)
		{
			Object.Destroy(m_tooltip);
			m_current = null;
			m_tooltip = null;
			m_hovered = null;
		}
	}

	public void Set(string topic, string text)
	{
		if (!(topic == m_topic) || !(text == m_text))
		{
			m_topic = topic;
			m_text = text;
			if (m_current == this && m_tooltip != null)
			{
				UpdateTextElements();
			}
		}
	}
}
