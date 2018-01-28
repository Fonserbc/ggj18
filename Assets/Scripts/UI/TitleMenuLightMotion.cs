using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleMenuLightMotion : MonoBehaviour
{
	void Update ()
	{
		float cicleX = (Mathf.Sin(Time.unscaledTime * 0.4f) + 1.0f) / 2.0f;
		float cicleY = (Mathf.Sin(Time.unscaledTime * 0.166f) + 1.0f) / 2.0f;

		transform.localPosition = new Vector3(
			(cicleX * 8.0f) - 5.0f,
			(cicleY * 4.0f) - 1.0f,
			-2);
	}
}
