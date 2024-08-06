using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZetanStudio
{
    [CreateAssetMenu(menuName = "Zetan Studio/翻译包 (Translation)")]
    public sealed class Translation : ScriptableObject
    {
        [SerializeField]
        private TranslationItem[] items = { };
        public ReadOnlyCollection<TranslationItem> Items => new ReadOnlyCollection<TranslationItem>(items);

        public Dictionary<string, string> AsDictionary(int lang)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var item in items)
            {
                try
                {
                    result[Regex.Unescape(item.Key)] = Regex.Unescape(item.Values[lang]);
                }
                catch { }
            }
            return result;
        }

        public string Tr(int lang, string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var item = Array.Find(items, i => Regex.Unescape(i.Key) == text);
            try
            {
                return Regex.Unescape(item.Values[lang]);
            }
            catch
            {
                return Regex.Replace(text, @"\[DUP\d+\]", "", RegexOptions.IgnoreCase);
            }
        }
        public string Tr(string text) => Tr(0, text);

#if UNITY_EDITOR
        [SerializeField]
        private string _name;

        public static class Editor
        {
            public static void SetName(Translation transl, string name)
            {
                if (transl) transl._name = name;
            }
            public static void SetItems(Translation transl, TranslationItem[] items)
            {
                if (transl) transl.items = items;
            }
        }
#endif
    }

    [Serializable]
    public sealed class TranslationItem
    {
        [field: SerializeField, TextArea]
        public string Key { get; private set; }

        [SerializeField, TextArea]
        private string[] values;
        public ReadOnlyCollection<string> Values => new ReadOnlyCollection<string>(values);

        public TranslationItem()
        {

        }

        public TranslationItem(string key, params string[] values)
        {
            Key = key;
            this.values = values;
        }
    }
}