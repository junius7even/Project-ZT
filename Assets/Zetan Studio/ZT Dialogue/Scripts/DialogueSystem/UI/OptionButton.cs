using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.DialogueSystem.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public class OptionButton : MonoBehaviour
    {
        [SerializeField]
        private Text text;
        [SerializeField]
        private GameObject selectMark;

        private Action callback;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Init(string text, Action callback)
        {
            this.text.text = text;
            this.callback = callback;
            SetSelected(false);
        }

        public void OnClick() => callback?.Invoke();

        public void SetSelected(bool value)
        {
            UtilityZT.SetActive(selectMark, value);
        }
    }
}