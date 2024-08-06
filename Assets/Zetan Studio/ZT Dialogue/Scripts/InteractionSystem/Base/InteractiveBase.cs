using UnityEngine;

namespace ZetanStudio.InteractionSystem
{
    using UI;

    public abstract class InteractiveBase : MonoBehaviour, IInteractive
    {
        [SerializeField]
        private string defaultName = "可交互对象";
        public virtual string Name
        {
            get
            {
                return defaultName;
            }
        }

        [SerializeField, SpriteSelector]
        private Sprite defaultIcon;
        public virtual Sprite Icon => defaultIcon;

        public abstract bool IsInteractive { get; }

        bool IInteractive.Interactable { get; set; }

        public abstract bool DoInteract();

        protected void OnDestroy()
        {
            IInteractive.RemoveFromPanel(this);
            OnDestroy_();
        }
        protected virtual void OnDestroy_() { }

        /// <summary>
        /// 加入到交互面板中时调用<br/>
        /// Call when this object is pushed into <see cref="InteractionPanel"/>.
        /// </summary>
        protected virtual void OnInteractable() { }
        /// <summary>
        /// 从交互面板 <see cref="InteractionPanel"/> 中移除时调用<br/>
        /// Call when this object is removed from <see cref="InteractionPanel"/>.
        /// </summary>
        protected virtual void OnNotInteractable() { }
        /// <summary>
        /// 当调用 <see cref="IInteractive.EndInteraction"/> 时调用<br/>
        /// Call when <see cref="IInteractive.EndInteraction"/> get invoked.
        /// </summary>
        protected virtual void OnEndInteraction() { }

        void IInteractive.OnInteractable() => OnInteractable();
        void IInteractive.OnNotInteractable() => OnNotInteractable();
        void IInteractive.OnEndInteraction() => OnEndInteraction();
    }
    public interface IInteractive
    {
        string Name { get; }
        Sprite Icon { get; }
        /// <summary>
        /// 是否可被加入交互面板 <see cref="InteractionPanel"/><br/>
        /// Whether this object can be pushed into <see cref="InteractionPanel"/>.
        /// </summary>
        bool IsInteractive { get; }
        /// <summary>
        /// 是否已加入交互面板 <see cref="InteractionPanel"/><br/>
        /// Is this object already pushed into the <see cref="InteractionPanel"/>.
        /// </summary>
        protected bool Interactable { get; set; }

        /// <summary>
        /// 进行交互<br/>
        /// Perform the interaction.
        /// </summary>
        /// <returns>
        /// 交互是否成功<br/>
        /// Whether the interaction is successful.
        /// </returns>
        bool DoInteract();
        /// <summary>
        /// 当 <see cref="InteractionWindow{T}"/> 关闭时调用，也可以在需要结束交互的地方调用<br/>
        /// Call by <see cref="InteractionWindow{T}"/> when it close, or other case that should end the interaction.
        /// </summary>
        void EndInteraction()
        {
            Interactable = false;
            OnEndInteraction();
        }

        /// <summary>
        /// 加入到交互面板中时调用<br/>
        /// Call when this object is pushed into <see cref="InteractionPanel"/>.
        /// </summary>
        protected void OnInteractable() { }
        /// <summary>
        /// 从交互面板 <see cref="InteractionPanel"/> 中移除时调用<br/>
        /// Call when this object is removed from <see cref="InteractionPanel"/>.
        /// </summary>
        protected void OnNotInteractable() { }
        /// <summary>
        /// 当调用 <see cref="EndInteraction"/> 时调用<br/>
        /// Call when <see cref="EndInteraction"/> get invoked.
        /// </summary>
        protected void OnEndInteraction() { }

        /// <summary>
        /// 尝试将 <i><paramref name="interactive"/></i> 加入交互面板 <see cref="InteractionPanel"/><br/>
        /// Try to push <i><paramref name="interactive"/></i> into the <see cref="InteractionPanel"/>.
        /// </summary>
        protected static void PushToPanel(IInteractive interactive)
        {
            if (!interactive.Interactable && interactive.IsInteractive)
            {
                InteractionPanel.Push(interactive);
                interactive.Interactable = true;
                interactive.OnInteractable();
            }
        }
        /// <summary>
        /// 将 <i><paramref name="interactive"/></i> 从交互面板 <see cref="InteractionPanel"/> 中移除<br/>
        /// Remove <i><paramref name="interactive"/></i> from the <see cref="InteractionPanel"/>.
        /// </summary>
        protected static void RemoveFromPanel(IInteractive interactive)
        {
            InteractionPanel.Remove(interactive);
            interactive.Interactable = false;
            interactive.OnNotInteractable();
        }
    }
}