namespace ZetanStudio.Examples
{
    using DialogueSystem;
    using DialogueSystem.UI;

    [Group("特殊"), Name("确认以继续"), Width(60f)]
    public class ConfirmToContinue : DialogueNode, ISoloMainOptionNode, IManualNode
    {
        public override bool IsValid => true;

        public ConfirmToContinue()
        {
            options = new DialogueOption[] { DialogueOption.Main };
        }

        public void DoManual(DialogueHandler handler)
        {
            DialogueWindow window = handler.window as DialogueWindow;
            var win = ConfirmWindow.StartConfirm(L.Tr("Common", "确认继续对话？"), () => handler.ContinueWith(this[0].Next));
            if (win)
            {
                win.OnClosed += () => window.Hide(false);
                window.Hide(true);
            }
        }
    }
}