using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ZetanStudio.SaveSystem
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Zetan Studio/管理器/存档管理器")]
    public static class SaveManager
    {
        private const string defaultDataName = "save_data_{0}.sav";
        public static string dataName;
        private static string DataName => string.IsNullOrEmpty(dataName) ? defaultDataName : dataName;

        private const string defaultEncryptionKey = "zetangamedatazetangamedatazetang";
        public static string encryptionKey;
        private static string EncryptionKey => string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length != 16 || encryptionKey.Length != 32 ? defaultEncryptionKey : encryptionKey;


        public static bool IsSaving { get; private set; }
        public static bool IsLoading { get; private set; }

        public static event Action OnSaveCompletely;
        public static event Action OnLoadCompletely;

        public static bool Save(int index = 0)
        {
            try
            {
                using FileStream fs = new FileStream(Application.persistentDataPath + '/' + string.Format(DataName, index), FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                SaveData saveData = new SaveData(Application.version);
                IsSaving = true;
                SaveMethodAttribute.SaveAll(saveData);
                bf.Serialize(fs, saveData);
                UtilityZT.Encrypt(fs, EncryptionKey);
                OnSaveCompletely?.Invoke();
                IsSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                IsSaving = false;
                Debug.LogException(ex);
                return false;
            }
        }

        public static bool Load(int index = 0)
        {
            try
            {
                using FileStream fs = new FileStream(Application.persistentDataPath + '/' + string.Format(DataName, index), FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                SaveData saveData = bf.Deserialize(UtilityZT.Decrypt(fs, EncryptionKey)) as SaveData;
                ISceneLoader.Instance?.LoadScene(saveData.ReadString("sceneName"), () =>
                {
                    try
                    {
                        IsLoading = true;
                        InitAttribute.InitAll();
                        InitMethodAttribute.InitAll();
                        LoadMethodAttribute.LoadAll(saveData);
                        UI.WindowManager.CloseAll();
                        OnLoadCompletely?.Invoke();
                        IsLoading = false;
                    }
                    catch (Exception ex)
                    {
                        IsLoading = false;
                        Debug.LogException(ex);
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Debug.LogException(ex);
                return false;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SaveMethodAttribute : Attribute
    {
        public readonly int priority;

        public SaveMethodAttribute(int priority = 0)
        {
            this.priority = priority;
        }

        public static void SaveAll(SaveData data)
        {
            var methods = new List<MethodInfo>(TypeCacheZT.GetMethodsWithAttribute<SaveMethodAttribute>());
            methods.Sort((x, y) =>
            {
                var attrx = x.GetCustomAttribute<SaveMethodAttribute>();
                var attry = y.GetCustomAttribute<SaveMethodAttribute>();
                if (attrx.priority < attry.priority)
                    return -1;
                else if (attrx.priority > attry.priority)
                    return 1;
                return 0;
            });
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(null, new object[] { data });
                }
                catch { }
            }
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class LoadMethodAttribute : Attribute
    {
        public readonly int priority;

        public LoadMethodAttribute(int priority = 0)
        {
            this.priority = priority;
        }

        public static void LoadAll(SaveData data)
        {
            var methods = new List<MethodInfo>(TypeCacheZT.GetMethodsWithAttribute<LoadMethodAttribute>());
            methods.Sort((x, y) =>
            {
                var attrx = x.GetCustomAttribute<LoadMethodAttribute>();
                var attry = y.GetCustomAttribute<LoadMethodAttribute>();
                if (attrx.priority < attry.priority)
                    return -1;
                else if (attrx.priority > attry.priority)
                    return 1;
                return 0;
            });
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(null, new object[] { data });
                }
                catch { }
            }
        }
    }
}