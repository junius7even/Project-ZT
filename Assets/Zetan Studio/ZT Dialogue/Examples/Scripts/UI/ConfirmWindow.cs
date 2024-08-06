using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.Examples
{
    using UI;

    public class ConfirmWindow : Window
    {
        [SerializeField]
        private Text dialogText;
        [SerializeField]
        private Button yes;
        [SerializeField]
        private Button no;

        private Action onYesClicked;
        private Action onNoClicked;

        protected override void OnAwake()
        {
            yes.onClick.AddListener(Confirm);
            no.onClick.AddListener(Cancel);
        }

        public static ConfirmWindow StartConfirm(string dialog)
        {
            return StartConfirm(dialog, null);
        }
        public static ConfirmWindow StartConfirm(string dialog, Action yesAction)
        {
            return StartConfirm(dialog, yesAction, null);
        }
        public static ConfirmWindow StartConfirm(string dialog, Action yesAction, Action noAction)
        {
            return WindowManager.OpenWindow<ConfirmWindow>(dialog, yesAction, noAction);
        }

        public void Confirm()
        {
            Close();
            onYesClicked?.Invoke();
        }

        public void Cancel()
        {
            Close();
            onNoClicked?.Invoke();
        }

        protected override bool OnOpen(params object[] args)
        {
            if (args != null && args.Length > 2)
            {
                dialogText.text = args[0] as string;
                onYesClicked = args[1] as Action;
                onNoClicked = args[2] as Action;
                UtilityZT.SetActive(no, onYesClicked != null || onNoClicked != null);
                return true;
            }
            return false;
        }
    }
}