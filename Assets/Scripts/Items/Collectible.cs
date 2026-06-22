using UnityEngine;

public class Collectible : MonoBehaviour
{
    public int collectibleIndex;
    public float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.CollectItem(collectibleIndex);
            }
            Destroy(gameObject);
        }
    }
}