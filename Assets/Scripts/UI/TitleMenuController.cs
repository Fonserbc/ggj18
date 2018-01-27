using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleMenuController : MonoBehaviour
{
	public TitleMenuEngagement Engagement;
	public TitleMenuMainMenuController MainMenuController;
	public TitleMenuPlayController PlayController;
	public TitleMenuEnterIP EnterIP;
	private GameObject currentlyFocus;

	private List<GameObject> screens = new List<GameObject>();

	private void Start()
	{
		screens.Add(Engagement.gameObject);
		screens.Add(MainMenuController.gameObject);
		screens.Add(PlayController.gameObject);
		screens.Add(EnterIP.gameObject);

		Engagement.MenuController = this;
		MainMenuController.MenuController = this;
		PlayController.MenuController = this;
		EnterIP.MenuController = this;

		GoToEngagement();
	}

	public void GoToEngagement()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		Engagement.gameObject.SetActive(true);
	}

	public void GoToMainMenu()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		MainMenuController.gameObject.SetActive(true);
	}

	public void GoToPlayMenu()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		PlayController.gameObject.SetActive(true);
	}

	public void GoToEnterIP()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		EnterIP.gameObject.SetActive(true);
	}
}
