using UnityEngine;

[CreateAssetMenu(fileName = "Constantes", menuName = "Constantes de juego")]
public class Constants : ScriptableObject {
    public float fixedDeltaTime = 1f / 32f;
    public int numPlayers = 2;
    public float antenaActivationRadius = 1f;
    public float antenaCollisionRadius = 0.5f;
    public float playerCollisionRadius = 0.5f;
    public float playerSpeed = 2f;
}
