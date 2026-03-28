using UnityEngine;

namespace Wuxing.UI
{
    public abstract class UIPage : MonoBehaviour
    {
        public virtual void OnOpen(object data) { }

        public virtual void OnClose() { }
    }
}
