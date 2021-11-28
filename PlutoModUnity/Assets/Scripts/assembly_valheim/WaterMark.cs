using UnityEngine;
using UnityEngine.UI;

public class WaterMark : MonoBehaviour
{
	public Text m_text;

	private void Awake()
	{
		m_text.text = "Version: " + Version.GetVersionString();
	}
}
