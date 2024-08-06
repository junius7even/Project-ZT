using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using Extension;
    using UI;

    /// <summary>
    /// 进入事件结点时将触发给定的事件，然后以下一个结点继续对话<br/>
    /// When enter the event node, it will trigger events, and then continue with the next node.
    /// </summary>
    [Group("特殊"), Name("触发事件"), Width(265)]
    [Description("进入此结点时触发给定的事件，然后以下一个结点继续对话。")]
    public sealed class EventNode : DialogueNode, IEventNode, ISoloMainOptionNode, IManualNode
    {
        public override bool IsValid => Array.TrueForAll(events, e => e?.IsValid ?? false);

        [SerializeReference, PolymorphismList("GetName")]
        private DialogueEvent[] events = { };
        public ReadOnlyCollection<DialogueEvent> Events => new ReadOnlyCollection<DialogueEvent>(events);

        public EventNode()
        {
            options = new DialogueOption[] { DialogueOption.Main };
        }

        public void DoManual(DialogueHandler handler)
        {
            if (handler.CurrentEntryData[this] is DialogueData data)
                Events.ForEach(e => handler.InvokeEvent(e, data));
            handler.ContinueWith(this[0]?.Next);
        }
    }
}