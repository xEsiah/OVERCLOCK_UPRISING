using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject permanentItems;
    public GameObject player;

    public int deathCount = 0;
    public event Action<int> OnDeathCountChanged;

    public bool[] collectedItems = new bool[3];
    public event Action<int> OnCollectiblePicked;

    public string targetSpawnPointName;
        
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
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
        if (permanentItems == null) return;

        PlayerController pc = permanentItems.GetComponentInChildren<PlayerController>(true);
        Attack attack = permanentItems.GetComponentInChildren<Attack>(true);

        switch (index)
        {
            case 0:
                if (pc != null) pc.jumpForce = 6.5f;
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
                if (attack != null) attack.pushForce = 20f;
                break;
        }
    }
}