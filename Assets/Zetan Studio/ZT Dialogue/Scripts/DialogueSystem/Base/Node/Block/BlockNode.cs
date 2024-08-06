using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 拦截结点，用于判断点击选项时是否可以进入选项所在的分支<br/>
    /// Block node is used to check if a branch can enter when click its option.
    /// </summary>
    [Group("拦截")]
    public abstract class BlockNode : DialogueNode, ISoloMainOptionNode
    {
        /// <summary>
        /// 是否让此结点在满足一次进入条件后，就不再拦截，即使这次条件不满足<br/>
        /// Whether to make this node stop blocking after meeting the enter conditions once, even if the conditions are not met this time.
        /// </summary>
        [field: SerializeField, Label("一次性的", "是否让此结点在满足一次进入条件后，就不再拦截，即使这次条件不满足")]
        public bool OneTime { get; protected set; }

        public BlockNode() => options = new DialogueOption[] { DialogueOption.Main };

        /// <summary>
        /// 检查是否能进入由此结点开始的分支<br/>
        /// Check if can enter the branch from this node.
        /// </summary>
        /// <param name="blockReason">一条提示为什么不能进入的信息<br/>
        /// A message that shows why this branch cannot enter.
        /// </param>
        public bool CanEnter(DialogueData entryData, out string blockReason)
        {
            bool result;
            if (OneTime && entryData[this].AdditionalData.TryReadBool("block", out _))
                result = true;
            else
            {
                result = CheckCondition();
                if (result && OneTime) entryData[this].AdditionalData["block"] = true;
            }
            if (!result) blockReason = GetBlockReason();
            else blockReason = string.Empty;
            return result;
        }
        /// <summary>
        /// 检查可进入条件，返回 <i>True</i> 表示可进入<br/>
        /// Check enter conditions, returns <i>True</i> if can be entered.
        /// </summary>
        /// <returns></returns>
        protected abstract bool CheckCondition();
        /// <summary>
        /// 获取为什么被拦截的通知<br/>
        /// Get a reason message string to tips player why this branch is blocked.
        /// </summary>
        protected virtual string GetBlockReason() => string.Empty;

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option)
        {
            return from is BlockNode or SentenceNode or OtherDialogueNode;
        }
#endif
    }
}