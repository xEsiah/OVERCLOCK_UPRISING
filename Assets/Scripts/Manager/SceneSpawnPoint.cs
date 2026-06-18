using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameObject player = GameManager.instance.player;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = transform.position;
            player.transform.rotation = transform.rotation;

            if (cc != null) cc.enabled = true;
        }
    }
}