using System;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 对话的根结点，有且仅有一个<br/>
    /// The root node of a dialogue, should have and can only have one entry node.
    /// </summary>
    [Name("开始对话")]
    [Description("对话的根结点。")]
    public sealed class EntryNode : SentenceNode
    {
        /// <summary>
        /// 是否可以关闭对话窗口以提前结束对话<br/>
        /// Whether this dialogue can exit early by closing dialogue window.
        /// </summary>
        [field: SerializeField, Label("对话可中断"), HideInNode]
        public bool Interruptable { get; private set; } = true;

        public EntryNode()
        {
            ID = "DLG-" + Guid.NewGuid().ToString("N");
            options = new DialogueOption[] { DialogueOption.Main };
            exitHere = true;
        }

        public EntryNode(string interlocutor, string content, bool interruptable = false) : this()
        {
            Interlocutor = interlocutor;
            Content = content;
            Interruptable = interruptable;
        }

        public EntryNode(string id, string interlocutor, string content, bool interruptable = false) : this(interlocutor, content, interruptable)
        {
            ID = id;
        }

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option) => false;
#endif
    }
}