using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public enum PortalType { LocalTeleport, SceneTransition }

    [Header("Type de Portail")]
    public PortalType type;

    [Header("Configuration : Téléportation Locale")]
    public Transform localDestination;

    [Header("Configuration : Changement de Scène")]
    public string sceneToLoad;
    public string targetSpawnPointName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (type == PortalType.LocalTeleport)
            {
                TeleportLocally(other.gameObject);
                AudioManager.instance.PlayPortalLocalTeleport();
            }
            else if (type == PortalType.SceneTransition)
            {
                AudioManager.instance.PlayPortalSceneTransition();
                LoadLevel();
            }
        }
    }

    private void TeleportLocally(GameObject player)
    {
        if (localDestination != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = localDestination.position;
            player.transform.rotation = localDestination.rotation;

            if (cc != null) cc.enabled = true;
        }
    }

    private void LoadLevel()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.targetSpawnPointName = targetSpawnPointName;
            }
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}