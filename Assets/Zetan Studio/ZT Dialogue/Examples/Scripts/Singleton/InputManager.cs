using UnityEngine;

namespace ZetanStudio.Examples
{
    using InteractionSystem.UI;
    using UI;
    using ZetanStudio.DialogueSystem.UI;

    [AddComponentMenu("")]
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateSingleton()
        {
            if (!Instance) DontDestroyOnLoad(new GameObject(typeof(InputManager).Name, typeof(InputManager)));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                if (WindowManager.Count > 0) WindowManager.CloseTop();
                else if(!Command.Instance.IsTyping) WindowManager.OpenWindow<SettingsWindow>();
            if (Input.GetKeyDown(KeyCode.F))
                if (WindowManager.IsWindowOpen<DialogueWindow>(out var window))
                    window.Next();
                else InteractionPanel.Interact();
            var wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel > 0)
            {
                if (InteractionPanel.CanScroll) InteractionPanel.Prev();
                else if (WindowManager.IsWindowOpen<DialogueWindow>(out var window)) window.PrevOption();
            }
            else if (wheel < 0)
                if (InteractionPanel.CanScroll) InteractionPanel.Next();
                else if (WindowManager.IsWindowOpen<DialogueWindow>(out var window)) window.NextOption();
        }
    }
}
