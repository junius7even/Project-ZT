using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.DialogueSystem.Editor
{
    using Extension.Editor;
    using ZetanStudio.Editor;

    public class DialogueEditor : EditorWindow
    {
        #region 静态方法 Static Methods
        [MenuItem("Window/Zetan Studio/对话编辑器 (Dialogue Editor)")]
        public static void CreateWindow()
        {
            var settings = DialogueEditorSettings.GetOrCreate();
            DialogueEditor wnd = GetWindow<DialogueEditor>(EDL.Tr("对话编辑器"));
            wnd.minSize = settings.minWindowSize;
        }
        public static void CreateWindow(Dialogue dialogue)
        {
            var settings = DialogueEditorSettings.GetOrCreate();
            DialogueEditor wnd = GetWindow<DialogueEditor>(EDL.Tr("对话编辑器"));
            wnd.minSize = settings.minWindowSize;
            wnd.list.SetSelection(wnd.dialogues.IndexOf(dialogue));
            EditorApplication.delayCall += () => wnd.list.ScrollToItem(wnd.dialogues.IndexOf(dialogue));
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is Dialogue dialogue)
            {
                CreateWindow(dialogue);
                return true;
            }
            return false;
        }
        #endregion

        #region 变量声明 Declaration
        private DialogueEditorSettings settings;
        private Button delete;
        private ToolbarSearchField searchField;
        private DialogueGraph dialogueGraph;
        private ListView list;
        private DropdownField searchType;
        private ListView searchList;
        private List<Dialogue> dialogues = new List<Dialogue>();
        [SerializeField] private Dialogue selectedDialogue;
        private IMGUIContainer inspector;
        private VisualElement eventsView;
        private IMGUIContainer eventsInspector;
        private Action onLoseFocus;

        [SerializeField] private bool miniMap = true;
        [SerializeField] private int searchTypeIndex;
        [SerializeField] private GraphViewTransformData viewTransformData;
        #endregion

        #region Unity回调 Unity Callbacks
        public void CreateGUI()
        {
            try
            {
                settings = settings ? settings : DialogueEditorSettings.GetOrCreate();

                VisualElement root = rootVisualElement;

                if (!settings.editorUxml)
                {
                    settings.editorUxml = DialogueEditorSettings.CreateUXML();
                    UtilityZT.Editor.SaveChange(settings);
                }
                settings.editorUxml.CloneTree(root);

                if (!settings.editorUss)
                {
                    settings.editorUss = DialogueEditorSettings.CreateUSS();
                    UtilityZT.Editor.SaveChange(settings);
                }
                root.styleSheets.Add(settings.editorUss);

                root.Query<Label>().ForEach(l => l.text = Tr(l.text));
                root.Query<Button>().ForEach(b => b.text = Tr(b.text));
                root.Q<Button>("create").clicked += OnCreateClicked;

                delete = root.Q<Button>("delete");
                delete.clicked += OnDeleteClicked;
                Toggle toggle = root.Q<Toggle>("minimap-toggle");
                toggle.RegisterValueChangedCallback(evt =>
                {
                    miniMap = evt.newValue;
                    dialogueGraph?.ToggleMiniMap(evt.newValue);
                });
                toggle.SetValueWithoutNotify(miniMap);

                searchType = root.Q<DropdownField>("search-type");
                searchType.choices = new List<string>() { Tr("当前"), Tr("全部") };
                searchType.index = searchTypeIndex;
                searchType.RegisterValueChangedCallback(evt =>
                {
                    searchTypeIndex = searchType.index;
                });
                searchField = root.Q<ToolbarSearchField>();
                searchField.RegisterValueChangedCallback(evt => OnSearchStringChanged(evt.newValue));
                root.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (!string.IsNullOrEmpty(searchField.value) && !searchList.Contains(evt.target as VisualElement) && !searchField.Contains(evt.target as VisualElement))
                        searchField.value = string.Empty;
                });
                root.Q<Button>("refresh-graph").clicked += () => dialogueGraph?.ViewDialogue(selectedDialogue);
                searchList = root.Q<ListView>("search-list");
                searchList.selectionType = SelectionType.Single;
                searchList.makeItem = () => new Label() { enableRichText = true };
                searchList.onSelectionChange += OnSearchListSelected;
                OnSearchStringChanged();

                dialogueGraph = new DialogueGraph();
                dialogueGraph.nodeSelectedCallback = OnNodeSelected;
                dialogueGraph.nodeUnselectedCallback = OnNodeUnselected;
                dialogueGraph.onViewTransformChanged = OnGraphViewTransformChanged;
                root.Q("right-container").Insert(0, dialogueGraph);
                dialogueGraph.StretchToParentSize();
                if (!viewTransformData.init)
                {
                    viewTransformData.position = dialogueGraph.viewTransform.position;
                    viewTransformData.scale = dialogueGraph.viewTransform.scale;
                    viewTransformData.init = true;
                }
                else dialogueGraph.UpdateViewTransform(viewTransformData.position, viewTransformData.scale);
                dialogueGraph.ToggleMiniMap(miniMap);

                list = root.Q<ListView>("dialogue-list");
                list.selectionType = SelectionType.Multiple;
                list.makeItem = () =>
                {
                    var label = new Label();
                    label.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        if (label.userData is Dialogue dialogue)
                        {
                            evt.menu.AppendAction(Tr("删除"), a =>
                            {
                                if (EditorUtility.DisplayDialog(Tr("删除"), Tr("确定要将该对话移至回收站吗？"), Tr("确定"), Tr("取消")))
                                    if (AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(dialogue)))
                                    {
                                        if (dialogue == selectedDialogue)
                                        {
                                            selectedDialogue = null;
                                            list.ClearSelection();
                                            InspectDialogue();
                                            dialogueGraph?.ViewDialogue(null);
                                        }
                                    }
                            });
                            evt.menu.AppendAction(Tr("定位"), a =>
                            {
                                EditorGUIUtility.PingObject(dialogue);
                            });
                            evt.menu.AppendAction(Tr("重命名"), a =>
                            {
                                var path = AssetDatabase.GetAssetPath(dialogue);
                                var input = new TextField();
                                var oldValue = label.text;
                                label.text = null;
                                input.value = dialogue.name;
                                label.Add(input);
                                input.Focus();
                                input.RegisterCallback<FocusOutEvent>(evt =>
                                {
                                    confirm();
                                });
                                input.RegisterCallback<KeyDownEvent>(evt =>
                                {
                                    if (evt.keyCode == KeyCode.Escape)
                                    {
                                        cancel();
                                        evt.StopImmediatePropagation();
                                    }
                                    else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                                    {
                                        confirm();
                                        evt.StopImmediatePropagation();
                                    }
                                });
                                onLoseFocus += cancel;

                                void cancel()
                                {
                                    label.Remove(input);
                                    label.text = oldValue;
                                    onLoseFocus -= cancel;
                                }
                                void confirm()
                                {
                                    cancel();
                                    if (!string.IsNullOrEmpty(input.value) && dialogue.name != input.value)
                                        if (AssetDatabase.LoadAssetAtPath<Dialogue>(path.Replace($"{dialogue.name}.asset", $"{input.value}.asset")))
                                        {
                                            EditorUtility.DisplayDialog(Tr("错误"), Tr("该目录下已存在使用此名称的对话资源"), Tr("确定"));
                                        }
                                        else
                                        {
                                            AssetDatabase.RenameAsset(path, input.value);
                                            RefreshDialogues();
                                        }
                                }
                            });
                        }
                    }));
                    label.RegisterTooltipCallback(() => Preview(label.userData as Dialogue));
                    return label;
                };
                list.bindItem = (e, i) =>
                {
                    (e as Label).text = $"{dialogues[i]._name}[{dialogues[i].name}]";
                    e.userData = dialogues[i];
                };
                list.onSelectionChange += (os) =>
                {
                    if (os != null) OnDialogueSelected(os.Select(x => x as Dialogue));
                };
                RefreshDialogues();

                inspector = root.Q<IMGUIContainer>("inspector");
                inspector.style.marginTop = 2;
                inspector.style.paddingBottom = 5;

                eventsView = root.Q("events-view");
                root.Q<Button>("events-button").clicked += OnEventsClicked;
                eventsInspector = root.Q<IMGUIContainer>("events-inspector");
                eventsView.style.display = DisplayStyle.None;
                eventsInspector.onGUIHandler = null;

                if (selectedDialogue)
                {
                    list.SetSelection(dialogues.IndexOf(selectedDialogue));
                    list.ScrollToItem(dialogues.IndexOf(selectedDialogue));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        private void OnFocus()
        {
            dialogueGraph?.CheckErrors();
        }
        private void OnLostFocus()
        {
            onLoseFocus?.Invoke();
        }
        private void OnProjectChange()
        {
            RefreshDialogues();
        }
        private void OnDestroy()
        {
            foreach (var dialogue in dialogues)
            {
                AssetDatabase.SaveAssetIfDirty(dialogue);
            }
        }
        #endregion

        #region 各种回调 Other Callbacks
        private void OnGraphViewTransformChanged(Vector3 position, Vector3 scale)
        {
            viewTransformData.position = position;
            viewTransformData.scale = scale;
        }

        private void OnSearchStringChanged(string keywords = null)
        {
            IList itemsSource = new List<object>();
            Action<VisualElement, int> bindItem = (e, i) => { };
            bool empty = string.IsNullOrEmpty(keywords);
            searchList.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
            if (!empty)
            {
                List<string> contents = new List<string>();
                List<string> tooltips = new List<string>();
                if (searchType.index == 0)
                {
                    if (selectedDialogue)
                        for (int i = 0; i < selectedDialogue.Nodes.Count; i++)
                        {
                            var node = selectedDialogue.Nodes[i];
                            if (searchNodeID(i, node, out var content, out var tooltip)
                                || searchNodeInter(i, node, out content, out tooltip)
                                || searchNodeCon(i, node, out content, out tooltip)
                                || searchNodeType(i, node, out content, out tooltip)
                                || searchNodeOpt(i, node, out content, out tooltip))
                            {
                                itemsSource.Add((Action)delegate
                                {
                                    dialogueGraph.ClearSelection();
                                    dialogueGraph.AddToSelection(dialogueGraph.GetNodeByGuid(node.ID));
                                    dialogueGraph.FrameSelection();
                                });
                                contents.Add(content);
                                tooltips.Add(tooltip);
                            }
                        }
                }
                else foreach (var dialog in dialogues)
                    {
                        if (searchID(dialog, out var content, out var tooltip)
                            || searchName(dialog, out content, out tooltip)
                            || searchDesc(dialog, out content, out tooltip))
                        {
                            itemsSource.Add((Action)delegate
                            {
                                list.SetSelection(dialogues.IndexOf(dialog));
                                list.ScrollToItem(dialogues.IndexOf(dialog));
                            });
                            contents.Add(content);
                            tooltips.Add(tooltip);
                        }
                        else
                        {
                            for (int i = 0; i < dialog.Nodes.Count; i++)
                            {
                                var node = dialog.Nodes[i];
                                if (searchNodeID(i, node, out content, out tooltip)
                                    || searchNodeInter(i, node, out content, out tooltip)
                                    || searchNodeCon(i, node, out content, out tooltip)
                                    || searchNodeType(i, node, out content, out tooltip)
                                    || searchNodeOpt(i, node, out content, out tooltip))
                                {
                                    itemsSource.Add((Action)delegate
                                    {
                                        list.SetSelection(dialogues.IndexOf(dialog));
                                        list.ScrollToItem(dialogues.IndexOf(dialog));
                                        EditorApplication.delayCall += () =>
                                        {
                                            dialogueGraph.ClearSelection();
                                            dialogueGraph.AddToSelection(dialogueGraph.GetNodeByGuid(node.ID));
                                            dialogueGraph.FrameSelection();
                                        };
                                    });
                                    contents.Add($"{dialog.name} : {content}");
                                    tooltips.Add($"{dialog.name} : {tooltip}");
                                }
                            }
                        }
                    }
                bindItem = (e, i) =>
                {
                    (e as Label).text = contents[i];
                    e.tooltip = tooltips[i];
                };

                #region 检索
                bool searchID(Dialogue dialogue, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    var text = dialogue.ID;
                    bool result = text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (result)
                    {
                        content = $"{dialogue.name}\n({Tr("ID")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, text.Length)})";
                        tooltip = $"{dialogue.name}\n({Tr("ID")}: {text})";
                    }
                    return result;
                }
                bool searchName(Dialogue dialogue, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    var text = dialogue.name;
                    bool result = text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (result)
                    {
                        content = $"{UtilityZT.Editor.HighlightKeyword(text, keywords, text.Length)}";
                        tooltip = text;
                    }
                    else
                    {
                        text = UtilityZT.RemoveTags(dialogue._name);
                        result = text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (result)
                        {
                            content = $"{dialogue.name}\n({Tr("名称")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, text.Length)})";
                            tooltip = text;
                        }
                    }
                    return result;
                }
                bool searchDesc(Dialogue dialogue, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    var text = UtilityZT.RemoveTags(dialogue._description);
                    bool result = text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (result)
                    {
                        content = $"{dialogue.name}\n({Tr("描述")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                        tooltip = $"{dialogue.name}\n({Tr("描述")}: {text})";
                    }
                    return result;
                }
                bool searchNodeInter(int index, DialogueNode node, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    if (node is SentenceNode sentence)
                    {
                        var text = UtilityZT.RemoveTags(sentence.Interlocutor);
                        if (text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("对话人")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("对话人")}: {text})";
                            return true;
                        }
                        else if (Keyword.Editor.HandleKeywords(text).IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var kvps = Keyword.Editor.ExtractKeyWords(text);
                            foreach (var kvp in kvps)
                            {
                                text = text.Replace(kvp.Key, $"{kvp.Key}({kvp.Value})");
                            }
                            content = $"{Tr("结点 {0}", index)}\n({Tr("对话人")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("对话人")}: {text})";
                            return true;
                        }
                    }
                    return false;
                }
                bool searchNodeID(int index, DialogueNode node, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    if (node)
                    {
                        var text = node.ID;
                        if (text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("结点ID")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("结点ID")}: {text})";
                            return true;
                        }
                    }
                    return false;
                }
                bool searchNodeCon(int index, DialogueNode node, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    if (node is SentenceNode sentence)
                    {
                        var text = UtilityZT.RemoveTags(sentence.Content);
                        if (text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("对话文本")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("对话文本")}: {text})";
                            return true;
                        }
                        else if (Keyword.Editor.HandleKeywords(text).IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var kvps = Keyword.Editor.ExtractKeyWords(text);
                            foreach (var kvp in kvps)
                            {
                                text = text.Replace(kvp.Key, $"{kvp.Key}({kvp.Value})");
                            }
                            content = $"{Tr("结点 {0}", index)}\n({Tr("对话文本")}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("对话文本")}: {text})";
                            return true;
                        }
                    }
                    return false;
                }
                bool searchNodeOpt(int index, DialogueNode node, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    for (int i = 0; i < node.Options.Count; i++)
                    {
                        var option = node.Options[i];
                        if (option.IsMain) continue;
                        var text = UtilityZT.RemoveTags(option.Title);
                        if (text.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("选项 {0}", i)}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("选项 {0}", i)}: {text})";
                            return true;
                        }
                        else if (Keyword.Editor.HandleKeywords(text).IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var kvps = Keyword.Editor.ExtractKeyWords(text);
                            foreach (var kvp in kvps)
                            {
                                text = text.Replace(kvp.Key, $"{kvp.Key}({kvp.Value})");
                            }
                            content = $"{Tr("结点 {0}", index)}\n({Tr("选项 {0}", i)}: {UtilityZT.Editor.HighlightKeyword(text, keywords, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("选项 {0}", i)}: {text})";
                            return true;
                        }
                    }
                    return false;
                }
                bool searchNodeType(int index, DialogueNode node, out string content, out string tooltip)
                {
                    content = null;
                    tooltip = null;
                    if (keywords.StartsWith("t:", StringComparison.OrdinalIgnoreCase) && keywords.Length > 2 && node)
                    {
                        var temp = keywords.Replace("t:", "", StringComparison.OrdinalIgnoreCase);
                        if (node.GetType().Name.IndexOf(temp, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("类型")}: {UtilityZT.Editor.HighlightKeyword(node.GetType().Name, temp, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("类型")}: {node.GetType().Name})";
                            return true;
                        }
                        var name = Tr(node.GetName());
                        if (name.IndexOf(temp, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            content = $"{Tr("结点 {0}", index)}\n({Tr("类型")}: {UtilityZT.Editor.HighlightKeyword(name, temp, 30)})";
                            tooltip = $"{Tr("结点 {0}", index)}\n({Tr("类型")}: {name})";
                            return true;
                        }
                    }
                    return false;
                }

                #endregion
            }
            searchList.itemsSource = itemsSource;
            searchList.bindItem = bindItem;
            searchList.RefreshItems();
        }
        private void OnSearchListSelected(IEnumerable<object> os)
        {
            (os.FirstOrDefault() as Action)?.Invoke();
            searchList.SetSelectionWithoutNotify(new int[0]);
            searchField.value = null;
        }

        private void OnDialogueSelected(IEnumerable<Dialogue> dialogues)
        {
            var bef = selectedDialogue;
            if (dialogues.Count() == 1) selectedDialogue = dialogues?.FirstOrDefault();
            else selectedDialogue = null;
            dialogueGraph?.ViewDialogue(selectedDialogue);
            if (bef == null) EditorApplication.delayCall += () => EditorApplication.delayCall += () => dialogueGraph?.FrameAll();
            InspectDialogue();
        }

        private void OnNodeSelected(DialogueGraphNode node)
        {
            inspector.onGUIHandler = null;
            inspector.Clear();
            if (node == null || dialogueGraph.nodes.Count(x => x.selected) > 1) return;
            inspector.onGUIHandler = () =>
            {
                if (node.SerializedNode?.serializedObject?.targetObject)
                {
                    node.SerializedNode.serializedObject.UpdateIfRequiredOrScript();
                    EditorGUI.BeginChangeCheck();
                    using var copy = node.SerializedNode.Copy();
                    SerializedProperty end = copy.GetEndProperty();
                    bool enter = true;
                    while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
                    {
                        enter = false;
                        if (!copy.IsRawName("options") && !copy.IsRawName("events") && !copy.IsRawName("exitHere")
                            && (node.Target is SentenceNode || !copy.IsRawName("Text") && !copy.IsRawName("Interlocutor")))
                            EditorGUILayout.PropertyField(copy, true);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        node.SerializedNode.serializedObject.ApplyModifiedProperties();
                        dialogueGraph?.CheckErrors();
                    }
                    if (node.Target is IEventNode)
                    {
                        var events = node.SerializedNode.FindPropertyRelative("events");
                        if (events.arraySize < 1)
                        {
                            if (GUILayout.Button(Tr("查看事件")))
                                InspectEvents(events);
                        }
                        else if (GUILayout.Button(Tr("查看{0}个事件", events.arraySize)))
                            InspectEvents(events);
                    }
                }
            };
        }
        private void OnNodeUnselected(DialogueGraphNode node)
        {
            if (dialogueGraph.nodes.Count(x => x.selected) < 1)
                InspectDialogue();
        }

        private void OnCreateClicked()
        {
            Dialogue dialogue = UtilityZT.Editor.SaveFilePanel(CreateInstance<Dialogue>);
            if (dialogue)
            {
                Selection.activeObject = dialogue;
                EditorGUIUtility.PingObject(dialogue);
                list.SetSelection(dialogues.IndexOf(dialogue));
                list.ScrollToItem(dialogues.IndexOf(dialogue));
            }
        }
        private void OnDeleteClicked()
        {
            if (EditorUtility.DisplayDialog(Tr("删除"), Tr("确定要将选中的对话移至回收站吗？"), Tr("确定"), Tr("取消")))
                if (AssetDatabase.MoveAssetsToTrash(list.selectedItems.Select(x => AssetDatabase.GetAssetPath(x as Dialogue)).ToArray(), new List<string>()))
                {
                    selectedDialogue = null;
                    list.ClearSelection();
                    InspectDialogue();
                    dialogueGraph?.ViewDialogue(null);
                }
        }
        private void OnEventsClicked()
        {
            eventsView.style.display = DisplayStyle.None;
            eventsInspector.onGUIHandler = null;
        }
        #endregion

        #region 其它 Other
        private void RefreshDialogues()
        {
            dialogues = UtilityZT.Editor.LoadAssets<Dialogue>();
            dialogues.Sort((x, y) => UtilityZT.CompareStringNumbericSuffix(x.name, y.name));
            if (list != null)
            {
                list.itemsSource = dialogues;
                list.Rebuild();
                if (dialogues.Contains(selectedDialogue))
                {
                    list.SetSelection(dialogues.IndexOf(selectedDialogue));
                    list.ScrollToItem(dialogues.IndexOf(selectedDialogue));
                }
                else
                {
                    selectedDialogue = null;
                    dialogueGraph?.ViewDialogue(selectedDialogue);
                }
            }
        }

        private void InspectDialogue()
        {
            if (inspector == null) return;
            inspector.Clear();
            inspector.onGUIHandler = null;
            if (selectedDialogue)
            {
                var editor = UnityEditor.Editor.CreateEditor(selectedDialogue);
                inspector.onGUIHandler = () =>
                {
                    var oldName = selectedDialogue._name;
                    if (editor && editor.serializedObject?.targetObject)
                        editor.OnInspectorGUI();
                    if (oldName != selectedDialogue._name)
                        list.RefreshItem(list.selectedIndex);
                };
            }
        }

        public void InspectEvents(SerializedProperty events)
        {
            if (events != null)
            {
                eventsView.style.display = DisplayStyle.Flex;
                eventsInspector.onGUIHandler = () =>
                {
                    events.serializedObject.UpdateIfRequiredOrScript();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(events, new GUIContent(Tr("对话事件")));
                    if (EditorGUI.EndChangeCheck())
                    {
                        events.serializedObject.ApplyModifiedProperties();
                        dialogueGraph?.CheckErrors();
                    }
                };
            }
        }

        private string Tr(string text)
        {
            return EDL.Tr(text);
        }
        private string Tr(string text, params object[] args)
        {
            return EDL.Tr(text, args);
        }

        public static string Preview(SentenceNode node)
        {
            string result = string.Empty;
            string interlocutor = EDL.Tr("[{0}]说：", string.IsNullOrEmpty(node.Interlocutor) ? $"({EDL.Tr("未定义")})" : Keyword.Editor.HandleKeywords(node.Interlocutor));
            interlocutor = Regex.Replace(interlocutor, @"{\[NPC\]}", $"({EDL.Tr("交互对象")})", RegexOptions.IgnoreCase);
            interlocutor = Regex.Replace(interlocutor, @"{\[PLAYER\]}", $"({EDL.Tr("玩家")})", RegexOptions.IgnoreCase);
            result += interlocutor;
            string text = $"{(string.IsNullOrEmpty(node.Content) ? $"({EDL.Tr("无内容")})" : Keyword.Editor.HandleKeywords(node.Content))}";
            text = Regex.Replace(text, @"{\[NPC\]}", $"[{EDL.Tr("交互对象")}]", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"{\[PLAYER\]}", $"[{EDL.Tr("玩家")}]", RegexOptions.IgnoreCase);
            result += text;
            return UtilityZT.RemoveTags(result);
        }
        public static string Preview(Dialogue dialogue)
        {
            if (!dialogue) return null;
            StringBuilder sb = new StringBuilder();
            foreach (var node in dialogue.Nodes)
            {
                if (node is SentenceNode sentence)
                {
                    sb.Append(Preview(sentence));
                    sb.Append('\n');
                }
                else if (node is OtherDialogueNode other && other.Dialogue)
                {
                    sb.Append(Preview(other.Dialogue.Entry));
                    sb.Append('\n');
                }
            }
            if (sb.Length > 0) sb.Remove(sb.Length - 1, 1); ;
            return sb.ToString();
        }
        #endregion

        [Serializable]
        private struct GraphViewTransformData
        {
            public Vector3 position;
            public Vector3 scale;
            public bool init;
        }
    }
}