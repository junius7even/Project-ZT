namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 用于决定是否显示由此结点进入的分支<br/>
    /// It's used to decide whether the branch start from this node should display.
    /// </summary>
    [Group("条件显示")]
    public abstract class ConditionNode : DialogueNode, ISoloMainOptionNode
    {
        public ConditionNode() => options = new DialogueOption[] { DialogueOption.Main };

        /// <summary>
        /// 检查此分支是否显示<br/>
        /// Check should this branch display.
        /// </summary>
        public bool Check(DialogueData entryData)
        {
            DialogueNode temp = this;
            while (temp is ConditionNode condition)
            {
                if (!condition.CheckCondition(entryData)) return false;
                temp = temp[0]?.Next;
            }
            return true;
        }
        /// <summary>
        /// 检查此分支是否显示<br/>
        /// Check should this branch display.
        /// </summary>
        protected abstract bool CheckCondition(DialogueData entryData);

#if UNITY_EDITOR
        public sealed override bool CanConnectFrom(DialogueNode from, DialogueOption option) => from is ConditionNode or BranchNode || !option.IsMain;
#endif
    }
}