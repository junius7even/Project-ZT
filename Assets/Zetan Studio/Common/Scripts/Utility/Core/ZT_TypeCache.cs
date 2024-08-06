using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZetanStudio
{
    public static class TypeCacheZT
    {
        private static readonly Dictionary<string, Type> types = new Dictionary<string, Type>();

        private static readonly HashSet<string> internalAssemblyNames = new HashSet<string>()
        {
            "AndroidPlayerBuildProgram.Data",
            "Assembly-CSharp-Editor",
            "asset-store-tools-editor",
            "Bee.BeeDriver",
            "BeeBuildProgramCommon.Data",
            "Excel",
            "ExCSS.Unity",
            "ICSharpCode.SharpZipLib",
            "log4net",
            "Microsoft.CSharp",
            "Mono.Security",
            "Mono.Posix",
            "mscorlib",
            "netstandard",
            "Newtonsoft.Json",
            "NiceIO",
            "unityplastic",
            "Unrelated",
            "nunit.framework",
            "PlayerBuildProgramLibrary.Data",
            "PsdPlugin",
            "ReportGeneratorMerged",
            "ScriptCompilationBuildProgram.Data",
            "SyntaxTree.VisualStudio.Unity.Bridge",
            "SyntaxTree.VisualStudio.Unity.Messaging",
        };

        static TypeCacheZT()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.GetName().Name;
                if (!asmName.StartsWith("System") && !asmName.StartsWith("Unity.")
                    && !asmName.StartsWith("UnityEngine") && !asmName.StartsWith("UnityEngine.")
#if UNITY_EDITOR
                    && !asmName.StartsWith("UnityEditor") && !asmName.StartsWith("UnityEditor.")
#endif
                    && !internalAssemblyNames.Contains(asmName))
                    foreach (var type in asm.GetTypes())
                    {
                        types[type.FullName] = type;
                    }
            }
        }

        public static Type GetType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            if (types.TryGetValue(fullName, out var type)) return type;
            else return null;
        }

        public static Type[] GetTypesDerivedFrom(Type type)
        {
            List<Type> results = new List<Type>();
            foreach (var t in types.Values)
            {
                if (type.IsAssignableFrom(type))
                    results.Add(t);
            }
            return results.ToArray();
        }
        public static Type[] GetTypesDerivedFrom<T>()
        {
            return GetTypesDerivedFrom(typeof(T));
        }
        public static Type[] GetTypesWithAttribute(Type attribute)
        {
            List<Type> results = new List<Type>();
            if (!typeof(Attribute).IsAssignableFrom(attribute)) return results.ToArray();
            foreach (var type in types.Values)
            {
                if (type.GetCustomAttribute(attribute) != null)
                    results.Add(type);
            }
            return results.ToArray();
        }
        public static Type[] GetTypesWithAttribute<T>() where T : Attribute
        {
            return GetTypesWithAttribute(typeof(T));
        }
        public static MethodInfo[] GetMethodsWithAttribute(Type attribute)
        {
            List<MethodInfo> results = new List<MethodInfo>();
            if (!typeof(Attribute).IsAssignableFrom(attribute)) return results.ToArray();
            foreach (var type in types.Values)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (method.GetCustomAttribute(attribute) != null)
                        results.Add(method);
                }
            }
            return results.ToArray();
        }
        public static MethodInfo[] GetMethodsWithAttribute<T>() where T : Attribute
        {
            return GetMethodsWithAttribute(typeof(T));
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        public static class Editor
        {
            private readonly static Dictionary<Type, UnityEditor.MonoScript> scripts;

            static Editor()
            {
                scripts = new Dictionary<Type, UnityEditor.MonoScript>();
                foreach (var script in UtilityZT.Editor.LoadAssets<UnityEditor.MonoScript>())
                    if (script.GetClass() is Type type) scripts[type] = script;
            }

            public static UnityEditor.MonoScript FindScriptOfType<T>() => FindScriptOfType(typeof(T));
            public static UnityEditor.MonoScript FindScriptOfType(Type type)
            {
                if (type == null) return null;
                scripts.TryGetValue(type, out var find);
                return find;
            }
        }
#endif
    }
}