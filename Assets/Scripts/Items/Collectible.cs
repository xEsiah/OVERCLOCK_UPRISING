using UnityEngine;

public class Collectible : MonoBehaviour
{
    public int collectibleIndex;
    public float rotationSpeed = 90f;
    public int itemIndex;

    void OnEnable()
    {
        if (GameManager.instance != null)
        {
            if (GameManager.instance.collectedItems.Length > itemIndex && 
                GameManager.instance.collectedItems[itemIndex])
            {
                gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.instance.PlayCollectedItem();
            if (GameManager.instance != null)
            {
                GameManager.instance.CollectItem(collectibleIndex);
            }
            Destroy(gameObject);
        }
    }
}