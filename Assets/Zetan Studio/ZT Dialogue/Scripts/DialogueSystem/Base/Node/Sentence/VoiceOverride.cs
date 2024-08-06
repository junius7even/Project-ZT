using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    public abstract class VoiceOverride : PortraitVoiceOverrideNode
    {
        public abstract AudioClip GetVoice(DialogueData data);
    }
}
