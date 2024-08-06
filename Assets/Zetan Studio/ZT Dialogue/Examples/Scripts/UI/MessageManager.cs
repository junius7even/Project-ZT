using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZetanStudio.Examples
{
    [DisallowMultipleComponent]
    public class MessageManager : SingletonMonoBehaviour<MessageManager>, IMessageDisplayer
    {
        [SerializeField, Label("消息根")]
        private UnityEngine.UI.VerticalLayoutGroup messageRoot;
        [SerializeField, Label("消息预制件")]
        private MessageItem messagePrefab;

        private Canvas rootCanvas;

        private SimplePool<MessageItem> pool;

        private readonly List<MessageItem> messages = new List<MessageItem>();

        void IMessageDisplayer.Push(string message) => PushInternal(message);

        public static void Push(string message, float? lifeTime = null)
        {
            if (Instance) Instance.PushInternal(message, lifeTime);
        }
        private void PushInternal(string message, float? lifeTime = null)
        {
            if (string.IsNullOrEmpty(message)) return;
            MessageItem ma = pool.Get(messageRoot.transform);
            ma.messageText.text = message;
            messages.Add(ma);
            StartCoroutine(Delay(lifeTime ?? 2, () => Recycle(ma)));
        }

        private IEnumerator Delay(float time, Action callback)
        {
            yield return new WaitForSecondsRealtime(time);
            callback?.Invoke();
        }

        private void Awake()
        {
            rootCanvas = messageRoot.GetComponent<Canvas>();
            if (!rootCanvas) rootCanvas = messageRoot.gameObject.AddComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingLayerID = SortingLayer.NameToID("UI");
            rootCanvas.sortingOrder = 999;
            pool = new SimplePool<MessageItem>(messagePrefab);
            IMessageDisplayer.Instance = this;
        }

        private void Recycle(MessageItem message)
        {
            messages.Remove(message);
            message.messageText.text = string.Empty;
            pool.Put(message);
        }

        public void Init()
        {
            foreach (var message in messages)
            {
                if (message && message.gameObject)
                {
                    message.messageText.text = string.Empty;
                    pool?.Put(message);
                }
            }
            messages.Clear();
        }
    }
}
