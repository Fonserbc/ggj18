using UnityEngine;

[CreateAssetMenu(fileName = "Constantes", menuName = "Constantes de juego")]
public class Constants : ScriptableObject {
    public float fixedDeltaTime = 1f / 32f;
    public int numPlayers = 2;
    public float antenaActivationRadius = 1f;
    public float antenaCollisionRadius = 0.5f;
    public float antenaLinkMaxRadius = 10f;
    public float playerCollisionRadius = 0.5f;
    public float playerSpeed = 2f;
    public float stunnedtime = 1f;
    public float invincibilityTime = 2f;
    public float messageTransmissionTime = 2f;
    public float timeBetweenMessages = 10f;
    public int numMessages = 16;
    public Color[] antennaColors = {
        Color.white, Color.yellow, Color.green, Color.blue, Color.red
    };

    public Color[] connectionColors = {
        Color.yellow, Color.green, Color.blue, Color.red
    };
}
