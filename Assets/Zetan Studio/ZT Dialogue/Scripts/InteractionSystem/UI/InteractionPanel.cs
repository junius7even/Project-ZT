using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.InteractionSystem.UI
{
    using System.Linq;
    using ZetanStudio.Extension;
    using ZetanStudio.UI;

    public class InteractionPanel : SingletonWindow<InteractionPanel>
    {
        [SerializeField]
        private RectTransform view;

        [SerializeField]
        private InteractionButton buttonPrefab;
        [SerializeField]
        private RectTransform buttonParent;
        [SerializeField]
        private RectTransform buttonCacheParent;

        public override bool IsOpen => true;
        protected override bool HideOnAwake => false;

        public static bool CanInteract => Instance && Instance.buttons.Count > 0;
        public static bool CanScroll => Instance && !Instance.IsHidden && Instance.buttons.Count > 1;

        private readonly HashSet<IInteractive> objects = new HashSet<IInteractive>();
        private readonly Dictionary<IInteractive, InteractionButton> buttons = new Dictionary<IInteractive, InteractionButton>();

        private readonly Dictionary<IInteractive, bool> showStates = new Dictionary<IInteractive, bool>();

        private int selectedIndex;

        private SimplePool<InteractionButton> pool;

        #region UI相关
        /// <summary>
        /// 给交互对象生成一个交互按钮并加入到交互面板中<br/>
        /// Create a interaction button for the input interactive object, and add it into the panel.
        /// </summary>
        /// <returns>如果对象已存在或其它原因，返回 <i>false</i><br/>
        /// If the target object is already exist, or other case, returns <i>false</i>.
        /// </returns>
        public static bool Push(IInteractive interactive)
        {
            if (!Instance || Instance.objects.Contains(interactive)) return false;
            Instance.objects.Add(interactive);
            InteractionButton button = Instance.pool.Get(Instance.buttonParent);
            button.Init(interactive);
            Instance.buttons.Add(interactive, button);
            if (Instance.buttonParent.childCount < 2) Instance.selectedIndex = button.transform.GetSiblingIndex();
            Instance.UpdateSelected();
            Instance.UpdateView();
            return true;
        }

        public static void HidePanelBy(IInteractive interactive, bool show)
        {
            if (!Instance || interactive == null) return;
            if (show)
            {
                if (Instance.showStates.TryGetValue(interactive, out var state))
                    Instance.Hide(state);
                Instance.showStates.Remove(interactive);
            }
            else
            {
                if (Instance.showStates.ContainsKey(interactive)) return;
                Instance.showStates.Add(interactive, Instance.IsHidden);
                Instance.Hide(true);
            }
        }

        public static bool Remove(IInteractive interactive)
        {
            if (!Instance) return false;
            if (Instance.buttons.TryGetValue(interactive, out var button))
            {
                int index = button.transform.GetSiblingIndex();
                Instance.pool.Put(button);
                Instance.buttons.Remove(interactive);
                Instance.objects.Remove(interactive);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Instance.buttonParent);
                if (Instance.buttonParent.rect.height >= Instance.view.rect.height)
                    Instance.buttonParent.anchoredPosition += Vector2.up * button.GetComponent<RectTransform>().rect.height;
                else Instance.buttonParent.anchoredPosition = Vector2.zero;
                if (index == Instance.selectedIndex)
                    if (index > 0) Prev();
                    else Instance.UpdateSelected();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 与所选对象进行交互<br/>
        /// Interact with the selected object.
        /// </summary>
        public static void Interact()
        {
            if (!Instance || Instance.IsHidden || Instance.buttonParent.childCount < 1 || Instance.selectedIndex >= Instance.buttonParent.childCount) return;
            var button = Instance.buttonParent.GetChild(Instance.selectedIndex);
            if (button) button.GetComponent<InteractionButton>().OnClick();
        }

        public static void Next()
        {
            if (!Instance || Instance.IsHidden) return;
            if (Instance.selectedIndex < Instance.buttonParent.transform.childCount - 1) Instance.selectedIndex++;
            Instance.UpdateSelected();
            Instance.UpdateView();
        }
        public static void Prev()
        {
            if (!Instance || Instance.IsHidden) return;
            if (Instance.selectedIndex > 0) Instance.selectedIndex--;
            Instance.UpdateSelected();
            Instance.UpdateView();
        }

        private void UpdateSelected()
        {
            if (selectedIndex > buttonParent.childCount - 1) selectedIndex = buttonParent.childCount - 1;
            if (selectedIndex < 0) selectedIndex = 0;
            foreach (var btn in buttons.Values)
            {
                btn.SetSelected(btn.transform.GetSiblingIndex() == selectedIndex);
            }
        }
        private void UpdateView()
        {
            if (selectedIndex >= buttonParent.childCount) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent);
            if (buttonParent.rect.height < view.rect.height) return;
            var buttonTrans = buttonParent.GetChild(selectedIndex) as RectTransform;
            //获取四个顶点的位置，顶点序号
            //  1 ┏━┓ 2
            //  0 ┗━┛ 3
            Vector3[] vCorners = new Vector3[4];
            view.GetWorldCorners(vCorners);
            Vector3[] bCorners = new Vector3[5];
            buttonTrans.GetWorldCorners(bCorners);
            if (bCorners[1].y > vCorners[1].y)//按钮上方被挡住
                buttonParent.position += Vector3.up * (vCorners[1].y - bCorners[1].y);
            if (bCorners[0].y < vCorners[0].y)//按钮下方被挡住
                buttonParent.position += Vector3.up * (vCorners[0].y - bCorners[0].y);
        }
        private void Refresh() => buttons.ForEach(b => b.Value.Refresh());

        #endregion

        #region 重写 Override

        protected override void OnAwake()
        {
            pool = new SimplePool<InteractionButton>(buttonPrefab, buttonCacheParent);
        }

        protected override void RegisterNotification()
        {
            Language.OnLanguageChanged += Refresh;
        }
        protected override void UnregisterNotification()
        {
            Language.OnLanguageChanged -= Refresh;
        }

        protected override bool OnOpen(params object[] args) => false;
        protected override bool OnClose(params object[] args) => false;
        #endregion
    }
}