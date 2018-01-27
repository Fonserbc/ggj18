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

	Button AddOption(string text, UnityAction action)
	{
		GameObject option = Instantiate(OptionPrefab);
		option.name = text.Replace(" ", "");
		option.transform.SetParent(OptionContainer.transform, false);
		Button optionButton = option.GetComponent<Button>();
		Text optionText = option.transform.Find("Text").GetComponent<Text>();
		optionButton.onClick.AddListener(action);
		CancelEventHandler optionCancel = optionButton.gameObject.AddComponent<CancelEventHandler>();
		optionCancel.onCancel.AddListener(OnCancel);
		optionText.text = text;

		return optionButton;
	}

	void Awake ()
	{
		buttonPlay = AddOption("Play", OnPlaySelected);
		buttonHowToPlay = AddOption("How to play", OnHowToPlaySelected);
		buttonCredits = AddOption("Credits", OnCreditsSelected);
		buttonExit = AddOption("Exit", OnExitSelected);
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
		Debug.Log("OnConnectSelected");
	}

	void OnCreditsSelected()
	{
		Debug.Log("OnHostSelected");
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
