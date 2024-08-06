using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.DialogueSystem.UI
{
    [DisallowMultipleComponent]
    public class OptionList : MonoBehaviour
    {
        [SerializeField]
        private OptionButton prefab;
        [SerializeField]
        private RectTransform container;

        private RectTransform view;
        private int selectedIndex;
        private List<OptionCallback> callbacks;
        private SimplePool<OptionButton> cache;
        private readonly List<OptionButton> buttons = new List<OptionButton>();
        public ReadOnlyCollection<OptionButton> Buttons => new ReadOnlyCollection<OptionButton>(buttons);

        private void Awake()
        {
            cache = new SimplePool<OptionButton>(prefab);
            view = transform.parent.GetComponent<RectTransform>();
        }

        public void Refresh(IEnumerable<OptionCallback> callbacks)
        {
            this.callbacks = new List<OptionCallback>(callbacks ?? new OptionCallback[0]);
            while (buttons.Count < this.callbacks.Count)
            {
                CreateButton();
            }
            while (buttons.Count > this.callbacks.Count)
            {
                RemoveButton(buttons[^1]);
            }
            for (int i = 0; i < this.callbacks.Count; i++)
            {
                var button = buttons[i];
                button.Init(this.callbacks[i].title, this.callbacks[i].callback);
            }
            selectedIndex = 0;
            UpdateSelected();
            UpdateView();
        }

        public void Clear() => Refresh(null);

        public void DoSelectedOption()
        {
            if (buttons.Count < 1 || selectedIndex >= buttons.Count) return;
            buttons[selectedIndex].OnClick();
        }

        public void Next()
        {
            if (selectedIndex < buttons.Count - 1) selectedIndex++;
            UpdateSelected();
            UpdateView();
        }
        public void Prev()
        {
            if (selectedIndex > 0) selectedIndex--;
            UpdateSelected();
            UpdateView();
        }

        private void UpdateSelected()
        {
            if (selectedIndex > buttons.Count - 1) selectedIndex = buttons.Count - 1;
            if (selectedIndex < 0) selectedIndex = 0;
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].SetSelected(i == selectedIndex);
            }
        }
        private void UpdateView()
        {
            if (!view || selectedIndex >= buttons.Count) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            if (container.rect.height < view.rect.height) return;
            var buttonTrans = buttons[selectedIndex].GetComponent<RectTransform>();
            //获取四个顶点的位置，顶点序号
            //  1 ┏━┓ 2
            //  0 ┗━┛ 3
            Vector3[] vCorners = new Vector3[4];
            view.GetWorldCorners(vCorners);
            Vector3[] bCorners = new Vector3[5];
            buttonTrans.GetWorldCorners(bCorners);
            if (bCorners[1].y > vCorners[1].y)//按钮上方被挡住
                container.position += Vector3.up * (vCorners[1].y - bCorners[1].y);
            if (bCorners[0].y < vCorners[0].y)//按钮下方被挡住
                container.position += Vector3.up * (vCorners[0].y - bCorners[0].y);
        }

        private void CreateButton() => buttons.Add(cache.Get(container));
        private void RemoveButton(OptionButton button)
        {
            buttons.Remove(button);
            cache.Put(button);
        }
    }
}