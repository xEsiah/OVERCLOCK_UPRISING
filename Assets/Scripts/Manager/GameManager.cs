using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject player;
    public GameObject targetCam;
    public GameObject mainCamera;
    public GameObject freeLookCamera;
    public GameObject globalVolume;
    public GameObject UI;

    public int deathCount = 0;
    public event Action<int> OnDeathCountChanged;

    public bool[] collectedItems = new bool[3];
    public event Action<int> OnCollectiblePicked;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);

            if (player != null) DontDestroyOnLoad(player);
            if (targetCam != null) DontDestroyOnLoad(targetCam);
            if (mainCamera != null) DontDestroyOnLoad(mainCamera);
            if (freeLookCamera != null) DontDestroyOnLoad(freeLookCamera);
            if (globalVolume != null) DontDestroyOnLoad(globalVolume);
            if (UI != null) DontDestroyOnLoad(UI);
        }
        else
        {
            Destroy(gameObject);
            
            if (player != null) Destroy(player);
            if (targetCam != null) Destroy(targetCam);
            if (mainCamera != null) Destroy(mainCamera);
            if (freeLookCamera != null) Destroy(freeLookCamera);
            if (globalVolume != null) Destroy(globalVolume);
            if (UI != null) Destroy(UI);
        }
    }

    public void RegisterDeath()
    {
        deathCount++;
        OnDeathCountChanged?.Invoke(deathCount);
    }

    public void CollectItem(int index)
    {
        if (index >= 0 && index < collectedItems.Length && !collectedItems[index])
        {
            collectedItems[index] = true;
            ApplyUpgrades(index);
            OnCollectiblePicked?.Invoke(index);
        }
    }

    private void ApplyUpgrades(int index)
    {
        if (player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        Attack attack = player.GetComponent<Attack>();

        switch (index)
        {
            case 0:
                if (pc != null) pc.jumpForce = 6.5f;
                if (pc != null) pc.runSpeed = 15f;
                break;
            case 1:
                if (attack != null)
                {
                    attack.attackRange = 1.75f;
                    attack.attackCooldown = 1.25f;
                }
                break;
            case 2:
                if (pc != null) pc.jumpForce = 8.5f;
                if (pc != null) pc.runSpeed = 15f;
                if (attack != null) attack.pushForce = 20f;
                break;
        }
    }
}