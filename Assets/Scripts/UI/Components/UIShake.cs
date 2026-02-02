using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    public IEnumerator ShakeRect(RectTransform rect, float duration, float strength)
    {
        if (rect == null || duration <= 0f || strength <= 0f)
            yield break;

        Vector2 original = rect.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            Vector2 offset = Random.insideUnitCircle * strength; // strength in pixels
            rect.anchoredPosition = original + offset;

            yield return null;
        }

        rect.anchoredPosition = original; // restore
    }
}