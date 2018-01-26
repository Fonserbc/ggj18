using UnityEngine;
using UnityEngine.SceneManagement;

public class Init : MonoBehaviour
{
	static private bool isInit = false;
	static public bool IsInit { get { return isInit; } }

	void Start ()
	{
		isInit = true;
		SceneManager.LoadScene("TitleMenu");
	}
}
