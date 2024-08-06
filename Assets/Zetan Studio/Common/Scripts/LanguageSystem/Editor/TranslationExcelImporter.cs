using Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ZetanStudio.LanguageSystem.Editor
{
    using ZetanStudio.Editor;

    public class TranslationExcelImporter : EditorWindow
    {
        [MenuItem("Window/Zetan Studio/从Excel导入翻译包 (Excel To Translation)")]
        private static void CreateWindow()
        {
            var wnd = GetWindow<TranslationExcelImporter>(EDL.Tr("Excel翻译包导入工具"), true);
            wnd.minSize = new Vector2(960, 540);
            wnd.Show();
        }

        [SerializeField] private string path;
        private DataSet excel;
        private string[] sheetNames;
        private int[] sheetIndices;
        private int sheetIndex;
        private TableView<DataRow> table;
        [SerializeField] private TableViewState treeViewState;
        private FileSystemWatcher watcher;

        private bool onEnable;
        private bool dirty;

        private void OnEnable()
        {
            onEnable = true;
            LoadExcel();
            onEnable = false;
        }

        private void OnGUI()
        {
            if (dirty)
            {
                if (EditorUtility.DisplayDialog(EDL.Tr("发生修改"), EDL.Tr("检测到Excel文件发生改动，是否刷新当前内容？"), EDL.Tr("确定"), EDL.Tr("取消")))
                    LoadExcel();
                dirty = false;
            }
            var rect = EditorGUILayout.GetControlRect();
            var tempRect = new Rect(rect.x, rect.y, 40, rect.height);
            EditorGUI.LabelField(tempRect, EDL.Tr("路径"));
            tempRect = new Rect(rect.x + 42, rect.y, rect.width - 206, rect.height);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(tempRect, path);
            EditorGUI.EndDisabledGroup();
            tempRect = new Rect(rect.x + rect.width - 162, rect.y, 80, rect.height);
            if (GUI.Button(tempRect, new GUIContent(EDL.Tr("打开"))))
            {
                var temp = EditorUtility.OpenFilePanel(EDL.Tr("选择Excel文件"), UtilityZT.GetFileDirectory(path), "xlsx,xls");
                if (!string.IsNullOrEmpty(temp)) LoadExcel(temp);
            }
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(path));
            tempRect = new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height);
            if (GUI.Button(tempRect, new GUIContent(EDL.Tr("刷新")))) LoadExcel();
            EditorGUI.EndDisabledGroup();
            if (excel != null)
            {
                rect = EditorGUILayout.GetControlRect();
                tempRect = new Rect(rect.x, rect.y, 40, rect.height);
                EditorGUI.LabelField(tempRect, EDL.Tr("工作簿"));
                tempRect = new Rect(rect.x + 42, rect.y, rect.width - 206, rect.height);
                var oldIndex = sheetIndex;
                sheetIndex = EditorGUI.IntPopup(tempRect, sheetIndex, sheetNames, sheetIndices);
                if (oldIndex != sheetIndex) RefreshTableView();
                tempRect = new Rect(rect.x + rect.width - 162, rect.y, 80, rect.height);
                if (GUI.Button(tempRect, new GUIContent(EDL.Tr("导入"))))
                {
                    var sheet = excel.Tables[sheetIndex];
                    UtilityZT.Editor.SaveFilePanel(CreateInstance<Translation>, transl =>
                    {
                        import(transl, sheet);
                    }, ping: true);
                }
                tempRect = new Rect(rect.x + rect.width - 80, rect.y, 80, rect.height);
                if (GUI.Button(tempRect, new GUIContent(EDL.Tr("全部导入"))))
                {
                    UtilityZT.Editor.SaveFolderPanel(path =>
                    {
                        var transls = new List<Translation>();
                        foreach (DataTable sheet in excel.Tables)
                        {
                            var transl = CreateInstance<Translation>();
                            import(transl, sheet);
                            AssetDatabase.CreateAsset(transl, AssetDatabase.GenerateUniqueAssetPath(path + "/new " +
                                Regex.Replace(typeof(Translation).Name, "([a-z])([A-Z])", "$1 $2").ToLower() + ".asset"));
                            transls.Add(transl);
                        }
                        foreach (var lang in transls)
                        {
                            EditorGUIUtility.PingObject(lang);
                        }
                    });
                }
                table?.OnGUI(new Rect(5, 42, position.width - 10, position.height - 47));
            }

            static void import(Translation transl, DataTable sheet)
            {
                Translation.Editor.SetName(transl, sheet.TableName);
                var column = sheet.Columns.Count;
                var items = new List<TranslationItem>();
                for (int i = 1; i < sheet.Rows.Count; i++)
                {
                    var values = new string[column - 1];
                    for (int j = 1; j < column; j++)
                    {
                        values[j - 1] = sheet.Rows[i][j].ToString();
                    }
                    items.Add(new TranslationItem(sheet.Rows[i][0].ToString(), values));
                }
                Translation.Editor.SetItems(transl, items.ToArray());
            }
        }

        private void OnDestroy()
        {
            watcher?.Dispose();
        }

        private void LoadExcel(string path = null)
        {
            path ??= this.path;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    if (path.EndsWith(".xlsx") || path.EndsWith(".xls"))
                    {
                        using var stream = UtilityZT.OpenFile(path, FileMode.Open, FileAccess.Read);
                        var reader = path.EndsWith(".xlsx") ? ExcelReaderFactory.CreateOpenXmlReader(stream) : ExcelReaderFactory.CreateBinaryReader(stream);
                        if (reader.AsDataSet() is DataSet set)
                        {
                            excel = set;
                            sheetNames = new string[excel.Tables.Count];
                            for (int i = 0; i < excel.Tables.Count; i++)
                            {
                                sheetNames[i] = excel.Tables[i].TableName;
                            }
                            sheetIndices = new int[excel.Tables.Count];
                            for (int i = 0; i < excel.Tables.Count; i++)
                            {
                                sheetIndices[i] = i;
                            }
                            this.path = path;
                            watcher?.Dispose();
                            watcher = new FileSystemWatcher(UtilityZT.GetFileDirectory(path), UtilityZT.GetFileName(path)) { NotifyFilter = NotifyFilters.LastWrite };
                            watcher.Changed += (sender, args) =>
                            {
                                dirty = true;
                            };
                            watcher.EnableRaisingEvents = true;
                        }
                        else Debug.LogWarning(EDL.Tr("读取Excel失败，可能路径不存在，或Excel处于打开状态，此时无法读取。"));
                    }
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning(EDL.Tr("读取Excel失败，可能路径不存在，或Excel处于打开状态，此时无法读取。"));
                }
            }
            else excel = null;
            RefreshTableView();
        }

        private void RefreshTableView()
        {
            if (excel == null)
            {
                table = null;
                return;
            }
            try
            {
                while (sheetIndex > excel.Tables.Count && sheetIndex > 0)
                {
                    sheetIndex--;
                }
                var columns = new string[excel.Tables[sheetIndex].Columns.Count];
                for (int i = 0; i < excel.Tables[sheetIndex].Columns.Count; i++)
                {
                    columns[i] = excel.Tables[sheetIndex].Rows[0][i].ToString();
                }
                List<DataRow> rows = new List<DataRow>();
                for (int i = 0; i < excel.Tables[sheetIndex].Rows.Count; i++)
                {
                    rows.Add(excel.Tables[sheetIndex].Rows[i]);
                }
                table = new TableView<DataRow>(treeViewState ??= new TableViewState(), rows, excel.Tables[sheetIndex].Columns.Count, 0, drawCell, sort);
            }
            catch// (System.Exception ex)
            {
                //Debug.LogException(ex);
                if (onEnable) Debug.LogError(EDL.Tr("读取失败，请检查所选Excel表的格式和数据！"));
                else if (EditorUtility.DisplayDialog(EDL.Tr("失败"), EDL.Tr("读取失败，请检查所选Excel表的格式和数据！"), EDL.Tr("编辑"), EDL.Tr("取消")))
                    EditorUtility.OpenWithDefaultApp(path);
            }

            static void drawCell(Rect rect, DataRow data, int column, int row, bool focused, bool selected)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(rect, data[column].ToString());
                EditorGUI.EndDisabledGroup();
            }
            static int sort(int column, DataRow left, DataRow right)
            {
                return left[column].ToString().CompareTo(right[column].ToString());
            }
        }
    }
}