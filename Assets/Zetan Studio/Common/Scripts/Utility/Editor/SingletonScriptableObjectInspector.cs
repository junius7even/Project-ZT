using System;
using UnityEditor;

namespace ZetanStudio.Editor
{
    [CustomEditor(typeof(SingletonScriptableObject), true)]
    public class SingletonScriptableObjectInspector : UnityEditor.Editor
    {
        private Type type;

        private void OnEnable()
        {
            type = target.GetType();
        }

        public sealed override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(EDL.Tr("单例"), type.BaseType.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null) as SingletonScriptableObject, type, false);
            EditorGUI.EndDisabledGroup();
            var list = UtilityZT.Editor.LoadResources(type);
            if (list.Count > 1)
            {
                string paths = EDL.Tr("存在多个实例：") + "\n";
                for (int i = 0; i < list.Count; i++)
                {
                    paths += EDL.Tr("路径") + " " + (i + 1) + ": " + AssetDatabase.GetAssetPath(list[i]);
                    if (i != list.Count - 1) paths += '\n';
                }
                EditorGUILayout.HelpBox(paths, MessageType.Error);
            }
            else OnInspectorGUI_();
        }

        protected virtual void OnInspectorGUI_()
        {
            base.OnInspectorGUI();
        }
    }
}