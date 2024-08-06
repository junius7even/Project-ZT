using UnityEditor;
using UnityEngine;

namespace ZetanStudio.DialogueSystem.Editor
{
    using ZetanStudio.Editor;

    [CustomEditor(typeof(DialogueEditorSettings))]
    public class DialogueEditorSettingsInspector : UnityEditor.Editor
    {
        SerializedProperty uxml;
        SerializedProperty uss;
        private bool enablePortrait = false;
        private bool enableVoice = false;

        private void OnEnable()
        {
            uxml = serializedObject.FindProperty("editorUxml");
            uss = serializedObject.FindProperty("editorUss");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
#if !ZTDS_DISABLE_PORTRAIT
            enablePortrait = true;
#endif
            if (enablePortrait != EditorGUILayout.Toggle(EDL.Tr("启用对话肖像"), enablePortrait))
            {
                enablePortrait = !enablePortrait;
                SwitchPortraitDefine();
            }
#if !ZTDS_DISABLE_VOICE
            enableVoice = true;
#endif
            if (enableVoice != EditorGUILayout.Toggle(EDL.Tr("启用对话语音"), enableVoice))
            {
                enableVoice = !enableVoice;
                SwitchVoiceDefine();
            }
            if (!uxml.objectReferenceValue && GUILayout.Button(EDL.Tr("创建UXML"))) uxml.objectReferenceValue = DialogueEditorSettings.CreateUXML();
            if (!uss.objectReferenceValue && GUILayout.Button(EDL.Tr("创建USS"))) uss.objectReferenceValue = DialogueEditorSettings.CreateUSS();
        }

        private void SwitchPortraitDefine()
        {
#if !ZTDS_DISABLE_PORTRAIT
            UtilityZT.Editor.AddScriptingDefineSymbol("ZTDS_DISABLE_PORTRAIT");
#else
            UtilityZT.Editor.RemoveScriptingDefineSymbol("ZTDS_DISABLE_PORTRAIT");
#endif
        }
        private void SwitchVoiceDefine()
        {
#if !ZTDS_DISABLE_VOICE
            UtilityZT.Editor.AddScriptingDefineSymbol("ZTDS_DISABLE_VOICE");
#else
            UtilityZT.Editor.RemoveScriptingDefineSymbol("ZTDS_DISABLE_VOICE");
#endif
        }
    }
}