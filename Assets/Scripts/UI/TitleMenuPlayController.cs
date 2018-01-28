using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

static public class TitleMenuHelper
{
	public static Button AddOption(string text, UnityAction onClick, UnityAction onCancel, GameObject OptionContainer, GameObject option)
	{
		option.name = text.Replace(" ", "");
		option.transform.SetParent(OptionContainer.transform, false);
		Button optionButton = option.GetComponent<Button>();
		Text optionText = option.transform.Find("Text").GetComponent<Text>();
		optionText.text = text;
		optionButton.onClick.AddListener(onClick);

		CancelEventHandler optionCancel = optionButton.gameObject.GetComponent<CancelEventHandler>();
		if (optionCancel != null)
		{
			optionCancel.onCancel.AddListener(onCancel);
		}

		return optionButton;
	}

	public static void BuildNavigation(Button[] listButton)
	{
		for (int i = 0; i < listButton.Length; i++)
		{
			int next_i = (i + 1) % listButton.Length;
			Button next = listButton[next_i];
			Button curr = listButton[i];

			Navigation currNav = curr.navigation;
			Navigation nextNav = next.navigation;

			currNav.mode = Navigation.Mode.Explicit;
			currNav.selectOnLeft = null;
			currNav.selectOnRight = null;
			currNav.selectOnDown = next;
			nextNav.selectOnUp = curr;

			curr.navigation = currNav;
			next.navigation = nextNav;
		}
	}
}

public class TitleMenuPlayController : MonoBehaviour
{
	public TitleMenuController MenuController;
	public GameObject OptionContainer;
	public GameObject OptionPrefab;

	private Button buttonLocal;
	private Button buttonConnect;
	private Button buttonHost;
	private Button buttonBack;

	private Button AddOption(string name, UnityAction onClick)
	{
		return TitleMenuHelper.AddOption(name, onClick, OnCancel, OptionContainer, Instantiate(OptionPrefab));
	}

	void Awake ()
	{
		Button[] listButton = new Button[4];
		listButton[0] = buttonLocal = AddOption("Local game", OnLocalSelected);
		listButton[1] = buttonConnect = AddOption("Connect host", OnConnectSelected);
		listButton[2] = buttonHost = AddOption("Host Game", OnHostSelected);
		listButton[3] = buttonBack = AddOption("Back", OnBackSelected);

		TitleMenuHelper.BuildNavigation(listButton);
	}

	public void OnEnable()
	{
		buttonLocal.Select();
	}

	void OnLocalSelected()
	{
		NetworkInitParameters prms = new NetworkInitParameters();
		prms.op_id = NetworkInitParameters.Operation.NONE;
		NetworkManager.Instance.Init(prms);

		MenuController.GoToPlayMenu();
	}

	void OnConnectSelected()
	{
		MenuController.GoToEnterIP();
	}

	void OnHostSelected()
	{
		MenuController.GoToHost();
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
