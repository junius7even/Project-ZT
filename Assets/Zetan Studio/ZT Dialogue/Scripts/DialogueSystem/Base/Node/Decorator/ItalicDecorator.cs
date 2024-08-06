namespace ZetanStudio.DialogueSystem
{
    [Name("斜体选项")]
    [Description("以斜体显示选项标题。")]
    public sealed class ItalicDecorator : DecoratorNode
    {
        public override bool IsValid => true;

        public override void Decorate(DialogueData data, ref string title)
        {
            title = UtilityZT.ItalicText(title);
        }
    }
}