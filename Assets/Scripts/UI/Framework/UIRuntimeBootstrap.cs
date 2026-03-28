using UnityEngine;

namespace Wuxing.UI
{
    public static class UIRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindObjectOfType<UIBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject("UIBootstrap");
            bootstrapObject.AddComponent<UIBootstrap>();
        }
    }
}
