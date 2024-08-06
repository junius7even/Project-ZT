using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZetanStudio.LanguageSystem.Editor
{
    using ZetanStudio.Editor;

    [CustomEditor(typeof(Translation))]
    public class TranslationInspector : UnityEditor.Editor
    {
        SerializedProperty _name;
        SerializedProperty items;

        PaginatedReorderableList list;

        private void OnEnable()
        {
            _name = serializedObject.FindProperty("_name");
            list = new PaginatedReorderableList(items = serializedObject.FindProperty("items"), 40);
        }

        public override void OnInspectorGUI()
        {
            var items = (target as Translation).Items;
            string duplicatedKey = null;
            Action callback = null;
            foreach (var item in items)
            {
                if (items.Any(m => m != item && m.Key == item.Key))
                {
                    duplicatedKey = item.Key;
                    break;
                }
            }
            if (duplicatedKey != null)
            {
                EditorGUILayout.HelpBox($"{EDL.Tr("存在相同的键")}：{duplicatedKey}", MessageType.Error);
                callback = () =>
                {
                    list.Search(duplicatedKey);
                };
            }
            else if (items.FirstOrDefault(m => string.IsNullOrEmpty(m.Key)) is TranslationItem empty)
            {
                EditorGUILayout.HelpBox(EDL.Tr("存在空的键"), MessageType.Error);
                callback = () =>
                {
                    var index = items.IndexOf(empty);
                    list.Select(index);
                    this.items.GetArrayElementAtIndex(index).isExpanded = true;
                };
            }
            else EditorGUILayout.HelpBox(EDL.Tr("无错误"), MessageType.Info);
            EditorGUI.BeginDisabledGroup(callback == null);
            if (GUILayout.Button(EDL.Tr("查看错误"))) callback();
            EditorGUI.EndDisabledGroup();

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_name);
            list?.DoLayoutList();
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }
    }
}
