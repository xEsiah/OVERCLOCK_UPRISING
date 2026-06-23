using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    void Start()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameObject player = GameManager.instance.player;
            Rigidbody rb = player.GetComponent<Rigidbody>();

            player.transform.position = transform.position;
            player.transform.rotation = transform.rotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}