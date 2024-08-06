using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.DialogueSystem.UI
{
    using InteractionSystem.UI;
    using ZetanStudio.UI;

    public class DialogueWindow : InteractionWindow<IInterlocutor>
    {
        #region UI声明 UI Declaration
        [SerializeField, Label("名字文本")]
        protected Text nameText;
        [SerializeField, Label("对话文本")]
        protected Text contentText;
        [SerializeField, Label("对话文本按钮")]
        protected Button contentButton;

        [SerializeField, Label("首页按钮")]
        protected Button homeButton;
        [SerializeField, Label("继续按钮")]
        protected Button nextButton;
        [SerializeField, Label("继续按钮文本")]
        protected Text nextButtonText;

        [SerializeField, Label("选项区")]
        protected GameObject optionArea;
        [SerializeField, Label("选项区标题")]
        protected GameObject optionTitle;
        [SerializeField, Label("选项区标题文本")]
        protected Text optionTitleText;
        [SerializeField, Label("选项列表")]
        protected OptionList optionList;

#if !ZTDS_DISABLE_PORTRAIT
        [SerializeField, Label("左侧肖像")]
        protected Image leftPortrait;
        [SerializeField, Label("右侧肖像")]
        protected Image rightPortrait;
        [SerializeField, Label("肖像尺寸自适应")]
        protected bool adaptiveSize = true;
#endif
#if !ZTDS_DISABLE_VOICE
        [SerializeField, Label("语音播放器")]
        protected AudioSource voicePlayer;
#endif

        [SerializeField, Label("当前句跳过延迟", "当前句至少显示多久才可以跳过")]
        protected float skipDelay = 0.5f;
        /// <summary>
        /// 对话结束时关闭窗口<br/>
        /// Close window when dialogue ends.
        /// </summary>
        [SerializeField, Label("结束时关闭窗口")]
        protected bool closeWhenExit = true;
        #endregion

        #region 运行时声明 Runtime Declaration
        protected DialogueHandler handler;
        protected Action nextClicked;

        public override IInterlocutor Target => handler.Interlocutor;
        protected override string LanguageSelector => "Dialogue";
        #endregion

        #region 刷新相关
        protected virtual void OnHandleNode(DialogueNode node)
        {
            UtilityZT.SetActive(homeButton, handler.Interlocutor != null && node != handler.Home);
            UtilityZT.SetActive(closeButton, handler.Home.Interruptable);
        }
        protected virtual void OnReachLastSentence()
        {
            if (closeWhenExit)
            {
                UtilityZT.SetActive(closeButton, false);
                RefreshNextButton(() => Close(true), Tr("结束"));
            }
        }
        protected virtual string HandleKeywords(string text)
        {
            text = Keyword.HandleKeywords(text, true);
            if (Regex.IsMatch(text, @"{\[NPC\]}", RegexOptions.IgnoreCase))
                text = Regex.Replace(text, @"{\[NPC\]}", Target != null ? ((IKeyword)Target).Name : Tr("神秘人"), RegexOptions.IgnoreCase);
            if (Regex.IsMatch(text, @"{\[PLAYER\]}", RegexOptions.IgnoreCase))
                text = Regex.Replace(text, @"{\[PLAYER\]}", IPlayerNameHolder.Instance?.Name ?? Tr("神秘人"), RegexOptions.IgnoreCase);
            return text;
        }
        public virtual void RefreshNextButton(Action action, string text)
        {
            UtilityZT.SetActive(nextButton, (nextClicked = action) != null);
            if (nextButtonText) nextButtonText.text = text;
        }
        protected virtual void RefreshOptions(List<OptionCallback> callbacks, string title)
        {
            UtilityZT.SetActive(optionTitle, !string.IsNullOrEmpty(title));
            if (optionTitleText) optionTitleText.text = title;
            optionList.Refresh(callbacks);
            UtilityZT.SetActive(optionArea, callbacks != null && callbacks.Count > 0);
        }
#if !ZTDS_DISABLE_PORTRAIT
        protected virtual void SetPortrait(bool left, Sprite portrait)
        {
            if (left && leftPortrait)
            {
                UtilityZT.SetActive(leftPortrait, leftPortrait.overrideSprite = portrait);
                if (adaptiveSize) leftPortrait.SetNativeSize();
                else
                {
                    leftPortrait.type = Image.Type.Simple;
                    leftPortrait.preserveAspect = true;
                }
            }
            else if (!left && rightPortrait)
            {
                UtilityZT.SetActive(rightPortrait, rightPortrait.overrideSprite = portrait);
                if (adaptiveSize) rightPortrait.SetNativeSize();
                else
                {
                    rightPortrait.type = Image.Type.Simple;
                    rightPortrait.preserveAspect = true;
                }
            }
        }
        protected virtual void SetPortraitDark(bool left, bool dark)
        {
            if (left) setDark(leftPortrait);
            else setDark(rightPortrait);

            void setDark(Image portrait)
            {
                if (!portrait) return;
                if (dark) portrait.color = Color.gray;
                else portrait.color = Color.white;
            }
        }
#endif
        #endregion

        #region 操作相关 Actions
        /// <summary>
        /// 用于一键操作，比如只按确认键就能进行跳过打字动画、进入下一句、点击选中选项等操作<br/>
        /// Used for one-key operation, such as just pressing the submit button to skip the typing animation, enter the next sentence, or click the selected option, etc.
        /// </summary>
        public virtual void Next()
        {
            if (handler.IsPlaying) handler.Skip();
            else if (nextClicked != null) nextClicked.Invoke();
            else if (optionList.Buttons.Count > 0) optionList.DoSelectedOption();
        }
        protected virtual void OnContentClick()
        {
            if (handler.IsPlaying) handler.Skip();
            else nextClicked?.Invoke();
        }
        public void NextOption() => optionList.Next();
        public void PrevOption() => optionList.Prev();
        #endregion

        #region 其它 Other
        protected override void OnAwake()
        {
            handler = new DialogueHandler(new DialogueHandlerCallbacks()
            {
                setName = t => nameText.text = t,
                setContent = t => contentText.text = t,
                preprocessText = Tr,
                processText = HandleKeywords,
#if !ZTDS_DISABLE_PORTRAIT
                setPortrait = SetPortrait,
                setPortraitDark = SetPortraitDark,
#endif
#if !ZTDS_DISABLE_VOICE
                playVoice = (v, t) =>
                {
                    if (voicePlayer)
                    {
                        voicePlayer.clip = v;
                        voicePlayer.time = t;
                        voicePlayer.Play();
                    }
                },
                stopVoice = () => { if (voicePlayer) voicePlayer.Stop(); },
#endif
                startCoroutine = StartCoroutine,
                stopCoroutine = StopCoroutine,
                onHandleNode = OnHandleNode,
                onReachLastSentence = OnReachLastSentence,
                refreshNextAction = a => RefreshNextButton(a, Tr("继续")),
                refreshOptions = cs => RefreshOptions(cs, Tr("互动")),
                sendMessage = m => IMessageDisplayer.Instance?.Push(L.Tr("Message", m)),
                getSkipDelay = () => skipDelay,
            }, this);
            if (homeButton) homeButton.onClick.AddListener(handler.BackHome);
            if (contentButton) contentButton.onClick.AddListener(OnContentClick);
            if (nextButton) nextButton.onClick.AddListener(() => nextClicked?.Invoke());
        }
        protected override bool OnInterrupt()
        {
            return handler.CurrentEntry?.Interruptable ?? true;
        }
        #endregion

        #region 窗口显示相关 Windows Diaplay
        protected override bool OnOpen_(params object[] args)
        {
            if (handler.IsDoingManual || args.Length < 1) return false;
            handler.Init();
            if (args[0] is IInterlocutor interlocutor) handler.StartWith(interlocutor);
            else if (args[0] is Dialogue dialogue) handler.StartWith(dialogue);
            else if (args[0] is EntryNode entry) handler.StartWith(entry);
            else if (args[0] is string interlocutor2 && args[1] is string content) handler.StartWith(interlocutor2, content);
            else return false;
            WindowManager.HideAllExcept(true, this);
            return true;
        }
        protected override bool OnClose_(params object[] args)
        {
            if ((handler.Home?.Interruptable ?? true) || args.Length > 0 && args[0] is bool force && force)
            {
                handler.Init();
                WindowManager.HideAll(false);
                return true;
            }
            return false;
        }
        #endregion
    }
}