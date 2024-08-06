using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZetanStudio.DialogueSystem
{
    using Extension;
    using InteractionSystem;

    public class DialogueHandlerCallbacks
    {
        public Action<string> setName;
        public Action<string> setContent;
        public Func<string, string> preprocessText;
        public Func<string, string> processText;
        public Action<bool, Sprite> setPortrait;
        public Action<bool, bool> setPortraitDark;
        public Action<AudioClip, float> playVoice;
        public Action stopVoice;
        public Func<IEnumerator, Coroutine> startCoroutine;
        public Action<Coroutine> stopCoroutine;
        public Action<DialogueNode> onHandleNode;
        public Action onEndWriting;
        public Action onReachLastSentence;
        public Action<Action> refreshNextAction;
        public Action<List<OptionCallback>> refreshOptions;
        public Action<string> sendMessage;
        /// <summary>
        /// 当前句至少显示多久才可以跳过<br/>
        /// How long to display current sentence at least.
        /// </summary>
        public Func<float> getSkipDelay;
    }

    /// <summary>
    /// 处理对话逻辑的核心类型<br/>
    /// The core type to handle dialogue logic.
    /// </summary>
    public class DialogueHandler
    {
        internal readonly DialogueHandlerCallbacks callbacks;
        public readonly object window;

        #region 运行时声明 Runtime Declaration
        public IInterlocutor Interlocutor { get; protected set; }

        public EntryNode Home { get; protected set; }
        public EntryNode CurrentEntry { get; protected set; }
        public virtual DialogueData CurrentEntryData => DialogueManager.GetOrCreateData(CurrentEntry) ?? null;
        protected DialogueNode currentNode;
        protected virtual DialogueData CurrentNodeData => CurrentEntryData && currentNode ? CurrentEntryData[currentNode] : null;

        protected readonly HashSet<IManualNode> manualNodes = new HashSet<IManualNode>();
        public bool IsDoingManual => manualNodes.Count > 0;

        protected readonly Stack<DialogueNode> continueNodes = new Stack<DialogueNode>();
        protected readonly Dictionary<DialogueEvent, DialogueData> invokedEvents = new Dictionary<DialogueEvent, DialogueData>();

#if !ZTDS_DISABLE_PORTRAIT
        protected bool isUsingLeftPortrait = true;
        protected bool hasLeftPortrait;
        protected bool hasRightPortrait;
        protected string currentInterlocutor;
        protected string portraitInterlocutor;
#endif

        #region 逐字显示相关声明 Typing Animation Declaration
        protected string targetContent;
        protected Coroutine writeCoroutine;
        protected float playTime;
        public virtual bool IsPlaying => writeCoroutine != null;
        #endregion

        #region 等待相关声明 Wait Declaration
        protected Coroutine waitCoroutine;
        protected bool resumeOnWaitOver;
        public bool IsWaiting { get; protected set; }
        #endregion
        #endregion

        public DialogueHandler(DialogueHandlerCallbacks callbacks, object window = null)
        {
            this.callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            this.window = window;
        }

        #region 对话发起相关 Start Dialogue
        public void StartWith(IInterlocutor interlocutor) => StartWith((Interlocutor = interlocutor).Dialogue);
        public void StartWith(Dialogue dialogue) => StartWith(dialogue.Entry);
        /// <summary>
        /// 发起临时对话<br/>
        /// Start a temporary dialogue.
        /// </summary>
        public void StartWith(string interlocutor, string content) => StartWith(new EntryNode(interlocutor, content, true));
        public void StartWith(EntryNode entry)
        {
            if (!entry) return;
            Home ??= entry;
            CurrentEntry = entry;
            invokedEvents.ForEach(e => e.Key.CancelInvoke(e.Value));
            invokedEvents.Clear();
            HandleNode(entry);
        }
        /// <summary>
        /// 从属于本次对话的指定结点继续<br/>
        /// Continue with a specified node which belongs to current dialogue.
        /// </summary>
        public virtual void ContinueWith(DialogueNode node)
        {
            if (Dialogue.Reachable(CurrentEntry, node))
            {
                if (node is IManualNode manual) DoManual(manual);
                else HandleNode(node);
            }
        }
        #endregion

        #region 结点刷新相关 Refresh Node Content
        protected virtual void HandleNode(DialogueNode node)
        {
            if (!HandleSpecialNodes(ref node) || !node || node is ExternalOptionsNode) return;
            ResetInteraction();
            currentNode = node;
            CurrentNodeData?.Access();
            if (node is SentenceNode sentence)
            {
                PlayVoice(sentence);
#if !ZTDS_DISABLE_PORTRAIT
                currentInterlocutor = UtilityZT.RemoveTags(ProcessText(sentence.Interlocutor));
                callbacks.setName?.Invoke(currentInterlocutor);
#else
                callbacks.setName?.Invoke(UtilityZT.RemoveTags(ProcessText(sentence.Interlocutor)));
#endif
                RefreshPortrait(sentence);
                DisplaySentence(sentence);
            }
            (currentNode as IEventNode)?.Events.ForEach(e => InvokeEvent(e, CurrentNodeData));
            callbacks.onHandleNode?.Invoke(currentNode);
        }
        protected virtual bool HandleSpecialNodes(ref DialogueNode node)
        {
            while (node is DecoratorNode)
            {
                CurrentEntryData[node]?.Access();
                node = node[0]?.Next;
            }
            while (node is PortraitVoiceOverrideNode)
            {
                node = node[0]?.Next;
            }
            while (node is ConditionNode condition)
            {
                if (!condition.Check(CurrentEntryData))
                {
                    callbacks.sendMessage?.Invoke("条件未满足");
                    return false;
                }
                CurrentEntryData[node]?.Access();
                node = node[0]?.Next;
            }
            while (node is BlockNode block)
            {
                if (!block.CanEnter(CurrentEntryData, out var reason))
                {
                    callbacks.sendMessage?.Invoke(reason);
                    return false;
                }
                CurrentEntryData[node]?.Access();
                node = node[0]?.Next;
            }
            if (node is IManualNode manual)
            {
                DoManual(manual);
                return false;
            }
            if (node is BranchNode branch)
            {
                CurrentEntryData[node]?.Access();
                node = branch.GetBranch(CurrentEntryData);
                if (!node)
                {
                    HandleLastSentence();
                    return false;
                }
            }
            return true;
        }
        protected virtual void HandleLastSentence()
        {
            if (PopInteraction() is DialogueNode node)
            {
                currentNode = node;
                HandleInteraction();
            }
            else callbacks.onReachLastSentence?.Invoke();
        }

        protected void HandleInteraction()
        {
            if (!IsWaiting) RefreshInteraction();
            else resumeOnWaitOver = true;
        }
        protected virtual void RefreshInteraction()
        {
            if (currentNode.ExitHere) HandleLastSentence();
            else if (currentNode[0]?.Next is BranchNode branch && branch.GetBranch(CurrentEntryData) is null)
            {
                CurrentEntryData[branch]?.Access();
                HandleLastSentence();
            }
            else
            {
                if (currentNode[0]?.Next is PortraitVoiceOverrideNode oNode)
                {
                    while (oNode)
                    {
                        if (oNode.ExitHere)
                        {
                            HandleLastSentence();
                            return;
                        }
                        else oNode = oNode[0]?.Next as PortraitVoiceOverrideNode;
                    }
                }
                if (currentNode.Options.Count == 1 && currentNode[0].IsMain && peekExternalOptions() is null)
                {
                    if (currentNode[0].Next is not BranchNode node || node.GetBranch(CurrentEntryData))
                        RefreshNextAction(() => DoOption(currentNode[0]));
                }
                else
                {
                    IList<DialogueOption> options = null;
                    if (peekExternalOptions() is ExternalOptionsNode eo)
                    {
                        options = eo.GetOptions(CurrentEntryData, currentNode);
                        CurrentEntryData[eo]?.Access();
                    }
                    else options = currentNode.Options;
                    HandleOptions(options);
                }

                ExternalOptionsNode peekExternalOptions()
                {
                    if (currentNode[0].Next is ExternalOptionsNode external) return external;
                    else if (currentNode[0].Next is PortraitVoiceOverrideNode temp)
                    {
                        while (temp)
                        {
                            if (temp[0]?.Next is RevertOptions options) return options;
                            temp = temp[0]?.Next as PortraitVoiceOverrideNode;
                        }
                    }
                    return null;
                }
            }
        }
        /// <summary>
        /// 重置所有交互按钮，包括选项<br/>
        /// Reset all interaction buttons, include options.
        /// </summary>
        protected virtual void ResetInteraction()
        {
            RefreshOptions(null);
            RefreshNextAction(null);
        }

        protected virtual void DisplaySentence(SentenceNode sentence)
        {
            StopWriting();
            writeCoroutine = StartCoroutine(Write(sentence));
        }
        /// <summary>
        /// 播放打字机效果<br/>
        /// Play typing animation.
        /// </summary>
        protected virtual IEnumerator Write(SentenceNode sentence)
        {
            playTime = 0;
            targetContent = PreprocessText(sentence.Content);
            //原文本与预处理过的文本的长度可能不一样，所以需要一个乘算因子来保持说出全句的时长一样
            //The length of raw content is different from the one of preprocessed content, so we need a multiplication factor to keep the same time spent of saying the whole sentence
            var factor = targetContent.Length < 1 ? 1 : (sentence.Content.Length * 1.0f / targetContent.Length);
            targetContent = callbacks.processText?.Invoke(targetContent) ?? targetContent;
            if (!string.IsNullOrEmpty(targetContent))
            {
                Stack<string> ends = new Stack<string>();
                string suffix = string.Empty;
                float interval = 0;
                for (int i = 0; i < targetContent.Length; i++)
                {
                    //处理富文本标签
                    //Handling rich-text tags.
                    string end = ends.Count > 0 ? ends.Peek() : string.Empty;
                    if (Regex.Match(peekString(i, 20),
                                    @"^(<color=?>|<color=[a-z]+>|<color='[a-z]+'>|<color=""[a-z]+"">|<color=#[a-f0-9]{6}>|<color=#[a-f0-9]{8}>)",
                                    RegexOptions.IgnoreCase) is Match match && match.Success)
                    {
                        ends.Push("</color>");
                        suffix = suffix.Insert(0, "</color>");
                        i += match.Value.Length - 1;
                    }
                    else if (Regex.Match(peekString(i, 10), @"^(<size>|<size=\d*>)", RegexOptions.IgnoreCase) is Match match2 && match2.Success)
                    {
                        ends.Push("</size>");
                        suffix = suffix.Insert(0, "</size>");
                        i += match2.Value.Length - 1;
                    }
                    else if (Regex.Match(peekString(i, 3), @"^(<i>)", RegexOptions.IgnoreCase) is Match match3 && match3.Success)
                    {
                        ends.Push("</i>");
                        suffix = suffix.Insert(0, "</i>");
                        i += match3.Value.Length - 1;
                    }
                    else if (Regex.Match(peekString(i, 3), @"^(<b>)", RegexOptions.IgnoreCase) is Match match4 && match4.Success)
                    {
                        ends.Push("</b>");
                        suffix = suffix.Insert(0, "</b>");
                        i += match4.Value.Length - 1;
                    }
                    else if (ends.Count > 0 && i + end.Length <= targetContent.Length && targetContent[i..(i + end.Length)] == end)
                    {
                        i += end.Length - 1;
                        suffix = suffix[ends.Pop().Length..^0];
                    }
                    else
                    {
                        playTime += interval;
                        //下面的操作是，前段正常显示，后段设置为透明
                        //The following operation intends, display the front text, and set the remainder text to transprent.
                        var remainder = Regex.Replace(targetContent[i..^0],
                                                      @"<color=?>|<color=[a-z]+>|<color=""[a-z]+"">|<color='[a-z]+'>|<color=#*[a-f\d]{6}>|<color=#*[a-f\d]{8}>|<\/color>|<b>|<\/b>|<i>|<\/i>",
                                                      "", RegexOptions.IgnoreCase);
                        //处理字号标签，保证每个字符都在正确位置上
                        //Handling the font size tags, make sure every char stay at the correct position.
                        var prefix = string.Empty;
                        var ms = Regex.Matches(remainder, @"<size>|<size=\d{0,3}>|<\/size>", RegexOptions.IgnoreCase);
                        var tags = new Queue<Match>(Regex.Matches(targetContent[0..i], @"<size=?>|<size=\d{0,3}>", RegexOptions.IgnoreCase));
                        for (int j = 0; j < ms.Count; j++)
                        {
                            if (tags.Count > 0 && Regex.IsMatch(ms[j].Value, @"</size>", RegexOptions.IgnoreCase) && (j == 0 || !Regex.IsMatch(ms[j - 1].Value, @"<size=?>|<size=\d{0,3}>", RegexOptions.IgnoreCase)))
                                prefix += tags.Dequeue().Value;
                        }
                        remainder = prefix + remainder;
                        var result = targetContent[0..i] + suffix + (!string.IsNullOrEmpty(remainder) ? $"<color=#00000000>{remainder}</color>" : string.Empty);
                        callbacks.setContent?.Invoke(result);
                        //计算下个字符的显示间隔，当然，空格和标点符号不进行逐字，直接跟随上一个字符出来
                        //Calculate the picking interval of next char, and not animating the space and punctuation, they display along with previous char.
                        if (i < targetContent.Length && (targetContent[i] == '…' || !char.IsPunctuation(targetContent[i]) && !char.IsWhiteSpace(targetContent[i])))
                        {
                            interval = factor * getNextSpeakInterval(i);
                            if (interval > 0) yield return new WaitForSecondsRealtime(interval);
                        }
                        else interval = 0;
                    }
                }

                string peekString(int i, int length) => targetContent[i..(i + length <= targetContent.Length ? i + length : ^0)];
                float getNextSpeakInterval(int i)
                {
                    if (i < 0 || i > targetContent.Length - 1 || (sentence.SpeakInterval?.keys.Length ?? 0) < 2) return 0;
                    return sentence.SpeakInterval.Evaluate((i + 1.0f) / targetContent.Length);
                }
            }
            EndWriting();
        }
        /// <summary>
        /// 跳过打字效果以显示全部文本<br/>
        /// Skip the typing animation to display all text.
        /// </summary>
        public virtual void Skip()
        {
            if (!IsWaiting && IsPlaying && playTime >= (callbacks.getSkipDelay?.Invoke() ?? 0.5f))
                EndWriting();
        }
        protected void EndWriting()
        {
            callbacks.setContent?.Invoke(targetContent);
            HandleInteraction();
            StopWriting();
            callbacks.onEndWriting?.Invoke();
        }
        protected void StopWriting()
        {
            StopCoroutine(writeCoroutine);
            writeCoroutine = null;
            playTime = 0;
        }

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            return callbacks.startCoroutine?.Invoke(routine);
        }
        protected void StopCoroutine(Coroutine routine)
        {
            if (routine != null) callbacks.stopCoroutine?.Invoke(routine);
        }

        /// <summary>
        /// 预处理要显示的文本，会在调用 <see cref="DialogueHandlerCallbacks.processText"/> 之前调用, 返回的值会被用来计算同步语速时的乘算因子<br/>
        /// Preprocess text to display, get called before <see cref="DialogueHandlerCallbacks.processText"/>, the return value will be used to calculate the multiplication factor to synchronize speech speed.
        /// </summary>
        /// <returns>预处理过的文本<br/>
        /// Preprocessed text.
        /// </returns>
        protected string PreprocessText(string text)
        {
            return callbacks.preprocessText?.Invoke(text) ?? text;
        }
        protected string ProcessText(string text)
        {
            text = PreprocessText(text);
            return callbacks.processText?.Invoke(text) ?? text;
        }

        protected virtual void RefreshNextAction(Action action)
        {
            callbacks.refreshNextAction?.Invoke(action);
        }
        protected virtual void HandleOptions(IList<DialogueOption> options)
        {
            List<OptionCallback> callbacks = new List<OptionCallback>();
            //是否所有可使对话结束的选项(以下简称"结束选项")都会被剔除？如果是我们要强制保留第一个选项来显示
            //Is that all options which will cause the dialogue exit (abbreviated as 'Exit Option') will be culled? If it is then we force the first one to display
            bool needPreserve = options.Count > 0 && options
                .Where(opt => opt.Next && opt.Next.Exitable).All(opt =>
                {
                    return opt.Next is ConditionNode condition && !condition.Check(CurrentEntryData);
                });
            //是否已经保留了一个结束选项？
            //Is that an exit option is preserved?
            bool preserved = false;
            foreach (var option in options)
            {
                bool culled = false;
                if (option.Next is ConditionNode condition)
                {
                    //如果不在此选项结束对话或无需保留，则按需标记剔除
                    //If there's no need to preserve option or not exit from here, then cull as we need.
                    if ((!condition.Exitable || !needPreserve) && !condition.Check(CurrentEntryData))
                        culled = true;
                    //如果需要保留且已经保留了一个结束选项，然后这个也是结束选项，也剔除掉
                    //If already preserved one necessary option, and this is an exit option, then cull it, too.
                    else if (needPreserve && preserved && condition.Exitable)
                        culled = true;
                    if (!preserved) preserved = needPreserve && condition.Exitable;
                }
                if (!culled)
                {
                    string title = ProcessText(option.Title);
                    var temp = option.Next;
                    while (temp is DecoratorNode decorator)
                    {
                        decorator.Decorate(CurrentEntryData, ref title);
                        temp = temp[0]?.Next;
                    }
                    callbacks.Add(new OptionCallback(title, () => DoOption(option)));
                }
            }
            RefreshOptions(callbacks);
        }
        protected virtual void RefreshOptions(List<OptionCallback> callbacks)
        {
            this.callbacks.refreshOptions?.Invoke(callbacks);
        }

        protected virtual void RefreshPortrait(SentenceNode sentence)
        {
#if !ZTDS_DISABLE_PORTRAIT
            Sprite portrait = getRealPortrait();
            if (sentence is EntryNode)
            {
                ResetPortraits();
                callbacks.setPortrait(true, portrait);
                isUsingLeftPortrait = true;
                portraitInterlocutor = currentInterlocutor;
            }
            else
            {
                if (portrait)
                {
                    if (shouldChangeSide())
                    {
                        isUsingLeftPortrait = !isUsingLeftPortrait;
                        portraitInterlocutor = currentInterlocutor;
                    }
                    callbacks.setPortrait(isUsingLeftPortrait, portrait);
                }
                callbacks.setPortraitDark(true, !portrait || !isUsingLeftPortrait);
                callbacks.setPortraitDark(false, !portrait || isUsingLeftPortrait);
            }
            hasLeftPortrait = isUsingLeftPortrait && portrait;
            hasRightPortrait = !isUsingLeftPortrait && portrait;

            bool shouldChangeSide()
            {
                return !sentence.KeepPortraitSide && portraitInterlocutor != currentInterlocutor && (isUsingLeftPortrait && hasLeftPortrait || !isUsingLeftPortrait && hasRightPortrait);
            }
            Sprite getRealPortrait()
            {
                Sprite portrait = null;
                if (sentence[0]?.Next is PortraitVoiceOverrideNode temp)
                {
                    while (temp)
                    {
                        CurrentEntryData[temp]?.Access();
                        if (temp is PortraitOverride po && (portrait = po.GetPortrait(CurrentNodeData))) break;
                        else temp = temp[0]?.Next as PortraitVoiceOverrideNode;
                    }
                }
                return portrait ? portrait : sentence.Portrait;
            }
#endif
        }
        protected virtual void ResetPortraits()
        {
#if !ZTDS_DISABLE_PORTRAIT
            callbacks.setPortrait(true, null);
            callbacks.setPortrait(false, null);
            callbacks.setPortraitDark(true, false);
            callbacks.setPortraitDark(false, false);
            isUsingLeftPortrait = true;
            hasLeftPortrait = false;
            hasRightPortrait = false;
            portraitInterlocutor = null;
#endif
        }

        protected virtual void PlayVoice(SentenceNode sentence)
        {
#if !ZTDS_DISABLE_VOICE
            StopVoice();
            callbacks.playVoice(getRealVoice(), sentence.VoiceOffset);

            AudioClip getRealVoice()
            {
                AudioClip voice = null;
                if (sentence[0]?.Next is PortraitVoiceOverrideNode temp)
                {
                    while (temp)
                    {
                        if (temp is VoiceOverride vo && (voice = vo.GetVoice(CurrentNodeData))) break;
                        else temp = temp[0]?.Next as PortraitVoiceOverrideNode;
                    }
                }
                return voice ? voice : sentence.Voice;
            }
#endif
        }
        protected virtual void StopVoice()
        {
#if !ZTDS_DISABLE_VOICE
            callbacks.stopVoice();
#endif
        }
        #endregion

        #region 操作相关 Actions
        public virtual void BackHome()
        {
            ResetInteraction();
            continueNodes.Clear();
            manualNodes.Clear();
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            StartWith(Home);
        }
        public virtual void BackHomeImmediate()
        {
            BackHome();
            if (IsPlaying)
            {
                StopWriting();
                callbacks.setContent?.Invoke(targetContent);
                writeCoroutine = null;
            }
        }
        /// <summary>
        /// 执行选项所连结点的功能<br/>
        /// Perform function of node that input option connected.
        /// </summary>
        protected virtual void DoOption(DialogueOption option)
        {
            if (IsWaiting) return;
            HandleNode(option?.Next);
        }
        /// <summary>
        /// 执行结点的自定义操作<br/>
        /// Perform the custom action of given node.
        /// </summary>
        /// <param name="node"></param>
        protected void DoManual(IManualNode node)
        {
            if (node == null) return;
            if (manualNodes.Contains(node))
            {
                Debug.LogError($"Endless loop happends in type '{node.GetType().Name}'");
                return;
            }
            manualNodes.Add(node);
            CurrentEntryData[node as DialogueNode]?.Access();
            node.DoManual(this);
            manualNodes.Remove(node);
        }
        public void InvokeEvent(DialogueEvent evt, DialogueData ownerData)
        {
            if (evt != null && evt.Invoke(ownerData))
                invokedEvents[evt] = ownerData;
        }
        #endregion

        #region 其它 Other
        public virtual void Init()
        {
            StopWriting();
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            Home = null;
            CurrentEntry = null;
            currentNode = null;
            Interlocutor = null;
            ResetInteraction();
            continueNodes.Clear();
            manualNodes.Clear();
            invokedEvents.ForEach(e => e.Key.CancelInvoke(e.Value));
            invokedEvents.Clear();
            ResetPortraits();
            StopVoice();
        }
        /// <summary>
        /// 等待满足条件才显示交互按钮<br/>
        /// Hide interaction buttons until the given condition is met.
        /// </summary>
        /// <param name="predicate"></param>
        public void WaitUntil(Func<bool> predicate)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = StartCoroutine(waitUtil());

            IEnumerator waitUtil()
            {
                IsWaiting = true;
                yield return new WaitUntil(predicate);
                IsWaiting = false;
                if (resumeOnWaitOver) RefreshInteraction();
                resumeOnWaitOver = false;
            }
        }
        /// <summary>
        /// 将结点入栈。当本次对话结束时，将会显示栈顶结点的选项<br/>
        /// Push the node onto stack. When current dialogue ends, we will show the options of top node of that stack.
        /// </summary>
        public void PushInteraction(DialogueNode node) => continueNodes.Push(node);
        protected DialogueNode PopInteraction() => continueNodes.Count > 0 ? continueNodes.Pop() : null;
        #endregion
    }

    public interface IInterlocutor : IInteractive
    {
        /// <summary>
        /// 用来开始交谈的对话<br/>
        /// The dialogue for starting conversation.
        /// </summary>
        Dialogue Dialogue { get; set; }
    }

    public struct OptionCallback
    {
        public string title;
        public Action callback;

        public OptionCallback(string title, Action callback)
        {
            this.title = title;
            this.callback = callback;
        }
    }
}