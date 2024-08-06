using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZetanStudio.DialogueSystem
{
    using System.Collections;
    using UI;

    [Serializable]
    public abstract class DialogueNode
    {
        [field: SerializeField, TextArea, HideInNode]
        public string ID { get; protected set; } = "CON-" + Guid.NewGuid().ToString("N");

        [SerializeField, HideInNode]
        protected DialogueOption[] options = { };
        public ReadOnlyCollection<DialogueOption> Options => new ReadOnlyCollection<DialogueOption>(options);

        [SerializeField, HideInNode]
        protected bool exitHere;
        /// <summary>
        /// 对话是否在此结点结束，仅当结点是 <see cref="IExitableNode"/> 时可能为 <i>True</i><br/>
        /// Whether the dialogue exit at this node, can probably be <i>True</i> only if this node is an <see cref="IExitableNode"/>.
        /// </summary>
        public bool ExitHere => this is IExitableNode && exitHere;

        /// <summary>
        /// 从此结点进入是否可以结束对话<br/>
        /// Whether this node will cause the dialogue exit.
        /// </summary>
        public bool Exitable => Dialogue.Traverse(this, n => n.ExitHere);

        /// <summary>
        /// 结点是否填写完整，否则此结点不参与对话<br/>
        /// Whether this node is completely set up, otherwise it will not be handled.
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// 快捷访问指定下标的选项<br/>
        /// Shortcut to access option at specified index.
        /// </summary>
        /// <returns>给定下标处的选项<br/>
        /// Option at given index
        /// </returns>
        public DialogueOption this[int index] => index >= 0 && index < options.Length ? options[index] : null;

        public static implicit operator bool(DialogueNode obj) => obj != null;

        #region 特性 Attributes
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class NameAttribute : Attribute
        {
            public readonly string name;

            public NameAttribute(string name)
            {
                this.name = name;
            }
        }
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class GroupAttribute : Attribute
        {
            public readonly string group;

            public GroupAttribute(string group)
            {
                this.group = group;
            }
        }
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class DescriptionAttribute : Attribute
        {
            public readonly string desc;

            public DescriptionAttribute(string desc)
            {
                this.desc = desc;
            }
        }
        /// <summary>
        /// 用于加到结点类型上，指定其图形结点的宽度<br/>
        /// Add this to node type to specify width of its graph node.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class WidthAttribute : Attribute
        {
            public readonly float width;

            public WidthAttribute(float width)
            {
                this.width = width;
            }
        }
        /// <summary>
        /// 用于指定哪些字段要在图形结点中隐藏<br/>
        /// Add this to fields that need to hide in graph node.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        protected sealed class HideInNodeAttribute : Attribute { }
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// 用于在编辑器中设置结点位置，不应在游戏逻辑中使用<br/>
        /// It's used to set node position in editor, should not use in game logic code.
        /// </summary>
        [HideInInspector] public Vector2 _position;

        /// <summary>
        /// 用于在编辑器中获取结点名称，不应在游戏逻辑中使用<br/>
        /// It's used to get name of this node is editor, should not use in game logic code.
        /// </summary>
        public string GetName() => Editor.GetName(GetType());

        /// <summary>
        /// 用于在编辑器中筛选端口，不应在游戏逻辑中使用<br/>
        /// It's used to filter what ports can this node connect from in editor, should not use in game logic code.
        /// </summary>
        public virtual bool CanConnectFrom(DialogueNode from, DialogueOption option) => true;
        /// <summary>
        /// 用于在编辑器中筛选端口，不应在游戏逻辑中使用<br/>
        /// It's used to filter what ports can this node connect from in editor, should not use in game logic code.
        /// </summary>
        public virtual bool CanConnectTo(DialogueNode to, DialogueOption option) => true;

        /// <summary>
        /// 用于在编辑器结点中筛选要显示的字段，不应在游戏逻辑中使用<br/>
        /// It's used to filter what fields to display in graph node in editor, should not use in game logic code.
        /// </summary>
        public HashSet<string> GetHiddenFields()
        {
            HashSet<string> fields = new HashSet<string>();
            Type type = GetType();
            while (type != null)
            {
                collect(type);
                type = type.BaseType;
            }
            return fields;

            void collect(Type type)
            {
                foreach (var field in type.GetFields(UtilityZT.CommonBindingFlags))
                {
                    if (field.GetCustomAttribute<HideInNodeAttribute>() is not null)
                        fields.Add(field.Name);
                }
            }
        }

        /// <summary>
        /// 用于在编辑器中复制结点，不应在游戏逻辑中使用<br/>
        /// It's used to copy node in editor, should not use in game logic code.
        /// </summary>
        public DialogueNode Copy()
        {
            var type = GetType();
            var copy = Activator.CreateInstance(type) as DialogueNode;
            EditorUtility.CopySerializedManagedFieldsOnly(this, copy);
            copy.ID = "CON-" + Guid.NewGuid().ToString("N");
            copy.exitHere = false;
            for (int i = 0; i < options.Length; i++)
            {
                DialogueOption.Editor.SetNext(copy.options[i], null);
            }
            foreach (var field in type.GetFields(UtilityZT.CommonBindingFlags))
            {
                if (typeof(ICopiable).IsAssignableFrom(field.FieldType))
                {
                    var value = field.GetValue(this) as ICopiable;
                    field.SetValue(this, value.Copy());
                }
                else if (field.FieldType.IsArray)
                {
                    var eType = field.FieldType.GetElementType();
                    if (typeof(ICopiable).IsAssignableFrom(eType))
                    {
                        var array = field.GetValue(this) as IList;
                        for (int i = 0; i < array.Count; i++)
                        {
                            if (array[i] != null)
                                array[i] = (array[i] as ICopiable).Copy();
                        }
                    }
                }
                else if (field.FieldType.IsGenericType && typeof(List<>) == field.FieldType.GetGenericTypeDefinition())
                {
                    var eType = field.FieldType.GetGenericArguments()[0];
                    if (typeof(ICopiable).IsAssignableFrom(eType))
                    {
                        var array = field.GetValue(this) as IList;
                        for (int i = 0; i < array.Count; i++)
                        {
                            array[i] = (array[i] as ICopiable).Copy();
                        }
                    }
                }
            }
            return copy;
        }

        /// <summary>
        /// 编辑器专用类，不应在游戏逻辑中使用<br/>
        /// An Editor-Use class, should not use in game logic code.
        /// </summary>
        public static class Editor
        {
            public static string GetGroup(Type type) => type.GetCustomAttribute<GroupAttribute>(true)?.group ?? string.Empty;
            public static string GetName(Type type) => type.GetCustomAttribute<NameAttribute>()?.name ?? ObjectNames.NicifyVariableName(type.Name);
            public static string GetDescription(Type type) => type.GetCustomAttribute<DescriptionAttribute>()?.desc ?? string.Empty;

            public static float GetWidth(Type type) => type.GetCustomAttribute<WidthAttribute>()?.width ?? 0f;

            public static DialogueOption AddOption(DialogueNode node, bool main, string title = null)
            {
                if (node is ISoloMainOptionNode or BifurcationNode) return null;
                DialogueOption option = new DialogueOption(main, title);
                ArrayUtility.Add(ref node.options, option);
                return option;
            }
            public static void RemoveOption(DialogueNode node, DialogueOption option)
            {
                ArrayUtility.Remove(ref node.options, option);
            }

            public static void MoveOptionUpward(DialogueNode node, int index)
            {
                if (index < 1) return;
                (node.options[index], node.options[index - 1]) = (node.options[index - 1], node.options[index]);
            }
            public static void MoveOptionDownward(DialogueNode node, int index)
            {
                if (index >= node.options.Length - 1) return;
                (node.options[index], node.options[index + 1]) = (node.options[index + 1], node.options[index]);
            }

            public static void SetAsExit(DialogueNode node, bool exit = true)
            {
                if (node is not IExitableNode || node.options.Length > 1) return;
                if (exit)
                    if (node.options.Length == 1) DialogueOption.Editor.SetNext(node.options[0], null);
                    else AddOption(node, true);
                node.exitHere = exit;
            }
        }
#endif
    }

    #region 接口 Interfaces
    /// <summary>
    /// 如果想让结点只有单个主要选项，则继承这个接口。注意，继承该接口后需要在构造函数自动生成一个主选项<br/>
    /// Inherit this interface if you want the node to only have one main option. You need to create a main option in its constructor if a node type inherit this interface.
    /// </summary>
    public interface ISoloMainOptionNode { }
    /// <summary>
    /// 如果想让结点在完成后调用事件，则继承这个接口<br/>
    /// Inherit this interface if you want the node to invoke event when it gets done.
    /// </summary>
    public interface IEventNode
    {
        public ReadOnlyCollection<DialogueEvent> Events { get; }
    }
    /// <summary>
    /// 如果想让结点可以被设置为退出结点，则继承这个接口<br/>
    /// Inherit this interface if you want the node can be an end node.
    /// </summary>
    public interface IExitableNode { }
    /// <summary>
    /// 如果想不依赖对话窗口用自定义行为处理某种结点，则继承这个接口<br/>
    /// Inherit this interface if you want handle a type of node in other custom way.
    /// </summary>
    public interface IManualNode
    {
        /// <summary>
        /// 处理结点的自定义行为<br/>
        /// The custom method to handle this node.
        /// </summary>
        void DoManual(DialogueHandler handler);
    }
    #endregion
}