using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HomeMenuManager : MonoBehaviour
{
    [Header("Références")]
    public GameObject EixoWrapper;
    public CanvasGroup elementsToFade;

    [Header("Paramètres")]
    private float vitessePulsation = 1f;
    [Range(0f, 1f)] private float luminositeMin = 0.1f;
    [Range(0f, 1f)] private float luminositeMax = 1.5f;
    public float fadeDuration = 2.5f;

    private RawImage[] imagesEixo;
    private float[] decalagesAsynchrones;

    void Start()
    {
        if (elementsToFade != null) elementsToFade.alpha = 0f;
        AudioManager.instance.PlayMusic(AudioManager.instance.menuMusic);
        StartCoroutine(FadeInRoutine());

        if (GameManager.instance != null && GameManager.instance.permanentItems != null)
        {
            GameManager.instance.permanentItems.SetActive(false);
        }

        if (EixoWrapper != null)
        {
            imagesEixo = EixoWrapper.GetComponentsInChildren<RawImage>();
            
            decalagesAsynchrones = new float[imagesEixo.Length];

            for (int i = 0; i < imagesEixo.Length; i++)
            {
                decalagesAsynchrones[i] = Random.Range(0f, Mathf.PI * 2f);
            }
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

    private IEnumerator FadeInRoutine()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayAppearSound();

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            if (elementsToFade != null) elementsToFade.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
    }

    public void StartGame()
    {        
        StartCoroutine(PlaySoundAndStart());
    }

    private IEnumerator PlaySoundAndStart()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayStartSound();
            yield return new WaitForSeconds(AudioManager.instance.GetStartSoundLength());
        }

        if (GameManager.instance != null && GameManager.instance.permanentItems != null)
        {
            GameManager.instance.permanentItems.SetActive(true);
        }
        
        SceneManager.LoadScene("Init");
    }
}