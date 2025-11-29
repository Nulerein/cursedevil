using UnityEngine;
using TMPro;
using System.Collections;

public class ClickAnimator : MonoBehaviour
{
    public RectTransform canvasRect;           // ссылка на Canvas (RectTransform)
    public TextMeshProUGUI plusPrefab;         // префаб TextMeshProUGUI (с пустым текстом)
    public float rise = 60f;
    public float duration = 0.8f;
    public Vector2 randomOffset = new Vector2(10f, 0f);
    public Camera renderCamera;               // можно назначить в инспекторе, если null — будет Camera.main

    void Awake()
    {
        if (renderCamera == null)
            renderCamera = Camera.main;
        if (canvasRect == null)
            Debug.LogWarning("ClickAnimator: canvasRect не назначен.");
        if (plusPrefab == null)
            Debug.LogWarning("ClickAnimator: plusPrefab не назначен.");
    }

    public void Play(string text, Vector3 worldPosition, RectTransform popTarget = null)
    {
        if (canvasRect == null || plusPrefab == null)
        {
            Debug.LogWarning("ClickAnimator.Play: пропущено (canvasRect или plusPrefab == null).");
            return;
        }
        // создаём копию в Canvas
        var go = Instantiate(plusPrefab, canvasRect);
        go.text = text;
        // Гарантируем начальный альфа = 1 и нормальный масштаб
        var startCol = go.color;
        startCol.a = 1f;
        go.color = startCol;
        go.rectTransform.localScale = Vector3.one;

        var rt = go.rectTransform;

        // переводим мировую позицию в локальную координату Canvas
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, worldPosition);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, renderCamera, out localPoint);
        rt.anchoredPosition = localPoint + new Vector2(Random.Range(-randomOffset.x, randomOffset.x), Random.Range(-randomOffset.y, randomOffset.y));

        StartCoroutine(AnimatePlus(rt, go));
        if (popTarget != null) StartCoroutine(PopButton(popTarget));
    }

    IEnumerator AnimatePlus(RectTransform rt, TextMeshProUGUI tmp)
    {
        float t = 0f;
        Color startCol = tmp.color;
        Vector2 startPos = rt.anchoredPosition;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            rt.anchoredPosition = Vector2.Lerp(startPos, startPos + Vector2.up * rise, k);
            float alpha = Mathf.Lerp(startCol.a, 0f, k);
            tmp.color = new Color(startCol.r, startCol.g, startCol.b, alpha);
            float scale = 1f + 0.25f * Mathf.Sin(k * Mathf.PI); // лёгкий пульс
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    IEnumerator PopButton(RectTransform target)
    {
        Vector3 orig = target.localScale;
        float popDur = 0.12f;
        float t = 0f;
        // вверх
        while (t < popDur)
        {
            t += Time.deltaTime;
            float k = t / popDur;
            target.localScale = Vector3.Lerp(orig, orig * 1.15f, Mathf.Sin(k * Mathf.PI * 0.5f));
            yield return null;
        }
        // назад
        t = 0f;
        while (t < popDur)
        {
            t += Time.deltaTime;
            float k = t / popDur;
            target.localScale = Vector3.Lerp(orig * 1.15f, orig, k);
            yield return null;
        }
        target.localScale = orig;
    }
}