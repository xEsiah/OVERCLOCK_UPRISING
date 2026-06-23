using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    public Transform spawnPoint;
    public float fallThreshold = -10f;
    public AudioClip levelMusic;

    private GameObject player;
    private Rigidbody playerRb;
    private PlayerController playerC;
    private CharacterController playerCc;
    private bool isRespawning;
    private bool canFallDie = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (GameManager.instance != null && !string.IsNullOrEmpty(GameManager.instance.targetSpawnPointName))
        {
            GameObject dynamicSpawn = GameObject.Find(GameManager.instance.targetSpawnPointName);
            if (dynamicSpawn != null)
            {
                spawnPoint = dynamicSpawn.transform;
            }
            GameManager.instance.targetSpawnPointName = "";
        }

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            if (AudioManager.instance != null)
            {
                if (levelMusic != null) AudioManager.instance.PlayMusic(levelMusic);
                else AudioManager.instance.musicSource.Stop();
            }
            
            player = GameManager.instance.player;
            playerRb = player.GetComponent<Rigidbody>();
            playerC = player.GetComponent<PlayerController>();
            playerCc = player.GetComponent<CharacterController>();
            RespawnPlayerImmediate();
        }

        if (SceneManager.GetActiveScene().name == "Menu") 
        {
            AudioManager.instance.StartAmbientLoop(60f);
        }
        else
        {
            AudioManager.instance.StopAmbientLoop();
        }

        Invoke("EnableFallDeath", 0.5f);
    }

    private void EnableFallDeath()
    {
        canFallDie = true;
    }

    void Update()
    {
        if (player != null && !isRespawning && canFallDie)
        {
            if (player.transform.position.y < fallThreshold)
            {
                StartCoroutine(RespawnRoutine());
            }
        }
    }

    public void SetNewSpawnPoint(Transform newSpawn)
    {
        spawnPoint = newSpawn;
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

        if (AudioManager.instance != null) AudioManager.instance.PlayDisintegrationSound();
        ParticleManager.instance.SpawnParticle(ParticleManager.instance.DisintegrationParticles, player.transform.position, Quaternion.identity);

        player.SetActive(false);

        yield return new WaitForSeconds(1f);

        if (playerCc != null) playerCc.enabled = false;
        
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        if (playerCc != null) playerCc.enabled = true;

        if (AudioManager.instance != null) AudioManager.instance.PlayIntegrationSound();
        ParticleManager.instance.SpawnParticle(ParticleManager.instance.IntegrationParticles, spawnPoint.position, spawnPoint.rotation);

        if (playerC != null) playerC.ResetStates();

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
            if (playerCc != null) playerCc.enabled = false;
            player.SetActive(false);

            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;

            if (playerC != null) playerC.ResetStates();

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
            }

            player.SetActive(true);
            if (playerCc != null) playerCc.enabled = true;
        }
    }
}