using UnityEngine;

namespace ZetanStudio.Examples
{
    using UI;

    public class GameEntrySettings : SingletonScriptableObject<GameEntrySettings>
    {
        [field: SerializeField]
        public UIRoot UIRootPrefab { get; private set; }

        [field: SerializeField]
        public int DefaultLanguage { get; private set; }

        [field: SerializeField]
        public Localization Localization { get; private set; }

        [field: SerializeField]
        public WindowPrefabs WindowPrefabs { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void PresetSingletons()
        {
            Localization.Instance = Instance ? Instance.Localization : null;
            WindowPrefabs.Instance = Instance ? Instance.WindowPrefabs : null;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Zetan Studio/游戏启动设置 (Game Entry Settings)")]
        private static void Create()
        {
            CreateSingleton();
        }
#endif
    }
}
