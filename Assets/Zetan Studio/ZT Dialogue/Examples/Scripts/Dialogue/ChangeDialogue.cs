

using System.Collections;
using UnityEngine;
using System.ComponentModel;
using ZetanStudio;
using ZetanStudio.DialogueSystem;
using ZetanStudio.Examples;
using ZetanStudio.UI;
namespace J7.Extension
{
    /// <summary>
    /// 用于调整下一个对话内容。结束时退出对话界面。
    /// </summary>
    [Group("特殊"), Name("Change Dialogue")]
    [Description("Change script after the dialogue gets closed")]
    public sealed class ChangeDialogue: DialogueNode, IExitableNode, IManualNode
    {
        [field: SerializeField, Label("Next Dialogue")]
        public Dialogue Dialogue { get; private set; }

        private DialogueHandler dialogueHandler;
        public override bool IsValid => Dialogue && Dialogue.Exitable;
        public void DoManual(DialogueHandler handler)
        {
            // handler.PushInteraction(new ExitNode());
            // dialogueHandler = handler;
            // handler.callbacks.onReachLastSentence += DialogueCb;
            
            // 求其次的方法，因为IExitableNode无法让这个直接结束对话，必须添加一个dialogue content
            handler.StartWith("", "");
            handler.Interlocutor.Dialogue = Dialogue;
        }

        /// <summary>
        /// 本来想用callback尝试完成，以成功在对话结束之后替换dialogue
        /// 但是没有找到这样做会失败的原因，所以求其次选择了
        /// 由一个多余的 startwith "" content 来结束对话
        /// </summary>
        private void DialogueCb()
        {
            
            dialogueHandler.Interlocutor.Dialogue = Dialogue;
            // handler.callbacks.onReachLastSentence -= DialogueCb;
        }
    }
}