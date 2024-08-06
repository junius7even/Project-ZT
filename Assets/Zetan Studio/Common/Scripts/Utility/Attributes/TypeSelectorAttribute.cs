using System;
using UnityEngine;

namespace ZetanStudio
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TypeSelectorAttribute : PropertyAttribute
    {
        public readonly Type baseType;
        public readonly bool includeAbstract;
        public readonly bool groupByNamespace;

        public TypeSelectorAttribute(Type baseType, bool groupByNamespace = false, bool includeAbstract = false)
        {
            this.baseType = baseType;
            this.groupByNamespace = groupByNamespace;
            this.includeAbstract = includeAbstract;
        }
    }
}