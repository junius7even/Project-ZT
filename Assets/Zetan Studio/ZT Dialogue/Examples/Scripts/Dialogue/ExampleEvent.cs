using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;

    [SerializeField, Name("示例事件")]
    public sealed class ExampleEvent : DialogueEvent
    {
        [field: SerializeField]
        public string Message { get; private set; }

        public override bool IsValid => !string.IsNullOrEmpty(Message);

        protected override bool Invoke()
        {
            MessageManager.Push(L.Tr("Message", Message));
            return true;
        }
    }
}