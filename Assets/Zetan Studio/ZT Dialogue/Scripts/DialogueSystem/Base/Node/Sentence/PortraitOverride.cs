using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    public abstract class PortraitOverride : PortraitVoiceOverrideNode
    {
        public abstract Sprite GetPortrait(DialogueData data);
    }
}