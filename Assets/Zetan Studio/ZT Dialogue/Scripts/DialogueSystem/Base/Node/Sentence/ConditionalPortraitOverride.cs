using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using ConditionSystem;

    [Name("按条件覆盖肖像"), Width(250f)]
    [Description("满足条件时覆盖它的上一个语句结点的肖像。")]
    public sealed class ConditionalPortraitOverride : PortraitOverride
    {
        [field: SerializeField, Label("肖像"), SpriteSelector]
        public Sprite Portrait { get; private set; }

        [field: SerializeField, Label("条件")]
        public ConditionGroup Condition { get; private set; }

        public override bool IsValid => Portrait && (Condition?.IsValid ?? false);

        public override Sprite GetPortrait(DialogueData data)
        {
            return Condition.IsMet() ? Portrait : null;
        }
    }
}