namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 用于覆盖语句结点的肖像或语音<br/>
    /// This's used to override the portrait or voice of sentence node.
    /// </summary>
    [Group("覆盖")]
    public abstract class PortraitVoiceOverrideNode : DialogueNode, IExitableNode, ISoloMainOptionNode
    {
        public PortraitVoiceOverrideNode()
        {
            options = new DialogueOption[] { DialogueOption.Main };
        }

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option) => from is SentenceNode or PortraitVoiceOverrideNode && option.IsMain;
        public override bool CanConnectTo(DialogueNode to, DialogueOption option) => to is SentenceNode or PortraitVoiceOverrideNode or RevertOptions && option.IsMain;
#endif
    }
}