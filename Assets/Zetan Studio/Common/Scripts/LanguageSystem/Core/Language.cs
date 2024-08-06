using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZetanStudio
{
    /// <summary>
    /// <see cref="Language"/> 类的简写<br/>
    /// A shortcut of <see cref="Language"/> class.
    /// </summary>
    public static class L
    {
        public static string Tr(string selector, string text) => Language.Tr(selector, text);
        public static string Tr(string selector, string text, params object[] args) => Language.Tr(selector, text, args);
        public static string[] TrM(string selector, string text, params string[] texts) => Language.TrM(selector, text, texts);
        public static string[] TrM(string selector, string[] texts) => Language.TrM(selector, texts);

        #region 直接用翻译包 Use Translation Set Directly
        public static string Tr(Translation transl, int lang, string text) => Language.Tr(transl, lang, text);
        public static string Tr(Translation transl, int lang, string text, params object[] args) => Language.Tr(transl, lang, text, args);
        public static string[] TrM(Translation transl, int lang, string text, params string[] texts) => Language.TrM(transl, lang, text, texts);
        public static string[] TrM(Translation transl, int lang, string[] texts) => Language.TrM(transl, lang, texts);
        public static string Tr(Translation transl, string text) => Language.Tr(transl, 0, text);
        public static string Tr(Translation transl, string text, params object[] args) => Language.Tr(transl, 0, text, args);
        public static string[] TrM(Translation transl, string text, params string[] texts) => Language.TrM(transl, 0, text, texts);
        public static string[] TrM(Translation transl, string[] texts) => Language.TrM(transl, 0, texts);
        #endregion
    }

    public static class Language
    {
        private static Dictionary<string, List<Dictionary<string, string>>> cache = new Dictionary<string, List<Dictionary<string, string>>>();
        private static int languageIndex = 0;
        public static int LanguageIndex
        {
            get => languageIndex;
            set
            {
                if (languageIndex != value)
                {
                    languageIndex = value;
                    Init();
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        private static int languageIndexOffset = 1;
        /// <summary>
        /// 索引 <see cref="TranslationItem.Values"/> 时下标的偏移量，即 <see cref="LanguageIndex"/> 应减去的数值<br/>
        /// The index offset when indexing <see cref="TranslationItem.Values"/>, it's a number that <see cref="LanguageIndex"/> should subtract.
        /// </summary>
        public static int LanguageIndexOffset
        {
            get => languageIndexOffset;
            set
            {
                if (value < 0) Debug.LogError($"{nameof(LanguageIndexOffset)} should greater than or equals to 0");
                else if (languageIndexOffset != value)
                {
                    languageIndexOffset = value;
                    Init();
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// 语言变化事件<br/>
        /// Language changed event.
        /// </summary>
        public static event Action OnLanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Init()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Debug.LogWarning(Editor.EDL.Tr("请勿在编辑模式初始化 “{0}” 静态类", typeof(Language).Name));
#endif
            if (Localization.Instance) cache = Localization.Instance.AsDictionary(languageIndex - languageIndexOffset);
            else cache = new Dictionary<string, List<Dictionary<string, string>>>();
        }

        private static List<Dictionary<string, string>> FindDictionaries(string selector)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Debug.LogWarning(Editor.EDL.Tr("请勿在编辑模式使用 “{0}” 静态类的运行时方法", typeof(Language).Name));
#endif
            if (string.IsNullOrEmpty(selector)) return null;
            if (!cache.TryGetValue(selector, out var list))
            {
                if (Localization.Instance)
                {
                    var dicts = Localization.Instance.FindDictionaries(selector, languageIndex - languageIndexOffset);
                    if (dicts.Count > 0) cache[selector] = list = dicts;
                }
            }
            return list;
        }

        private static string Translate(string text, List<Dictionary<string, string>> dicts)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (dicts != null)
            {
                foreach (var dict in dicts)
                {
                    if (dict.TryGetValue(text, out var result)) return result;
                    var match = Regex.Match(text, @"(?<=^<color=[\w]*>)(.*)(?=</color>$)");
                    if (match.Success && dict.TryGetValue(match.Value, out result))
                        return text.Replace(match.Value, result); ;
                }
            }
            return Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase);
        }

        public static string Tr(string selector, string text) => Translate(text, FindDictionaries(selector));
        public static string Tr(string selector, string text, params object[] args) => string.Format(Tr(selector, text), args);

        public static string[] TrM(string selector, string text, params string[] texts)
        {
            var dicts = FindDictionaries(selector);
            List<string> result = new List<string>();
            if (dicts == null)
            {
                result.Add(Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase));
                foreach (var t in texts)
                {
                    result.Add(Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase));
                }
            }
            else
            {
                result.Add(Translate(text, dicts));
                result.AddRange(texts.Select(t => Translate(t, dicts)));
            }
            return result.ToArray();
        }
        public static string[] TrM(string selector, string[] texts)
        {
            var dicts = FindDictionaries(selector);
            if (dicts == null) return texts;
            else
            {
                List<string> result = new List<string>();
                result.AddRange(texts.Select(t => Translate(t, dicts)));
                return result.ToArray();
            }
        }

        #region 直接用翻译包 Use Translation Set Directly
        public static string Tr(Translation transl, int lang, string text) => transl ? transl.Tr(lang, text) : Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase);
        public static string Tr(Translation transl, int lang, string text, params object[] args) => string.Format(Tr(transl, lang, text), args);
        public static string[] TrM(Translation transl, int lang, string text, params string[] texts)
        {
            List<string> result = new List<string>();
            if (!transl)
            {
                result.Add(Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase));
                foreach (var t in texts)
                {
                    result.Add(Regex.Replace(text, @"^\[DUP\d+\]", "", RegexOptions.IgnoreCase));
                }
            }
            else
            {
                result.Add(transl.Tr(lang, text));
                result.AddRange(texts.Select(t => transl.Tr(lang, t)));
            }
            return result.ToArray();
        }
        public static string[] TrM(Translation transl, int lang, string[] texts)
        {
            List<string> result = new List<string>();
            if (!transl) return texts;
            else result.AddRange(texts.Select(t => transl.Tr(lang, t)));
            return result.ToArray();
        }
        public static string Tr(Translation transl, string text) => Tr(transl, 0, text);
        public static string Tr(Translation transl, string text, params object[] args) => Tr(transl, 0, text, args);
        public static string[] TrM(Translation transl, string text, params string[] texts) => TrM(transl, 0, text, texts);
        public static string[] TrM(Translation transl, string[] texts) => TrM(transl, 0, texts);
        #endregion
    }
}