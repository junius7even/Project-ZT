using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using ZetanStudio.Extension;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace ZetanStudio
{
    public static partial class UtilityZT
    {
#if UNITY_EDITOR
        public static class Editor
        {
            #region 杂项
            public static Texture2D GetIconForObject(Object obj)
            {
                if (!obj) return null;
                return UnityEditorInternal.InternalEditorUtility.GetIconForFile(AssetDatabase.GetAssetPath(obj));
            }

            /// <summary>
            /// 高亮给定内容中第一个遇到的关键字<br/>
            /// Bold the first keyword found in the given content.
            /// </summary>
            /// <param name="length">截取长度<br/>
            /// Interception length
            /// </param>
            /// <returns>截取到的包含加粗关键字的内容<br/>
            /// Intercepted content with bold keyword.
            /// </returns>
            public static string HighlightKeyword(string input, string key, int? length = null, bool ignoreCase = true)
            {
                string output;
                if (!length.HasValue || length > input.Length) length = input.Length;
                int cut = (length.Value - key.Length) / 2;
                int index = input.IndexOf(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture);
                if (index < 0) return input;
                int start = index - cut;
                int end = index + key.Length + cut;
                while (start < 0)
                {
                    start++;
                    if (end < input.Length - 1) end++;
                }
                while (end > input.Length - 1)
                {
                    end--;
                    if (start > 0) start--;
                }
                start = start < 0 ? 0 : start;
                end = end > input.Length - 1 ? input.Length - 1 : end;
                int len = end - start + 1;
                output = input.Substring(start, Mathf.Min(len, input.Length - start));
                index = output.IndexOf(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture);
                output = output.Insert(index, "<b>");
                end = index + 3 + key.Length;
                if (end > output.Length - 1) output += "</b>";
                else output = output.Insert(end, "</b>");
                return output;
            }

            public static void AddScriptingDefineSymbol(string define)
            {
                GetScriptingDefineSymbols(out var defines);
                if (!defines.Contains(define))
                {
                    ArrayUtility.Add(ref defines, define);
                    SetScriptingDefineSymbols(defines);
                }
            }
            public static void RemoveScriptingDefineSymbol(string define)
            {
                GetScriptingDefineSymbols(out var defines);
                if (defines.Contains(define))
                {
                    ArrayUtility.Remove(ref defines, define);
                    SetScriptingDefineSymbols(defines);
                }
            }
            public static void GetScriptingDefineSymbols(out string[] defines)
            {
                var target = (NamedBuildTarget)typeof(NamedBuildTarget).GetMethod("FromActiveSettings", CommonBindingFlags | BindingFlags.Static).Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget });
                PlayerSettings.GetScriptingDefineSymbols(target, out defines);
            }
            public static void SetScriptingDefineSymbols(string[] defines)
            {
                var target = (NamedBuildTarget)typeof(NamedBuildTarget).GetMethod("FromActiveSettings", CommonBindingFlags | BindingFlags.Static).Invoke(null, new object[] { EditorUserBuildSettings.activeBuildTarget });
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
            }
            #endregion

            #region 路径相关
            public static bool IsValidFolder(string path)
            {
                return path.Contains(Application.dataPath);
            }
            #endregion

            #region 序列化相关
            /// <summary>
            /// 获取SerializedProperty关联字段的值
            /// </summary>
            public static bool TryGetValue(SerializedProperty property, out object value)
            {
                return TryGetValue(property, out value, out _);
            }

            /// <summary>
            /// 获取SerializedProperty关联字段的值
            /// </summary>
            /// <param name="property"><see cref="SerializedProperty"/></param>
            /// <param name="fieldInfo">字段信息，找不到关联字段时是null，若<paramref name="property"/>处于<see cref="IList"/>中，此字段信息指向<see cref="IList"/></param>
            /// <returns>获取到的字段值</returns>
            public static bool TryGetValue(SerializedProperty property, out object value, out FieldInfo fieldInfo)
            {
                value = default;
                fieldInfo = null;
                if (property.serializedObject.targetObject)
                {
                    try
                    {
                        string[] paths = property.propertyPath.Replace(".Array.data[", "[").Split('.');
                        value = property.serializedObject.targetObject;
                        for (int i = 0; i < paths.Length; i++)
                        {
                            if (paths[i].EndsWith(']'))
                            {
                                if (int.TryParse(paths[i].Split('[', ']')[^2], out var index))
                                {
                                    fieldInfo = value.GetType().GetField(paths[i][..^$"[{index}]".Length], CommonBindingFlags);
                                    value = (fieldInfo.GetValue(value) as IList)[index];
                                }
                            }
                            else
                            {
                                fieldInfo = value.GetType().GetField(paths[i], CommonBindingFlags);
                                value = fieldInfo.GetValue(value);
                            }
                        }
                        return fieldInfo != null;
                    }
                    catch// (Exception ex)
                    {
                        //Debug.LogException(ex);
                        value = default;
                        fieldInfo = null;
                        return false;
                    }
                }
                return false;
            }

            /// <summary>
            /// 设置SerializedProperty关联字段的值
            /// </summary>
            /// <returns>是否成功</returns>
            public static bool TrySetValue(SerializedProperty property, object value)
            {
                object temp = property.serializedObject.targetObject;
                FieldInfo fieldInfo = null;
                if (temp != null)
                {
                    try
                    {
                        string[] paths = property.propertyPath.Replace(".Array.data[", "[").Split('.');
                        for (int i = 0; i < paths.Length; i++)
                        {
                            if (paths[i].EndsWith(']'))
                            {
                                if (int.TryParse(paths[i].Split('[', ']')[^2], out var index))
                                {
                                    fieldInfo = temp.GetType().GetField(paths[i][..^$"[{index}]".Length], CommonBindingFlags);
                                    temp = (fieldInfo.GetValue(temp) as IList)[index];
                                }
                            }
                            else
                            {
                                fieldInfo = temp.GetType().GetField(paths[i], CommonBindingFlags);
                                if (fieldInfo != null)
                                {
                                    if (i < paths.Length - 1)
                                        temp = fieldInfo.GetValue(temp);
                                }
                                else break;
                            }
                        }
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(temp, value);
                            return true;
                        }
                        else return false;
                    }
                    catch// (Exception ex)
                    {
                        //Debug.LogException(ex);
                        return false;
                    }
                }
                return false;
            }
            #endregion

            #region 资源相关
            #region 加载
            public static T LoadResource<T>() where T : Object
            {
                return LoadResource(typeof(T)) as T;
            }
            public static Object LoadResource(Type type)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", new string[] { "Assets" });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || !path.Contains("/Resources/")) continue;
                    try
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset) return asset;
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return null;
            }

            public static List<T> LoadResources<T>()
            {
                var type = typeof(T);
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", new string[] { "Assets" });
                List<T> assets = new List<T>();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || !path.Contains("/Resources/")) continue;
                    try
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset is T result) assets.Add(result);
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return assets;
            }
            public static List<Object> LoadResources(Type type)
            {
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", new string[] { "Assets" });
                List<Object> assets = new List<Object>();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || !path.Contains("/Resources/")) continue;
                    try
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset) assets.Add(asset);
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return assets;
            }


            /// <summary>
            /// 加载所有<typeparamref name="T"/>类型的资源
            /// </summary>
            /// <typeparam name="T">UnityEngine.Object类型</typeparam>
            /// <param name="folder">以Assets开头的指定加载文件夹路径</param>
            /// <returns>找到的资源</returns>
            public static List<T> LoadAssets<T>(string folder = null, string extension = null, bool ignorePackages = true) where T : Object
            {
                return LoadAssetsWhere<T>(null, folder, extension, ignorePackages);
            }
            public static List<T> LoadAssetsWhere<T>(Predicate<T> predicate, string folder = null, string extension = null, bool ignorePackages = true) where T : Object
            {
                List<string> folders = null;
                if (ignorePackages || !string.IsNullOrEmpty(folder))
                {
                    folders = new List<string>();
                    if (ignorePackages) folders.Add("Assets");
                    if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
                }
                string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders?.ToArray());
                List<T> assets = new List<T>();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || (!string.IsNullOrEmpty(extension) && extension.Split(',').None(e => path.EndsWith("." + e)))) continue;
                    try
                    {
                        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                        if (asset && (predicate == null || predicate(asset))) assets.Add(asset);
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return assets;
            }
            public static List<Object> LoadAssets(Type type, string folder = null, string extension = null, bool ignorePackages = true)
            {
                return LoadAssetsWhere(null, type, folder, extension, ignorePackages);
            }
            public static List<Object> LoadAssetsWhere(Predicate<Object> predicate, Type type, string folder = null, string extension = null, bool ignorePackages = true)
            {
                List<string> folders = null;
                if (ignorePackages || !string.IsNullOrEmpty(folder))
                {
                    folders = new List<string>();
                    if (ignorePackages) folders.Add("Assets");
                    if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
                }
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", folders?.ToArray());
                List<Object> assets = new List<Object>();
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || (!string.IsNullOrEmpty(extension) && extension.Split(',').None(e => path.EndsWith("." + e)))) continue;
                    try
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset && (predicate == null || predicate(asset))) assets.Add(asset);
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return assets;
            }

            /// <summary>
            /// 加载第一个T类型的资源
            /// </summary>
            /// <typeparam name="T">UnityEngine.Object类型</typeparam>
            /// <returns>找到的资源</returns>
            public static T LoadAsset<T>(string folder = null, string extension = null, bool ignorePackages = true) where T : Object
            {
                return LoadAssetWhere<T>(null, folder, extension, ignorePackages);
            }
            public static T LoadAssetWhere<T>(Predicate<T> predicate, string folder = null, string extension = null, bool ignorePackages = true) where T : Object
            {
                List<string> folders = null;
                if (ignorePackages || !string.IsNullOrEmpty(folder))
                {
                    folders = new List<string>();
                    if (ignorePackages) folders.Add("Assets");
                    if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
                }
                string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders?.ToArray());
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || (!string.IsNullOrEmpty(extension) && extension.Split(',').None(e => path.EndsWith("." + e)))) continue;
                    try
                    {
                        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                        if (asset && (predicate == null || predicate(asset))) return asset;
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return null;
            }
            public static Object LoadAsset(Type type, string folder = null, string extension = null, bool ignorePackages = true)
            {
                return LoadAssetWhere(null, type, folder, extension, ignorePackages);
            }
            public static Object LoadAssetWhere(Predicate<Object> predicate, Type type, string folder = null, string extension = null, bool ignorePackages = true)
            {
                List<string> folders = null;
                if (ignorePackages || !string.IsNullOrEmpty(folder))
                {
                    folders = new List<string>();
                    if (ignorePackages) folders.Add("Assets");
                    if (!string.IsNullOrEmpty(folder)) folders.Add(folder);
                }
                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}", folders?.ToArray());
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || (!string.IsNullOrEmpty(extension) && extension.Split(',').None(e => path.EndsWith("." + e)))) continue;
                    try
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                        if (asset && (predicate == null || predicate(asset))) return asset;
                    }
                    catch
                    {
                        Debug.LogWarning($"找不到路径：{path}");
                    }
                }
                return null;
            }
            #endregion

            public static void SaveChange(Object asset)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            public static T SaveFilePanel<T>(Func<T> creation, string assetName = null, string title = "选择保存位置", string extension = "asset", string folder = null, string root = null, bool ping = false, bool select = false) where T : Object
            {
                return SaveFilePanel(creation, null, assetName, title, extension, folder, root, ping, select);
            }
            public static T SaveFilePanel<T>(Func<T> creation, Action<T> afterCreation, string assetName = null, string title = "选择保存位置", string extension = "asset", string folder = null, string root = null, bool ping = false, bool select = false) where T : Object
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(assetName)) assetName = "New " + Regex.Replace(typeof(T).Name, "([a-z])([A-Z])", "$1 $2");
                    string path = EditorUtility.SaveFilePanelInProject(title, assetName, extension, null, string.IsNullOrEmpty(folder) ? "Assets" : folder);
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!string.IsNullOrEmpty(root) && !path.Contains($"Assets/{root}"))
                            if (!EditorUtility.DisplayDialog("路径错误", $"请选择Assets/{root}范围内的路径", "继续", "取消"))
                                return null;
                        try
                        {
                            T obj = creation();
                            afterCreation?.Invoke(obj);
                            AssetDatabase.CreateAsset(obj, ConvertToAssetsPath(path));
                            if (select) Selection.activeObject = obj;
                            if (ping) EditorGUIUtility.PingObject(obj);
                            return obj;
                        }
                        catch
                        {
                            if (!EditorUtility.DisplayDialog("保存失败", "请检查路径或者资源的有效性。", "继续", "取消"))
                                return null;
                        }
                    }
                    else return null;
                }
            }
            public static void SaveFolderPanel(Action<string> callback, string path = null)
            {
                while (true)
                {
                    path = EditorUtility.SaveFolderPanel("选择保存路径", path ?? "Assets", null);
                    if (!string.IsNullOrEmpty(path))
                        if (!IsValidFolder(path))
                        {
                            if (!EditorUtility.DisplayDialog("路径错误", $"请选择Assets范围内的路径", "继续", "取消"))
                                break;
                        }
                        else
                        {
                            path = ConvertToAssetsPath(path);
                            callback?.Invoke(path);
                            break;
                        }
                    else break;
                }
            }
            #endregion

            public static class Style
            {
                public static GUIStyle middleRight
                {
                    get
                    {
                        GUIStyle style = GUIStyle.none;
                        style.alignment = TextAnchor.MiddleRight;
                        style.normal.textColor = GUI.skin.label.normal.textColor;
                        return style;
                    }
                }
            }
        }
#endif
    }
}
