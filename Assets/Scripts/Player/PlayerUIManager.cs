using UnityEngine;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [Header("References")]
    public PlayerStateManager stateManager;

    [Header("UI Elements")]
    public TextMeshProUGUI actionText;

    void OnEnable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateChanged += HandleStateChanged;
            stateManager.OnLedgeAvailable += HandleLedgeAvailable;
        }
        
        actionText.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateChanged -= HandleStateChanged;
            stateManager.OnLedgeAvailable -= HandleLedgeAvailable;
        }
    }

    private void HandleLedgeAvailable(bool available)
    {
        if (stateManager.CurrentState == PlayerState.Default)
        {
            if (available)
            {
                actionText.gameObject.SetActive(true);
                actionText.text = "[E] Hang";
            }
            else
            {
                actionText.gameObject.SetActive(false);
            }
        }
    }

    private void HandleStateChanged(PlayerState newState)
    {
        actionText.gameObject.SetActive(false);

        switch (newState)
        {
            case PlayerState.Tutorial:
                actionText.gameObject.SetActive(true);
                actionText.text = "Jump [Space]\nSprint [L Shift]\nDash [2 tap Q/A|D]\nHang [E]\nWalk [ZQSD|WASD]";
                break;

            case PlayerState.Hanging:
                actionText.gameObject.SetActive(true);
                actionText.text = "[Espace] Climb\n[Q/A] Drop";
                break;

            case PlayerState.Mantling:
                actionText.gameObject.SetActive(false);
                break;

            case PlayerState.Default:
                actionText.gameObject.SetActive(false);
                break;
        }
    }
}