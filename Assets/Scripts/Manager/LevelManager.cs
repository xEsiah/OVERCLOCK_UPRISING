using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public Transform spawnPoint;
    public float fallThreshold = -10f;

    private GameObject player;
    private Rigidbody playerRb;
    private bool isRespawning;

    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            player = GameManager.instance.player;
            playerRb = player.GetComponent<Rigidbody>();
            RespawnPlayerImmediate();
        }
    }

    void Update()
    {
        if (player != null && !isRespawning)
        {
            if (player.transform.position.y < fallThreshold)
            {
                StartCoroutine(RespawnRoutine());
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        if (GameManager.instance != null)
        {
            GameManager.instance.RegisterDeath();
        }

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
        }

        ParticleManager.instance.SpawnParticle(ParticleManager.instance.DisintegrationParticles, player.transform.position, Quaternion.identity);

        player.SetActive(false);

        yield return new WaitForSeconds(1f);

        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        ParticleManager.instance.SpawnParticle(ParticleManager.instance.IntegrationParticles, spawnPoint.position, spawnPoint.rotation);

        player.SetActive(true);

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
        }

        isRespawning = false;
    }

    public void RespawnPlayerImmediate()
    {
        if (spawnPoint != null && player != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
            }
        }
    }
}