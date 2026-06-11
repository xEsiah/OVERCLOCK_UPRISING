using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position;
        }
    }
}