using System.Collections.Generic;

namespace ZetanStudio
{
    public static class NotificationCenter
    {
        public delegate void NotificationListener(params object[] msg);

        private static readonly Dictionary<string, Notification> notifications = new Dictionary<string, Notification>();

        private static readonly Dictionary<object, Dictionary<string, HashSet<NotificationListener>>> notificationsWithOwner = new Dictionary<object, Dictionary<string, HashSet<NotificationListener>>>();

        /// <summary>
        /// 消息订阅。接收方法不需要额外对消息进行判空，发布消息前已做非空处理。<br/>
        /// Register notification listener. There's no need to check null message in listening method, non-null processing has been performed before posting messages.
        /// </summary>
        /// <param name="owner">接收方法拥有人<br/>
        /// Listening method owner.
        /// </param>
        public static void Register(string msgType, NotificationListener listener, object owner = null)
        {
            if (notifications.TryGetValue(msgType, out Notification find)) find.AddListener(listener);
            else notifications.Add(msgType, new Notification(listener));

            object target = owner ?? listener.Target;
            if (target != null)
            {
                if (notificationsWithOwner.TryGetValue(target, out var dict))
                {
                    if (dict.TryGetValue(msgType, out var set))
                    {
                        if (!set.Contains(listener))
                            set.Add(listener);
                    }
                    else dict.Add(msgType, new HashSet<NotificationListener>() { listener });
                }
                else
                    notificationsWithOwner.Add(target, new Dictionary<string, HashSet<NotificationListener>>() { { msgType, new HashSet<NotificationListener>() { listener } } });
            }
        }
        public static void Unregister(string msgType, NotificationListener listener)
        {
            if (notifications.TryGetValue(msgType, out Notification find))
                find.RemoveListener(listener);
            if (notificationsWithOwner.TryGetValue(listener.Target, out var dict))
                if (dict.TryGetValue(msgType, out var list))
                    list.Remove(listener);
        }
        public static void Unregister(object owner)
        {
            if (notificationsWithOwner.TryGetValue(owner, out var dict))
                foreach (var item in dict)
                {
                    foreach (var listener in item.Value)
                    {
                        if (notifications.TryGetValue(item.Key, out Notification find))
                            find.RemoveListener(listener);
                    }
                }
            notificationsWithOwner.Remove(owner);
        }
        /// <summary>
        /// 消息发布。发布者不需要额外对消息进行非空处理，发布消息时会做非空处理。<br/>
        /// Post notification. There's no need to check null message in posting method, non-null processing will be performed before posting messages.
        /// </summary>
        public static void Post(string msgType, params object[] msg)
        {
            msg ??= new object[0];
            if (notifications.TryGetValue(msgType, out Notification find))
                find.Invoke(msg);
        }

        private class Notification
        {
            private event NotificationListener Event;

            public Notification(NotificationListener listener)
            {
                AddListener(listener);
            }

            public void AddListener(NotificationListener listener)
            {
                //预防重复监听
                //Avoiding repeated listening.
                Event -= listener;
                Event += listener;
            }

            public void RemoveListener(NotificationListener listener)
            {
                Event -= listener;
            }

            public void Invoke(params object[] msg)
            {
                Event?.Invoke(msg);
            }
        }
    }
}