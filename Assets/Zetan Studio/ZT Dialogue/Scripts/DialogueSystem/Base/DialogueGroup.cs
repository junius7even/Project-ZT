#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 用于在编辑器中设置分组，不应在游戏逻辑中使用<br/>
    /// It's used to group nodes in editor, should not use in game logic code.
    /// </summary>
    [Serializable]
    public sealed class DialogueGroup
    {
        public string _name;
        [SerializeReference]
        public List<DialogueNode> _nodes = new List<DialogueNode>();
        public Vector2 _position;

        public DialogueGroup(string name, Vector2 position)
        {
            _name = name;
            _position = position;
        }
    }
}
#endif