using UnityEditor;
using UnityEngine;

namespace ZetanStudio.DialogueSystem.Editor
{
    using ZetanStudio.Editor;

    [CustomEditor(typeof(Dialogue))]
    public class DialogueInspector : UnityEditor.Editor
    {
        SerializedProperty _name;
        SerializedProperty description;

        private void OnEnable()
        {
            try
            {
                _name = serializedObject.FindProperty("_name");
                description = serializedObject.FindProperty("_description");
            }
            catch { }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_name, new GUIContent(EDL.Tr("名称")));
            EditorGUILayout.PropertyField(description, new GUIContent(EDL.Tr("描述")));
            EditorGUILayout.LabelField(EDL.Tr("预览"));
            var style = new GUIStyle(EditorStyles.textArea);
            style.wordWrap = true;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(DialogueEditor.Preview(target as Dialogue), style);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }
    }
}
