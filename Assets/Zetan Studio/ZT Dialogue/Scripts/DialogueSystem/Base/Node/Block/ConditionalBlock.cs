using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using ConditionSystem;

    /// <summary>
    /// 未满足条件拦截<br/>
    /// Block the branch if the condition are not met.
    /// </summary>
    [Name("按条件拦截"), Width(250f)]
    [Description("满足指定条件时才可进入从本结点开始的分支。")]
    public class ConditionalBlock : BlockNode
    {

        [field: SerializeField, Label("拦截提示语")]
        public string BlockReason { get; private set; } = "未满足条件";

        [field: SerializeField, Label("进入条件")]
        public ConditionGroup Condition { get; private set; } = new ConditionGroup();
        public override bool IsValid => Condition?.IsValid ?? false;

        protected override bool CheckCondition() => Condition.IsMet();

        protected override string GetBlockReason() => L.Tr("Message", BlockReason);
    }
}