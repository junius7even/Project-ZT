using UnityEngine;

namespace ZetanStudio.Examples
{
    using InteractionSystem;

    public abstract class Interactive2D : InteractiveBase
    {
        public bool activated = true;

        #region MonoBehaviour
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (activated && collision.CompareTag("Player")) IInteractive.PushToPanel(this);
        }

        protected virtual void OnTriggerStay2D(Collider2D collision)
        {
            if (activated && collision.CompareTag("Player")) IInteractive.PushToPanel(this);
        }

        protected virtual void OnTriggerExit2D(Collider2D collision)
        {
            if (activated && collision.CompareTag("Player")) IInteractive.RemoveFromPanel(this);
        }
        #endregion
    }
}