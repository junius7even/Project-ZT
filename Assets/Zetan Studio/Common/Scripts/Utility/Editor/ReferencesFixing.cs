using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ZetanStudio.Editor
{
    public class ReferencesFixing : EditorWindow
    {
        [MenuItem("Tools/Zetan Studio/引用丢失修复 (Missing References Fixing)")]
        public static void CreateWindow()
        {
            var wnd = GetWindow<ReferencesFixing>(EDL.Tr("引用修复"));
            wnd.minSize = new Vector2(400, 165);
            wnd.Show();
        }
        public static void CreateWindow(Action<int> finishCallback)
        {
            var wnd = GetWindow<ReferencesFixing>(EDL.Tr("引用修复"));
            wnd.minSize = new Vector2(400, 165);
            wnd.FinishCallback += finishCallback;
            wnd.Show();
        }

        private string searchTypeName = "ScriptableObject";
        private string oldTypeName;
        private string newTypeName;
        private Vector2 scroll;
        public event Action<int> FinishCallback;

        private void OnEnable()
        {
            oldTypeName = EDL.Tr("(类名), (命名空间), Assembly-CSharp");
            newTypeName = EDL.Tr("(类名), (命名空间), Assembly-CSharp");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            bool errors = error(oldTypeName) || error(newTypeName);
            if (errors) EditorGUILayout.HelpBox(EDL.Tr("类名格式不正确，应为：\"类名, 命名空间(如果有), 程序集\""), MessageType.Error);
            else if (oldTypeName == newTypeName)
            {
                EditorGUILayout.HelpBox(EDL.Tr("要替换的内容一样"), MessageType.Warning);
                errors = true;
            }
            else EditorGUILayout.HelpBox(EDL.Tr("无错误"), MessageType.Info);
            searchTypeName = EditorGUILayout.TextField(EDL.Tr("检索类名"), searchTypeName);
            EditorGUILayout.LabelField(EDL.Tr("替换"));
            oldTypeName = EditorGUILayout.TextArea(oldTypeName);
            EditorGUILayout.LabelField(EDL.Tr("为"));
            newTypeName = EditorGUILayout.TextArea(newTypeName);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(searchTypeName) || errors);
            if (GUILayout.Button(EDL.Tr("修复")))
            {
                var temp = Regex.Replace(oldTypeName, "[ \n\t\r]", "").Split(',');
                var oldName = temp[0];
                var oldNs = temp[1];
                var oldAsm = temp[2];
                temp = Regex.Replace(newTypeName, "[ \n\t\r]", "").Split(',');
                var newName = temp[0];
                newName = string.IsNullOrEmpty(newName) ? oldName : newName;
                var newNs = temp[1];
                var newAsm = temp[2];
                List<string> paths = new List<string>();
                string[] guids = AssetDatabase.FindAssets($"t:{searchTypeName}");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path)) paths.Add(path);
                }
                int count = 0;
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(EDL.Tr("查找替换中"), EDL.Tr("当前路径: {0}", paths[i]), i / paths.Count);
                    string pattern = $"type: *{{class: *{oldName}, *ns: *{oldNs}, *asm: *{oldAsm}}}";
                    string text = File.ReadAllText(paths[i]);
                    if (Regex.IsMatch(text, pattern))
                    {
                        File.WriteAllText(paths[i], Regex.Replace(text, pattern, $"type: {{class: {newName}, ns: {newNs}, asm: {newAsm}}}"));
                        AssetDatabase.ImportAsset(paths[i]);
                        count++;
                    }
                }
                EditorUtility.ClearProgressBar();
                Debug.Log(EDL.Tr("共修复了 {0} 个资源", count));
                FinishCallback?.Invoke(count);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();

            static bool error(string typeName)
            {
                return !Regex.IsMatch(typeName.Replace(" ", ""), @"^[a-zA-Z_]\w*, *([a-zA-Z_][\w]*(\.[\w])*)*, *[a-zA-Z_][\w-]*$");
            }
        }
    }
}
