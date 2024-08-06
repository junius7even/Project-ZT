using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;
    using DialogueSystem.UI;
    using UI;

    [Name("打开窗口")]
    public class OpenWindow : SuffixNode, IManualNode
    {
        [field: SerializeField, TypeSelector(typeof(Window))]
        public string Type { get; private set; }

        public override bool IsValid => !string.IsNullOrEmpty(Type);

        public void DoManual(DialogueHandler handler)
        {
            DialogueWindow window = handler.window as DialogueWindow;
            var win = WindowManager.OpenWindow(Type);
            if (win)
            {
                win.OnClosed += () => window.Hide(false);
                window.Hide(true);
            }
        }
    }
}
