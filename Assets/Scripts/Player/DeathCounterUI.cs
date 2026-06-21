using UnityEngine;
using TMPro;

public class DeathCounterUI : MonoBehaviour
{
    public TextMeshProUGUI deathText;

    void Start()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDeathCountChanged += UpdateUI;
            UpdateUI(GameManager.instance.deathCount);
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDeathCountChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int count)
    {
        if (deathText != null)
        {
            deathText.text = "Deaths : " + count;
        }
    }
}