using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.DialogueSystem.Editor
{
    using Extension;
    using UnityEngine.Pool;

    public class DialogueGraph : GraphView
    {
        public new class UxmlFactory : UxmlFactory<DialogueGraph, UxmlTraits> { }

        #region 声明 Declaration
        public readonly DialogueEditorSettings settings;
        public Action<DialogueGraphNode> nodeSelectedCallback;
        public Action<DialogueGraphNode> nodeUnselectedCallback;
        public Action<Vector3, Vector3> onViewTransformChanged;
        private readonly MiniMap miniMap;
        private DialogueGraphNode entry;
        private DialogueGraphNode exit;
        private List<DialogueNode> invalidNodes = new List<DialogueNode>();
        private DialogueGraphGroup invalidNodeGroup;
        private Dialogue dialogue;
        private readonly VisualElement errorsList;
        private readonly ObjectPool<Label> errorCache;
        private readonly List<Label> errors = new List<Label>();
        private SerializedObject serializedDialog;

        private List<DialogueGroup> copiedGroups;
        private List<DialogueNode> copiedNodes;
        private GenericData copiedData;
        private Vector2 localMousePosition;
        private Vector2 copiedPosition;
        #endregion

        #region 属性 Properties
        public Dialogue Dialogue { get => dialogue; set => ViewDialogue(value); }
        public SerializedProperty SerializedNodes { get; private set; }
        public SerializedProperty SerializedGroups { get; private set; }
        protected override bool canPaste => copiedNodes != null && copiedData != null;
        #endregion

        public DialogueGraph()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer() { minScale = 0.15f });
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            settings = DialogueEditorSettings.GetOrCreate();
            styleSheets.Add(settings.editorUss);

            Add(miniMap = new MiniMap());
            miniMap.style.backgroundColor = new StyleColor(new Color32(29, 29, 30, 200));
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                miniMap.style.width = miniMap.maxWidth = evt.newRect.width / 4;
                miniMap.style.height = miniMap.maxHeight = evt.newRect.height / 4;
            });

            var errorPanel = new VisualElement();
            errorPanel.pickingMode = PickingMode.Ignore;
            errorPanel.style.position = Position.Absolute;
            errorPanel.style.left = 0;
            errorPanel.style.right = 0;
            errorPanel.style.top = 0;
            errorPanel.style.bottom = 0;
            errorPanel.style.flexDirection = FlexDirection.ColumnReverse;
            errorPanel.style.alignItems = Align.FlexStart;
            Add(errorPanel);
            errorPanel.Add(errorsList = new VisualElement());
            errorsList.style.flexDirection = FlexDirection.ColumnReverse;
            errorsList.style.paddingLeft = 3f;
            errorsList.pickingMode = PickingMode.Ignore;
            errorCache = new ObjectPool<Label>(() =>
            {
                var label = new Label() { enableRichText = true };
                label.AddManipulator(new Clickable(() =>
                {
                    (label.userData as Action)?.Invoke();
                }));
                return label;
            }, l => { errorsList.Add(l); errors.Add(l); }, l => { l.text = null; l.userData = null; errorsList.Remove(l); });
            CheckErrors();

            viewTransformChanged = OnViewTransformChanged;
            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = OnUnserializeAndPaste;
            elementsAddedToGroup = OnElementsAddedToGroup;
            elementsRemovedFromGroup = OnElementsRemovedFromGroup;

            RegisterCallback<MouseEnterEvent>(evt =>
            {
                localMousePosition = evt.localMousePosition;
            });
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                localMousePosition = evt.localMousePosition;
            });

            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        private void OnViewTransformChanged(GraphView graphView)
        {
            onViewTransformChanged?.Invoke(viewTransform.position, viewTransform.scale);
        }

        #region 重写 Override
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!Dialogue) return;
            if (evt.target == this)
            {
                Vector2 position = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
                evt.menu.AppendAction(Tr("分组"), a =>
                {
                    CreateGraphGroup(position);
                });
                if (canPaste) evt.menu.AppendAction(Tr("粘贴"), a => PasteCallback());
                evt.menu.AppendSeparator();
                var menuItems = new List<(string, Action<DropdownMenuAction>)>();
                foreach (var type in TypeCache.GetTypesDerivedFrom<DialogueNode>())
                {
                    if (!type.IsAbstract && type != typeof(EntryNode) && type != typeof(ExitNode))
                        menuItems.Add(($"{GetMenuGroup(type)}{Tr(DialogueNode.Editor.GetName(type))}", a => CreateNode(type, position)));
                }
                foreach (var item in menuItems.OrderBy(m => m.Item1))
                {
                    evt.menu.AppendAction(item.Item1, item.Item2);
                }
                evt.menu.AppendSeparator();
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr("普通")}", a =>
                {
                    CreateNewScript(ScriptTemplate.Node);
                });
                evt.menu.AppendSeparator($"{Tr("增加新类型")}/");
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(BifurcationNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Bifurcation);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(BlockNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Block);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(ConditionNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Condition);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(DecoratorNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Decorator);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(ExternalOptionsNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.ExternalOptions);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(SentenceNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Sentence);
                });
                evt.menu.AppendAction($"{Tr("增加新类型")}/{Tr(DialogueNode.Editor.GetGroup(typeof(SuffixNode)))}", a =>
                {
                    CreateNewScript(ScriptTemplate.Suffix);
                });
            }
            else if (evt.target is DialogueGraphNode node)
            {
                if (node.Target is not EntryNode && node != exit)
                {
                    evt.menu.AppendAction(Tr("删除"), a => DeleteSelection());
                    evt.menu.AppendAction(Tr("复制"), a => CopySelectionCallback());
                }
                IEnumerable<Node> dnodes = selection.FindAll(s => s is DialogueGraphNode).Cast<Node>();
                if (node.GetContainingScope() is DialogueGraphGroup group && dnodes.All(s => (s as DialogueGraphNode)!.GetContainingScope() == group))
                {
                    evt.menu.AppendAction(Tr("移出本组"), a =>
                    {
                        var nodes = dnodes.Where(s => s is DialogueGraphNode n && n.GetContainingScope() == group);
                        BeforeModify();
                        foreach (var n in nodes)
                        {
                            group.Target._nodes.Remove(n.userData as DialogueNode);
                            n.SetPosition(new Rect(n.layout.position + new Vector2(-10, 10), n.layout.size));
                        }
                        group.RemoveElementsWithoutNotification(nodes);
                    });
                }
                if (dnodes.All(s => (s as DialogueGraphNode)!.GetContainingScope() is null))
                {
                    evt.menu.AppendAction(Tr("添加到") + '/' + Tr("新建一个分组"), a =>
                    {
                        BeforeModify();
                        List<Vector2> positions = new List<Vector2>();
                        foreach (var s in selection)
                        {
                            if (s is Node n)
                                positions.Add(n.layout.center);
                        }
                        var group = CreateGraphGroup(GetCenter(positions));
                        group.AddElements(dnodes);
                    });
                    List<DialogueGraphGroup> groups = new List<DialogueGraphGroup>(graphElements.Where(g => g is DialogueGraphGroup).Cast<DialogueGraphGroup>());
                    if (groups.Count > 0)
                    {
                        BeforeModify();
                        evt.menu.AppendSeparator(Tr("添加到") + '/');
                        Dictionary<string, int> d = new Dictionary<string, int>();
                        foreach (var g in groups)
                        {
                            var count = 0;
                            if (d.ContainsKey(g.title)) count = d[g.title] + 1;
                            d[g.title] = count;
                            evt.menu.AppendAction($"{Tr("添加到")}/{g.title}{(count > 0 ? Tr(" (重名 {0})", count) : string.Empty)}", a =>
                            {
                                g.AddElements(dnodes);
                            });
                        }
                    }
                }
                if (node.Target is RecursionSuffix recursion && Dialogue.Reachable(recursion))
                    evt.menu.AppendAction(Tr("选中递归点"), a =>
                    {
                        if (recursion.FindRecursionPoint(Dialogue.Entry) is DialogueNode find)
                        {
                            ClearSelection();
                            AddToSelection(GetNodeByGuid(find.ID));
                            FrameSelection();
                        }
                    });
                if (TypeCacheZT.Editor.FindScriptOfType(node.Target?.GetType()) is MonoScript script)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction(Tr("编辑脚本"), a => AssetDatabase.OpenAsset(script));
                }
            }

        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(endPort => canLink(startPort, endPort)).ToList();

            bool canLink(Port startPort, Port endPort)
            {
                if (endPort.direction == startPort.direction) return false;
                if (endPort.node == startPort.node) return false;
                if (startPort.direction == Direction.Input) (startPort, endPort) = (endPort, startPort);
                if (Dialogue.Reachable(node(endPort), node(startPort))) return false;
                if (node(startPort) is BranchNode && node(endPort) is not ConditionNode) return false;
                if (node(startPort) is IExitableNode && endPort.node == exit) return true;
                return node(startPort).CanConnectTo(node(endPort), startPort.userData as DialogueOption) && node(endPort).CanConnectFrom(node(startPort), startPort.userData as DialogueOption);

                static DialogueNode node(Port port) => port.node.userData as DialogueNode;
            }
        }
        public override EventPropagation DeleteSelection()
        {
            RemoveFromSelection(entry);
            RemoveFromSelection(exit);
            selection.ForEach(s =>
            {
                if (s is DialogueGraphGroup group)
                {
                    try
                    {
                        group.RemoveElementsWithoutNotification(new GraphElement[] { entry, exit });
                    }
                    catch { }
                }
            });
            return base.DeleteSelection();
        }
        #endregion

        #region 创建相关 Creation
        private void CreateNode(Type type, Vector2 position, Action<DialogueGraphNode> callback = null)
        {
            BeforeModify();
            var node = Dialogue.Editor.AddNode(Dialogue, type);
            if (node != null)
            {
                node._position = position;
                serializedDialog.Update();
                var gNode = CreateGraphNode(node);
                callback?.Invoke(gNode);
                ClearSelection();
                AddToSelection(gNode);
            }
        }

        private DialogueGraphNode CreateGraphNode(DialogueNode node)
        {
            var gNode = new DialogueGraphNode(this, node, BeforeModify, CheckErrors, nodeSelectedCallback, nodeUnselectedCallback, OnDeleteOutput);
            AddElement(gNode);
            if (node is EntryNode) entry = gNode;
            return gNode;
        }
        private void CreateEdges(DialogueNode node)
        {
            if (!node) return;
            DialogueGraphNode parent = GetNodeByGuid(node.ID) as DialogueGraphNode;
            if (node.ExitHere) AddElement(parent.Outputs[0].ConnectTo(exit.Input));
            else if (node is not SuffixNode)
                for (int i = 0; i < node.Options.Count; i++)
                {
                    var o = node.Options[i];
                    if (o.Next != null)
                    {
                        DialogueGraphNode child = GetNodeByGuid(o.Next.ID) as DialogueGraphNode;
                        AddElement(parent.Outputs[i].ConnectTo(child?.Input));
                    }
                }
        }

        private DialogueGraphGroup CreateGraphGroup(Vector2 position)
        {
            var group = new DialogueGroup(Tr("新分组"), position);
            BeforeModify();
            dialogue._groups.Add(group);
            var g = CreateGraphGroup(group);
            g.FocusTitleTextField();
            return g;
        }
        private DialogueGraphGroup CreateGraphGroup(DialogueGroup group)
        {
            var nodes = new HashSet<DialogueNode>(group._nodes);
            DialogueGraphGroup gg = new DialogueGraphGroup(base.nodes.Where(n => nodes.Contains(n.userData as DialogueNode)), group, OnGroupRightClick, BeforeModify);
            AddElement(gg);
            return gg;
        }
        #endregion

        #region 操作回调 Actions
        #region 结点变化相关 Change Actions
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                HashSet<DialogueGraphNode> removedNodes = new HashSet<DialogueGraphNode>();
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if (elem is DialogueGraphNode node)
                    {
                        BeforeModify();
                        Dialogue.Editor.RemoveNode(Dialogue, node.Target);
                        removedNodes.Add(node);
                    }
                });
                if (removedNodes.Count > 0)
                    nodes.ForEach(n =>
                    {
                        if (!removedNodes.Contains(n)) (n as DialogueGraphNode).RefreshProperty();
                    });
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if (elem is Edge edge)
                    {
                        DialogueGraphNode parent = edge.output.node as DialogueGraphNode;
                        if (parent.Target.ExitHere)
                        {
                            BeforeModify();
                            DialogueNode.Editor.SetAsExit(parent.Target, false);
                            parent.RefreshAddButton();
                        }
                        else
                        {
                            DialogueGraphNode child = edge.input.node as DialogueGraphNode;
                            BeforeModify();
                            var index = parent.Outputs.IndexOf(edge.output as DialogueOutput);
                            if (index >= 0 && index < parent.Target.Options.Count)
                                DialogueOption.Editor.SetNext(parent.Target.Options[index], null);
                        }
                    }
                    if (elem is DialogueGraphGroup group)
                    {
                        BeforeModify();
                        Dialogue._groups.Remove(group.Target);
                    }
                });
            }
            graphViewChange.edgesToCreate?.ForEach(edge =>
            {
                BeforeModify();
                DialogueGraphNode parent = edge.output.node as DialogueGraphNode;
                if (edge.input.node.userData is ExitNode)
                {
                    DialogueNode.Editor.SetAsExit(parent.Target);
                    parent.RefreshAddButton();
                }
                else
                {
                    DialogueNode.Editor.SetAsExit(parent.Target, false);
                    DialogueGraphNode child = edge.input.node as DialogueGraphNode;
                    if (edge.output is DialogueOutput parentOutput)
                    {
                        DialogueOption.Editor.SetNext(parentOutput.Option, child.Target);
                    }
                }
            });

            if (graphViewChange.elementsToRemove != null || graphViewChange.edgesToCreate != null)
            {
                EditorUtility.SetDirty(Dialogue);
                serializedDialog.UpdateIfRequiredOrScript();
                CheckErrors();
            }

            return graphViewChange;
        }
        private void OnUndoPerformed()
        {
            List<string> selectedNodes = new List<string>();
            foreach (var item in selection)
            {
                if (item is Node node) selectedNodes.Add(node.viewDataKey);
            }
            ViewDialogue(Dialogue);
            foreach (var id in selectedNodes)
            {
                if (GetNodeByGuid(id) is Node node)
                    AddToSelection(node);
            }
        }
        private void OnDeleteOutput(DialogueOutput output)
        {
            DeleteElements(output.connections);
        }
        #endregion

        #region 分组相关 Group Actions
        private void OnGroupRightClick(DialogueGraphGroup group, ContextualMenuPopulateEvent evt)
        {
            if (selection.Count == 1)
            {
                if (group.containedElements.Any())
                    evt.menu.AppendAction(Tr("全选"), a =>
                    {
                        ClearSelection();
                        foreach (var e in group.containedElements)
                        {
                            AddToSelection(e);
                        }
                    });
                if (invalidNodeGroup != group)
                {
                    if (group.containedElements.Any(n => n.userData is DialogueNode node && node is not EntryNode and not ExitNode))
                    {
                        evt.menu.AppendAction(Tr("复制"), a => CopySelectionCallback());
                        evt.menu.AppendAction(Tr("全部分离"), a => separate(group));
                        evt.menu.AppendAction(Tr("全部删除"), a => DeleteSelection());
                    }
                    evt.menu.AppendAction(group.containedElements.Any() ? Tr("仅删除组") : Tr("删除"), a =>
                    {
                        separate(group);
                        dialogue._groups.Remove(group.Target);
                        RemoveElement(group);
                    });
                }
                else evt.menu.AppendAction(Tr("全部删除"), a => DeleteSelection());
            }

            void separate(DialogueGraphGroup group)
            {
                BeforeModify();
                group.Target._nodes.Clear();
                EditorUtility.SetDirty(dialogue);
                group.RemoveElementsWithoutNotification(new List<GraphElement>(group.containedElements));
            }
        }
        private void OnElementsAddedToGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (elements.Any() && group.userData is DialogueGroup groupData)
            {
                BeforeModify();
                HashSet<DialogueNode> exist = new HashSet<DialogueNode>(groupData._nodes);
                foreach (var e in elements)
                {
                    if (!exist.Contains(e.userData as DialogueNode))
                    {
                        (group.userData as DialogueGroup)._nodes.Add(e.userData as DialogueNode);
                    }
                }
            }
        }
        private void OnElementsRemovedFromGroup(Group group, IEnumerable<GraphElement> elements)
        {
            if (elements.Any() && group.userData is DialogueGroup groupData)
            {
                BeforeModify();
                foreach (var e in elements)
                {
                    groupData._nodes.Remove(e.userData as DialogueNode);
                }
                if (group == invalidNodeGroup && group.containedElements.None())
                {
                    RemoveElement(group);
                    invalidNodeGroup = null;
                    ViewDialogue(dialogue);
                }
            }
        }
        #endregion

        #region 复制粘贴相关 Copy-Paste Actions
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            copiedGroups = new List<DialogueGroup>();
            copiedNodes = new List<DialogueNode>();
            copiedData = new GenericData();
            List<Vector2> positions = new List<Vector2>();
            foreach (var element in elements)
            {
                if (element is DialogueGraphNode node && node.Target is not EntryNode and not ExitNode)
                {
                    var nd = new GenericData();
                    var copy = node.Target.Copy();
                    nd["ID"] = copy.ID;
                    copiedNodes.Add(copy);
                    nd.WriteAll(node.Target.Options.Where(o => o.Next).Select(o => o.Next.ID));
                    copiedData.Write(node.viewDataKey, nd);
                    positions.Add(node.layout.center);
                }
                else if (element is DialogueGraphGroup group)
                    copiedGroups.Add(new DialogueGroup(group.Target._name, group.Target._position) { _nodes = group.Target._nodes });
            }
            if (positions.Count > 0) copiedPosition = GetCenter(positions);
            else
            {
                copiedNodes = null;
                copiedData = null;
            }
            if (copiedGroups.Count < 1) copiedGroups = null;
            //无实际意义
            //No practical significance
            return "Copy";
        }
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            if (copiedNodes != null && copiedData != null && copiedNodes.Count > 0)
            {
                BeforeModify();
                var offset = copiedPosition - this.ChangeCoordinatesTo(contentViewContainer, localMousePosition);
                var nodes = new List<DialogueGraphNode>();
                foreach (var copy in copiedNodes)
                {
                    Dialogue.Editor.AddCopiedNode(dialogue, copy);
                    copy._position -= offset;
                    nodes.Add(CreateGraphNode(copy));
                }
                foreach (var nd in copiedData.ReadDataDict().Values)
                {
                    var node = GetNodeByGuid(nd.ReadString("ID"));
                    if (node is DialogueGraphNode n)
                    {
                        var cds = nd.ReadStringList();
                        for (int i = 0; i < cds.Count; i++)
                        {
                            try
                            {
                                var child = GetNodeByGuid(realID(cds[i]));
                                if (child is DialogueGraphNode gn) DialogueOption.Editor.SetNext(n.Target[i], gn.Target);
                            }
                            catch { }
                        }
                    }
                }
                copiedNodes.ForEach(c => CreateEdges(c));
                if (copiedGroups != null)
                {
                    ClearSelection();
                    foreach (var group in copiedGroups)
                    {
                        group._position -= offset;
                        group._nodes = group._nodes.ConvertAll(c => GetNodeByGuid(realID(c.ID)).userData as DialogueNode);
                        dialogue._groups.Add(group);
                        AddToSelection(CreateGraphGroup(group));
                    }
                }
                else
                {
                    ClearSelection();
                    foreach (var node in nodes)
                    {
                        AddToSelection(node);
                    }
                }
            }
            copiedGroups = null;
            copiedNodes = null;
            copiedData = null;

            string realID(string oldID) => copiedData.ReadData(oldID).ReadString("ID");
        }
        #endregion

        public void OnEdgeDropOutside(DialogueOutput output, Vector2 nodePosition)
        {
            DialogueGraphNode from = output.node;
            if (from.userData is RecursionSuffix || nodes.Any(x => x.ContainsPoint(x.WorldToLocal(nodePosition)))) return;
            var option = output.userData as DialogueOption;
            nodePosition = contentViewContainer.WorldToLocal(nodePosition);
            var exitHere = from.Target.ExitHere;
            GenericMenu menu = new GenericMenu();
            if (from.Outputs.Count == 1 && output.Option.IsMain && !exitHere && from.Target is IExitableNode)
            {
                menu.AddItem(new GUIContent(Tr(DialogueNode.Editor.GetName(typeof(ExitNode)))), false, () =>
                {
                    BeforeModify();
                    DialogueNode.Editor.SetAsExit(from.Target);
                    serializedDialog.Update();
                    CheckErrors();
                    if (output.connections is not null)
                    {
                        foreach (var edge in output.connections)
                        {
                            edge.input.Disconnect(edge);
                            RemoveElement(edge);
                        }
                        output.DisconnectAll();
                    }
                    AddElement(output.ConnectTo(exit.Input));
                    from.RefreshAddButton();
                });
                menu.AddSeparator("");
            }
            var menuItems = new List<(string, GenericMenu.MenuFunction)>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<DialogueNode>())
            {
                if (type.IsAbstract || type.IsGenericType || type.IsGenericTypeDefinition || typeof(EntryNode) == type || typeof(ExitNode) == type
                    || from.Target is BranchNode && !typeof(ConditionNode).IsAssignableFrom(type)) continue;
                var temp = Activator.CreateInstance(type) as DialogueNode;
                var fromNode = from.userData as DialogueNode;
                if (!fromNode.CanConnectTo(temp, output.Option) || !temp.CanConnectFrom(fromNode, output.Option)) continue;
                menuItems.Add(($"{GetMenuGroup(type)}{Tr(DialogueNode.Editor.GetName(type))}", () => CreateNode(type, nodePosition, followUp)));
            }
            foreach (var item in menuItems.OrderBy(m => m.Item1))
            {
                menu.AddItem(new GUIContent(item.Item1), false, item.Item2);
            }
            menu.ShowAsContext();

            void followUp(DialogueGraphNode child)
            {
                DialogueOption.Editor.SetNext(option, child.Target);
                if (exitHere) child.SetAsExit(exit);
                serializedDialog.Update();
                if (output.connections is not null)
                {
                    foreach (var edge in output.connections)
                    {
                        edge.input.Disconnect(edge);
                        RemoveElement(edge);
                    }
                    output.DisconnectAll();
                }
                AddElement(output.ConnectTo(child.Input));
                if (exitHere)
                {
                    DialogueNode.Editor.SetAsExit(from.Target, false);
                    serializedDialog.Update();
                }
            }
        }
        #endregion

        #region 其它 Others
        public void ViewDialogue(Dialogue newDialogue)
        {
            Vacate();
            dialogue = newDialogue;
            if (dialogue)
            {
                serializedDialog = new SerializedObject(Dialogue);
                SerializedNodes = serializedDialog.FindProperty("nodes");
                SerializedGroups = serializedDialog.FindProperty("groups");
                exit = CreateGraphNode(Dialogue._exit);
                invalidNodes.Clear();
                invalidNodes.AddRange(new List<DialogueNode>(dialogue.Nodes.Where(x => x is null)));
                if (invalidNodes.Count > 0)
                {
                    AddElement(invalidNodeGroup = new DialogueGraphGroup(null, null, OnGroupRightClick, BeforeModify));
                    invalidNodeGroup.title = Tr("无效的结点");
                    var row = -1;
                    for (int i = 0; i < invalidNodes.Count; i++)
                    {
                        if (i % 5 == 0) row++;
                        var n = CreateGraphNode(invalidNodes[i]);
                        n.SetPosition(new Rect(new Vector2(i % 5 * 235f, row * 40f), Vector2.zero));
                        invalidNodeGroup.AddElement(n);
                    }
                }
                else
                {
                    dialogue.Nodes.ForEach(c => CreateGraphNode(c));
                    dialogue.Nodes.ForEach(c => CreateEdges(c));
                    dialogue._groups.ForEach(g => CreateGraphGroup(g));
                }
            }
            else
            {
                entry = null;
                exit = null;
                invalidNodes.Clear();
                invalidNodeGroup = null;
                serializedDialog = null;
                SerializedNodes = null;
                SerializedGroups = null;
            }
            CheckErrors();
        }
        private void Vacate()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;
            foreach (var l in errors)
            {
                errorCache.Release(l);
            }
            errors.Clear();
        }

        public void ToggleMiniMap(bool display)
        {
            if (miniMap != null) miniMap.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private string GetMenuGroup(Type type)
        {
            string group = Tr(DialogueNode.Editor.GetGroup(type));
            if (string.IsNullOrEmpty(group)) return group;
            else
            {
                var groups = group.Split('/');
                group = string.Empty;
                for (int i = 0; i < groups.Length; i++)
                {
                    group += Tr(groups[i]) + '/';
                }
                return group;
            }
        }
        private Vector2 GetCenter(IEnumerable<Vector2> positions)
        {
            Vector2 min = new Vector2(positions.Min(p => p.x), positions.Min(p => p.y));
            Vector2 max = new Vector2(positions.Max(p => p.x), positions.Max(p => p.y));
            return UtilityZT.CenterBetween(min, max);
        }

        private void BeforeModify() => Undo.RecordObject(Dialogue, Tr("修改 {0}", Dialogue.name));
        public void CheckErrors()
        {
            foreach (var l in errors)
            {
                errorCache.Release(l);
            }
            errors.Clear();
            if (Dialogue)
            {
                if (invalidNodes.Count > 0)
                {
                    var label = errorCache.Get();
                    label.text = UtilityZT.ColorText($"{Tr("错误")}: {Tr("请先修复或删除所有无效结点然后刷新")}", Color.red);
                    Action action = () =>
                    {
                        ClearSelection();
                        AddToSelection(invalidNodeGroup.containedElements.FirstOrDefault());
                        FrameSelection();
                    };
                    label.userData = action;
                }
                else
                {
                    var contentInvalid = new HashSet<DialogueNode>();
                    var optionInvalid = new HashSet<DialogueNode>();
                    if (!Dialogue.Exitable)
                    {
                        var label = errorCache.Get();
                        label.text = UtilityZT.ColorText($"{Tr("错误")}: {Tr("对话无结束点")}", Color.red);
                        Action action = () =>
                        {
                            ClearSelection();
                            AddToSelection(exit);
                            FrameSelection();
                        };
                        label.userData = action;
                    }
                    if (Dialogue.Traverse(dialogue.Entry, n => n.Options.Count > 0 && n.Options.All(o => !o.IsMain && o.Next is ConditionNode && o.Next.Exitable)))
                    {
                        var label = errorCache.Get();
                        label.text = UtilityZT.ColorText($"{Tr("警告")}: {Tr("对话可能无结束点")}", Color.yellow);
                    }
                    Dialogue.Traverse(dialogue.Entry, node =>
                    {
                        var i = dialogue.Nodes.IndexOf(node);
                        if (!contentInvalid.Contains(node) && !node.IsValid)
                        {
                            var label = errorCache.Get();
                            label.text = UtilityZT.ColorText($"{Tr("错误")}: {Tr("第{0}个结点填写错误", i)}", Color.red);
                            Action action = () =>
                            {
                                ClearSelection();
                                AddToSelection(GetNodeByGuid(node.ID));
                                FrameSelection();
                            };
                            label.userData = action;
                            contentInvalid.Add(node);
                        }
                        if (!optionInvalid.Contains(node) && !node.ExitHere && node.Options.Any(x => x.Next is null))
                        {
                            var label = errorCache.Get();
                            label.text = UtilityZT.ColorText($"{Tr("错误")}: {Tr("第{0}个结点存在无效选项", i)}", Color.red);
                            Action action = () =>
                            {
                                ClearSelection();
                                AddToSelection(GetNodeByGuid(node.ID));
                                FrameSelection();
                            };
                            label.userData = action;
                            optionInvalid.Add(node);
                        }
                    });
                }
            }
        }

        private void CreateNewScript(ScriptTemplate template)
        {
            UtilityZT.Editor.SaveFolderPanel(path =>
            {
                if (path.EndsWith("/")) path = path[..^1];

                UnityEngine.Object script = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);

                string templatePath = AssetDatabase.GetAssetPath(template.templateFile);
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, template.fileName);
            });
        }
        #endregion

        private string Tr(string text) => ZetanStudio.Editor.EDL.Tr(text);
        private string Tr(string text, params object[] args) => ZetanStudio.Editor.EDL.Tr(text, args);
    }
}