namespace ZetanStudio.DialogueSystem
{
    [Name("粗体选项")]
    [Description("以粗体显示选项标题。")]
    public sealed class BoldDecorator : DecoratorNode
    {
        public override bool IsValid => true;

        public override void Decorate(DialogueData data, ref string title)
        {
            title = UtilityZT.BoldText(title);
        }
    }
}