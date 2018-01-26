using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TitleMenuPlayController : MonoBehaviour
{
	public GameObject OptionContainer;
	public GameObject OptionPrefab;

	Button AddOption(string text, UnityAction action)
	{
		GameObject option = Instantiate(OptionPrefab);
		option.transform.SetParent(OptionContainer.transform, false);
		Button optionButton = option.GetComponent<Button>();
		Text optionText = option.transform.Find("Text").GetComponent<Text>();
		optionButton.onClick.AddListener(action);
		optionText.text = text;

		return optionButton;
	}

	void Start ()
	{
		AddOption("Local game", OnLocalSelected).Select();
		AddOption("Connect host", OnConnectSelected);
		AddOption("Host Game", OnHostSelected);
		AddOption("Back", OnBackSelected);
	}

	void OnLocalSelected()
	{
		Debug.Log("OnLocalSelected");
	}

	void OnConnectSelected()
	{
		Debug.Log("OnConnectSelected");
	}

	void OnHostSelected()
	{
		Debug.Log("OnHostSelected");
	}

	void OnBackSelected()
	{
		Debug.Log("OnBackSelected");
	}
}
