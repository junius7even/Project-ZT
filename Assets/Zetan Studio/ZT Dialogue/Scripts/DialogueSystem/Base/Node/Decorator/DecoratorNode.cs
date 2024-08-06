namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 用于修饰选项标题的文本<br/>
    /// It's used to decorate the option's title text.
    /// </summary>
    [Group("选项修饰"), Width(50f)]
    public abstract class DecoratorNode : DialogueNode, ISoloMainOptionNode
    {
        public DecoratorNode() => options = new DialogueOption[] { DialogueOption.Main };

        public abstract void Decorate(DialogueData data, ref string title);

#if UNITY_EDITOR
        public sealed override bool CanConnectFrom(DialogueNode from, DialogueOption option) => from is DecoratorNode || !option.IsMain;
#endif
    }
}