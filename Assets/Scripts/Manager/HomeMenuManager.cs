using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeMenuManager : MonoBehaviour
{
    [Header("Références")]
    public GameObject EixoWrapper;

    [Header("Paramètres de pulsation")]
    private float vitessePulsation = 0.75f;
    [Range(0f, 1f)] private float luminositeMin = 0.2f;
    [Range(0f, 1f)] private float luminositeMax = 1f;

    private RawImage[] imagesEixo;
    private float[] decalagesAsynchrones;

    void Start()
    {
        if (EixoWrapper != null)
        {
            imagesEixo = EixoWrapper.GetComponentsInChildren<RawImage>();
            
            decalagesAsynchrones = new float[imagesEixo.Length];

            for (int i = 0; i < imagesEixo.Length; i++)
            {
                decalagesAsynchrones[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }
        else
        {
            Debug.LogWarning("Attention : EixoWrapper n'est pas assigné dans l'inspecteur !");
        }
    }

    void Update()
    {
        if (imagesEixo == null) return;

        for (int i = 0; i < imagesEixo.Length; i++)
        {
            if (imagesEixo[i] != null)
            {
                float ondeSinusoidale = (Mathf.Sin(Time.time * vitessePulsation + decalagesAsynchrones[i]) + 1f) / 2f;
                
                float alphaActuel = Mathf.Lerp(luminositeMin, luminositeMax, ondeSinusoidale);

                Color couleurImage = imagesEixo[i].color;
                couleurImage.a = alphaActuel;
                imagesEixo[i].color = couleurImage;
            }
        }
    }


    public void LoadInitLevel()
    {
        SceneManager.LoadScene("Init");
    }
}