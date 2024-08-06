using System.Collections.ObjectModel;

namespace ZetanStudio.DialogueSystem
{
    [Name("还原选项"), Width(60f)]
    [Description("用于还原因需要使用覆盖器而无法放置选项的语句结点应有的选项。")]
    public sealed class RevertOptions : ExternalOptionsNode
    {
        public override bool IsValid => true;

        public override ReadOnlyCollection<DialogueOption> GetOptions(DialogueData entryData, DialogueNode owner) => Options;

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option) => from is PortraitVoiceOverrideNode;
#endif
    }
}