using UnityEditor;

namespace ZetanStudio.LanguageSystem.Editor
{
    [CustomEditor(typeof(MultilingualText)), CanEditMultipleObjects]
    public class MultilingualTextInspector : UnityEditor.UI.TextEditor
    {
        SerializedProperty selector;

        protected override void OnEnable()
        {
            base.OnEnable();
            selector = serializedObject.FindProperty("m_selector");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selector);
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
