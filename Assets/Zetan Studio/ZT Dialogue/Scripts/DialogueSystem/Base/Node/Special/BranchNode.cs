using System.Linq;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 分支结点可以将第一个满足显示条件的分支作为前一个结点的下一句，若无可用分支，则前一个结点将被视为结束结点<br/>
    /// Branch node can make the first option that meet display conditions as the next node of its previous node, 
    /// if there's no available branch, then the previous node will be regarded as an end node.
    /// </summary>
    [Group("特殊"), Name("分支"), Width(60f)]
    [Description("以第一个满足条件的分支作为前一个结点的下一句，若无可用分支，则前一个结点将被视为结束结点。")]
    public sealed class BranchNode : DialogueNode
    {
        public override bool IsValid => options.Length > 0 && options.All(o => o.IsMain);

        /// <summary>
        /// 获取第一个满足显示条件的分支<br/>
        /// Get the first branch that meet its display conditions.
        /// </summary>
        public DialogueNode GetBranch(DialogueData entryData)
        {
            foreach (var option in options)
            {
                if (option?.Next is ConditionNode condition && condition.Check(entryData))
                {
                    var temp = condition.Options[0]?.Next;
                    while (temp is ConditionNode)
                    {
                        temp = temp[0]?.Next;
                    }
                    return temp;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option)
        {
            return from is SentenceNode or OtherDialogueNode && option.IsMain;
        }
#endif
    }
}