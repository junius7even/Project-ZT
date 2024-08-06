using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using UnityEngine;

namespace ZetanStudio
{
    [CreateAssetMenu(menuName = "Zetan Studio/本地化 (Localization)")]
    public class Localization : ScriptableObject
    {
        [SerializeField]
        private string[] languageNames = { "简体中文" };
        public ReadOnlyCollection<string> LanguageNames => new ReadOnlyCollection<string>(languageNames);

        [SerializeField]
        private LocalizationItem[] items = { };
        public ReadOnlyCollection<LocalizationItem> Items => new ReadOnlyCollection<LocalizationItem>(items);

        public static Localization Instance { get; set; }

        public Dictionary<string, List<Dictionary<string, string>>> AsDictionary(int lang)
        {
            Dictionary<Translation, Dictionary<string, string>> dicts = new Dictionary<Translation, Dictionary<string, string>>();
            Dictionary<string, List<Dictionary<string, string>>> result = new Dictionary<string, List<Dictionary<string, string>>>();
            foreach (var item in items)
            {
                if (item.Translation)
                    foreach (var selector in item.Selectors)
                    {
                        if (!result.TryGetValue(selector, out var find))
                            result[selector] = new List<Dictionary<string, string>>() { makeDict(item.Translation) };
                        else find.Add(makeDict(item.Translation));
                    }
            }
            return result;

            Dictionary<string, string> makeDict(Translation transl)
            {
                if (!dicts.TryGetValue(transl, out var find))
                    dicts[transl] = find = transl.AsDictionary(lang);
                return find;
            }
        }

        public List<Dictionary<string, string>> FindDictionaries(string selector, int languageIndex)
        {
            return items.Where(d => d.Selectors.Contains(selector)).Select(d => d.Translation.AsDictionary(languageIndex)).ToList();
        }
    }

    [Serializable]
    public class LocalizationItem
    {
        [SerializeField]
        private string note;

        [field: SerializeField]
        public Translation Translation { get; private set; }

        [SerializeField]
        private string[] selectors = { };
        public ReadOnlyCollection<string> Selectors => new ReadOnlyCollection<string>(selectors);
    }
}