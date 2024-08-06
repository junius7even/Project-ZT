using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using ConditionSystem;

    [Name("按条件显示"), Width(250f)]
    [Description("满足条件时可进入从本结点开始的分支。")]
    public class CommonCondition : ConditionNode
    {
        [field: SerializeField, Label("条件")]
        public ConditionGroup Condition { get; private set; } = new ConditionGroup();

        public override bool IsValid => Condition?.IsValid ?? false;

        protected override bool CheckCondition(DialogueData entryData) => Condition.IsMet();
    }
}