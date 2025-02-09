using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio
{
    [DisallowMultipleComponent, RequireComponent(typeof(Text))]
    public class StaticMultilingualText : MonoBehaviour
    {
        [SerializeField]
        private string selector;

        private Text component;
        private string original;

        private void Awake()
        {
            component = GetComponent<Text>();
            original = component.text;
            component.text = L.Tr(selector, original);
            Language.OnLanguageChanged += () => component.text = L.Tr(selector, original);
        }

        public void SetText(string text)
        {
            original = text;
            component.text = L.Tr(selector, original);
        }
        public void SetSelector(string selector) => component.text = L.Tr(this.selector = selector, original);
    }
}
