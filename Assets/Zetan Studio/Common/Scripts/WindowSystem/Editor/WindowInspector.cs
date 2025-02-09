﻿using UnityEditor;
using UnityEngine;
using ZetanStudio.Editor;

namespace ZetanStudio.UI.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(Window), true)]
    public class WindowInspector : UnityEditor.Editor
    {
        protected Window window;
        protected SerializedProperty animated;
        protected SerializedProperty duration;
        protected SerializedProperty content;
        protected SerializedProperty closeButton;

        protected void OnEnable()
        {
            window = target as Window;
            animated = serializedObject.FindProperty("animated");
            duration = serializedObject.FindProperty("duration");
            content = serializedObject.FindProperty("content");
            closeButton = serializedObject.FindProperty("closeButton");
            EnableOther();
        }
        protected virtual void EnableOther() { }

        public sealed override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying && !AssetDatabase.Contains(target))
            {
                if (GUILayout.Button(EDL.Tr("打开")))
                    window.Open();
                if (GUILayout.Button(EDL.Tr("关闭")))
                    window.Close();
                if (GUILayout.Button(EDL.Tr("显隐")))
                    window.Hide(!window.IsHidden);
            }
            else
            {
                if (content.objectReferenceValue is CanvasGroup group)
                {
                    if (GUILayout.Button(EDL.Tr("显示")))
                    {
                        group.alpha = 1;
                        group.blocksRaycasts = true;
                    }
                    if (GUILayout.Button(EDL.Tr("隐藏")))
                    {
                        group.alpha = 0;
                        group.blocksRaycasts = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width / 2 - 1, rect.height), animated);
            if (animated.boolValue) EditorGUI.PropertyField(new Rect(rect.x + rect.width / 2 + 1, rect.y, rect.width / 2 - 1, rect.height), duration);
            EditorGUILayout.PropertyField(content);
            EditorGUILayout.PropertyField(closeButton);
            InspectOther();
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InspectOther()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            int count = 0;
            while (iterator.NextVisible(enterChildren))
            {
                if (count > 4)
                    EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
                count++;
            }
        }
    }
}