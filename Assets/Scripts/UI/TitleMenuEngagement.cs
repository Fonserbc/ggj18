using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuEngagement : MonoBehaviour
{
	public TitleMenuController MenuController;
	private Button anyButton;

	void Awake()
	{
		anyButton = GetComponent<Button>();
		anyButton.onClick.AddListener(PressedAnyButton);
	}

	void OnEnable()
	{
		anyButton.Select();
	}

	void PressedAnyButton()
	{
		MenuController.GoToMainMenu();
	}
}
