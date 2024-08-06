using UnityEditor;
using UnityEngine;

namespace ZetanStudio.Editor
{
    using Extension.Editor;

    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfDrawer : EnhancedPropertyDrawer
    {
        private bool shouldHide;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideAttr = (HideIfAttribute)attribute;
            if (!shouldHide || hideAttr.readOnly)
            {
                label = EditorGUI.BeginProperty(position, label, property);
                EditorGUI.BeginDisabledGroup(shouldHide && hideAttr.readOnly);
                PropertyField(position, property, label);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideAttr = (HideIfAttribute)attribute;
            this.TryGetOwnerValue(property, out var owner);
            shouldHide = (hideAttr as ICheckValueAttribute).Check(owner);
            if (!shouldHide || hideAttr.readOnly) return base.GetPropertyHeight(property, label);
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
}