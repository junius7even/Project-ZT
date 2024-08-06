using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using ZetanStudio.Editor;
#endif

namespace ZetanStudio
{
    public static class Keyword
    {
        /// <summary>
        /// 把关键字转为对应的名称<br/>
        /// Turn a keyword into its related name.
        /// </summary>
        /// <param name="color">是否用富文本给关键字染色<br/>
        /// Whether to color keyword's replacing name in rich-text.
        /// </param>
        public static string Translate(string keyword, bool color = false)
        {
            return Translate(keyword, color, CollectKeywords());
        }
        private static string Translate(string keyword, bool color, Dictionary<string, Dictionary<string, IKeyword>> keywords)
        {
            if (keyword.Equals("{[PLAYER]}", StringComparison.OrdinalIgnoreCase)) return IPlayerNameHolder.Instance.Name;
            if (IsKeyword(keyword))
            {
                var temp = keyword[2..^1];
                string[] split = temp.Split(']');
                if (keywords.TryGetValue(split[0], out var dict) && dict.TryGetValue(split[1], out var result) && result != null)
                    return UtilityZT.ColorText(L.Tr("Keyword", result.Name), color ? result.Color : default);
            }
            return keyword;
        }

        private static Dictionary<string, Dictionary<string, IKeyword>> CollectKeywords()
        {
            Dictionary<string, Dictionary<string, IKeyword>> keywords = new Dictionary<string, Dictionary<string, IKeyword>>();
            foreach (var method in TypeCacheZT.GetMethodsWithAttribute<GetKeywordsMethodAttribute>())
            {
                try
                {
                    foreach (var keyword in method.Invoke(null, null) as IEnumerable<IKeyword>)
                    {
                        if (keywords.TryGetValue(keyword.IDPrefix, out var dict)) dict[keyword.ID] = keyword;
                        else keywords[keyword.IDPrefix] = new Dictionary<string, IKeyword>() { { keyword.ID, keyword } };
                    }
                }
                catch { }
            }
            return keywords;
        }

        /// <summary>
        /// 把给定字符串中的关键字替换为相应的名称<br/>
        /// Replace all keywords in the input string with their related name.
        /// </summary>
        /// <param name="color">是否用富文本给关键字染色<br/>
        /// Whether to color keyword's replacing name in rich-text.
        /// </param>
        public static string HandleKeywords(string input, bool color = false)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            Dictionary<string, Dictionary<string, IKeyword>> keywords = CollectKeywords();
            StringBuilder output = new StringBuilder();
            StringBuilder keywordGetter = new StringBuilder();
            bool startGetting = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (i + 1 < input.Length && input[i] == '{' && input[i + 1] != '{') startGetting = true;
                else if (startGetting && input[i] == '}' && (i + 1 >= input.Length || input[i + 1] != '}'))
                {
                    startGetting = false;
                    keywordGetter.Append(input[i]);
                    output.Append(Translate(keywordGetter.ToString(), color, keywords));
                    keywordGetter.Clear();
                }
                else if (!startGetting) output.Append(input[i]);
                if (startGetting) keywordGetter.Append(input[i]);
            }

            return output.ToString();
        }

        /// <summary>
        /// 提取给定字符串中的所有关键字，以键值对列表的方式返回<br/>
        /// Extract all keywords in the input string, return them as a key-value-pair list.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> ExtractKeyWords(string input)
        {
            if (string.IsNullOrEmpty(input)) return new KeyValuePair<string, string>[0];
            Dictionary<string, Dictionary<string, IKeyword>> keywords = CollectKeywords();
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
            StringBuilder keyWordsGetter = new StringBuilder();
            bool startGetting = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (i + 1 < input.Length && input[i] == '{' && input[i + 1] != '{') startGetting = true;
                else if (startGetting && input[i] == '}' && (i + 1 >= input.Length || input[i + 1] != '}'))
                {
                    startGetting = false;
                    keyWordsGetter.Append(input[i]);
                    pairs.Add(KeyValuePair.Create(keyWordsGetter.ToString(), Translate(keyWordsGetter.ToString(), false, keywords)));
                    keyWordsGetter.Clear();
                }
                if (startGetting) keyWordsGetter.Append(input[i]);
            }
            return pairs;
        }

        /// <summary>
        /// 根据给定的关键字对象生成一个关键字串<br/>
        /// Generate a keyword-string for the given keyword object.
        /// </summary>
        public static string Generate(IKeyword keyword)
        {
            return $"{{[{keyword.IDPrefix}]{keyword.ID}}}";
        }
        public static bool IsKeyword(string keyword)
        {
            return Regex.IsMatch(keyword, @"^{\[\w+\]\w+}$");
        }

