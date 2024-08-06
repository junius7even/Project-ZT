namespace ZetanStudio.InteractionSystem.UI
{
    using ZetanStudio.UI;

    public abstract class InteractionWindow<T> : Window where T : IInteractive
    {
        [Label("交互时隐藏交互面板")]
        public bool hidePanelWhenInteracting;

        /// <summary>
        /// 当前正在交互的对象<br/>
        /// The object which in interacting currently.
        /// </summary>
        public abstract T Target { get; }

        public void Interrupt()
        {
            if (OnInterrupt()) Close();
        }
        /// <summary>
        /// 尝试中断交互<br/>
        /// Call when the interaction is tried to interrupt.
        /// </summary>
        /// <returns>交互可否中断<br/>
        /// Can the interaction be interrupted?
        /// </returns>
        protected virtual bool OnInterrupt() => true;

        protected sealed override bool OnOpen(params object[] args)
        {
            if (hidePanelWhenInteracting) InteractionPanel.HidePanelBy(Target, false);
            return OnOpen_(args);
        }

        protected sealed override bool OnClose(params object[] args)
        {
            if (hidePanelWhenInteracting) InteractionPanel.HidePanelBy(Target, true);
            Target?.EndInteraction();
            return OnClose_(args);
        }

        protected virtual bool OnOpen_(params object[] args) => true;
        protected virtual bool OnClose_(params object[] args) => true;
    }
}