﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TitleMenuPlayController : MonoBehaviour
{
	public TitleMenuController MenuController;
	public GameObject OptionContainer;
	public GameObject OptionPrefab;

	private Button buttonLocal;
	private Button buttonConnect;
	private Button buttonHost;
	private Button buttonBack;

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
		buttonLocal = AddOption("Local game", OnLocalSelected);
		buttonConnect = AddOption("Connect host", OnConnectSelected);
		buttonHost = AddOption("Host Game", OnHostSelected);
		buttonBack = AddOption("Back", OnBackSelected);
	}

	public void OnEnable()
	{
		buttonLocal.Select();
	}


	void OnLocalSelected()
	{
		Debug.Log("OnLocalSelected");
	}

	void OnConnectSelected()
	{
		MenuController.GoToEnterIP();
	}

	void OnHostSelected()
	{
		Debug.Log("OnHostSelected");
	}

	void OnBackSelected()
	{
		MenuController.GoToMainMenu();
	}

	void OnCancel()
	{
		OnBackSelected();
	}
}