#if UNITY_EDITOR

        public static class Editor
        {
            public static string Translate(string keyword)
            {
                return Translate(keyword, CollectKeywords());
            }
            private static string Translate(string keyword, Dictionary<string, Dictionary<string, IKeyword>> keywords)
            {
                var match = Regex.Match(keyword, @"^{\[\w+\]\w+}$");
                if (match.Success)
                {
                    var temp = keyword[2..^1];
                    string[] split = temp.Split(']');
                    if (keywords.TryGetValue(split[0], out var dict) && dict.TryGetValue(split[1], out var result))
                        return result.Name;
                }
                return keyword;
            }
            private static Dictionary<string, Dictionary<string, IKeyword>> CollectKeywords()
            {
                Dictionary<string, Dictionary<string, IKeyword>> keywords = new Dictionary<string, Dictionary<string, IKeyword>>();
                foreach (var method in TypeCache.GetMethodsWithAttribute<GetKeywordsMethodAttribute>())
                {
                    try
                    {
                        foreach (var keyword in method.Invoke(null, null) as IEnumerable<IKeyword>)
                        {
                            if (keywords.TryGetValue(keyword.IDPrefix, out var dict)) dict[keyword.ID] = keyword;
                            else keywords[keyword.IDPrefix] = new Dictionary<string, IKeyword>() { { keyword.ID, keyword } };
                        }
                    }
                    catch { }
                }
                return keywords;
            }

            public static string HandleKeywords(string input)
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                Dictionary<string, Dictionary<string, IKeyword>> keywords = CollectKeywords();
                StringBuilder output = new StringBuilder();
                StringBuilder keywordGetter = new StringBuilder();
                bool startGetting = false;
                for (int i = 0; i < input.Length; i++)
                {
                    if (i + 1 < input.Length && input[i] == '{' && input[i + 1] != '{') startGetting = true;
                    else if (startGetting && input[i] == '}' && (i + 1 >= input.Length || input[i + 1] != '}'))
                    {
                        startGetting = false;
                        keywordGetter.Append(input[i]);
                        output.Append(Translate(keywordGetter.ToString(), keywords));
                        keywordGetter.Clear();
                    }
                    else if (!startGetting) output.Append(input[i]);
                    if (startGetting) keywordGetter.Append(input[i]);
                }

                return output.ToString();
            }

            public static IEnumerable<KeyValuePair<string, string>> ExtractKeyWords(string input)
            {
                if (string.IsNullOrEmpty(input)) return new KeyValuePair<string, string>[0];
                Dictionary<string, Dictionary<string, IKeyword>> keywords = CollectKeywords();
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                StringBuilder keyWordsGetter = new StringBuilder();
                bool startGetting = false;
                for (int i = 0; i < input.Length; i++)
                {
                    if (i + 1 < input.Length && input[i] == '{' && input[i + 1] != '{') startGetting = true;
                    else if (startGetting && input[i] == '}' && (i + 1 >= input.Length || input[i + 1] != '}'))
                    {
                        startGetting = false;
                        keyWordsGetter.Append(input[i]);
                        pairs.Add(KeyValuePair.Create(keyWordsGetter.ToString(), Translate(keyWordsGetter.ToString(), keywords)));
                        keyWordsGetter.Clear();
                    }
                    if (startGetting) keyWordsGetter.Append(input[i]);
                }
                return pairs;
            }

            public static void OpenKeywordsSelection(Vector2 position, Action<string> callback)
            {
                List<IKeyword> objects = new List<IKeyword>();
                foreach (var method in TypeCache.GetMethodsWithAttribute<GetKeywordsMethodAttribute>())
                {
                    try
                    {
                        objects.AddRange(method.Invoke(null, null) as IEnumerable<IKeyword>);
                    }
                    catch { }
                }
                var dropdown = new AdvancedDropdown<IKeyword>(new Vector2(200, 300), objects,
                               s => makeKeywords(s), s => s.Name, getGroup, tooltipGetter: getTooltip, title: L10n.Tr("Keywords"));
                dropdown.Show(position);

                void makeKeywords(IKeyword obj) => callback?.Invoke(Generate(obj));

                static string getGroup(IKeyword obj)
                {
                    var type = obj.GetType();
                    string group = type.Name + "/";
                    if (type.GetCustomAttribute<KeywordsSetAttribute>() is KeywordsSetAttribute attr)
                    {
                        group = attr.name;
                        group = group.EndsWith('/') ? group : (group + '/');
                    }
                    group += obj.Group;
                    return group;
                }
                static string getTooltip(IKeyword obj)
                {
                    return $"[{obj.IDPrefix}]{obj.ID}";
                }
            }

            public static void SetAsKeywordsField(TextField text)
            {
                text.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    var index = text.cursorIndex;
                    if (index == text.selectIndex)
                        evt.menu.AppendAction(EDL.Tr("插入关键字"), a =>
                        {
                            OpenKeywordsSelection(a.eventInfo.mousePosition, k =>
                            {
                                text.value = text.value.Insert(index, k);
                                EditorApplication.delayCall += () => text.SelectRange(index, index + k.Length);
                            });
                        });
                    var input = typeof(TextField).GetProperty("textInput", UtilityZT.CommonBindingFlags).GetValue(text) as VisualElement;
                    evt.target = input;
                    input.GetType().BaseType.GetMethod("BuildContextualMenu", UtilityZT.CommonBindingFlags).Invoke(input, new object[] { evt });
                }));
            }
        }
