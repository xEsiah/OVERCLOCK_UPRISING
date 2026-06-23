using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIFade : MonoBehaviour
{
    public float fadeDuration = 1.5f;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (_canvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }
}