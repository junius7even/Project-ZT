using System;
using UnityEngine;

namespace ZetanStudio.UI
{
    public sealed class GenericWindow : Window
    {
        public string _name;
        public bool hideOnAwake;
        protected override bool HideOnAwake => hideOnAwake;

        public string languageSelector;
        protected override string LanguageSelector => string.IsNullOrEmpty(languageSelector) ? _name : languageSelector;

        public GameObject[] elements = { };

        public override bool IsOpen
        {
            get => openGetter?.Invoke() ?? base.IsOpen;
            protected set
            {
                if (openSetter != null) openSetter?.Invoke(value);
                else base.IsOpen = value;
            }
        }
        public override bool IsHidden
        {
            get => hiddenGetter?.Invoke() ?? base.IsHidden;
            protected set
            {
                if (hiddenSetter != null) hiddenSetter?.Invoke(value);
                else base.IsHidden = value;
            }
        }

        public Func<bool> openGetter;
        public Action<bool> openSetter;
        public Func<bool> hiddenGetter;
        public Action<bool> hiddenSetter;

        public Action awakeAction;
        public Action destroyAction;
        public Action registerNotificationAction;
        public Action unregisterNotificationAction;
        public Func<object[], bool> openAction;
        public Action openCompletelyAction;
        public Func<object[], bool> closeAction;
        public Action closeCompletelyAction;
        public Func<bool, object[], bool> hideAction;

        protected override void OnAwake()
        {
            awakeAction?.Invoke();
        }
        protected override void OnDestroy_()
        {
            destroyAction?.Invoke();
        }
        protected override void RegisterNotification()
        {
            registerNotificationAction?.Invoke();
        }
        protected override void UnregisterNotification()
        {
            unregisterNotificationAction?.Invoke();
        }

        protected override bool OnOpen(params object[] args)
        {
            return openAction?.Invoke(args) ?? false;
        }
        protected override void OnCompletelyOpened()
        {
            openCompletelyAction?.Invoke();
        }
        protected override bool OnClose(params object[] args)
        {
            return closeAction?.Invoke(args) ?? false;
        }
        protected override void OnCompletelyClosed()
        {
            closeCompletelyAction?.Invoke();
        }
        protected override bool OnHide(bool hide, params object[] args)
        {
            return hideAction?.Invoke(hide, args) ?? false;
        }
    }
}
