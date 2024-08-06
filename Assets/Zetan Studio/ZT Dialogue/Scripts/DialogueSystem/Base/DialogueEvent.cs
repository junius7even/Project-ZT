using System;
using System.Reflection;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 对话事件可在特定情况下触发事件，一般由继承了 <see cref="IEventNode"/> 的结点持有<br/>
    /// Dialogue event can trigger event under certain circumstances, generally held by nodes that inherit <see cref="IEventNode"/>
    /// </summary>
    [Serializable]
    public abstract class DialogueEvent
#if UNITY_EDITOR
        : ICopiable
#endif
    {
        [field: SerializeField]
        public string ID { get; private set; } = "EVT-" + Guid.NewGuid().ToString("N");

        /// <summary>
        /// 事件是否是一次性的<br/>
        /// Whether this event can only invoke once.
        /// </summary>
        [field: SerializeField, Label("一次性的")]
        public bool OneTime { get; protected set; } = true;

        public abstract bool IsValid { get; }

        public bool Invoke(DialogueData ownerData)
        {
            if (!IsValid) return false;
            if (OneTime)
            {
                if (!ownerData) return false;
                if (!ownerData.EventStates.TryGetValue(ID, out var state) || !state)
                    if (Invoke())
                    {
                        ownerData.AccessEvent(ID);
                        return true;
                    }
            }
            else return Invoke();
            return false;
        }
        /// <summary>
        /// 关闭对话窗口或返回首页时调用<br/>
        /// Get called when close dialogue window or return to home page.
        /// </summary>
        public virtual void CancelInvoke(DialogueData ownerData) { }

        protected abstract bool Invoke();
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用，复制此事件，并赋予新ID<br/>
        /// Editor-Use, copy this event with new ID.
        /// </summary>
        public virtual object Copy()
        {
            var evt = MemberwiseClone() as DialogueEvent;
            evt.ID = "EVT-" + Guid.NewGuid().ToString("N");
            return evt;
        }
#endif

        public static string GetName(Type type) => type.GetCustomAttribute<NameAttribute>()?.name ?? type.Name;

        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class NameAttribute : Attribute
        {
            public readonly string name;

            public NameAttribute(string name)
            {
                this.name = name;
            }
        }
    }
}