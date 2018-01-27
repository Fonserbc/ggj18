using UnityEngine;

[CreateAssetMenu(fileName = "Stones", menuName = "Stones de juego")]
public class StoneHolder : ScriptableObject
{
    public GameObject[] stones;
    public Material stoneMat;
}
