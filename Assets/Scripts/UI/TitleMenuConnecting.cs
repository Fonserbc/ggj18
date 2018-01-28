using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleMenuConnecting : MonoBehaviour
{
	public TitleMenuController MenuController;

	Text ConnectingText;
	Button button;
	CancelEventHandler Cancel;

	string Address = null;

	void Start ()
	{
		ConnectingText = transform.Find("Title").GetComponent<Text>();

		button = GetComponent<Button>();
		Cancel = gameObject.GetComponent<CancelEventHandler>();
		Cancel.onCancel.AddListener(GoBack);
	}

	private void GoBack()
	{
		NetworkManager.Instance.Deinit();
		MenuController.GoToPlayMenu();
	}
	
	public void Connect(string add)
	{
		NetworkInitParameters prms = new NetworkInitParameters();
		prms.address = add;
		prms.op_id = NetworkInitParameters.Operation.CLIENT;
		prms.port = 8888;

		NetworkManager.Instance.Init(prms);

		Address = add;
	}

	public void Host()
	{
		NetworkInitParameters prms = new NetworkInitParameters();
		prms.address = "";
		prms.op_id = NetworkInitParameters.Operation.HOST;
		prms.port = 8888;

		NetworkManager.Instance.Init(prms);
		Address = null;
	}

	void Update ()
	{
		if (button != null)
		{
			bool selected = EventSystem.current.currentSelectedGameObject == button;
			if (!selected)
				button.Select();
		}

		if (Address != null)
		{
			ConnectingText.text = "Connecting to " + Address;
		}
		else
		{
			ConnectingText.text = "Waiting for players";
		}

		int points = 1 + ((int) Time.unscaledTime % 3);
		for (int i = 0; i < points; i++)
		{
			ConnectingText.text += ".";
		}
	}
}
