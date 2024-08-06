using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.UI
{
    [RequireComponent(typeof(Button))]
    public class WindowButton : MonoBehaviour
    {
        [SerializeField, TypeSelector(typeof(Window))]
        private string type;
        public string Type
        {
            get => type;
            private set
            {
                if (type != value)
                {
                    type = value;
                    typeCache = TypeCacheZT.GetType(type);
                }
            }
        }

        public bool openClose = true;

        private Type typeCache;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            typeCache = TypeCacheZT.GetType(type);
        }

        private void OnClick()
        {
            if (!openClose) WindowManager.OpenWindow(typeCache);
            else WindowManager.OpenOrCloseWindow(typeCache);
        }
    }
}