using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ZetanStudio.Editor
{
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelDrawer : EnhancedPropertyDrawer
    {
        private static EditorMiscSettings settings;

        static LabelDrawer()
        {
            settings = EditorMiscSettings.GetOrCreate();
            EditorApplication.projectChanged += () =>
            {
                if (!EditorApplication.isCompiling && !settings) settings = EditorMiscSettings.GetOrCreate();
            };
        }

        [MenuItem("Tools/Zetan Studio/收集所有Label标签 (Collect Labels)")]
        private static void Collect()
        {
            if (EditorUtility.DisplayDialog(EDL.Tr("提示"), EDL.Tr("将会在本地创建一个标签的翻译映射表并引用，是否继续？"), EDL.Tr("继续"), EDL.Tr("取消")))
            {
                var language = UtilityZT.Editor.SaveFilePanel(ScriptableObject.CreateInstance<Translation>, "Label Translation");
                var items = new List<TranslationItem>(); ;
                items.Clear();
                var keys = new HashSet<string>();
                var count = 0;
                var fields = TypeCache.GetFieldsWithAttribute<LabelAttribute>();
                foreach (var field in fields)
                {
                    EditorUtility.DisplayProgressBar(EDL.Tr("收集中"), EDL.Tr("当前字段: {0}", field.Name + " : " + field.DeclaringType.FullName), ++count / fields.Count);
                    string label = field.GetCustomAttribute<LabelAttribute>().name;
                    if (!string.IsNullOrEmpty(label) && !keys.Contains(label))
                    {
                        keys.Add(label);
                        items.Add(new TranslationItem(label, label));
                    }
                    label = field.GetCustomAttribute<LabelAttribute>().tooltip;
                    if (!string.IsNullOrEmpty(label) && !keys.Contains(label))
                    {
                        keys.Add(label);
                        items.Add(new TranslationItem(label, label));
                    }
                }
                EditorUtility.ClearProgressBar();
                Translation.Editor.SetItems(language, items.ToArray());
                UtilityZT.Editor.SaveChange(language);
                EditorGUIUtility.PingObject(language);
                typeof(EditorMiscSettings).GetField("labelTranslation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, language);
                UtilityZT.Editor.SaveChange(settings);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute attribute = this.attribute as LabelAttribute;
            if (!string.IsNullOrEmpty(label.text))
            {
                label = new GUIContent(label);
                label.text = Tr(attribute.name);
                if (!string.IsNullOrEmpty(attribute.tooltip))
                    label.tooltip = Tr(attribute.tooltip);
            }
            PropertyField(position, property, label);
        }

        private static string Tr(string text)
        {
            return Language.Tr(settings ? settings.LabelTranslation : null, text);
        }
    }
}