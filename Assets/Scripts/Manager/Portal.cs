using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public enum PortalType { LocalTeleport, SceneTransition }

    [Header("Type de Portail")]
    public PortalType type;

    [Header("Configuration : Téléportation Locale")]
    [Tooltip("Glisse ici un Empty GameObject placé là où le joueur doit réapparaître")]
    public Transform localDestination;

    [Header("Configuration : Changement de Scène")]
    [Tooltip("Le nom exact de la scène à charger")]
    public string sceneToLoad;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (type == PortalType.LocalTeleport)
            {
                TeleportLocally(other.gameObject);
            }
            else if (type == PortalType.SceneTransition)
            {
                LoadNextLevel();
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
        else
        {
            Debug.LogWarning("Attention : Aucune destination assignée sur le portail " + gameObject.name);
        }
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Attention : Aucun nom de scène configuré sur " + gameObject.name);
        }
    }
}