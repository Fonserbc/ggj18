using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleMenuController : MonoBehaviour
{
	public TitleMenuEngagement Engagement;
	public TitleMenuMainMenuController MainMenuController;
	public TitleMenuPlayController PlayController;
	public TitleMenuEnterIP EnterIP;
	public TitleMenuConnecting Connecting;
	public TitleMenuGenericBack HowToPlay;
	public TitleMenuGenericBack Credits;

	private GameObject currentlyFocus;

	private List<GameObject> screens = new List<GameObject>();

	private void Start()
	{
		screens.Add(Engagement.gameObject);
		screens.Add(MainMenuController.gameObject);
		screens.Add(PlayController.gameObject);
		screens.Add(EnterIP.gameObject);
		screens.Add(Connecting.gameObject);
		screens.Add(HowToPlay.gameObject);
		screens.Add(Credits.gameObject);

		Engagement.MenuController = this;
		MainMenuController.MenuController = this;
		PlayController.MenuController = this;
		EnterIP.MenuController = this;
		Connecting.MenuController = this;
		HowToPlay.MenuController = this;
		Credits.MenuController = this;

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

	public void GoToHost()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		Connecting.gameObject.SetActive(true);
		Connecting.Host();
	}

	public void GoToConnecting(string address)
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		Connecting.gameObject.SetActive(true);
		Connecting.Connect(address);
	}

	public void GoToHowToPlay()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		HowToPlay.gameObject.SetActive(true);
	}

	public void GoToCredits()
	{
		foreach (GameObject screen in screens)
			screen.SetActive(false);

		Credits.gameObject.SetActive(true);
	}
}
