using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ZetanStudio
{
    /// <summary>
    /// 把这个加给具有初始化方法的类型，性能比 <see cref="InitMethodAttribute"/> 更好<br/>
    /// Add this to types that has an initialization method, it has better performance than <see cref="InitMethodAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class InitAttribute : Attribute
    {
        public readonly string method;
        public readonly int priority;

        public InitAttribute(string method, int priority = 0)
        {
            this.method = method;
            this.priority = priority;
        }

        public static void InitAll()
        {
            var types = new List<Type>(TypeCacheZT.GetTypesWithAttribute<InitAttribute>());
            types.Sort((x, y) =>
            {
                var attrx = x.GetCustomAttribute<InitAttribute>();
                var attry = y.GetCustomAttribute<InitAttribute>();
                if (attrx.priority < attry.priority)
                    return -1;
                else if (attrx.priority > attry.priority)
                    return 1;
                return 0;
            });
            foreach (var type in types)
            {
                try
                {
                    type.GetMethod(type.GetCustomAttribute<InitAttribute>().method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }

    /// <summary>
    /// 把这个加到初始化方法上面，方便，但性能比较差<br/>
    /// Add this to initialization methods, it's convenient, but has poor performance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InitMethodAttribute : Attribute
    {
        public readonly int priority;

        public InitMethodAttribute(int priority = 0)
        {
            this.priority = priority;
        }

        public static void InitAll()
        {
            var methods = TypeCacheZT.GetMethodsWithAttribute<InitMethodAttribute>();
            Array.Sort(methods, (x, y) =>
            {
                var attrx = x.GetCustomAttribute<InitMethodAttribute>();
                var attry = y.GetCustomAttribute<InitMethodAttribute>();
                if (attrx.priority < attry.priority) return -1;
                else if (attrx.priority > attry.priority) return 1;
                return 0;
            });
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}