using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Init : MonoBehaviour
{
	static private bool isInit = false;
	static public bool IsInit { get { return isInit; } }

	public UnityEngine.UI.Image Fade;
	public AudioSource NotengoVoice;

	void Start ()
	{
		StartCoroutine(Start_Coroutine());
	}

	IEnumerator Start_Coroutine()
	{
		bool skip = false;
		float TimeLeft;

		TimeLeft = 0.0f;
		while (TimeLeft < 1.0f && !skip)
		{
			if (Input.anyKeyDown)
				skip = true;

			Color color = Fade.color;
			color.a = 1.0f;
			Fade.color = color;

			TimeLeft += Time.unscaledDeltaTime;
			yield return null;
		}

		TimeLeft = 0.0f;
		while(TimeLeft < 1.0f && !skip)
		{
			if (Input.anyKeyDown)
				skip = true;

			Color color = Fade.color;
			color.a = 1.0f - TimeLeft;
			Fade.color = color;

			TimeLeft += Time.unscaledDeltaTime;
			yield return null;
		}

		if (!skip)
		{
			if (NotengoVoice != null)
				NotengoVoice.Play();

			TimeLeft = 0.0f;
			while (TimeLeft < 3.0f && !skip)
			{
				if (Input.anyKeyDown)
					skip = true;

				Color color = Fade.color;
				color.a = 0.0f;
				Fade.color = color;

				TimeLeft += Time.unscaledDeltaTime;
				yield return null;
			}

			if (NotengoVoice != null)
				NotengoVoice.Stop();
		}

		if (!skip)
		{
			TimeLeft = 0.0f;
			while (TimeLeft < 1.0f && !skip)
			{
				if (Input.anyKeyDown)
					skip = true;

				Color color = Fade.color;
				color.a = TimeLeft;
				Fade.color = color;

				TimeLeft += Time.unscaledDeltaTime;
				yield return null;
			}
		}

		AsyncOperation op = SceneManager.LoadSceneAsync("TitleMenu");
		op.allowSceneActivation = true;
		isInit = true;
	}
}
