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
}