using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PressButtonFade : MonoBehaviour
{
	public Graphic graphic;

	void Update ()
	{
		float time = Time.unscaledTime;
		float alpha = (Mathf.Sin(time) * 4) + 1.0f;
		alpha = Mathf.Abs(alpha) / 2.0f;

		Color color = graphic.color;
		color.a = alpha;
		graphic.color = color;
	}
}
