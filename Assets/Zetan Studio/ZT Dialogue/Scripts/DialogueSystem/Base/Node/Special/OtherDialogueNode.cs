using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using UI;

    /// <summary>
    /// 用于开始一段新的对话，但会保持首个对话的根结点作为首页，并在结束时返回原对话<br/>
    /// It's used to start a new dialogue, but will keep the entry node of first dialogue as home node, and return to the origin dialogue at the end.
    /// </summary>
    [Group("特殊"), Name("其它对话")]
    [Description("开始一段新的对话，但会保持首个对话的根结点作为首页，并在结束时返回原对话。")]
    public sealed class OtherDialogueNode : DialogueNode, IExitableNode, IManualNode
    {
        [field: SerializeField, Label("子对话")]
        public Dialogue Dialogue { get; private set; }

        public override bool IsValid => Dialogue && Dialogue.Exitable;

        public void DoManual(DialogueHandler handler)
        {
            handler.PushInteraction(this);
            handler.StartWith(Dialogue);
        }
    }
}