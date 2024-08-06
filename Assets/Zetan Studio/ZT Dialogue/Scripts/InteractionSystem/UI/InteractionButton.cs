using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.InteractionSystem.UI
{
    [RequireComponent(typeof(Button))]
    public class InteractionButton : MonoBehaviour
    {
        [SerializeField]
        private Image buttonIcon;
        [SerializeField]
        private Text buttonText;
        [SerializeField]
        private GameObject selectMark;

        private Button button;

        private IInteractive interactive;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            buttonIcon.type = Image.Type.Simple;
            buttonIcon.preserveAspect = true;
        }

        public void Init(IInteractive interactive)
        {
            this.interactive = interactive;
            buttonText.text = L.Tr("Interaction", interactive.Name);
            buttonIcon.overrideSprite = interactive.Icon;
            SetSelected(false);
        }

        public void Refresh()
        {
            buttonText.text = L.Tr("Interaction", interactive.Name);
            buttonIcon.overrideSprite = interactive.Icon;
        }

        public void OnClick()
        {
            if (interactive?.DoInteract() ?? false)
                InteractionPanel.Remove(interactive);
        }

        public void SetSelected(bool value)
        {
            UtilityZT.SetActive(selectMark, value);
        }
    }
}
