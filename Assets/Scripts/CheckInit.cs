using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckInit : MonoBehaviour
{
	void Start ()
	{
		if (!Init.IsInit)
		{
			SceneManager.LoadScene("Init");
		}
	}
}
