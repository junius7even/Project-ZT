namespace ZetanStudio
{
    public sealed class LabelAttribute : EnhancedPropertyAttribute
    {
        public readonly string name;
        public readonly string tooltip;

        public LabelAttribute(string name)
        {
            this.name = name;
        }
        public LabelAttribute(string name, string tooltip)
        {
            this.name = name;
            this.tooltip = tooltip;
        }
    }
}