using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.UI
{
    public static class WindowManager
    {
        private static int startSortingOrder = 100;
        public static int StartSortingOrder
        {
            get => startSortingOrder;
            set
            {
                if (startSortingOrder != value)
                {
                    startSortingOrder = value;
                    for (int i = 0; i < openedWindows.Count; i++)
                    {
                        openedWindows[i].SortingOrder = i + startSortingOrder;
                    }
                }
            }
        }

        private static Transform windowsContainer;
        public static Transform WindowsContainer
        {
            get
            {
                if (windowsContainer == null)
                {
                    isContainerAutoCreated = true;
                    windowsContainer = new GameObject("WindowsCanva", typeof(CanvasScaler), typeof(GraphicRaycaster)).transform;
                    windowsContainer.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var cs = windowsContainer.GetComponent<CanvasScaler>();
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.matchWidthOrHeight = 1;
                    cs.referenceResolution.Set(1920, 1080);
                }
                return windowsContainer;
            }
            set
            {
                if (value && windowsContainer != value)
                {
                    if (isContainerAutoCreated)
                    {
                        UnityEngine.Object.Destroy(windowsContainer.gameObject);
                        isContainerAutoCreated = false;
                    }
                    windowsContainer = value;
                }
            }
        }

        public static int Count => openedWindows.Count;

        public static event Action OnCloseAll;
        public static event Action<bool> OnHideAll;

        private static bool isContainerAutoCreated;
        private static bool isHidingAll;
        private static readonly Dictionary<string, Window> caches = new Dictionary<string, Window>();
        private static readonly List<Window> openedWindows = new List<Window>();
        private static readonly Dictionary<Window, bool> hiddenStates = new Dictionary<Window, bool>();

        #region 打开窗口 Open Window
        /// <summary>
        /// 打开类型名称为 <paramref name="name"/> 的窗口，参数格式详见它的 “OnOpen()” 方法<br/>
        /// Open window of type name <paramref name="name"/>, parameter format see its 'OnOpen()' method for more infomation.
        /// </summary>
        /// <returns>成功打开的窗口<br/>
        /// The window that opened successfully.
        /// </returns>
        public static Window OpenWindow(string name, params object[] args)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (FindWindow(name) is Window window)
            {
                if (window.Open(args ?? new object[0]))
                    return window;
            }
            else Debug.LogWarning($"找不到类型为{name}的窗口");
            return null;
        }
        /// <summary>
        /// 打开类型为 <paramref name="type"/> 的窗口，参数格式详见它的 “OnOpen()” 方法<br/>
        /// Open window of type <paramref name="type"/>, parameter format see its 'OnOpen()' method for more infomation.
        /// </summary>
        /// <returns>成功打开的窗口<br/>
        /// The window that opened successfully.
        /// </returns>
        public static Window OpenWindow(Type type, params object[] args)
        {
            if (type == null) return null;
            if (FindWindow(type) is Window window)
            {
                if (window.Open(args ?? new object[0]))
                    return window;
            }
            else Debug.LogWarning($"找不到类型为{type}的窗口");
            return null;
        }
        /// <summary>
        /// 打开类型为 <typeparamref name="T"/> 的窗口，参数格式详见 <typeparamref name="T"/>.OnOpen()<br/>
        /// Open window of type <typeparamref name="T"/>, parameter format see <typeparamref name="T"/>.OnOpen() for more infomation.
        /// </summary>
        /// <returns>成功打开的窗口<br/>
        /// The window that opened successfully.
        /// </returns>
        public static T OpenWindow<T>(params object[] args) where T : Window
        {
            if (FindWindow<T>() is T window)
            {
                if (window.Open(args ?? new object[0]))
                    return window;
            }
            else Debug.LogWarning($"找不到类型为{typeof(T)}的窗口");
            return null;
        }
        /// <summary>
        /// 打开名称为 <paramref name="name"/> 的窗口，参数格式详见它的 <see cref="GenericWindow.openAction"/> 回调<br/>
        /// Open window of type name <paramref name="name"/>, parameter format see its <see cref="GenericWindow.openAction"/> callback for more infomation.
        /// </summary>
        /// <returns>成功打开的窗口<br/>
        /// The window that opened successfully.
        /// </returns>
        public static GenericWindow OpenGenericWindow(string name, params object[] args)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (FindGenericWindow(name) is GenericWindow generic)
            {
                if (generic.Open(args ?? new object[0]))
                    return generic;
            }
            else Debug.LogWarning($"找不到名称为{name}的窗口");
            return null;
        }
        public static void OpenOrCloseGenericWindow(string name)
        {
            var window = FindGenericWindow(name, false);
            if (!window) window = FindGenericWindow(name);
            if (!window) return;
            if (window.IsOpen) window.Close();
            else window.Open();
        }
        public static void OpenOrCloseWindow(string name)
        {
            var window = FindWindow(name, false);
            if (!window) window = FindWindow(name);
            if (!window) return;
            if (window.IsOpen) window.Close();
            else window.Open();
        }
        public static void OpenOrCloseWindow(Type type)
        {
            var window = FindWindow(type, false);
            if (!window) window = FindWindow(type);
            if (!window) return;
            if (window.IsOpen) window.Close();
            else window.Open();
        }
        public static void OpenOrCloseWindow<T>() where T : Window
        {
            var window = FindWindow<T>(false);
            if (!window) window = FindWindow<T>();
            if (!window) return;
            if (window.IsOpen) window.Close();
            else window.Open();
        }
        public static Window OpenOrUnhideWindow(string name)
        {
            if (IsWindowHidden(name, out var window))
            {
                window.Hide(false);
                return window;
            }
            else return OpenWindow(name);
        }
        public static Window OpenOrUnhideWindow(Type type)
        {
            if (IsWindowHidden(type, out var window))
            {
                window.Hide(false);
                return window;
            }
            else return OpenWindow(type);
        }
        public static T OpenOrUnhideWindow<T>() where T : Window
        {
            if (IsWindowHidden<T>(out var window))
            {
                window.Hide(false);
                return window;
            }
            else return OpenWindow<T>();
        }
        #endregion

        #region 关闭窗口 Close Window
        /// <summary>
        /// 关闭类型名称为 <paramref name="name"/> 的窗口，参数格式详见它的 “OnClose()” 方法<br/>
        /// Close window of type name <paramref name="name"/>, parameter format see its 'OnClose()' method for more infomation.
        /// </summary>
        /// <returns>是否成功关闭<br/>
        /// Whether specified window closed successfully or not.
        /// </returns>
        public static bool CloseWindow(string name, params object[] args)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (FindWindow(name, false) is Window window)
                return window.Close(args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 打开类型为 <paramref name="type"/> 的窗口，参数格式详见它的 “OnOpen()” 方法<br/>
        /// Close window of type <paramref name="type"/>, parameter format see its 'OnOpen()' method for more infomation.
        /// </summary>
        /// <returns>是否成功关闭<br/>
        /// Whether specified window closed successfully or not.
        /// </returns>
        public static bool CloseWindow(Type type, params object[] args)
        {
            if (type == null) return false;
            if (FindWindow(type, false) is Window window)
                return window.Close(args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 关闭类型为 <typeparamref name="T"/> 的窗口，参数格式详见 <typeparamref name="T"/>.OnClose()<br/>
        /// Close window of type <typeparamref name="T"/>, parameter format see <typeparamref name="T"/>.OnClose() for more infomation.
        /// </summary>
        /// <returns>是否成功关闭<br/>
        /// Whether specified window closed successfully or not.
        /// </returns>
        public static bool CloseWindow<T>(params object[] args) where T : Window
        {
            if (FindWindow<T>(false) is T window)
                return window.Close(args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 关闭名称为 <paramref name="name"/> 的窗口，参数格式详见它的 <see cref="GenericWindow.closeAction"/> 方法<br/>
        /// Close window of type name <paramref name="name"/>, parameter format see its <see cref="GenericWindow.closeAction"/> method for more infomation.
        /// </summary>
        /// <returns>是否成功关闭<br/>
        /// Whether specified window closed successfully or not.
        /// </returns>
        public static bool CloseGenericWindow(string name, params object[] args)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (FindGenericWindow(name, false) is GenericWindow window)
                return window.Close(args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 关闭最顶层的可见窗口<br/>
        /// Close the top visible opened window.
        /// </summary>
        public static void CloseTop()
        {
            if (Peek() is Window window) window.Close();
        }
        public static void CloseAll()
        {
            foreach (var window in openedWindows.ConvertAll(w => w))
            {
                window.Close();
            }
            OnCloseAll?.Invoke();
        }
        public static void CloseAllExceptName(params string[] exceptions)
        {
            HashSet<string> names = new HashSet<string>(exceptions ?? new string[0]);
            foreach (var window in openedWindows)
            {
                string name = window is GenericWindow generic ? generic._name : window.GetType().Name;
                if (!names.Contains(name))
                    window.Close();
            }
        }
        public static void CloseAllExceptType(params Type[] exceptions)
        {
            HashSet<Type> types = new HashSet<Type>(exceptions ?? new Type[0]);
            foreach (var window in openedWindows)
            {
                Type type = window.GetType();
                if (!types.Contains(type))
                    window.Close();
            }
        }
        public static void CloseAllExcept(params Window[] exceptions)
        {
            HashSet<Window> windows = new HashSet<Window>(exceptions ?? new Window[0]);
            foreach (var window in windows)
            {
                if (!windows.Contains(window))
                    window.Close();
            }
        }
        #endregion

        #region 窗口显隐相关 Hide Window
        /// <summary>
        /// 隐藏类型名称为 <paramref name="name"/> 的窗口，参数格式详见它的 “OnClose()” 方法<br/>
        /// Close window of type name <paramref name="name"/>, parameter format see its 'OnClose()' method for more infomation.
        /// </summary>
        /// <param name="name">窗口类型名称<br/>
        /// Window's type name
        /// </param>
        /// <param name="hide">隐藏还是显示<br/>
        /// Hide or display
        /// </param>
        /// <param name="args">变长参数<br/>
        /// Variable-length parameter
        /// </param>
        /// <returns>是否成功隐藏<br/>
        /// Whether specified window hidden successfully or not.
        /// </returns>
        public static bool HideWindow(string name, bool hide, params object[] args)
        {
            if (FindWindow(name, false) is Window window) return window.Hide(hide, args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 隐藏类型为 <paramref name="type"/> 的窗口，参数格式详见它的 “OnClose()” 方法<br/>
        /// Close window of type <paramref name="type"/>, parameter format see its 'OnClose()' method for more infomation.
        /// </summary>
        /// <param name="type">窗口类型<br/>
        /// Window's type
        /// </param>
        /// <param name="hide">隐藏还是显示<br/>
        /// Hide or display
        /// </param>
        /// <param name="args">变长参数<br/>
        /// Variable-length parameter
        /// </param>
        /// <returns>是否成功隐藏<br/>
        /// Whether specified window hidden successfully or not.
        /// </returns>
        public static bool HideWindow(Type type, bool hide, params object[] args)
        {
            if (FindWindow(type, false) is Window window) return window.Hide(hide, args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 隐藏类型为 <typeparamref name="T"/> 的窗口，参数格式详见 <typeparamref name="T"/>.OnHide()<br/>
        /// Close window of type <typeparamref name="T"/>, parameter format see <typeparamref name="T"/>.OnHide() for more infomation.
        /// </summary>
        /// <param name="hide">隐藏还是显示<br/>
        /// Hide or display
        /// </param>
        /// <param name="args">变长参数<br/>
        /// Variable-length parameter
        /// </param>
        /// <returns>是否成功隐藏<br/>
        /// Whether specified window hidden successfully or not.
        /// </returns>
        public static bool HideWindow<T>(bool hide, params object[] args) where T : Window
        {
            if (FindWindow<T>(false) is Window window) return window.Hide(hide, args ?? new object[0]);
            return false;
        }
        /// <summary>
        /// 隐藏名称为 <paramref name="name"/> 的窗口，参数格式详见它的 <see cref="GenericWindow.hideAction"/> 回调<br/>
        /// Close window of type name <paramref name="name"/>, parameter format see its <see cref="GenericWindow.hideAction"/> callback for more infomation.
        /// </summary>
        /// <param name="name">窗口类型名称<br/>
        /// Window's type name
        /// </param>
        /// <param name="hide">隐藏还是显示<br/>
        /// Hide or display
        /// </param>
        /// <param name="args">变长参数<br/>
        /// Variable-length parameter
        /// </param>
        /// <returns>是否成功隐藏<br/>
        /// Whether specified window hidden successfully or not.
        /// </returns>
        public static bool HideGenericWindow(string name, bool hide, params object[] args)
        {
            if (FindWindow(name, false) is Window window) return window.Hide(hide, args ?? new object[0]);
            return false;
        }

        public static void HideAll(bool hide)
        {
            isHidingAll = true;
            foreach (var window in caches.Values)
            {
                if (window.IsOpen)
                {
                    if (hiddenStates.TryGetValue(window, out var hidden))
                        //在 HideAll(true) 之前就隐藏了，但现在想通过 HideAll(true) 显示，是不可以的
                        //If this window was hidden before HideAll(true) called, you are not allowed to unhide it by HideAll(false) now.
                        if (hidden && !hide) continue;
                    window.Hide(hide);
                }
            }
            OnHideAll?.Invoke(hide);
            isHidingAll = false;
        }
        public static void HideAllExceptName(bool hide, params string[] exceptions)
        {
            isHidingAll = true;
            var temp = new HashSet<string>(exceptions ?? new string[0]);
            foreach (var window in caches.Values)
            {
                var name = window is GenericWindow generic ? generic._name : window.GetType().Name;
                if (window.IsOpen && !temp.Contains(name))
                {
                    if (hiddenStates.TryGetValue(window, out var hidden))
                        if (hidden && !hide) continue;
                    window.Hide(hide);
                }
            }
            OnHideAll?.Invoke(hide);
            isHidingAll = false;
        }
        public static void HideAllExceptType(bool hide, params Type[] exceptions)
        {
            isHidingAll = true;
            var temp = new HashSet<Type>(exceptions ?? new Type[0]);
            foreach (var window in caches.Values)
            {
                var type = window.GetType();
                if (window.IsOpen && !temp.Contains(type))
                {
                    if (hiddenStates.TryGetValue(window, out var hidden))
                        if (hidden && !hide) continue;
                    window.Hide(hide);
                }
            }
            OnHideAll?.Invoke(hide);
            isHidingAll = false;
        }
        public static void HideAllExcept(bool hide, params Window[] exceptions)
        {
            isHidingAll = true;
            var temp = new HashSet<Window>(exceptions ?? new Window[0]);
            foreach (var window in caches.Values)
            {
                if (window.IsOpen && !temp.Contains(window))
                {
                    if (hiddenStates.TryGetValue(window, out var hidden))
                        if (hidden && !hide) continue;
                    window.Hide(hide);
                }
            }
            OnHideAll?.Invoke(hide);
            isHidingAll = false;
        }
        /// <summary>
        /// 记录窗口隐藏状态，仅供 <see cref="Window.Close(object[])"/>、<see cref="Window.Hide(bool, object[])"/> 内部使用，其它地方请勿使用<br/>
        /// Record hidden state of specified window, is only used by <see cref="Window.Close(object[])"/>, <see cref="Window.Hide(bool, object[])"/> internally, do not use it in other case.
        /// </summary>
        public static void RecordHiddenState(Window window)
        {
            if (isHidingAll || !window) return;
            hiddenStates[window] = window.IsHidden;
        }
        #endregion

        #region 查找相关 Find Window
        public static Window FindWindow(string name)
        {
            return FindWindow(name, true);
        }
        private static Window FindWindow(string name, bool create)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (caches.TryGetValue(name, out var window) && window) return window;
            else return FindWindow(TypeCacheZT.GetType(name), create);
        }
        public static Window FindWindow(Type type)
        {
            return FindWindow(type, true);
        }
        private static Window FindWindow(Type type, bool create)
        {
            if (type == null || type == typeof(GenericWindow)) return null;
            if (caches.TryGetValue(type.Name, out var window) && window) return window;
            else window = UnityEngine.Object.FindObjectOfType(type, true) as Window;
            if (!window && create)
            {
                if (WindowPrefabs.FindWindowOfType(type) is Window prefab)
                    window = UnityEngine.Object.Instantiate(prefab, WindowsContainer);
            }
            Cache(window);
            return window;
        }
        public static T FindWindow<T>() where T : Window
        {
            return FindWindow<T>(true);
        }
        private static T FindWindow<T>(bool create) where T : Window
        {
            string name = typeof(T).Name;
            if (caches.TryGetValue(name, out var window) && window) return window as T;
            else return FindWindow(typeof(T), create) as T;
        }
        public static GenericWindow FindGenericWindow(string name)
        {
            return FindGenericWindow(name, false);
        }
        private static GenericWindow FindGenericWindow(string name, bool create)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (caches.TryGetValue("[G]" + name, out var window) && window) return window as GenericWindow;
            else window = UnityEngine.Object.FindObjectsOfType<GenericWindow>().FirstOrDefault(w => w._name == name);
            if (!window && create)
            {
                if (WindowPrefabs.FindGenericWindow(name) is GenericWindow prefab)
                    window = UnityEngine.Object.Instantiate(prefab, WindowsContainer);
            }
            Cache(window);
            return window as GenericWindow;
        }
        public static void Cache(Window window)
        {
            if (!window) return;
            string name = window is GenericWindow generic ? "[G]" + generic._name : window.GetType().Name;
            caches[name] = window;
        }
        #endregion

        #region 判断相关 Detection
        public static bool IsWindowOpen(string name)
        {
            var window = FindWindow(name, false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsWindowOpen(Type type)
        {
            var window = FindWindow(type, false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsWindowOpen<T>() where T : Window
        {
            var window = FindWindow<T>(false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsGenericWindowOpen(string name)
        {
            var window = FindGenericWindow(name, false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsWindowOpen(string name, out Window window)
        {
            window = FindWindow(name, false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsWindowOpen(Type type, out Window window)
        {
            window = FindWindow(type, false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsWindowOpen<T>(out T window) where T : Window
        {
            window = FindWindow<T>(false);
            if (!window) return false;
            else return window.IsOpen;
        }
        public static bool IsGenericWindowOpen(string name, out GenericWindow window)
        {
            window = FindGenericWindow(name, false);
            if (!window) return false;
            else return window.IsOpen;
        }

        public static bool IsWindowHidden(string name)
        {
            if (FindWindow(name, false) is not Window window) return false;
            else return window.IsHidden;
        }
        public static bool IsWindowHidden(Type type)
        {
            if (FindWindow(type, false) is not Window window) return false;
            else return window.IsHidden;
        }
        public static bool IsWindowHidden<T>() where T : Window
        {
            var window = FindWindow<T>(false);
            if (!window) return false;
            else return window.IsHidden;
        }
        public static bool IsGenericWindowHidden(string name)
        {
            if (FindGenericWindow(name, false) is not GenericWindow window) return false;
            else return window.IsHidden;
        }
        public static bool IsWindowHidden(string name, out Window window)
        {
            window = FindWindow(name, false);
            if (!window) return false;
            else return window.IsHidden;
        }
        public static bool IsWindowHidden(Type type, out Window window)
        {
            window = FindWindow(type, false);
            if (!window) return false;
            else return window.IsHidden;
        }
        public static bool IsWindowHidden<T>(out T window) where T : Window
        {
            window = FindWindow<T>(false);
            if (!window) return false;
            else return window.IsHidden;
        }
        public static bool IsGenericWindowHidden(string name, out GenericWindow window)
        {
            window = FindGenericWindow(name, false);
            if (!window) return false;
            else return window.IsHidden;
        }
        #endregion

        #region 窗口栈结构 Stack
        /// <summary>
        /// 将窗口压入窗口栈，仅供 <see cref="Window.Open(object[])"/> 内部使用，其它地方请勿使用<br/>
        /// Push a window to the opening-stack, is only used by <see cref="Window.Open(object[])"/> internally, do not use it in other case.
        /// </summary>
        public static void Push(Window window)
        {
            if (!window) return;
            Remove(window);
            window.SortingOrder = openedWindows.Count + startSortingOrder;
            openedWindows.Add(window);
        }
        /// <summary>
        /// 返回已打开且未隐藏的最顶层窗口<br/>
        /// Return the top window that is opened but not hidden.
        /// </summary>
        public static Window Peek()
        {
            for (int i = openedWindows.Count - 1; i >= 0; i--)
            {
                var window = openedWindows[i];
                if (!window.IsHidden)
                    return window;
            }
            return null;
        }
        /// <summary>
        /// 返回最顶层窗口<br/>
        /// Return the top window.
        /// </summary>
        public static Window Top()
        {
            if (openedWindows.Count > 0) return openedWindows[^1];
            return null;
        }
        /// <summary>
        /// 将窗口从窗口栈移除，仅供 <see cref="Window.Close(object[])"/> 内部使用，其它地方请勿使用<br/>
        /// Remove a window from the opening-stack, is only used by <see cref="Window.Close(object[])"/> internally, do not use it in other case.
        /// </summary>
        public static void Remove(Window window)
        {
            if (!window) return;
            openedWindows.Remove(window);
            hiddenStates.Remove(window);
            for (int i = 0; i < openedWindows.Count; i++)
            {
                openedWindows[i].SortingOrder = i + startSortingOrder;
            }
        }
        #endregion

        [InitMethod(int.MinValue)]
        public static void Init()
        {
            CloseAll();
            caches.Clear();
            hiddenStates.Clear();
            isHidingAll = false;
            openedWindows.Clear();
            foreach (var window in UnityEngine.Object.FindObjectsOfType<Window>())
            {
                Cache(window);
            }
        }
    }
}