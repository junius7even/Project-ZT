using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.Extension
{
    public static class IEnumerableExtension
    {
        public static bool None<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return !source.Any(predicate);
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action?.Invoke(item);
            }
        }
    }

    public static class ComponentExtension
    {
        public static T GetOrAddComponent<T>(this Component source) where T : Component
        {
            var comp = source.GetComponent<T>();
            return comp != null ? comp : source.gameObject.AddComponent<T>();
        }
    }

#if UNITY_EDITOR
    namespace Editor
    {
        public static class SerializedObjectExtension
        {
            public static SerializedProperty FindAutoProperty(this SerializedObject obj, string propertyPath)
            {
                return obj.FindProperty($"<{propertyPath}>k__BackingField");
            }
            public static SerializedProperty FindPropertyEx(this SerializedObject obj, string propertyPath)
            {
                return obj.FindProperty(propertyPath) ?? obj.FindAutoProperty(propertyPath);
            }
        }

        public static class SerializedPropertyExtension
        {
            public static SerializedProperty FindAutoProperty(this SerializedProperty prop, string propertyPath)
            {
                return prop.FindPropertyRelative($"<{propertyPath}>k__BackingField");
            }

            public static bool IsRawName(this SerializedProperty source, string name)
            {
                return source.name == name || source.name == $"<{name}>k__BackingField";
            }

            /// <summary>
            /// 将所给下标处的数据移动到指定下标，若数据位于下标位置下方，则插入插入其下方，而非目标位置<br/>
            /// Move all elements indexed by <i><paramref name="srcIndices"/></i> to <i><paramref name="dstIndex"/></i>, if the data is below <i><paramref name="dstIndex"/></i>, then insert the data below it instead of itself.
            /// </summary>
            /// <returns>是否发生了移动<br/>
            /// Have any moving happened.
            /// </returns>
            public static bool MoveArrayElements(this SerializedProperty source, int[] srcIndices, int dstIndex, out int[] newIndices)
            {
                if (source is null) throw new ArgumentNullException(nameof(source));
                if (!source.isArray) throw new ArgumentException(nameof(source) + "不是数组");
                if (srcIndices is null) throw new ArgumentNullException(nameof(srcIndices));

                newIndices = new int[0];
                if (srcIndices.Length < 1) return false;

                var position = new Dictionary<int, int>();
                for (int i = 0; i < srcIndices.Length; i++)
                {
                    position[srcIndices[i]] = i;
                }

                Array.Sort(srcIndices);
                var operands = new int[srcIndices.Length];
                srcIndices.CopyTo(operands, 0);

                if (dstIndex < -1) dstIndex = -1;

                bool hasMoved = false;
                var upperIndices = new HashSet<int>();
                for (int i = 0; i < operands.Length; i++)
                {
                    if (operands[i] <= dstIndex) upperIndices.Add(operands[i]);
                }
                if (upperIndices.Count > 0)
                {
                    for (int i = operands[0]; i <= dstIndex; i++)
                    {
                        if (!upperIndices.Contains(i)) hasMoved |= true;
                    }
                    if (hasMoved)
                        for (int i = 0; i < upperIndices.Count; i++)
                        {
                            source.MoveArrayElement(operands[i], dstIndex);
                            for (int j = i + 1; j < upperIndices.Count; j++)
                            {
                                operands[j]--;
                            }
                        }
                }
                var belowIndices = new HashSet<int>();
                for (int i = 0; i < operands.Length; i++)
                {
                    if (operands[i] > dstIndex + 1) belowIndices.Add(operands[i]);
                }
                if (belowIndices.Count > 0)
                {
                    var remainderCount = 0;
                    for (int i = dstIndex + 1; i < operands[^1]; i++)
                    {
                        if (!belowIndices.Contains(i)) remainderCount++;
                    }
                    if (hasMoved |= remainderCount > 0)
                        for (int i = upperIndices.Count; i < operands.Length; i++)
                        {
                            source.MoveArrayElement(operands[i], dstIndex + 1 + i - upperIndices.Count);
                        }
                }
                if (hasMoved)
                {
                    newIndices = new int[srcIndices.Length];
                    for (int i = 0; i < srcIndices.Length; i++)
                    {
                        newIndices[position[srcIndices[i]]] = dstIndex - upperIndices.Count + 1 + i;
                    }
                }

                return hasMoved;
            }
        }

        public static class PropertyDrawerExtension
        {
            public static PropertyDrawer GetCustomDrawer(this PropertyDrawer source)
            {
                var drawers = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>().ToArray();
                for (int i = 0; i < drawers.Length; i++)
                {
                    var type = drawers[i];
                    foreach (var attr in type.GetCustomAttributes<CustomPropertyDrawer>())
                    {
                        var child = (bool)typeof(CustomPropertyDrawer).GetField("m_UseForChildren", UtilityZT.CommonBindingFlags).GetValue(attr);
                        var forType = typeof(CustomPropertyDrawer).GetField("m_Type", UtilityZT.CommonBindingFlags).GetValue(attr) as Type;
                        if (forType.Equals(source.fieldInfo.FieldType)) return makeDrawer(type);
                        else if (child && forType.IsAssignableFrom(source.fieldInfo.FieldType))
                        {
                            for (int j = i + 1; j < drawers.Length; j++)
                            {
                                foreach (var attr2 in drawers[j].GetCustomAttributes<CustomPropertyDrawer>())
                                {
                                    var forType2 = typeof(CustomPropertyDrawer).GetField("m_Type", UtilityZT.CommonBindingFlags).GetValue(attr2) as Type;
                                    if (forType2.Equals(source.fieldInfo.FieldType)) return makeDrawer(drawers[j]);
                                }
                            }
                            return makeDrawer(type);
                        }
                    }
                }
                return null;

                PropertyDrawer makeDrawer(Type type)
                {
                    var drawer = Activator.CreateInstance(type) as PropertyDrawer;
                    typeof(PropertyDrawer).GetField("m_FieldInfo", UtilityZT.CommonBindingFlags).SetValue(drawer, source.fieldInfo);
                    return drawer;
                }
            }
            /// <summary>
            /// 尝试获取拥有此成员的对象值。当<paramref name="property"/>位于<see cref="IList"/>中时，返回对应<see cref="IList"/>
            /// </summary>
            /// <returns>是否成功获取</returns>
            public static bool TryGetOwnerValue(this PropertyDrawer source, SerializedProperty property, out object owner)
            {
                owner = default;
                if (property.serializedObject.targetObject)
                {
                    try
                    {
                        string[] paths = property.propertyPath.Replace(".Array.data[", "[").Split('.');
                        object temp = property.serializedObject.targetObject;
                        FieldInfo fieldInfo = null;
                        for (int i = 0; i < paths.Length; i++)
                        {
                            if (paths[i].EndsWith(']'))
                            {
                                if (int.TryParse(paths[i].Split('[', ']')[^2], out var index))
                                {
                                    fieldInfo = temp.GetType().GetField(paths[i][..^$"[{index}]".Length], UtilityZT.CommonBindingFlags);
                                    if (fieldInfo == source.fieldInfo)
                                    {
                                        owner = fieldInfo.GetValue(temp);
                                        return true;
                                    }
                                    temp = (fieldInfo.GetValue(temp) as IList)[index];
                                }
                            }
                            else
                            {
                                fieldInfo = temp.GetType().GetField(paths[i], UtilityZT.CommonBindingFlags);
                                if (fieldInfo == source.fieldInfo)
                                {
                                    owner = temp;
                                    return true;
                                }
                                temp = fieldInfo.GetValue(temp);
                            }
                        }
                    }
                    catch
                    {
                        owner = default;
                        return false;
                    }
                }
                return false;
            }
        }

        public static class VisualElementExtension
        {
            public static void RegisterTooltipCallback(this VisualElement element, Func<string> tooltip)
            {
                element.tooltip = "";
                element.RegisterCallback<TooltipEvent>(e =>
                {
                    if (e.currentTarget == element)
                    {
                        e.rect = element.worldBound;
                        e.tooltip = tooltip?.Invoke();
                        e.StopImmediatePropagation();
                    }
                });
            }
        }
    }
#endif
}