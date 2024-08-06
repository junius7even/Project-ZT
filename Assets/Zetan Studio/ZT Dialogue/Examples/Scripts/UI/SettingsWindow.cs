using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.Examples
{
    using SaveSystem;
    using UI;

    public class SettingsWindow : Window
    {
        [SerializeField]
        private Button saveButton;
        [SerializeField]
        private Button loadButton;
        [SerializeField]
        private Dropdown language;
        [SerializeField]
        private Button exitButton;

        private float timeScaleBef;

        protected override string LanguageSelector => "Common";

#if UNITY_WEBGL
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private extern static void ClosePage();
#endif

        protected override void OnAwake()
        {
            SaveManager.OnSaveCompletely += () => MessageManager.Push(Tr("存档成功！"));
            SaveManager.OnLoadCompletely += () => MessageManager.Push(Tr("读档成功！"));
            saveButton.onClick.AddListener(() => SaveManager.Save());
            loadButton.onClick.AddListener(() => SaveManager.Load());
            if (Localization.Instance)
            {
                UtilityZT.SetActive(language, true);
                language.ClearOptions();
                language.AddOptions(Localization.Instance.LanguageNames.ToList());
                language.value = Language.LanguageIndex;
                language.onValueChanged.AddListener(lang => Language.LanguageIndex = lang);
            }
            else UtilityZT.SetActive(language, false);
            exitButton.onClick.AddListener(() => ConfirmWindow.StartConfirm(Tr("确定退出 {0} 吗？", Application.productName), () =>
            {
                Application.Quit();
#if UNITY_WEBGL
                try
                {
                    ClosePage();
                }
                catch { }
#endif
            }));
        }

        protected override bool OnOpen(params object[] args)
        {
            if (IsOpen) return false;
            WindowManager.HideAll(true);
            timeScaleBef = Time.timeScale;
            Time.timeScale = 0;
            return true;
        }
        protected override bool OnClose(params object[] args)
        {
            WindowManager.HideAll(false);
            Time.timeScale = timeScaleBef;
            return true;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            Language.OnLanguageChanged += () =>
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    var settings = new GenericData();
                    settings["language"] = Language.LanguageIndex;
                    using var fs = UtilityZT.OpenFile(Application.persistentDataPath + "/settings.ini", System.IO.FileMode.OpenOrCreate);
                    bf.Serialize(fs, settings);
                }
                catch { }
            };
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadSettings()
        {
            try
            {
                using var fs = UtilityZT.OpenFile(Application.persistentDataPath + "/settings.ini", System.IO.FileMode.Open, System.IO.FileAccess.Read);
                if (fs != null)
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    var settings = bf.Deserialize(fs) as GenericData;
                    typeof(Language).GetField("languageIndex", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, settings.ReadInt("language"));
                }
                else typeof(Language).GetField("languageIndex", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, GameEntrySettings.Instance.DefaultLanguage);
            }
            catch { }
        }
    }
}