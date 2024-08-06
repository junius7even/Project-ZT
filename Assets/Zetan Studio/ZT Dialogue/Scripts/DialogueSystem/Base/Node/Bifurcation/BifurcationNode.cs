namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 分叉结点，通常用于叉开对话流程，与分支结点类似，但它没有具体实现，且只有两个固定的主要选项<br/>
    /// Bifurcation node, usually used to bifurcate the dialogue flow, similar to branch node, but it has no specific implementation, and has only two fixed main options
    /// </summary>
    [Group("分叉")]
    public abstract class BifurcationNode : DialogueNode
    {
        public DialogueNode First => this[0]?.Next;
        public DialogueNode Second => this[1]?.Next;

        public BifurcationNode()
        {
            options = new DialogueOption[] { DialogueOption.Main, DialogueOption.Main };
        }

        public abstract bool CheckIsDone(DialogueData data);
    }
}
