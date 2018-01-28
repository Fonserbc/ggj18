using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MenuPause : MonoBehaviour
{
	public GameObject OptionContainer;
	public GameObject OptionPrefab;

	private Button buttonContinue;
	private Button buttonTitle;

	private Button AddOption(string name, UnityAction onClick)
	{
		return TitleMenuHelper.AddOption(name, onClick, OnCancel, OptionContainer, Instantiate(OptionPrefab));
	}

	void Awake()
	{
		Button[] listButton = new Button[2];
		listButton[0] = buttonContinue = AddOption("Continue", OnContinue);
		listButton[1] = buttonTitle = AddOption("To Title Menu", OnTitleMenu);

		TitleMenuHelper.BuildNavigation(listButton);
	}

	public void OnEnable()
	{
		buttonContinue.Select();
	}

	void OnContinue()
	{
		gameObject.SetActive(false);
	}

	void OnTitleMenu()
	{
		NetworkManager.Instance.Deinit();
	}

	void OnCancel()
	{
		OnContinue();
	}
}