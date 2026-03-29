using UnityEngine;
using UnityEngine.EventSystems;

namespace Wuxing.UI
{
    public class UIScrollFollowController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        public bool IsDragging { get; private set; }

        public bool AutoFollow { get; private set; } = true;

        public void ResetToAutoFollow()
        {
            IsDragging = false;
            AutoFollow = true;
        }

        public void DisableAutoFollow()
        {
            AutoFollow = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
            AutoFollow = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Mathf.Abs(eventData.scrollDelta.y) > 0f)
            {
                AutoFollow = false;
            }
        }
    }
}

