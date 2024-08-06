using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace ZetanStudio
{
    public abstract class SingletonScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void FindEditorInstance()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<SingletonScriptableObject>())
            {
                if (type.BaseType.IsGenericType
                    && typeof(SingletonScriptableObject<>).IsAssignableFrom(type.BaseType.GetGenericTypeDefinition())
                    && !type.IsAbstract
                    && !type.IsGenericType
                    && !type.IsGenericTypeDefinition)
                    type.BaseType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, instance(type));
            }

            static SingletonScriptableObject instance(Type type) => UtilityZT.Editor.LoadResource(type) as SingletonScriptableObject;
        }

        private class SingletonModificationProcessor : AssetModificationProcessor
        {
#pragma warning disable IDE0060
            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
#pragma warning restore IDE0060
            {
                if (AssetDatabase.LoadAssetAtPath<SingletonScriptableObject>(path) is SingletonScriptableObject singleton && singleton.GetType() is Type type)
                {
                    if (type.BaseType.IsGenericType
                        && typeof(SingletonScriptableObject<>).IsAssignableFrom(type.BaseType.GetGenericTypeDefinition())
                        && !type.IsAbstract
                        && !type.IsGenericType
                        && !type.IsGenericTypeDefinition)
                        type.BaseType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, UtilityZT.Editor.LoadResource(type) as SingletonScriptableObject);
                }
                return AssetDeleteResult.DidNotDelete;
            }
        }
#endif
    }

    public abstract class SingletonScriptableObject<T> : SingletonScriptableObject where T : SingletonScriptableObject<T>
    {
        private static T instance;
        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!instance) instance = UtilityZT.Editor.LoadResource<T>();
#else
                if (!instance) instance = UtilityZT.LoadResource<T>();
#endif
                if (Application.isPlaying && !instance) instance = CreateInstance<T>();
                return instance;
            }
        }

        public SingletonScriptableObject()
        {
            instance = this as T;
        }

#if UNITY_EDITOR
        public static T GetOrCreate()
        {
            if (!instance) instance = UtilityZT.Editor.LoadResource<T>();
            if (!instance)
            {
                instance = CreateInstance<T>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath($"Assets/Resources/New {ObjectNames.NicifyVariableName(typeof(T).Name)}.asset"));
            }
            return instance;
        }

        protected static void CreateSingleton()
        {
            if (typeof(T).IsAbstract || typeof(T).IsGenericType || typeof(T).IsGenericTypeDefinition || typeof(T).IsGenericParameter) return;
            if (UtilityZT.Editor.LoadResource<T>() is T instance)
            {
                Debug.LogWarning(Editor.EDL.Tr("创建“{0}”失败, 因为已存在单例, 路径: {1}", typeof(T).Name, AssetDatabase.GetAssetPath(instance)));
                EditorGUIUtility.PingObject(instance);
                return;
            }
            var path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (!AssetDatabase.IsValidFolder(path)) path = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
            {
                instance = CreateInstance<T>();
                AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(path + $"/New {ObjectNames.NicifyVariableName(typeof(T).Name)}.asset"));
                Selection.activeObject = instance;
            }
        }
#endif
    }
}