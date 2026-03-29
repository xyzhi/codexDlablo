using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wuxing.UI
{
    public class UIToastPopup : MonoBehaviour
    {
        [SerializeField] private Text messageText;

        public void Show(string message, float duration)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            StartCoroutine(Play(duration));
        }

        private IEnumerator Play(float duration)
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}

