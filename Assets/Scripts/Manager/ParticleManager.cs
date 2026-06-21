using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager instance;

    public GameObject DisintegrationParticles;
    public GameObject IntegrationParticles;
    public GameObject DodgeParticles;
    
    public GameObject EnemyIntegrationParticles;
    public GameObject EnemyDisintegrationParticles;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public GameObject SpawnParticle(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float lifetime = 4f)
    {
        if (prefab == null) return null;
        
        GameObject obj = Instantiate(prefab, position, rotation, parent);
        if (lifetime > 0f) Destroy(obj, lifetime);
        
        return obj;
    }
}