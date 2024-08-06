using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;
    using DialogueSystem.UI;
    using UI;

    [Name("按性别显示")]
    public class GenderCondition : ConditionNode
    {
        [field: SerializeField]
        public bool CheckPlayer { get; private set; } = true;

        [field: SerializeField]
        public Gender Gender { get; private set; }

        public override bool IsValid => true;

        protected override bool CheckCondition(DialogueData entryData)
        {
            if (CheckPlayer) return PlayerManager.player.Gender == Gender;
            else return (WindowManager.FindWindow<DialogueWindow>().Target as Interlocutor).gender == Gender;
        }
    }
}