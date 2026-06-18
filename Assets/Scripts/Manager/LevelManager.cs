using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Configuration du Spawn")]
    [Tooltip("Le point où le joueur commence le niveau et réapparaît s'il tombe")]
    public Transform spawnPoint;

    [Header("Sécurité Chute (Le Vide)")]
    [Tooltip("La limite en dessous de laquelle le joueur est téléporté au spawn")]
    public float fallThreshold = -10f;

    private GameObject player;
    private CharacterController playerCc;

    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            player = GameManager.instance.player;
            playerCc = player.GetComponent<CharacterController>();

            RespawnPlayer();
        }
    }

    void Update()
    {
        if (player != null)
        {
            if (player.transform.position.y < fallThreshold)
            {
                RespawnPlayer();
            }
        }
    }

    public void RespawnPlayer()
    {
        if (spawnPoint != null && player != null)
        {
            if (playerCc != null) playerCc.enabled = false;

            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            if (playerCc != null) playerCc.enabled = true;
        }
        else
        {
            Debug.LogWarning("Configuration incomplète sur le LevelManager de cette scène !");
        }
    }
}