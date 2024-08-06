using System;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace ZetanStudio.UI
{
    [CreateAssetMenu(menuName = "Zetan Studio/窗口预制件集合 (Window Prefabs Collection)")]
    public class WindowPrefabs : ScriptableObject
    {
        [SerializeField]
        private Window[] windows = { };
        public ReadOnlyCollection<Window> Windows => new ReadOnlyCollection<Window>(windows);

        [SerializeField]
        private GenericWindow[] genericWindows = { };
        public ReadOnlyCollection<GenericWindow> GenericWindows => new ReadOnlyCollection<GenericWindow>(genericWindows);

        public static WindowPrefabs Instance { get; set; }

        public static Window FindWindowOfType(Type type)
        {
            return Instance ? Instance.windows?.FirstOrDefault(p => p && p.GetType() == type) : null;
        }
        public static Window FindGenericWindow(string name)
        {
            return Instance ? Instance.genericWindows?.FirstOrDefault(p => p && p._name == name) : null;
        }
    }
}