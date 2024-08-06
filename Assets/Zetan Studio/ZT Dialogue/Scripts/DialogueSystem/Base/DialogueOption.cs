using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using ZetanStudio.Editor;
#endif

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 对话选项是非后缀结点的构成要素<br/>
    /// Dialogue options are elements of non-suffix-node.
    /// </summary>
    [Serializable]
    public sealed class DialogueOption
    {
        /// <summary>
        /// UI 选项按钮的文本<br/>
        /// Text to show on UI option buttons.
        /// </summary>
        [field: SerializeField]
        public string Title { get; private set; }

        /// <summary>
        /// 选项是否为主要选项<br/>
        /// Is this option a main option.
        /// </summary>
        [field: SerializeField]
        public bool IsMain { get; private set; }

        /// <summary>
        /// 选项所连结点<br/>
        /// The connected node of this option.
        /// </summary>
        [field: SerializeReference]
        public DialogueNode Next { get; private set; }

        public static DialogueOption Main => new DialogueOption(true, null);

        public DialogueOption() { }

        public DialogueOption(bool main, string title)
        {
            IsMain = main;
            Title = title;
        }

        public static implicit operator bool(DialogueOption obj) => obj != null;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用类，不应在游戏逻辑中使用<br/>
        /// An Editor-Use class, should not use in game logic code.
        /// </summary>
        public static class Editor
        {
            public static void SetNext(DialogueOption option, DialogueNode node)
            {
                if (node is EntryNode or ExitNode) return;
                option.Next = node;
            }

            public static void SetIsMain(DialogueOption option, bool main) => option.IsMain = main;
        }

        [MenuItem("Tools/Zetan Studio/收集所有选项标题 (Collect Option Titles)")]
        private static void Collect()
        {
            if (EditorUtility.DisplayDialog(EDL.Tr("提示"), EDL.Tr("将会在本地创建一个选项标题的翻译映射表，是否继续？"), EDL.Tr("继续"), EDL.Tr("取消")))
            {
                var language = UtilityZT.Editor.SaveFilePanel(ScriptableObject.CreateInstance<Translation>, "Option Language");
                var items = new List<TranslationItem>(); ;
                items.Clear();
                var keys = new HashSet<string>();
                var count = 0;
                var dialogues = UtilityZT.Editor.LoadAssets<Dialogue>();
                foreach (var dialogue in dialogues)
                {
                    EditorUtility.DisplayProgressBar(EDL.Tr("收集中"), EDL.Tr("当前对话: {0}", dialogue.name), ++count / dialogues.Count);
                    foreach (var node in dialogue.Nodes)
                    {
                        foreach (var option in node.Options)
                        {
                            if (!option.IsMain && !string.IsNullOrEmpty(option.Title) && !keys.Contains(option.Title))
                            {
                                keys.Add(option.Title);
                                items.Add(new TranslationItem(option.Title, option.Title));
                            }
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
                Translation.Editor.SetItems(language, items.ToArray());
                UtilityZT.Editor.SaveChange(language);
                EditorGUIUtility.PingObject(language);
            }
        }
#endif
    }
}