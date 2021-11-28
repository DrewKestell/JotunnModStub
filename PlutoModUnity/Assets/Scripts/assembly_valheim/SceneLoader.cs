using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	public string m_scene = "";

	private void Start()
	{
		StartCoroutine(LoadYourAsyncScene());
	}

	private IEnumerator LoadYourAsyncScene()
	{
		ZLog.Log("Starting to load scene:" + m_scene);
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(m_scene, LoadSceneMode.Single);
		while (!asyncLoad.isDone)
		{
			yield return null;
		}
	}
}
