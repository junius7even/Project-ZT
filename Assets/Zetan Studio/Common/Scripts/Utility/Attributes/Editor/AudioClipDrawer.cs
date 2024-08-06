using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ZetanStudio.Editor
{
    [CustomPropertyDrawer(typeof(AudioClip))]
    public class AudioClipDrawer : PropertyDrawer
    {
        private readonly static Action<AudioClip> playPreviewClip =
            clip => typeof(PropertyDrawer).Assembly.GetType("UnityEditor.AudioUtil").GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { clip, 0, false });
        private readonly static Action stopAllPreviewClips =
            () => typeof(PropertyDrawer).Assembly.GetType("UnityEditor.AudioUtil").GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
        private readonly static Func<bool> isPreviewClipPlaying =
            () => (bool)typeof(PropertyDrawer).Assembly.GetType("UnityEditor.AudioUtil").GetMethod("IsPreviewClipPlaying", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue)
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 18, position.height), property, label);
                if (!isPreviewClipPlaying())
                {
                    if (GUI.Button(new Rect(position.x + position.width - 16, position.y, 16, position.height), new GUIContent(EditorGUIUtility.FindTexture("PlayButton")), EditorStyles.iconButton))
                        playPreviewClip(property.objectReferenceValue as AudioClip);
                }
                else if (GUI.Button(new Rect(position.x + position.width - 16, position.y, 16, position.height), new GUIContent(EditorGUIUtility.FindTexture("PauseButton")), EditorStyles.iconButton))
                    stopAllPreviewClips();
            }
            else EditorGUI.PropertyField(position, property, label);
        }
    }
}