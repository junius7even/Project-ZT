﻿#if UNITY_EDITOR
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 用于在编辑器中设置结束结点，不应在游戏逻辑中使用<br/>
    /// It's used to set what nodes should be end node, should not use in game logic code.
    /// </summary>
    [System.Serializable, Name("结束对话"), Width(100f)]
    [Description("用于标识对话的结束位置。")]
    public sealed class ExitNode : SuffixNode
    {
        public ExitNode() => _position = new Vector2(360, 0);

        public override bool IsValid => true;

        public override bool CanConnectFrom(DialogueNode from, DialogueOption option) =>
            option.IsMain && from is IExitableNode && from.Options.Count == 1;
    }
}
#endif