using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using ConditionSystem;

    [Name("按条件覆盖语音"), Width(250f)]
    [Description("满足条件时覆盖它的上一个语句结点的语音。")]
    public sealed class ConditionalVoiceOverride : VoiceOverride
    {
        [field: SerializeField, Label("语音")]
        public AudioClip Voice { get; private set; }

        [field: SerializeField, Label("条件")]
        public ConditionGroup Condition { get; private set; }

        public override bool IsValid => Voice && (Condition?.IsValid ?? false);

        public override AudioClip GetVoice(DialogueData data)
        {
            return Condition.IsMet() ? Voice : null;
        }
    }
}