using System;
using System.Collections.ObjectModel;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace ZetanStudio.DialogueSystem
{
    [CreateAssetMenu(menuName = "Zetan Studio/对话 (Dialogue)")]
    public sealed class Dialogue : ScriptableObject
    {
        public string ID => Entry?.ID ?? string.Empty;

        [SerializeReference]
        private DialogueNode[] nodes = { };
        public ReadOnlyCollection<DialogueNode> Nodes => new ReadOnlyCollection<DialogueNode>(nodes);

        /// <summary>
        /// 对话的根结点，<see cref="UI.DialogueWindow"/> 将从此结点开始进行对话<br/>
        /// The root node of this dialogue, <see cref="UI.DialogueWindow"/> will start with this node.
        /// </summary>
        public EntryNode Entry => nodes[0] as EntryNode;

        /// <summary>
        /// 对话是否有结束结点<br/>
        /// Dose this dialogue have any exitable node.
        /// </summary>
        public bool Exitable => Traverse(Entry, n => n.ExitHere);

        public Dialogue()
        {
            nodes = new DialogueNode[] { new EntryNode() };
        }

        /// <summary>
        /// 检查根结点是否可遍历到所给结点<br/>
        /// Check if the entry node of this dialogue can traverse to given node.
        /// </summary>
        public bool Reachable(DialogueNode node) => Reachable(Entry, node);
        /// <summary>
        /// 检查 from 结点是否可以遍历到 to 结点<br/>
        /// Check if the 'from' node can traverse to the 'to' node.
        /// </summary>
        public static bool Reachable(DialogueNode from, DialogueNode to)
        {
            if (!from || !to) return false;
            bool reachable = false;
            Traverse(from, c =>
            {
                reachable = c == to;
                return reachable;
            });
            return reachable;
        }

        /// <summary>
        /// 深度优先遍历<br/>
        /// Depth-first traversal.
        /// </summary>
        public static void Traverse(DialogueNode node, Action<DialogueNode> onAccess)
        {
            if (node)
            {
                onAccess?.Invoke(node);
                foreach (var option in node.Options)
                {
                    Traverse(option.Next, onAccess);
                }
            }
        }

        /// <summary>
        /// 可终止的深度优先遍历<br/>
        /// Depth-first traversal that can be aborted.
        /// </summary>
        /// <param name="onAccess">带中止条件的访问器，返回 <i>true</i> 时将中止遍历<br/>
        /// An accessor with abort condition, make it return true if you want to abort the traversal.
        /// </param>
        /// <returns>是否在遍历时产生中止<br/>
        /// Did any aborting happen while traversing.
        /// </returns>
        public static bool Traverse(DialogueNode node, Func<DialogueNode, bool> onAccess)
        {
            if (onAccess != null && node)
            {
                if (onAccess(node)) return true;
                foreach (var option in node.Options)
                {
                    if (Traverse(option.Next, onAccess))
                        return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 用于在编辑器中记录操作退出点，不应在游戏逻辑中使用<br/>
        /// It's used to set end node in editor, should not use in game logic code.
        /// </summary>
        public ExitNode _exit = new ExitNode();
        /// <summary>
        /// 用于在编辑器中命名本段对话，不应在游戏逻辑中使用<br/>
        /// It's used to nameing the dialogue in editor, should not use in game logic code.
        /// </summary>
        public string _name;
        /// <summary>
        /// 用于在编辑器中备注本段对话的用途，不应在游戏逻辑中使用<br/>
        /// It's used to describe the dialogue in editor, should not use in game logic code.
        /// </summary>
        [TextArea]
        public string _description;

        /// <summary>
        /// 用于在编辑器中设置分组，不应在游戏逻辑中使用<br/>
        /// It's used to set up node groups in editor, should not use in game logic code.
        /// </summary>
        public List<DialogueGroup> _groups = new();

        /// <summary>
        /// 编辑器专用类，不应在游戏逻辑中使用<br/>
        /// An Editor-Use class, should not use in game logic code.
        /// </summary>
        public static class Editor
        {
            public static DialogueNode AddNode(Dialogue dialogue, Type type)
            {
                if (!typeof(DialogueNode).IsAssignableFrom(type)) return null;
                var node = Activator.CreateInstance(type) as DialogueNode;
                ArrayUtility.Add(ref dialogue.nodes, node);
                return node;
            }
            public static void AddCopiedNode(Dialogue dialogue, DialogueNode node)
            {
                ArrayUtility.Add(ref dialogue.nodes, node);
            }
            public static void RemoveNode(Dialogue dialogue, DialogueNode node)
            {
                ArrayUtility.Remove(ref dialogue.nodes, node);
            }
        }
#endif
    }
}