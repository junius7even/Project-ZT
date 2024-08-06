using UnityEngine;
using UnityEngine.EventSystems;

namespace ZetanStudio.Examples
{
    using UI;

    public sealed class UIRoot : SingletonMonoBehaviour<UIRoot>
    {
        [SerializeField]
        private CanvasGroup[] layers;

        public static Transform BottomLayer => Instance ? Instance.layers[0].transform : null;
        public static Transform WindowsLayer => Instance ? Instance.layers[1].transform : null;

        private int[] stacks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateSingleton()
        {
            if (!Instance) DontDestroyOnLoad(Instantiate(GameEntrySettings.Instance.UIRootPrefab));
            if (FindObjectOfType<EventSystem>() is not EventSystem system)
                system = new GameObject(typeof(EventSystem).Name, typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
            DontDestroyOnLoad(system);
        }

        private void Awake()
        {
            if (layers.Length > 0 && layers[1])
                WindowManager.OnHideAll += hide => HideLayer(0, hide);
            if (layers.Length > 1 && layers[1])
                WindowManager.WindowsContainer = layers[1].transform;
            stacks = new int[layers.Length];
        }

        public static void HideLayer(int index, bool hide)
        {
            try
            {
                if (hide)
                {
                    Instance.stacks[index]++;
                    Instance.layers[index].alpha = 0;
                    Instance.layers[index].blocksRaycasts = false;
                }
                else
                {
                    if (Instance.stacks[index] > 0) Instance.stacks[index]--;
                    if (Instance.stacks[index] < 1)
                    {
                        Instance.layers[index].alpha = 1;
                        Instance.layers[index].blocksRaycasts = true;
                    }
                }
            }
            catch { }
        }
    }
}