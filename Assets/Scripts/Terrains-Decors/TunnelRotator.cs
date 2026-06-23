using UnityEngine;

public class TunnelRotator : MonoBehaviour
{
    [Header("Configuration de la Rotation")]
    private float rotationSpeed = 30f;
    private bool alternateDirection = true;

    private Transform[] tunnelParts;

    void Start()
    {
        int childCount = transform.childCount;
        tunnelParts = new Transform[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            tunnelParts[i] = transform.GetChild(i);
        }
    }

    void Update()
    {
        if (tunnelParts == null) return;

        for (int i = 0; i < tunnelParts.Length; i++)
        {
            if (tunnelParts[i] == null) continue;

            float direction = 1f; 

            if (alternateDirection && i % 2 == 0)
            {
                direction = -1f;
            }

            tunnelParts[i].Rotate(0f, 0f, rotationSpeed * direction * Time.deltaTime, Space.Self);
        }
    }
}