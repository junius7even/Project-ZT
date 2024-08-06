using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.DialogueSystem.Editor
{
    using Extension.Editor;
    using ZetanStudio.Editor;

    /// <summary>
    /// 对话编辑器图形结点<br/>
    /// A graph use for inspecting node in editor.
    /// </summary>
    public class DialogueGraphNode : Node
    {
        #region 端口声明 Ports Declaration
        public DialoguePort Input { get; private set; }
        private readonly List<DialogueOutput> outputs = new List<DialogueOutput>();
        public ReadOnlyCollection<DialogueOutput> Outputs => new ReadOnlyCollection<DialogueOutput>(outputs);
        #endregion

        #region 序列化相关 Serialization Declaration
        public DialogueNode Target { get; private set; }
        public SerializedProperty SerializedNode { get; private set; }
        public SerializedProperty SerializedOptions { get; private set; }
        private readonly List<SerializedProperty> serializedProperties = new List<SerializedProperty>();
        private readonly HashSet<string> hiddenFields = new HashSet<string>();
        #endregion

        #region 回调 Callbacks Declaration
        private readonly Action beforeModify;
        private readonly Action onModified;
        private readonly Action<DialogueGraphNode> onSelected;
        private readonly Action<DialogueGraphNode> onUnselected;
        private readonly Action<DialogueOutput> onDeleteOutput;
        #endregion

        #region 控件声明 Controls Declaration
        private readonly Button addOption;
        private readonly IMGUIContainer inspector;
        private readonly DialogueGraph graph;
#if !ZTDS_DISABLE_PORTRAIT
        private readonly PropertyField portrait;
        private readonly Toggle keepPortraitSide;
#endif
        private readonly TextField interlocutor;
        private readonly TextArea content;
        private readonly CurveField interval;
        #endregion

        /// <param name="graph">包含此结点的视图<br/>
        /// Graph that contains this node
        /// </param>
        /// <param name="node">此结点要绘制的对话结点<br/>
        /// Target dialogue node that this graph node need to draw
        /// </param>
        /// <param name="beforeModify">数据修改前回调<br/>
        /// Callback when want to modify dialogue node data
        /// </param>
        /// <param name="onModified">数据修改后回调<br/>
        /// Callback when any modification happended
        /// </param>
        /// <param name="onSelected">选中回调<br/>
        /// Callback when this node selected
        /// </param>
        /// <param name="onUnselected">取消选中回调<br/>
        /// Callback when this node unselected
        /// </param>
        /// <param name="onDeleteOutput">选项删除回调<br/>
        /// Callback when an output(that is the option) deleted
        /// </param>
        public DialogueGraphNode(DialogueGraph graph, DialogueNode node, Action beforeModify, Action onModified, Action<DialogueGraphNode> onSelected, Action<DialogueGraphNode> onUnselected, Action<DialogueOutput> onDeleteOutput)
        {
            titleContainer.style.height = 25;
            expanded = true;
            m_CollapseButton.pickingMode = PickingMode.Ignore;
            m_CollapseButton.Q("icon").visible = false;

            this.graph = graph;
            userData = Target = node;
            if (!node)
            {
                title = Tr("类型丢失的结点");
                titleButtonContainer.Add(new Button(() => graph.DeleteElements(new GraphElement[] { this })) { text = Tr("删除") });
                inputContainer.Add(new Button(() => EditorWindow.GetWindow<ReferencesFixing>().Show()) { text = Tr("尝试修复") });
                return;
            }

            viewDataKey = node.ID;
            title = Tr(node.GetName());
            style.left = node._position.x;
            style.top = node._position.y;
            hiddenFields = node.GetHiddenFields();

            var width = DialogueNode.Editor.GetWidth(node.GetType());
            if (width <= 0) inputContainer.style.minWidth = 228f;
            else inputContainer.style.minWidth = width;

            this.beforeModify = beforeModify;
            this.onModified = onModified;
            this.onSelected = onSelected;
            this.onUnselected = onUnselected;
            this.onDeleteOutput = onDeleteOutput;

            RefreshProperty();

            #region 初始化入口 Initialize Input
            if (node is not EntryNode)
            {
                inputContainer.Add(Input = new DialogueInput());
#if ZTDS_DISABLE_PORTRAIT
                Input.portName = Target is SentenceNode ? Tr("对话人") : string.Empty;
#else
                Input.portName = string.Empty;
#endif
                Input.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    if (Input.connections.Any())
                        evt.menu.AppendAction(Tr("断开所有"), a =>
                        {
                            Input.Graph.DeleteElements(Input.connections);
                        });
                }));
            }
            #endregion

            #region 初始化选项功能 Initialize Output
            if (node is not SuffixNode)
            {
                if (node is not (ISoloMainOptionNode or BifurcationNode))
                {
                    addOption = new Button(AddOption) { text = Tr("新选项") };
                    titleButtonContainer.Add(addOption);
                }
                else if (node.Options.Count < 1)
                {
                    DialogueNode.Editor.AddOption(node, true);
                    SerializedOptions.serializedObject.Update();
                }
                for (int i = 0; i < node.Options.Count; i++)
                {
                    AddOutputInternal(node.Options[i]);
                }
            }
            #endregion

            #region 初始化文本区 Initialize Sentence
            if (node is SentenceNode)
            {
                var interlocutorContainer = new VisualElement();
                interlocutorContainer.style.maxWidth = width > 0 ? width : 228f;
#if !ZTDS_DISABLE_PORTRAIT
                interlocutorContainer.style.flexDirection = FlexDirection.Row;
#endif
                if (Target is not EntryNode)
                {
                    interlocutorContainer.style.marginTop = -22;
#if ZTDS_DISABLE_PORTRAIT
                    interlocutorContainer.style.marginBottom = 1;
                    interlocutorContainer.style.marginLeft = 85;
#else
                    interlocutorContainer.style.marginLeft = 20;
#endif
                }
#if !ZTDS_DISABLE_PORTRAIT
                interlocutorContainer.Add(portrait = new PropertyField());
                portrait.BindProperty(SerializedNode.FindAutoProperty("Portrait"));
                portrait.label = string.Empty;
                portrait.style.width = 64;
                portrait.style.marginLeft = -4;
                var right = new VisualElement();
                right.style.flexGrow = 1;
                right.Add(keepPortraitSide = new Toggle(Tr("保持肖像位置")));
                keepPortraitSide.RegisterValueChangedCallback(_ => onModified?.Invoke());
                keepPortraitSide.BindProperty(SerializedNode.FindAutoProperty("KeepPortraitSide"));
                keepPortraitSide.labelElement.style.minWidth = 60;
                keepPortraitSide.tooltip = Tr("如果想让肖像在对话人不一样时保持上一句的位置，勾选这个");
                keepPortraitSide.SetEnabled(Target is not EntryNode);
                right.Add(interlocutor = new TextArea(Tr("对话人")));
                interlocutorContainer.Add(right);
#else
                interlocutorContainer.Add(interlocutor = new TextField(Target is not EntryNode ? string.Empty : Tr("对话人")) { multiline = true });
                interlocutor.labelElement.style.minWidth = 60f;
                interlocutor.Q("unity-text-input").style.whiteSpace = WhiteSpace.Normal;
#endif
                interlocutor.RegisterValueChangedCallback(_ => onModified?.Invoke());
                interlocutor.BindProperty(SerializedNode.FindAutoProperty("Interlocutor"));
                Keyword.Editor.SetAsKeywordsField(interlocutor);
                inputContainer.Add(interlocutorContainer);
                inputContainer.Add(content = new TextArea(string.Empty, 35f));
                content.style.maxWidth = width > 0 ? width : 228f;
                content.RegisterValueChangedCallback(_ => onModified?.Invoke());
                content.BindProperty(SerializedNode.FindAutoProperty("Content"));
                Keyword.Editor.SetAsKeywordsField(content);
                titleContainer.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    interlocutorContainer.style.maxWidth = inputContainer.layout.width;
                    content.style.maxWidth = inputContainer.layout.width;
                });
                titleButtonContainer.Insert(1, interval = new CurveField());
                interval.BindProperty(SerializedNode.FindAutoProperty("SpeakInterval"));
                interval.labelElement.tooltip = Tr("注意：是吐字“间隔”不是吐字“速度”，其纵向数值越高，语速越慢");
                interval.style.minWidth = 160f;
                interval.style.alignItems = Align.Center;
                interval.label = Tr("吐字间隔");
                interval.labelElement.style.minWidth = 0f;
                interval.labelElement.style.paddingTop = 0f;
            }
            #endregion

            #region 初始化检查器 Initialize Inspector
            if (node is not ExitNode)
            {
                inspector = new IMGUIContainer(() =>
                {
                    if (SerializedNode != null && SerializedNode.serializedObject != null && SerializedNode.serializedObject.targetObject)
                    {
                        float oldLW = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 80;
                        SerializedNode.serializedObject.UpdateIfRequiredOrScript();
                        EditorGUI.BeginChangeCheck();
                        foreach (var property in serializedProperties)
                        {
                            EditorGUILayout.PropertyField(property, true);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            SerializedNode.serializedObject.ApplyModifiedProperties();
                            onModified?.Invoke();
                        }
                        EditorGUIUtility.labelWidth = oldLW;
                    }
                });
                inspector.style.marginTop = 1;
                inspector.style.marginLeft = 3;
                inspector.style.marginRight = 3;
                inspector.AddManipulator(new ContextualMenuManipulator(evt => { }));
                inputContainer.Add(inspector);
            }
            #endregion

            RefreshExpandedState();
            RefreshPorts();
            RefreshAddButton();

            #region Tooltip
            titleContainer.Q<Label>("title-label").tooltip = Tr(DialogueNode.Editor.GetDescription(node.GetType()));
            this.RegisterTooltipCallback(() =>
            {
                if (node is SentenceNode sentence) return DialogueEditor.Preview(sentence);
                else if (node is OtherDialogueNode other && other.Dialogue) return DialogueEditor.Preview(other.Dialogue.Entry);
                else return null;
            });
            #endregion
        }

        #region 选项相关 Options
        private void AddOption()
        {
            if (Target is BranchNode) AddOption(true);
            else if (Target.Options.Count < 1)
            {
                if (Target is not ExternalOptionsNode)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent(Tr("主要选项")), false, () => AddOption(true));
                    menu.AddItem(new GUIContent(Tr("普通选项")), false, () => AddOption(false));
                    menu.ShowAsContext();
                }
                else AddOption(false);
            }
            else if (!Target.Options.FirstOrDefault().IsMain || Target is ExternalOptionsNode) AddOption(false);
        }
        private void AddOption(bool main)
        {
            if (!CanAddOption()) return;
            beforeModify?.Invoke();
            var option = DialogueNode.Editor.AddOption(Target, main, main ? Tr("继续") : ObjectNames.GetUniqueName(Target.Options.Select(x => x.Title).ToArray(), Tr("新选项")));
            if (option != null)
            {
                SerializedNode.serializedObject.UpdateIfRequiredOrScript();
                onModified?.Invoke();
                AddOutputInternal(option);
                RefreshPorts();
            }
        }

        public DialogueOutput AddOutput(DialogueOption option)
        {
            if (!CanAddOption()) return null;
            return AddOutputInternal(option);
        }
        private DialogueOutput AddOutputInternal(DialogueOption option)
        {
            var output = new DialogueOutput(option, Target is ISoloMainOptionNode or BifurcationNode ? null : DeleteOutput);
            if (option.IsMain) output.portName = Tr(Target is BranchNode or BifurcationNode ? "分支" : "主要");
            outputs.Add(output);
            outputContainer.Add(output);
            output.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (Target.Options.Count == 1)
                {
                    if (option.IsMain && !Target.ExitHere && Target is not (ISoloMainOptionNode or BifurcationNode or BranchNode) && canConnect(false))
                        evt.menu.AppendAction(Tr("转为普通选项"), a =>
                        {
                            beforeModify?.Invoke();
                            DialogueOption.Editor.SetIsMain(option, false);
                            SerializedNode.serializedObject.UpdateIfRequiredOrScript();
                            output.SerializedOption.FindAutoProperty("Title").stringValue = ObjectNames.GetUniqueName(Target.Options.Select(x => x.Title).ToArray(), Tr("新选项"));
                            SerializedNode.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            output.RefreshIsMain();
                            output.RefreshProperty();
                            RefreshAddButton();
                            output.portName = string.Empty;
                        });
                    else if (!option.IsMain && canConnect(true))
                        evt.menu.AppendAction(Tr("转为主要选项"), a =>
                        {
                            beforeModify?.Invoke();
                            DialogueOption.Editor.SetIsMain(option, true);
                            SerializedNode.serializedObject.UpdateIfRequiredOrScript();
                            output.SerializedOption.FindAutoProperty("Title").stringValue = string.Empty;
                            SerializedNode.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            output.RefreshIsMain();
                            output.RefreshProperty();
                            RefreshAddButton();
                            output.portName = Tr("主要");
                        });

                    bool canConnect(bool isMain)
                    {
                        return Target.CanConnectTo(option.Next, new DialogueOption(isMain, null)) || (option.Next?.CanConnectFrom(Target, new DialogueOption(isMain, null)) ?? true);
                    }
                }
                else if (!option.IsMain)
                {
                    var index = Target.Options.IndexOf(option);
                    if (index > 0)
                        evt.menu.AppendAction(Tr("上移选项"), a =>
                        {
                            beforeModify?.Invoke();
                            DialogueNode.Editor.MoveOptionUpward(Target, index);
                            SerializedOptions.serializedObject.UpdateIfRequiredOrScript();
                            output.PlaceBehind(outputs[index - 1]);
                            (outputs[index], outputs[index - 1]) = (outputs[index - 1], outputs[index]);
                            outputs.ForEach(o => o.RefreshProperty());
                        });
                    if (index < Target.Options.Count - 1)
                        evt.menu.AppendAction(Tr("下移选项"), a =>
                        {
                            beforeModify?.Invoke();
                            DialogueNode.Editor.MoveOptionDownward(Target, index);
                            SerializedOptions.serializedObject.UpdateIfRequiredOrScript();
                            output.PlaceInFront(outputs[index + 1]);
                            (outputs[index], outputs[index + 1]) = (outputs[index + 1], outputs[index]);
                            outputs.ForEach(o => o.RefreshProperty());
                        });
                }
            }));
            RefreshPorts();
            RefreshAddButton();
            output.RefreshProperty();
            return output;

        }
        private void DeleteOutput(DialogueOutput output)
        {
            onDeleteOutput?.Invoke(output);
            outputs.Remove(output);
            outputContainer.Remove(output);
            beforeModify?.Invoke();
            DialogueNode.Editor.RemoveOption(Target, output.userData as DialogueOption);
            SerializedNode.serializedObject.UpdateIfRequiredOrScript();
            onModified?.Invoke();
            RefreshPorts();
            RefreshAddButton();
            outputs.ForEach(o => o.RefreshProperty());
        }
        private bool CanAddOption()
        {
            return Target is not (SuffixNode or ISoloMainOptionNode or BifurcationNode) or BranchNode;
        }
        #endregion

        #region 重写 Override
        protected override void ToggleCollapse() { }
        public override void OnSelected()
        {
            base.OnSelected();
            onSelected?.Invoke(this);
        }
        public override void OnUnselected()
        {
            base.OnUnselected();
            onUnselected?.Invoke(this);
        }
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (!Target) return;
            beforeModify?.Invoke();
            Target._position.x = newPos.xMin;
            Target._position.y = newPos.yMin;
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }
        #endregion

        #region 刷新相关 Refreshing
        public void RefreshProperty()
        {
            if (Target is not ExitNode)
            {
                graph.SerializedNodes.serializedObject.Update();
                SerializedNode = graph.SerializedNodes.GetArrayElementAtIndex(graph.Dialogue.Nodes.IndexOf(Target));
                serializedProperties.Clear();
                using var copy = SerializedNode.Copy();
                SerializedProperty end = copy.GetEndProperty();
                bool enter = true;
                while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
                {
                    enter = false;
                    if (!hiddenFields.Contains(copy.name))
                        serializedProperties.Add(copy.Copy());
                }
                if (Target is not SuffixNode) SerializedOptions = SerializedNode.FindPropertyRelative("options");
                if (Target is SentenceNode)
                {
                    interlocutor?.BindProperty(SerializedNode.FindAutoProperty("Interlocutor"));
                    content?.BindProperty(SerializedNode.FindAutoProperty("Content"));
                    interval?.BindProperty(SerializedNode.FindAutoProperty("SpeakInterval"));
#if !ZTDS_DISABLE_PORTRAIT
                    portrait?.BindProperty(SerializedNode.FindAutoProperty("Portrait"));
                    if (portrait != null) portrait.label = string.Empty;
                    keepPortraitSide?.BindProperty(SerializedNode.FindAutoProperty("KeepPortraitSide"));
#endif
                }
                outputs.ForEach(o => o.RefreshProperty());
            }
        }

        public void RefreshAddButton()
        {
            addOption?.SetEnabled(Target is ExternalOptionsNode or BranchNode || Target.Options.Count < 1
                                  || !Target.Options[0].IsMain && Target is SentenceNode or OtherDialogueNode && !Target.ExitHere);
        }
        #endregion

        public void SetAsExit(DialogueGraphNode exit)
        {
            if (exit == null || Target is SuffixNode or ExternalOptionsNode or BranchNode) return;
            DialogueNode.Editor.SetAsExit(Target);
            SerializedNode.serializedObject.UpdateIfRequiredOrScript();
            DialogueOutput output = Outputs.Count < 1 ? AddOutput(Target.Options[0]) : Outputs[0];
            graph.AddElement(output.ConnectTo(exit.Input));
            graph.CheckErrors();
            RefreshAddButton();
        }

        private string Tr(string text) => EDL.Tr(text);
    }
}