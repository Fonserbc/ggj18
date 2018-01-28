using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TitleMenuMainMenuController : MonoBehaviour
{
	public TitleMenuController MenuController;
	public GameObject OptionContainer;
	public GameObject OptionPrefab;

	private Button buttonPlay;
	private Button buttonHowToPlay;
	private Button buttonCredits;
	private Button buttonExit;

	private Button AddOption(string name, UnityAction onClick)
	{
		return TitleMenuHelper.AddOption(name, onClick, OnCancel, OptionContainer, Instantiate(OptionPrefab));
	}

	void Awake ()
	{
		Button[] listButton = new Button[4];
		listButton[0] = buttonPlay = AddOption("Play", OnPlaySelected);
		listButton[1] = buttonHowToPlay = AddOption("How to play", OnHowToPlaySelected);
		listButton[2] = buttonCredits = AddOption("Credits", OnCreditsSelected);
		listButton[3] = buttonExit = AddOption("Exit", OnExitSelected);

		TitleMenuHelper.BuildNavigation(listButton);
	}

	public void OnEnable()
	{
		buttonPlay.Select();
	}


	void OnPlaySelected()
	{
		MenuController.GoToPlayMenu();
	}

	void OnHowToPlaySelected()
	{
		MenuController.GoToHowToPlay();
	}

	void OnCreditsSelected()
	{
		MenuController.GoToCredits();
	}

	void OnExitSelected()
	{
		Application.Quit();
	}

	void OnCancel()
	{
		MenuController.GoToEngagement();
	}
}
