using UnityEngine;

namespace Wuxing.UI
{
    public class UICanvasRoot : MonoBehaviour
    {
        [SerializeField] private Transform pageLayer;
        [SerializeField] private Transform popupLayer;
        [SerializeField] private Transform toastLayer;

        public Transform PageLayer
        {
            get { return pageLayer; }
        }

        public Transform PopupLayer
        {
            get { return popupLayer; }
        }

        public Transform ToastLayer
        {
            get { return toastLayer; }
        }

        public void Bind(Transform pageLayerTransform, Transform popupLayerTransform, Transform toastLayerTransform)
        {
            pageLayer = pageLayerTransform;
            popupLayer = popupLayerTransform;
            toastLayer = toastLayerTransform;
        }
    }
}

