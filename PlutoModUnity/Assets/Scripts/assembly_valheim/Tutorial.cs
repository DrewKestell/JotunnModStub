using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
	[Serializable]
	public class TutorialText
	{
		public string m_name;

		public string m_topic = "";

		public string m_label = "";

		[TextArea]
		public string m_text = "";
	}

	public List<TutorialText> m_texts = new List<TutorialText>();

	public RectTransform m_windowRoot;

	public Text m_topic;

	public Text m_text;

	public GameObject m_ravenPrefab;

	private static Tutorial m_instance;

	private Queue<string> m_tutQueue = new Queue<string>();

	public static Tutorial instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_windowRoot.gameObject.SetActive(value: false);
	}

	private void Update()
	{
	}

	public void ShowText(string name, bool force)
	{
		TutorialText tutorialText = m_texts.Find((TutorialText x) => x.m_name == name);
		if (tutorialText != null)
		{
			SpawnRaven(tutorialText.m_name, tutorialText.m_topic, tutorialText.m_text, tutorialText.m_label);
		}
		else
		{
			Debug.Log("Missing tutorial text for: " + name);
		}
	}

	private void SpawnRaven(string key, string topic, string text, string label)
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate(m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		Raven.AddTempText(key, topic, text, label, munin: false);
	}
}
