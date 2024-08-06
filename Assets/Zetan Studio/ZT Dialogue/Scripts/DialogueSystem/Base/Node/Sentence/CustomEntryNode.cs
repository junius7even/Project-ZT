using System;
using UnityEngine;
using System.ComponentModel;
using ZetanStudio;
using ZetanStudio.DialogueSystem;

namespace J7.Extension
{
    [Name("测试")]
    [Description("Customizable entry point")]
    public sealed class CustomEntryNode: SentenceNode
    {
        /// <summary>
        /// 是否可以关闭对话窗口以提前结束对话<br/>
        /// Whether this dialogue can exit early by closing dialogue window.
        /// </summary>
        [field: SerializeField, Label("对话可中断"), HideInNode]
        public bool Interruptable { get; private set; } = true;

        public CustomEntryNode()
        {
            ID = "DLG-" + Guid.NewGuid().ToString("N");
            options = new DialogueOption[] { DialogueOption.Main };
            exitHere = true;
        }
        
        public CustomEntryNode(string interlocutor, string content, bool interruptable = false) : this()
        {
            Interlocutor = interlocutor;
            Content = content;
            Interruptable = interruptable;
        }
        
        public CustomEntryNode(string id, string interlocutor, string content, bool interruptable = false) : this(interlocutor, content, interruptable)
        {
            ID = id;
        }
        
#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option) => false;
#endif
    }
}