using System.Collections.Generic;
using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;
    using DialogueSystem.UI;
    using InteractionSystem;
    using UI;

    [DisallowMultipleComponent, KeywordsSet("NPC")]
    public class Interlocutor : Character, IInterlocutor, IKeyword
    {
        public Gender gender;

        public override string Name
        {
            get
            {
                if (Application.isPlaying) return L.Tr("Common", base.Name);
                else return base.Name;
            }
            protected set => base.Name = value;
        }

        public bool IsInteractive
        {
            get
            {
                return true;
            }
        }

        [field: SerializeField, SpriteSelector]
        public Sprite Icon { get; private set; }

        [field: SerializeField]
        public Dialogue Dialogue { get; set; }

        bool IInteractive.Interactable { get; set; }

        string IKeyword.IDPrefix => "NPC";

        Color IKeyword.Color => Color.blue;

        string IKeyword.Group => gender.ToString();

        bool IInteractive.DoInteract()
        {
            return WindowManager.OpenWindow<DialogueWindow>(this);
        }

        void IInteractive.OnNotInteractable()
        {
            if (WindowManager.IsWindowOpen<DialogueWindow>(out var dialogue) && dialogue.Target == this as IInterlocutor)
                dialogue.Interrupt();
        }

        #region MonoBehaviour
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                IInteractive.PushToPanel(this);
            }
        }
        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                IInteractive.PushToPanel(this);
            }
        }
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                IInteractive.RemoveFromPanel(this);
            }
        }
        private void OnDestroy()
        {
            IInteractive.RemoveFromPanel(this);
        }
        #endregion

        [RuntimeGetKeywordsMethod, GetKeywordsMethod]
        public static IEnumerable<Interlocutor> GetInterlocutors()
        {
            return FindObjectsOfType<Interlocutor>(true);
        }

    }
    public enum Gender
    {
        Male,
        Female
    }
}