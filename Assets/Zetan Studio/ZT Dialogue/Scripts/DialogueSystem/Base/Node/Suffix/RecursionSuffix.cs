using System.Linq;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using UI;

    /// <summary>
    /// 递归结点用于返回到前面指定的语句结点<br/>
    /// Recursion node is used to return to specified sentence node in front of it.
    /// </summary>
    [Name("递归"), Width(100f)]
    [Description("返回到前面指定的语句结点。")]
    public sealed class RecursionSuffix : SuffixNode, IManualNode
    {
        [field: SerializeField, Label("深度"), Min(2)]
        public int Depth { get; private set; } = 2;

        public override bool IsValid => Depth > 1;

        /// <summary>
        /// 查找要返回到的结点<br/>
        /// Find the node that will return to.
        /// </summary>
        public DialogueNode FindRecursionPoint(EntryNode entry)
        {
            DialogueNode find = null;
            if (!entry) return find;
            int depth = 0;
            DialogueNode temp = this;
            while (temp && depth < Depth)
            {
                if (!Dialogue.Traverse(entry, n =>
                {
                    if (n.Options.Any(o => o.Next == temp))
                    {
                        temp = n;
                        if (temp is SentenceNode) depth++;
                        return true;
                    }
                    return false;
                })) temp = null;
                if (temp == entry) break;
            }
            if (depth == Depth || temp == entry) find = temp;
            return find;
        }

        public void DoManual(DialogueHandler handler)
        {
            handler.ContinueWith(FindRecursionPoint(handler.CurrentEntry));
        }

#if UNITY_EDITOR
        public override bool CanConnectFrom(DialogueNode from, DialogueOption option)
        {
            return from is SentenceNode and not EntryNode;
        }
#endif
    }
}