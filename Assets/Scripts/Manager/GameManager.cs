using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Objets à conserver (DontDestroyOnLoad)")]
    public GameObject player;
    public GameObject targetCam;
    public GameObject mainCamera;
    public GameObject freeLookCamera;
    public GameObject globalVolume;
    public GameObject UI;

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
}