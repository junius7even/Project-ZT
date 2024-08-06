using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.UI
{
    [DefaultExecutionOrder(-1)]
    public abstract class Window : MonoBehaviour, IFadeAble
    {
        /// <summary>
        /// 参数格式：([窗口类型: <see cref="Type"/>], [窗口状态: <see cref="WindowStates"/>])<br/>
        /// Parameters format: ([Window Type: <see cref="Type"/>], [Window State: <see cref="WindowStates"/>])
        /// </summary>
        public const string WindowStateChanged = "WindowStateChanged";

        [Label("淡入淡出")]
        public bool animated = true;
        [Label("持续时间"), HideIf("animated", false)]
        public float duration = 0.05f;

        [SerializeField, Label("窗体")]
        protected CanvasGroup content;
        [SerializeField, Label("关闭按钮")]
        protected Button closeButton;
        protected Canvas windowCanvas;

        /// <summary>
        /// 窗口关闭事件，每次绑定后仅在下次关闭时触发一次<br/>
        /// Window close event, trigger only once on next closing, after register.
        /// </summary>
        public event Action OnClosed;

        protected virtual bool HideOnAwake => true;
        public int SortingOrder { set => windowCanvas.sortingOrder = value; }
        public virtual bool IsOpen { get; protected set; }
        public virtual bool IsHidden { get; protected set; }

        MonoBehaviour IFadeAble.MonoBehaviour => this;
        CanvasGroup IFadeAble.FadeTarget => content;
        Coroutine IFadeAble.FadeCoroutine { get; set; }

        /// <summary>
        /// 由多语言系统使用的翻译包选择器<br/>
        /// A Translation Set seletor use by multilingual system.
        /// </summary>
        protected virtual string LanguageSelector => GetType().Name;

        /// <summary>
        /// 打开窗口<br/>
        /// Open the window.
        /// </summary>
        /// <returns>是否成功打开<br/>
        /// Whether this window opened successfully or not.
        /// </returns>
        public bool Open(params object[] args)
        {
            args ??= new object[0];
            if (OnOpen(args))
            {
                WindowManager.Push(this);
                IsOpen = true;
                if (animated) IFadeAble.FadeTo(this, 1, duration, () => { content.blocksRaycasts = true; OnCompletelyOpened(); });
                else
                {
                    content.alpha = 1;
                    content.blocksRaycasts = true;
                    OnCompletelyOpened();
                }
                NotificationCenter.Post(WindowStateChanged, GetType(), WindowStates.Open);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// 关闭窗口<br/>
        /// Close the window.
        /// </summary>
        /// <returns>是否成功打开<br/>
        /// Whether this window closed successfully or not.
        /// </returns>
        public bool Close(params object[] args)
        {
            args ??= new object[0];
            if (OnClose(args))
            {
                WindowManager.Remove(this);
                IsOpen = false;
                IsHidden = false;
                WindowManager.RecordHiddenState(this);
                OnClosed?.Invoke();
                OnClosed = null;
                content.blocksRaycasts = false;
                if (animated) IFadeAble.FadeTo(this, 0, duration, () => OnCompletelyClosed());
                else
                {
                    content.alpha = 0;
                    OnCompletelyClosed();
                }
                NotificationCenter.Post(WindowStateChanged, GetType(), WindowStates.Closed);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// 隐藏或取消隐藏窗口<br/>
        /// Hide or unhide the window.
        /// </summary>
        /// <param name="hide">隐藏还是显示<br/>
        /// Hide or display
        /// </param>
        /// <returns>是否成功隐藏或显示<br/>
        /// Whether this window hidden or display successfully.
        /// </returns>
        public bool Hide(bool hide, params object[] args)
        {
            if (!IsOpen) return false;
            args ??= new object[0];
            if (OnHide(hide, args))
            {
                content.alpha = hide ? 0 : 1;
                content.blocksRaycasts = !hide;
                IsHidden = hide;
                WindowManager.RecordHiddenState(this);
                NotificationCenter.Post(WindowStateChanged, GetType(), hide ? WindowStates.Hidden : WindowStates.Displayed);
            }
            return false;
        }

        #region 虚方法
        /// <summary>
        /// 打开窗口前调用，默认返回 true。在此方法实现窗口的打开逻辑<br/>
        /// Get called before window is actualy opened, return true as default. Write your window-open logic code here.
        /// </summary>
        /// <returns>是否可以打开<br/>
        /// Whether this window can be opened or not.
        /// </returns>
        protected virtual bool OnOpen(params object[] args) => true;
        /// <summary>
        /// 窗口完全淡入时调用，若无动画则在打开时立即调用<br/>
        /// Get called when this window is faded in, if there's no animation then get called immediately when this window is opened.
        /// </summary>
        protected virtual void OnCompletelyOpened() { }

        /// <summary>
        /// 关闭窗口前调用，默认返回 true。在此方法实现窗口的关闭逻辑<br/>
        /// Get called before window is actualy closed, return true as default. Write your window-close logic code here.
        /// </summary>
        /// <returns>是否可以关闭<br/>
        /// Whether this window can be closed or not.
        /// </returns>
        protected virtual bool OnClose(params object[] args) => true;
        /// <summary>
        /// 窗口完全淡出时调用，若无动画则在关闭时立即调用<br/>
        /// Get called when this window is faded out, if there's no animation then get called immediately when this window is closed.
        /// </summary>
        protected virtual void OnCompletelyClosed() { }

        /// <summary>
        /// 隐藏或取消隐藏窗口前调用，默认返回 true。在此方法实现窗口的关闭逻辑<br/>
        /// Get called before window is actualy hide or unhide, return true as default. Write your window-hide logic code here.
        /// </summary>
        /// <returns>是否可以关闭<br/>
        /// Whether this window can be hide or unhide or not.
        /// </returns>
        protected virtual bool OnHide(bool hide, params object[] args) => true;

        /// <summary>
        /// <see cref="Awake"/>时调用，默认为空<br/>
        /// Get called when <see cref="Awake"/> get called, it's empty as default.
        /// </summary>
        protected virtual void OnAwake() { }
        /// <summary>
        /// <see cref="OnDestroy"/>时调用，默认为空<br/>
        /// Get called when <see cref="OnDestroy"/> get called, it's empty as default.
        /// </summary>
        protected virtual void OnDestroy_() { }

        /// <summary>
        /// 注册消息监听，默认为空<br/>
        /// Register notification listeners, it's empty as default.
        /// </summary>
        protected virtual void RegisterNotification() { }
        /// <summary>
        /// 取消消息监听<br/>
        /// Unregister notification listeners.
        /// </summary>
        protected virtual void UnregisterNotification() { NotificationCenter.Unregister(this); }
        #endregion

        #region MonoBehaviour
        protected void Awake()
        {
            WindowManager.Cache(this);
            if (!content.gameObject.GetComponent<GraphicRaycaster>()) content.gameObject.AddComponent<GraphicRaycaster>();
            windowCanvas = content.GetComponent<Canvas>();
            windowCanvas.overrideSorting = true;
            if (HideOnAwake)
            {
                content.alpha = 0;
                content.blocksRaycasts = false;
            }
            if (closeButton) closeButton.onClick.AddListener(() => Close());
            OnAwake();
            RegisterNotification();
        }
        protected void OnDestroy()
        {
            OnDestroy_();
            UnregisterNotification();
        }
        #endregion

        /// <summary>
        /// 翻译文本<br/>
        /// Translate text.
        /// </summary>
        public string Tr(string text)
        {
            return L.Tr(LanguageSelector, text);
        }
        /// <summary>
        /// 按格式翻译文本<br/>
        /// Translate text in format.
        /// </summary>
        public string Tr(string text, params object[] args)
        {
            return L.Tr(LanguageSelector, text, args);
        }
        /// <summary>
        /// 批量翻译文本<br/>
        /// Translate texts.
        /// </summary>
        public string[] TrM(string text, params string[] texts)
        {
            return L.TrM(LanguageSelector, text, texts);
        }
        /// <summary>
        /// 批量翻译文本<br/>
        /// Translate texts.
        /// </summary>
        public string[] TrM(string[] texts)
        {
            return L.TrM(LanguageSelector, texts);
        }

        public static bool IsName<T>(string name) where T : Window
        {
            return typeof(T).Name == name;
        }
        public static bool IsType<T>(Type type) where T : Window
        {
            return typeof(T).IsAssignableFrom(type);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            try
            {
                Vector3[] corners = new Vector3[4];
                content.GetComponent<RectTransform>().GetWorldCorners(corners);
                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);
            }
            catch { }
        }
        [UnityEditor.MenuItem("GameObject/Zetan Studio/WindowPanel")]
        private static void CreateUI()
        {
            var win = new GameObject("UndifinedWindow", typeof(RectTransform));
            win.layer = LayerMask.NameToLayer("UI");
            if (UnityEditor.Selection.activeGameObject is GameObject go && go.transform is RectTransform transform)
            {
                win.transform.SetParent(transform, false);
            }
            var wTrans = win.GetComponent<RectTransform>();
            wTrans.anchorMin = Vector2.zero;
            wTrans.anchorMax = Vector2.one;
            wTrans.sizeDelta = Vector2.zero;
            var content = new GameObject("Content", typeof(CanvasGroup), typeof(Image));
            content.layer = LayerMask.NameToLayer("UI");
            content.transform.SetParent(win.transform, false);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 600);
            var title = new GameObject("WindowTitle", typeof(Text));
            title.layer = LayerMask.NameToLayer("UI");
            title.transform.SetParent(content.transform, false);
            var tTrans = title.GetComponent<RectTransform>();
            tTrans.anchorMin = Vector2.up;
            tTrans.anchorMax = Vector2.up;
            tTrans.anchoredPosition = new Vector2(90, -20);
            tTrans.sizeDelta = new Vector2(160, 40);
            var tText = title.GetComponent<Text>();
            tText.fontSize = 32;
            tText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.text = "Undifined";
            if (ColorUtility.TryParseHtmlString("#323232", out var tColor)) tText.color = tColor;
            else tText.color = Color.black;
            var close = new GameObject("Close", typeof(Image), typeof(Button));
            close.layer = LayerMask.NameToLayer("UI");
            close.transform.SetParent(content.transform, false);
            var cTrans = close.GetComponent<RectTransform>();
            cTrans.anchorMin = Vector2.one;
            cTrans.anchorMax = Vector2.one;
            cTrans.pivot = Vector2.one;
            cTrans.sizeDelta = new Vector2(60, 60);
            UnityEditor.Selection.activeGameObject = win;
        }
        [UnityEditor.MenuItem("GameObject/Zetan Studio/WindowPanel", true)]
        private static bool CanCreateUI()
        {
            return UnityEditor.Selection.activeGameObject is GameObject go && go.GetComponent<RectTransform>() && go.GetComponentInParent<Canvas>();
        }
#endif
    }

    public abstract class SingletonWindow<T> : Window where T : Window
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (!instance) instance = FindObjectOfType<T>(true);
                return instance;
            }
        }
    }
    public enum WindowStates
    {
        Open,
        Closed,
        Hidden,
        Displayed
    }
}