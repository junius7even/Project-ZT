using System.ComponentModel;
using UnityEngine;
using ZetanStudio;
using ZetanStudio.DialogueSystem;

namespace J7.Extension
{
    public class AudioEvent: DialogueEvent
    {
        [DisplayName("Audio Event")]
        [field: SerializeField, Label("语音")]
        public virtual AudioClip Clip { get; protected set; }
        
        public override bool IsValid => Clip != null;

        protected override bool Invoke()
        {
            AudioSource source = GameObject.FindWithTag("AudioManager").GetComponent<AudioSource>();

            if (source)
            {
                source.clip = Clip;
                source.Play();
            }
            
            return source;
        }
    }
}