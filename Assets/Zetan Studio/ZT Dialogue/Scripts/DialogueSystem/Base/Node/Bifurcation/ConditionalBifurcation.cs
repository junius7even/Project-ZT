using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using ConditionSystem;
    using UI;

    [Name("按条件分叉"), Width(250f)]
    [Description("当符合条件时，将使用第一个分支继续对话，否则使用第二个分支。")]
    public sealed class ConditionalBifurcation : BifurcationNode, IManualNode
    {
        [field: SerializeField, Label("条件")]
        public ConditionGroup Condition { get; private set; }

        public override bool IsValid => Condition?.IsValid ?? false;

        public void DoManual(DialogueHandler handler)
        {
            if (Condition.IsMet()) handler.ContinueWith(First);
            else handler.ContinueWith(Second);
        }

        public override bool CheckIsDone(DialogueData data)
        {
            if (Condition.IsMet()) return data[First]?.IsDone ?? false;
            else return data[Second]?.IsDone ?? false;
        }
    }
}