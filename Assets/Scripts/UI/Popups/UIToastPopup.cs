using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UIToastPopup : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Show(string message, float duration)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            StartCoroutine(Play(duration));
        }

        private IEnumerator Play(float duration)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                var fadeIn = 0.12f;
                var timer = 0f;
                while (timer < fadeIn)
                {
                    timer += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(timer / fadeIn);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(duration);

            if (canvasGroup != null)
            {
                var fadeOut = 0.18f;
                var timer = 0f;
                var startAlpha = canvasGroup.alpha;
                while (timer < fadeOut)
                {
                    timer += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOut);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}

