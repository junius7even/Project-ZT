using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.UI
{
    [RequireComponent(typeof(Button))]
    public class GenericWindowButton : MonoBehaviour
    {
        public string _name;

        public bool openClose = true;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (!openClose) WindowManager.OpenGenericWindow(_name);
            else WindowManager.OpenOrCloseGenericWindow(_name);
        }
    }
}