#endif
    }

    /// <summary>
    /// 如果想让某个类型的对象可被作为关键字，则继承这个接口<br/>
    /// Inherit this interface if you want an object of specified type can be a keyword object.
    /// </summary>
    public interface IKeyword
    {
        string ID { get; }

        /// <summary>
        /// 放在ID前面的前置，用于对关键字进行分类。例如“[NPC]NPC001”，前缀是“NPC”，不包括方括号<br/>
        /// The prefix to put in front of ID, is used to categorize keyword. Such as '[NPC]NPC001', the prefix is “NPC” that is bracketed.
        /// </summary>
        string IDPrefix { get; }

        string Name { get; }

        /// <summary>
        /// 在 UI 上显示时的文本颜色<br/>
        /// Color for text that display on UI.
        /// </summary>
        Color Color { get; }

        /// <summary>
        /// 由编辑器使用的属性，用于对分类中的关键字进行分组<br/>
        /// An Editor-Use property, is used to group keywords in their classification.
        /// </summary>
        string Group { get; }
    }

    /// <summary>
    /// 由编辑器使用的特性，用于在关键字选择窗口中对关键字进行根分组<br/>
    /// An Editor-Use attribute, is used to root-group keywords in keyword selection window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class KeywordsSetAttribute : Attribute
    {
        public readonly string name;

        public KeywordsSetAttribute(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// 把这个加到获取可用关键字的编辑器静态方法上<br/>
    /// Add this to the static editor method that be used to get available keywords.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class GetKeywordsMethodAttribute : Attribute { }

    /// <summary>
    /// 把这个加到获取可用关键字的运行时静态方法上<br/>
    /// Add this to the static in-game method that be used to get available keywords.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeGetKeywordsMethodAttribute : Attribute { }
}