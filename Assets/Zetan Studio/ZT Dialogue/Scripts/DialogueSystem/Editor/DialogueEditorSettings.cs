using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.DialogueSystem.Editor
{
    using ZetanStudio.Editor;

    public class DialogueEditorSettings : ScriptableObject
    {
        [Label("编辑器UXML")]
        public VisualTreeAsset editorUxml;
        [Label("编辑器USS")]
        public StyleSheet editorUss;
        [Label("编辑器最小尺寸")]
        public Vector2 minWindowSize = new Vector2(1280, 600);
        [Label("结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateNode;
        [Label("拦截结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateBlock;
        [Label("条件结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateCondition;
        [Label("修饰结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateDecorator;
        [Label("外置选项结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateExternalOptions;
        [Label("语句结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateSentence;
        [Label("后缀结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateSuffix;
        [Label("分叉结点脚本模板"), ObjectSelector(typeof(TextAsset), extension: "cs.txt")]
        public TextAsset scriptTemplateBifurcation;

        private static DialogueEditorSettings Find()
        {
            var settings = UtilityZT.Editor.LoadAssets<DialogueEditorSettings>();
            if (settings.Count > 1) Debug.LogWarning(EDL.Tr("找到多个 {0}，将使用第一个", typeof(DialogueEditorSettings).Name));
            if (settings.Count > 0) return settings[0];
            return null;
        }

        public static DialogueEditorSettings GetOrCreate()
        {
            if (Find() is not DialogueEditorSettings settings)
            {
                settings = CreateInstance<DialogueEditorSettings>();
                AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath($"Assets/{ObjectNames.NicifyVariableName(typeof(DialogueEditorSettings).Name)}.asset"));
            }
            return settings;
        }

        public static VisualTreeAsset CreateUXML()
        {
            var folder = "Zetan Studio/ZT Dialogue/Scripts/DialogueSystem/Editor/Resources";
            var fullFolder = Application.dataPath + "/" + folder;
            if (!Directory.Exists(fullFolder))
            {
                Directory.CreateDirectory(fullFolder);
                AssetDatabase.Refresh();
            }
            using var uxml = UtilityZT.OpenFile(fullFolder + "/DialogueEditor.uxml", FileMode.Create);
            if (uxml != null)
            {
                using StreamWriter sw = new StreamWriter(uxml);
                sw.Write(@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"" xmlns:uie=""UnityEditor.UIElements"" xsi=""http://www.w3.org/2001/XMLSchema-instance"" engine=""UnityEngine.UIElements"" editor=""UnityEditor.UIElements"" noNamespaceSchemaLocation=""../../../../UIElementsSchema/UIElements.xsd"" editor-extension-mode=""True"">
                               <uie:Toolbar>
                                   <uie:ToolbarButton text=""新建"" display-tooltip-when-elided=""true"" name=""create"" style=""width: 50px; -unity-text-align: upper-center;"" />
                                   <uie:ToolbarButton text=""删除"" display-tooltip-when-elided=""true"" name=""delete"" style=""-unity-text-align: upper-center; width: 50px;"" />
                                   <ui:Label text=""导航图"" display-tooltip-when-elided=""true"" name=""minimap-label"" style=""flex-direction: row; -unity-text-align: middle-center; padding-top: 0; padding-bottom: 2px; width: 60px;"" />
                                   <ui:Toggle name=""minimap-toggle"" style=""width: 17px; transform-origin: center; margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0; border-right-width: 1px; border-left-color: rgba(0, 0, 0, 0.5); border-right-color: rgba(0, 0, 0, 0.5); border-top-color: rgba(0, 0, 0, 0.5); border-bottom-color: rgba(0, 0, 0, 0.5); padding-right: 0;"" />
                                   <ui:DropdownField index=""-1"" choices=""System.Collections.Generic.List`1[System.String]"" name=""search-type"" style=""width: 65px;"" />
                                   <uie:ToolbarSearchField focusable=""true"" />
                                   <ui:Button text=""刷新视图"" display-tooltip-when-elided=""true"" name=""refresh-graph"" />
                               </uie:Toolbar>
                               <ZetanStudio.SplitView fixed-pane-initial-dimension=""250"" style=""min-width: auto;"">
                                   <ui:VisualElement name=""left-container"" style=""min-width: 200px; min-height: auto;"">
                                       <ZetanStudio.SplitView orientation=""Vertical"" fixed-pane-initial-dimension=""350"">
                                           <ui:VisualElement style=""min-height: 150px;"">
                                               <ui:Label text=""对话列表"" display-tooltip-when-elided=""true"" style=""-unity-text-align: middle-center; background-color: rgba(0, 0, 0, 0.25); border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px; border-left-color: rgb(89, 89, 89); border-right-color: rgb(89, 89, 89); border-top-color: rgb(89, 89, 89); border-bottom-color: rgb(89, 89, 89); border-top-left-radius: 3px; border-bottom-left-radius: 3px; border-top-right-radius: 3px; border-bottom-right-radius: 3px;"" />
                                               <ui:ListView focusable=""true"" name=""dialogue-list"" fixed-item-height=""20"" selection-type=""Multiple"" show-border=""true"" virtualization-method=""DynamicHeight"" show-alternating-row-backgrounds=""ContentOnly"" style=""margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0; padding-left: 3px; padding-right: 3px; padding-top: 3px; padding-bottom: 3px;"" />
                                           </ui:VisualElement>
                                           <ui:VisualElement style=""min-height: 150px;"">
                                               <ui:Label text=""检查器"" display-tooltip-when-elided=""true"" style=""-unity-text-align: middle-center; background-color: rgba(0, 0, 0, 0.25); border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px; border-left-color: rgb(89, 89, 89); border-right-color: rgb(89, 89, 89); border-top-color: rgb(89, 89, 89); border-bottom-color: rgb(89, 89, 89); border-top-left-radius: 3px; border-bottom-left-radius: 3px; border-top-right-radius: 3px; border-bottom-right-radius: 3px;"" />
                                               <ui:ScrollView style=""padding-left: 0; padding-right: 0; padding-top: 0; padding-bottom: 0; border-top-left-radius: 0; border-bottom-left-radius: 0; border-top-right-radius: 0; border-bottom-right-radius: 0; margin-left: 0; margin-right: 0; margin-top: 0; margin-bottom: 0;"">
                                                   <ui:IMGUIContainer name=""inspector"" style=""margin-left: 3px; margin-right: 3px;"" />
                                               </ui:ScrollView>
                                           </ui:VisualElement>
                                       </ZetanStudio.SplitView>
                                   </ui:VisualElement>
                                   <ui:VisualElement name=""right-container"" style=""min-width: 400px; margin-right: 0; margin-left: 0; margin-top: 0; margin-bottom: 0; padding-left: 3px; padding-right: 3px;"" />
                               </ZetanStudio.SplitView>
                               <ui:VisualElement name=""events-view"" style=""position: absolute; left: 0; top: 0; right: 0; bottom: 0; background-color: rgba(0, 0, 0, 0.25); align-items: center; justify-content: center; display: none;"">
                                   <ui:VisualElement style=""width: 300px; height: 300px; background-color: rgb(56, 56, 56); border-top-left-radius: 5px; border-top-right-radius: 5px; border-bottom-left-radius: 5px; border-bottom-right-radius: 5px; border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px; border-left-color: rgb(89, 89, 89); border-right-color: rgb(89, 89, 89); border-top-color: rgb(89, 89, 89); border-bottom-color: rgb(89, 89, 89);"">
                                       <ui:Label text=""对话事件"" display-tooltip-when-elided=""true"" style=""-unity-text-align: middle-center; background-color: rgb(40, 40, 40); border-top-left-radius: 5px; border-top-right-radius: 5px; height: 20px;"" />
                                       <ui:ScrollView style=""flex-grow: 1; padding-left: 3px; padding-right: 3px; padding-top: 3px; padding-bottom: 3px;"">
                                           <ui:IMGUIContainer name=""events-inspector"" style=""padding-left: 0; padding-right: 0; padding-top: 0; padding-bottom: 0;"" />
                                       </ui:ScrollView>
                                       <ui:Button text=""确定"" display-tooltip-when-elided=""true"" name=""events-button"" />
                                   </ui:VisualElement>
                               </ui:VisualElement>
                               <ui:ListView focusable=""true"" name=""search-list"" virtualization-method=""DynamicHeight"" show-alternating-row-backgrounds=""ContentOnly"" style=""max-height: 300px; width: 295px; position: absolute; background-color: rgb(56, 56, 56); border-left-color: rgba(0, 0, 0, 0.5); border-right-color: rgba(0, 0, 0, 0.5); border-top-color: rgba(0, 0, 0, 0.5); border-bottom-color: rgba(0, 0, 0, 0.5); border-left-width: 1px; border-right-width: 1px; border-top-width: 1px; border-bottom-width: 1px; border-top-left-radius: 5px; border-bottom-left-radius: 5px; border-top-right-radius: 5px; border-bottom-right-radius: 5px; top: 17px; left: 252px; padding-left: 2px; padding-right: 2px; padding-top: 2px; padding-bottom: 2px;"" />
                           </ui:UXML>");
                sw.Close(); uxml.Close();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folder + "/DialogueEditor");
            }
            return null;
        }
        public static StyleSheet CreateUSS()
        {
            var folder = "Zetan Studio/ZT Dialogue/Scripts/DialogueSystem/Editor/Resources";
            var fullFolder = Application.dataPath + "/" + folder;
            if (!Directory.Exists(fullFolder))
            {
                Directory.CreateDirectory(fullFolder);
                AssetDatabase.Refresh();
            }
            using var uss = UtilityZT.OpenFile(fullFolder + "/DialogueEditor.uss", FileMode.Create);
            if (uss != null)
            {
                using StreamWriter sw = new StreamWriter(uss);
                sw.Write(@"GridBackground {
                               --grid-background-color: rgb(40, 40, 40);
                               --line-color: rgba(193, 196, 192, 0.1);
                               --thick-line-color: rgba(193, 196, 192, 0.1);
                               --spacing: 15;
                           }");
                sw.Close(); uss.Close();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<StyleSheet>(folder + "/DialogueEditor");
            }
            return null;
        }

        [SettingsProvider]
        protected static SettingsProvider CreateSettingsProvider()
        {
            var settings = GetOrCreate();
            var provider = new SettingsProvider("Project/Zetan Studio/ZSDESettingsUIElementsSettings", SettingsScope.Project)
            {
                label = EDL.Tr("对话编辑器"),
                activateHandler = (searchContext, rootElement) =>
                {
                    SerializedObject serializedObject = new SerializedObject(GetOrCreate());

                    Label title = new Label() { text = EDL.Tr("对话编辑器设置") };
                    title.style.paddingLeft = 10f;
                    title.style.fontSize = 19f;
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Column }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new InspectorElement(serializedObject));

                    rootElement.Bind(serializedObject);
                },
            };

            return provider;
        }
    }
}