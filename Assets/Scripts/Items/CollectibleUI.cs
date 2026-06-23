using UnityEngine;
using UnityEngine.UI;

public class CollectibleUI : MonoBehaviour
{
    public RawImage[] collectibleIcons;

    void Start()
    {
        foreach (var icon in collectibleIcons)
        {
            icon.color = new Color(1, 1, 1, 0.25f);
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnCollectiblePicked += UpdateUI;
            
            for (int i = 0; i < GameManager.instance.collectedItems.Length; i++)
            {
                if (GameManager.instance.collectedItems[i])
                {
                    UpdateUI(i);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnCollectiblePicked -= UpdateUI;
        }
    }

    private void UpdateUI(int index)
    {
        if (index >= 0 && index < collectibleIcons.Length)
        {
            collectibleIcons[index].color = new Color(1, 1, 1, 1f);
        }
    }
}