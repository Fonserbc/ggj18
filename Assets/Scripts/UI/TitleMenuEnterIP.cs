using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuEnterIP : MonoBehaviour
{
	public TitleMenuController MenuController;
	public InputField EnterField;

	void OnEnable()
	{
		if (PlayerPrefs.HasKey("AddressField"))
		{
			string savedAddress = PlayerPrefs.GetString("AddressField");
			EnterField.text = savedAddress;
		}

		EnterField.Select();
		EnterField.onEndEdit.AddListener(OnEndEdit);

		CancelEventHandler optionCancel = EnterField.gameObject.AddComponent<CancelEventHandler>();
		optionCancel.onCancel.AddListener(OnCancel);
	}

	void OnEndEdit(string address)
	{
		PlayerPrefs.SetString("AddressField", address);
		PlayerPrefs.Save();

		MenuController.GoToConnecting(address);
	}

	void OnCancel()
	{
		MenuController.GoToPlayMenu();
	}
}
