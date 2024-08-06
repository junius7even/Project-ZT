using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ZetanStudio.ConditionSystem.Editor
{
    using ZetanStudio.Editor;

    [CustomPropertyDrawer(typeof(ConditionGroup))]
    public class ConditionGroupDrawer : PropertyDrawer
    {
        private SerializedProperty conditions;
        private readonly float lineHeight;
        private readonly float lineHeightSpace;

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public ConditionGroupDrawer()
        {
            lineHeight = EditorGUIUtility.singleLineHeight;
            lineHeightSpace = lineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            conditions = property.FindPropertyRelative("conditions");
            if (property.isExpanded) return lineHeightSpace + EditorGUI.GetPropertyHeight(conditions) + (conditions.arraySize < 1 ? 0 : EditorGUIUtility.singleLineHeight);
            else return lineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), property.isExpanded, label, true))
            {
                int lineCount = 1;
                if (conditions.arraySize > 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(new Rect(position.x, position.y + lineCount * lineHeightSpace, position.width, lineHeight),
                        property.FindPropertyRelative("formula"), new GUIContent(EDL.Tr("计算公式"),
                        EDL.Tr("条件计算公式\n1、操作数为条件的下标\n2、运算符可使用 \"(\"、\")\"、\"|\"(或)、\"&\"(且)、\"!\"(非)\n" +
                        "3、未对非法输入进行处理，需规范填写\n4、例：\"(0 | 1) & !2\" 表示满足条件0或1且不满足条件2\n5、为空时默认进行相互的“且”运算")));
                    lineCount++;
                    EditorGUI.indentLevel--;
                }
                label = new GUIContent(label);
                int notCmpltCount = 0;
                for (int i = 0; i < conditions.arraySize; i++)
                {
                    if (conditions.GetArrayElementAtIndex(i).managedReferenceValue is not Condition condition || !condition.IsValid) notCmpltCount++;
                }
                label.text = $"{EDL.Tr("条件表")}{(notCmpltCount > 0 ? $"({EDL.Tr("未补全")}: {notCmpltCount})" : string.Empty)}";
                EditorGUI.PropertyField(new Rect(position.x, position.y + lineCount * lineHeightSpace, position.width, position.height - lineCount * lineHeightSpace), conditions, label);
            }
        }
    }
}
