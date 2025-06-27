using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupLabelFader : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    public GameObject backgroundObject; // assign this in inspector

    private void SetBackgroundVisible(bool visible)
    {
        if (backgroundObject != null)
            backgroundObject.SetActive(visible);
    }

    public void SetText(string text)
    {
        textMesh.text = text;
        bool hasText = !string.IsNullOrWhiteSpace(text);

        if (hasText)
        {
            gameObject.SetActive(true);
            SetBackgroundVisible(true);
            FadeIn();
        }
        else
        {
            SetBackgroundVisible(false);
            FadeOut();
        }
    }


    public void FadeIn()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeTo(1f));
    }

    public void FadeOut()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeTo(0f));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
            gameObject.SetActive(false);
    }
}



