using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ZetanStudio.Editor
{
    [CustomPropertyDrawer(typeof(PolymorphismListAttribute))]
    public class PolymorphismItemDrawer : PropertyDrawer
    {
        private Type[] types;
        private Type elementType;
        private MethodInfo getGroup;
        private MethodInfo getName;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (types == null)
            {
                var attr = attribute as PolymorphismListAttribute;
                if (fieldInfo.FieldType.HasElementType) elementType = fieldInfo.FieldType.GetElementType();
                else if (typeof(List<>).IsAssignableFrom(fieldInfo.FieldType.GetGenericTypeDefinition())) elementType = fieldInfo.FieldType.GetGenericArguments()[0];
                if (elementType != null)
                {
                    types = TypeCache.GetTypesDerivedFrom(elementType).Where(x => !x.IsAbstract && !x.IsGenericType).Except(attr.excludedTypes).ToArray();
                    if (!string.IsNullOrEmpty(attr.getGroupMethod))
                    {
                        getGroup = elementType.GetMethod(attr.getGroupMethod, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (methodInvalid(getGroup)) getGroup = null;
                    }
                    if (!string.IsNullOrEmpty(attr.getNameMethod))
                    {
                        getName = elementType.GetMethod(attr.getNameMethod, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        if (methodInvalid(getName)) getName = null;
                    }
                }
            }
            if (elementType != null && typeof(IList).IsAssignableFrom(fieldInfo.FieldType) && property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var oldLW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.labelWidth -= EditorGUI.indentLevel * 15f;
                if (fieldInfo.FieldType.HasElementType) elementType = fieldInfo.FieldType.GetElementType();
                else if (typeof(List<>).IsAssignableFrom(fieldInfo.FieldType.GetGenericTypeDefinition())) elementType = fieldInfo.FieldType.GetGenericArguments()[0];
                if (property.managedReferenceValue == null)
                {
                    var index = EditorGUI.Popup(position, elementType.Name, 0, getNames());
                    if (index > 0)
                    {
                        var type = types[index - 1];
                        property.managedReferenceValue = Activator.CreateInstance(type);
                    }
                }
                else
                {
                    var type = property.managedReferenceValue.GetType();
                    var index = Array.IndexOf(types, type) + 1;
                    var oldIndex = index;
                    index = EditorGUI.Popup(new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y, position.width - EditorGUIUtility.labelWidth - 2, EditorGUIUtility.singleLineHeight),
                        index, getNames());
                    if (oldIndex != index)
                    {
                        if (index < 1) property.managedReferenceValue = null;
                        else
                        {
                            type = types[index - 1];
                            property.managedReferenceValue = Activator.CreateInstance(type);
                            EditorGUI.PropertyField(position, property, new GUIContent(type.Name, getName(type)), true);
                        }
                    }
                    else EditorGUI.PropertyField(position, property, new GUIContent(type.Name, getName(type)), true);
                }
                EditorGUIUtility.labelWidth = oldLW;

                string[] getNames() => types.Select(x => $"{getGroup(x)}{getName(x)}").Prepend(L10n.Tr("None")).ToArray();
                string getGroup(Type type)
                {
                    string group = (string)this.getGroup?.Invoke(null, new object[] { type }) ?? string.Empty;
                    if (!string.IsNullOrEmpty(group) && !group.EndsWith('/')) group += '/';
                    return group;
                }
                string getName(Type type)
                {
                    string name = name = this.getName?.Invoke(null, new object[] { type })?.ToString();
                    if (string.IsNullOrEmpty(name)) name = ObjectNames.NicifyVariableName(type.Name);
                    return name;
                }
            }
            else EditorGUI.PropertyField(position, property, label, true);


            static bool methodInvalid(MethodInfo method)
            {
                return method != null && (method.ReturnType != typeof(string) || method.GetParameters().Length < 1 || method.GetParameters().Any(x => x.ParameterType != typeof(Type)));
            }
        }
    }
}
