using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ZetanStudio.LanguageSystem.Editor
{
    using Extension.Editor;
    using ZetanStudio.Editor;

    public class TranslationEditor : EditorWindow
    {
        [MenuItem("Window/Zetan Studio/翻译包编辑器 (Translation Editor)")]
        private static void CreateWindow()
        {
            var wnd = CreateWindow<TranslationEditor>(EDL.Tr("翻译包编辑器"));
            wnd.minSize = new Vector2(960, 540);
            wnd.Show();
        }

        private static void CreateWindow(Translation tranlsation)
        {
            if (tranlsation && openedWindows.FirstOrDefault(x => x.translation == tranlsation) is TranslationEditor find)
                find.Focus();
            else
            {
                var wnd = CreateWindow<TranslationEditor>(EDL.Tr("翻译包编辑器"));
                wnd.minSize = new Vector2(960, 540);
                wnd.translation = tranlsation;
                wnd.RefreshSerializedObject();
                wnd.Show();
            }
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is Translation trans)
            {
                CreateWindow(trans);
                return true;
            }
            return false;
        }

        [SerializeField] private Translation translation;
        private SerializedObject serializedObject;
        private SerializedProperty serializedItems;

        [SerializeField] private TableViewState viewState;
        private TableView<int> table;
        private int row;
        private int langCount;
        [SerializeField] private bool ascending = true;

        private Action delayCall;
        private bool doDelayCall;

        private readonly static HashSet<TranslationEditor> openedWindows = new HashSet<TranslationEditor>();

        public TranslationEditor()
        {
            openedWindows.Add(this);
        }

        #region Unity 回调 Unity Callbacks
        //private void OnSelectionChange()
        //{
        //    if (Selection.objects.Length == 1 && Selection.activeObject is LanguageSet language)
        //    {
        //        this.language = language;
        //        RefreshSerializedObject();
        //        Repaint();
        //    }
        //}
        private void OnDestroy()
        {
            openedWindows.Remove(this);
        }

        private void OnEnable()
        {
            RefreshSerializedObject();
            Undo.undoRedoPerformed -= Repaint;
            Undo.undoRedoPerformed += Repaint;
            EditorApplication.projectChanged += RefreshSerializedObject;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            EditorApplication.projectChanged -= RefreshSerializedObject;
        }

        private void OnGUI()
        {
            serializedObject?.UpdateIfRequiredOrScript();
            var oldLang = translation;
            translation = EditorGUILayout.ObjectField(EDL.Tr("翻译包"), translation, typeof(Translation), false) as Translation;
            if (oldLang != translation)
            {
                if (translation && openedWindows.FirstOrDefault(x => x != this && x.translation == translation) is TranslationEditor find)
                {
                    Debug.Log(EDL.Tr("翻译包 {0} 已在其它窗口打开", translation.name));
                    translation = oldLang;
                }
                RefreshSerializedObject();
            }

            if (serializedObject != null)
            {
                var maxLangCount = translation.Items.Count > 0 ? translation.Items.Max(x => x.Values.Count) : 0;
                if (langCount != maxLangCount)
                {
                    langCount = maxLangCount;
                    RefreshTable();
                }
                else if (serializedItems != null && row != serializedItems.arraySize) RefreshTable();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_name"), new GUIContent(EDL.Tr("翻译包名称")));
                table?.OnGUI(new Rect(5, 44, position.width - 10, position.height - 49));
                if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            }

            if (doDelayCall)
            {
                delayCall?.Invoke();
                delayCall = null;
                doDelayCall = false;
            }
        }
        #endregion

        private void RefreshSerializedObject()
        {
            if (translation)
            {
                serializedObject = new SerializedObject(translation);
                serializedItems = serializedObject.FindProperty("items");
                row = serializedItems.arraySize;
                langCount = translation.Items.Count > 0 ? translation.Items.Max(x => x.Values.Count) : 0;
            }
            else
            {
                serializedObject?.Dispose();
                serializedObject = null;
                serializedItems = null;
            }
            RefreshTable();
        }
        private void RefreshTable()
        {
            if (serializedObject != null && serializedItems != null)
            {
                row = serializedItems.arraySize;

                var indices = new List<int>();
                for (int i = 0; i < serializedItems.arraySize; i++)
                {
                    indices.Add(i);
                }
                TableColumn[] columns = new TableColumn[langCount + 1];
                columns[0] = new TableColumn()
                {
                    headerContent = new GUIContent(EDL.Tr("索引")),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 80,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = true
                };
                for (int i = 1; i < columns.Length; i++)
                {
                    string lang = EDL.Tr("语言{0}", i);
                    columns[i] = new TableColumn()
                    {
                        headerContent = new GUIContent(lang),
                        headerTextAlignment = TextAlignment.Center,
                        sortedAscending = true,
                        sortingArrowAlignment = TextAlignment.Left,
                        width = 100,
                        minWidth = 60,
                        autoResize = true,
                        allowToggleVisibility = true,
                        canSort = false
                    };
                }
                viewState ??= new TableViewState();
                viewState.searchString = null;
                table = new TableView<int>(viewState, indices, columns, DrawRow)
                {
                    searchCallback = Search,
                    insertClicked = InsertRow,
                    deleteClicked = DeleteRows,
                    appendClicked = AppendRow,
                    replaceClicked = Replace,
                    deleteColumnClicked = DeleteLanguage,
                    addColumnClicked = AddLanguage,
                    checkErrorsCallback = CheckErrors,
                    dropRowsCallback = DropRows,
                    draggable = true,
                    displayFooter = true,
                    minColumnCanDelete = 1,
                };
                table.multiColumnHeader.ResizeToFit();
                table.multiColumnHeader.SetSorting(0, ascending);
                table.multiColumnHeader.sortingChanged += h => ascending = h.IsSortedAscending(0);
                doDelayCall = true;
            }
            else table = null;
        }
        private SerializedProperty GetValuesProperty(int i)
        {
            return serializedItems.GetArrayElementAtIndex(i).FindPropertyRelative("values");
        }

        private void DrawRow(Rect rect, int index, int column, int row, bool focused, bool selected)
        {
            SerializedProperty item = serializedItems.GetArrayElementAtIndex(index);
            if (column == 0)
            {
                var key = item.FindAutoProperty("Key");
                key.stringValue = EditorGUI.TextField(rect, key.stringValue);
            }
            else
            {
                var values = item.FindPropertyRelative("values");
                if (column > values.arraySize)
                {
                    if (GUI.Button(rect, EDL.Tr("插入内容")))
                        values.arraySize++;
                }
                else
                {
                    var value = values.GetArrayElementAtIndex(column - 1);
                    value.stringValue = EditorGUI.TextField(rect, value.stringValue);
                }
            }
        }
        private int CheckErrors(int column, out string error)
        {
            error = null;
            if (column != 0) return -1;
            int keyEmptyRow = -1;
            int keyDuplicateRow = -1;
            var items = translation.Items;
            string duplicatedKey = null;
            var exits = new HashSet<string>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(item.Key))
                {
                    keyEmptyRow = i;
                    break;
                }
                else if (!exits.Contains(item.Key)) exits.Add(item.Key);
                else
                {
                    duplicatedKey = item.Key;
                    keyDuplicateRow = i;
                    break;
                }
            }
            int errorRow;
            if (keyEmptyRow >= 0)
            {
                error = EDL.Tr("存在空的键");
                errorRow = keyEmptyRow;
            }
            else if (duplicatedKey != null)
            {
                error = EDL.Tr("存在相同的键");
                errorRow = keyDuplicateRow;
            }
            else errorRow = -1;

            return errorRow;
        }
        private bool DropRows(int[] indices, int insert, out int[] newIndices)
        {
            bool result = serializedItems.MoveArrayElements(indices, insert - 1, out newIndices);
            if (result) serializedObject.ApplyModifiedProperties();
            return result;
        }

        #region 表格按钮点击回调 Table Button Callbacks
        private void InsertRow(int index)
        {
            serializedItems.InsertArrayElementAtIndex(index < 0 ? 0 : index);
            serializedObject.ApplyModifiedProperties();
            delayCall += () => table.SetSelected(index + 1);
            doDelayCall = false;
        }
        private void DeleteRows(IList<int> indices)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int delete = indices[i];
                if (delete >= 0 && delete < serializedItems.arraySize)
                {
                    serializedItems.DeleteArrayElementAtIndex(delete);
                    for (int j = i + 1; j < indices.Count; j++)
                    {
                        if (indices[j] > delete) indices[j] = indices[j] - 1;
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
        private void AppendRow()
        {
            serializedItems.arraySize++;
            serializedObject.ApplyModifiedProperties();
            delayCall += () => table.SetSelected(serializedItems.arraySize - 1);
            doDelayCall = false;
        }

        private bool Search(string keywords, int index, int column)
        {
            if (serializedItems != null)
            {
                var row = serializedItems.GetArrayElementAtIndex(index);
                if (column == 0) return match(row.FindAutoProperty("Key").stringValue);
                else if (column != -1)
                {
                    var values = row.FindPropertyRelative("values");
                    if (column - 1 < values.arraySize)
                    {
                        var value = values.GetArrayElementAtIndex(column - 1);
                        return match(value.stringValue);
                    }
                }
                else
                {
                    if (match(row.FindAutoProperty("Key").stringValue))
                        return true;
                    var values = row.FindPropertyRelative("values");
                    for (int i = 0; i < values.arraySize; i++)
                    {
                        var value = values.GetArrayElementAtIndex(i);
                        if (match(value.stringValue))
                            return true;
                    }
                }

                bool match(string stringValue)
                {
                    if (!table.wholeMatching)
                        if (table.ignoreCase) return stringValue.IndexOf(keywords, StringComparison.OrdinalIgnoreCase) >= 0;
                        else return stringValue.Contains(keywords);
                    else
                        return stringValue.Equals(keywords, table.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture);
                }
            }
            return false;
        }
        private int Replace(string replaceString, int[] data, bool all)
        {
            if (!all)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var row = serializedItems.GetArrayElementAtIndex(data[i]);
                    if (replace(row, false))
                    {
                        serializedObject.ApplyModifiedProperties();
                        return data[i];
                    }
                }
            }
            else
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var row = serializedItems.GetArrayElementAtIndex(data[i]);
                    replace(row, true);
                }
                serializedObject.ApplyModifiedProperties();
            }
            return -1;

            bool replace(SerializedProperty row, bool all)
            {
                var key = row.FindAutoProperty("Key");
                var values = row.FindPropertyRelative("values");
                if (table.columnToSearch == -1)
                {
                    if (key.stringValue.IndexOf(table.searchString, table.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture) >= 0)
                    {
                        key.stringValue = new Regex(table.searchString, table.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Replace(key.stringValue, replaceString, all ? -1 : 1);
                        if (!all) return true;
                    }
                    for (int j = 0; j < values.arraySize; j++)
                    {
                        var value = values.GetArrayElementAtIndex(j);
                        if (value.stringValue.IndexOf(table.searchString, table.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture) >= 0)
                        {
                            value.stringValue = new Regex(table.searchString, table.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Replace(value.stringValue, replaceString, all ? -1 : 1);
                            if (!all) return true;
                        }
                    }
                }
                else if (table.columnToSearch == 0)
                {
                    if (key.stringValue.IndexOf(table.searchString, table.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture) >= 0)
                    {
                        key.stringValue = new Regex(table.searchString, table.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Replace(key.stringValue, replaceString, all ? -1 : 1);
                        if (!all) return true;
                    }
                }
                else if (values.arraySize >= table.columnToSearch)
                {
                    var value = values.GetArrayElementAtIndex(table.columnToSearch - 1);
                    if (value.stringValue.IndexOf(table.searchString, table.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.CurrentCulture) >= 0)
                    {
                        value.stringValue = new Regex(table.searchString, table.ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None).Replace(value.stringValue, replaceString, all ? -1 : 1);
                        if (!all) return true;
                    }
                }
                return false;
            }
        }

        private void DeleteLanguage(int columnToDelete)
        {
            if (EditorUtility.DisplayDialog(EDL.Tr("删除"), EDL.Tr("确定删除第 {0} 个语言吗？", columnToDelete), EDL.Tr("确定"), EDL.Tr("取消")))
            {
                for (int i = 0; i < serializedItems.arraySize; i++)
                {
                    var values = GetValuesProperty(i);
                    if (columnToDelete - 1 < values.arraySize)
                    {
                        values.DeleteArrayElementAtIndex(columnToDelete - 1);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
        private void AddLanguage()
        {
            langCount++;
            for (int i = 0; i < serializedItems.arraySize; i++)
            {
                var values = GetValuesProperty(i);
                while (values.arraySize < langCount)
                {
                    values.arraySize++;
                }
            }
            serializedObject.ApplyModifiedProperties();
            table.SetDirty();
            RefreshTable();
        }
        #endregion
    }
}