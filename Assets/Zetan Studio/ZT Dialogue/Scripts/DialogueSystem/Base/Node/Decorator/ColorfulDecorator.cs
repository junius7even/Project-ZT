using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    [Name("染色选项")]
    [Description("以指定颜色显示选项标题。")]
    public sealed class ColorfulDecorator : DecoratorNode
    {
        [field: SerializeField, Label("随机颜色")]
        public bool RandomColor { get; private set; }

        [field: SerializeField, Label("颜色范围"), HideIf("RandomColor", false)]
        public Gradient Gradient { get; private set; }

        [field: SerializeField, Label("颜色"), HideIf("RandomColor", true)]
        public Color Color { get; private set; } = Color.black;

        public override bool IsValid => RandomColor || Color.a > 0;

        public override void Decorate(DialogueData data, ref string title)
        {
            title = UtilityZT.ColorText(title, RandomColor ? Gradient.Evaluate(UnityEngine.Random.Range(0f, 1f)) : Color);
        }
    }
}