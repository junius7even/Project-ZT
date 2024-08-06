using System.Collections.ObjectModel;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 把上一个结点的选项外置在此处，可用于自动打乱选项等功能。要注意的是，它不能拥有主要选项<br/>
    /// Externalizes previous node's options here, which can be used for functions such as random order options. What to note is, it cannot own main option.
    /// </summary>
    [Group("外置选项")]
    public abstract class ExternalOptionsNode : DialogueNode
    {
        /// <summary>
        /// 获取实际的选项<br/>
        /// Get the actual options.
        /// </summary>
        public abstract ReadOnlyCollection<DialogueOption> GetOptions(DialogueData entryData, DialogueNode owner);

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option)
        {
            return from is SentenceNode or OtherDialogueNode && option.IsMain;
        }
#endif
    }
}