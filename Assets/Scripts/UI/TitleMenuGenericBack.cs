using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleMenuGenericBack : MonoBehaviour
{
	public TitleMenuController MenuController;
	Button button;
	CancelEventHandler Cancel;

	void Start ()
	{
		button = GetComponent<Button>();
		Cancel = gameObject.GetComponent<CancelEventHandler>();
		Cancel.onCancel.AddListener(GoBack);
	}
	
	void Update ()
	{
		if (button != null)
		{
			bool selected = EventSystem.current.currentSelectedGameObject == button;
			if (!selected)
				button.Select();
		}
	}

	private void GoBack()
	{
		MenuController.GoToMainMenu();
	}
}
