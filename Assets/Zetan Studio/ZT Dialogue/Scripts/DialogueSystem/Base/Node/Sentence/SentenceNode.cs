using System;
using System.Collections.ObjectModel;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using ZetanStudio.Editor;
#endif

namespace ZetanStudio.DialogueSystem
{
    /// <summary>
    /// 语句结点是对话的基本单元，可以按需扩展<br/>
    /// Sentence node is the base unit of a dialogue, you can extend it if you need.
    /// </summary>
    [Group("语句"), Name("语句")]
    [Description("最基本的对话单位。")]
    public class SentenceNode : DialogueNode, IExitableNode, IEventNode
    {
        [field: SerializeField, Label("对话人"), HideInNode]
        public virtual string Interlocutor { get; protected set; }

        [field: SerializeField, Label("对话内容"), TextArea, HideInNode]
        public virtual string Content { get; protected set; }

        [field: SerializeField, Label("吐字间隔"), HideInNode]
        public virtual AnimationCurve SpeakInterval { get; protected set; } = new AnimationCurve(new Keyframe(0, 0.02f), new Keyframe(1, 0.02f));

#if !ZTDS_DISABLE_PORTRAIT
        [field: SerializeField, Label("肖像"), SpriteSelector, HideInNode]
        public virtual Sprite Portrait { get; protected set; }

        [field: SerializeField, Label("保持肖像位置", "如果想让肖像在对话人不一样时保持上一句的位置，勾选这个"), HideInNode]
        public virtual bool KeepPortraitSide { get; protected set; }
#endif
#if !ZTDS_DISABLE_VOICE
        [field: SerializeField, Label("语音")]
        public virtual AudioClip Voice { get; protected set; }
        [field: SerializeField, Label("语音偏移"), HideInNode, Min(0)]
        public virtual float VoiceOffset { get; protected set; }
#endif

        [SerializeReference, PolymorphismList("GetName"), HideInNode]
        protected DialogueEvent[] events = { };
        public ReadOnlyCollection<DialogueEvent> Events => new ReadOnlyCollection<DialogueEvent>(events);

        public override bool IsValid => !string.IsNullOrEmpty(Interlocutor) && !string.IsNullOrEmpty(Content) && Array.TrueForAll(events, e => e?.IsValid ?? false);

#if UNITY_EDITOR
        [MenuItem("Tools/Zetan Studio/收集所有对话人 (Collect Interlocutors)")]
        private static void CollectInterlocutors()
        {
            if (EditorUtility.DisplayDialog(EDL.Tr("提示"), EDL.Tr("将会在本地创建一个对话人的翻译映射表，是否继续？"), EDL.Tr("继续"), EDL.Tr("取消")))
            {
                var language = UtilityZT.Editor.SaveFilePanel(ScriptableObject.CreateInstance<Translation>, "Interlocutor Language");
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
                        if (node is SentenceNode sentence)
                        {
                            string interlocutor = sentence.Interlocutor;
                            if (!string.IsNullOrEmpty(interlocutor)
                                && !Keyword.IsKeyword(interlocutor)
                                && !interlocutor.Equals("{[NPC]}", StringComparison.OrdinalIgnoreCase)
                                && !interlocutor.Equals("{[PLAYER]}", StringComparison.OrdinalIgnoreCase)
                                && !keys.Contains(interlocutor))
                            {
                                keys.Add(interlocutor);
                                items.Add(new TranslationItem(interlocutor, interlocutor));
                            }
                        }
                    }
                }
                Translation.Editor.SetItems(language, items.ToArray());
                UtilityZT.Editor.SaveChange(language);
                EditorGUIUtility.PingObject(language);
            }
        }
        [MenuItem("Tools/Zetan Studio/收集所有对话内容 (Collect Speech Contents)")]
        private static void CollectContents()
        {
            if (EditorUtility.DisplayDialog(EDL.Tr("提示"), EDL.Tr("将会在本地创建一个对话语句的翻译映射表，是否继续？"), EDL.Tr("继续"), EDL.Tr("取消")))
            {
                var language = UtilityZT.Editor.SaveFilePanel(ScriptableObject.CreateInstance<Translation>, "Dialogue Language");
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
                        if (node is SentenceNode sentence)
                        {
                            if (!string.IsNullOrEmpty(sentence.Content) && !keys.Contains(sentence.Content))
                            {
                                keys.Add(sentence.Content);
                                items.Add(new TranslationItem(sentence.Content, sentence.Content));
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