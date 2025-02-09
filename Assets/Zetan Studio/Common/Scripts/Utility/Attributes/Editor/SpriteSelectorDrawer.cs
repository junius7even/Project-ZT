using UnityEditor;
using UnityEngine;

namespace ZetanStudio.Editor
{
    [CustomPropertyDrawer(typeof(SpriteSelectorAttribute))]
    public class SpriteSelectorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue as Sprite, typeof(Sprite), false);
            EditorGUI.EndProperty();
        }
    }
}