namespace ZetanStudio.Examples
{
    using DialogueSystem;
    using DialogueSystem.UI;

    [Name("确认以分叉"), Width(60f)]
    [Description("确认或取消以开始不同的分支")]
    public class ConfirmToBifurcate : BifurcationNode, IManualNode
    {
        public override bool IsValid => true;

        public override bool CheckIsDone(DialogueData data)
        {
            if (data.AdditionalData.TryReadBool("confirm", out var value))
                return data.Accessed && (value ? data[First].IsDone : data[Second].IsDone);
            else return false;
        }


        public void DoManual(DialogueHandler handler)
        {
            DialogueWindow window = handler.window as DialogueWindow;
            var win = ConfirmWindow.StartConfirm(L.Tr("Common", "确认或取消以开始不同的分支"), () =>
            {
                handler.CurrentEntryData[this].AdditionalData["confirm"] = true;
                handler.ContinueWith(First);
            }, () =>
            {
                handler.CurrentEntryData[this].AdditionalData["confirm"] = false;
                handler.ContinueWith(Second);
            });
            if (win)
            {
                window.Hide(true);
                win.OnClosed += () => window.Hide(false);
            }
        }
    }
}