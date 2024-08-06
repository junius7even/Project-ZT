using System.ComponentModel;
using J7.Extension;
using UnityEngine;
using ZetanStudio.DialogueSystem;

namespace J7.Extension
{
    [DisplayName("Animation Event")]
    public class AnimationEvent : DialogueEvent
    {
        [field: SerializeField] public AnimEvent Anim { get; private set; }

        public override bool IsValid => Anim != null && !string.IsNullOrEmpty(Anim.animTrigger) &&
                                        !string.IsNullOrEmpty(Anim.objectName);

        protected override bool Invoke()
        {
            Animator animator = GameObject.Find(Anim.objectName).GetComponent<Animator>();
            if (animator)
                animator.SetTrigger(Anim.animTrigger);
            return animator;
        }
    }
}