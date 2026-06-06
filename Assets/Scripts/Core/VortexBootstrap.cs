using UnityEngine;

namespace VortexGame.Core
{
    public static class VortexBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindObjectOfType<VortexGameManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("VortexGameRuntime");
            root.AddComponent<VortexGameManager>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
