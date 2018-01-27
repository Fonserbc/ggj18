using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuConnecting : MonoBehaviour
{
	public TitleMenuController MenuController;

	Text ConnectingText;
	CancelEventHandler Cancel;

	string Address = null;

	void Start ()
	{
		ConnectingText = transform.Find("Title").GetComponent<Text>();
		Cancel = gameObject.AddComponent<CancelEventHandler>();
		Cancel.onCancel.AddListener(GoBack);
	}

	private void GoBack()
	{
		NetworkInitParameters prms = new NetworkInitParameters();
		prms.op_id = NetworkInitParameters.Operation.NONE;
		NetworkManager.Instance.Init(prms);

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
