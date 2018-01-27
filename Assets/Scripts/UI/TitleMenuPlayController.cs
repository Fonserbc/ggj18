using System.Collections;
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
		optionCancel.TargetGraphic = optionText;
		optionCancel.Selected = new Color(0.8f, 0.3f, 0.3f, 1.0f);
		optionCancel.Unselected = optionText.color;
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
