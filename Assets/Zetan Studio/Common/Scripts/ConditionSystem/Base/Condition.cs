using System;
using System.Reflection;

namespace ZetanStudio.ConditionSystem
{
    [Serializable]
    public abstract class Condition
    {
        public abstract bool IsValid { get; }

        public abstract bool IsMet();

        public static string GetGroup(Type type)
        {
            if (type.GetCustomAttribute<GroupAttribute>() is GroupAttribute attr) return attr.group;
            return string.Empty;
        }
        public static string GetName(Type type)
        {
            if (type.GetCustomAttribute<NameAttribute>() is NameAttribute attr) return attr.name;
            return string.Empty;
        }

        protected sealed class GroupAttribute : Attribute
        {
            public readonly string group;

            public GroupAttribute(string group)
            {
                this.group = group;
            }
        }
        protected sealed class NameAttribute : Attribute
        {
            public readonly string name;

            public NameAttribute(string name)
            {
                this.name = name;
            }
        }

        public static implicit operator bool(Condition obj)
        {
            return obj != null;
        }
    }
}