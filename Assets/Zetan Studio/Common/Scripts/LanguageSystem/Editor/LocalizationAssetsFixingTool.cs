using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ZetanStudio.LanguageSystem.Editor
{
    using ZetanStudio.Editor;

    public class LocalizationAssetsFixingTool : EditorWindow
    {
        [MenuItem("Tools/Zetan Studio/本地化文件修复工具 (Localization Assets Fixing Tool)")]
        private static void CreateWindow()
        {
            var wnd = GetWindow<LocalizationAssetsFixingTool>(EDL.Tr("本地化文件修复"));
            wnd.minSize = new Vector2(400, 60);
            wnd.Show();
        }

        private const string oldName = "Language";
        private const string newName = "Translation";

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(EDL.Tr("此工具可修复1.0.0版本的本地化文件在升级后翻译包的引用丢失的问题"), MessageType.Info);
            if (GUILayout.Button(EDL.Tr("一键修复")))
            {
                var localizations = UtilityZT.Editor.LoadAssets<Localization>();
                int count = 0;
                foreach (var localization in localizations)
                {
                    string path = AssetDatabase.GetAssetPath(localization);
                    string pattern = $"<{oldName}>k__BackingField:";
                    string text = File.ReadAllText(path);
                    if (Regex.IsMatch(text, pattern, RegexOptions.Multiline))
                    {
                        File.WriteAllText(path, Regex.Replace(text, pattern, $"<{newName}>k__BackingField:", RegexOptions.Multiline));
                        AssetDatabase.ImportAsset(path);
                        count++;
                    }
                }
                if (count > 0) Debug.Log(EDL.Tr("成功处理 {0} 个本地化文件", count));
                else Debug.Log(EDL.Tr("没有要处理的本地化文件"));
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}