#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.Editor
{
    public class EditorMiscSettings : ScriptableObject
    {
        [SerializeField]
        private Translation translation;
        public Translation Translation => translation;

        [field: SerializeField]
        public Translation labelTranslation;
        public Translation LabelTranslation => labelTranslation;

#if UNITY_EDITOR
        private static EditorMiscSettings Find()
        {
            var settings = UtilityZT.Editor.LoadAssets<EditorMiscSettings>();
            if (settings.Count > 1) Debug.LogWarning(EDL.Tr("找到多个 {0}，将使用第一个", typeof(EditorMiscSettings).Name));
            if (settings.Count > 0) return settings[0];
            return null;
        }
        public static EditorMiscSettings GetOrCreate()
        {
            if (Find() is not EditorMiscSettings settings)
            {
                settings = CreateInstance<EditorMiscSettings>();
                AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath($"Assets/{ObjectNames.NicifyVariableName(typeof(EditorMiscSettings).Name)}.asset"));
            }
            return settings;
        }

        [SettingsProvider]
        protected static SettingsProvider CreateSettingsProvider()
        {
            var settings = GetOrCreate();
            var provider = new SettingsProvider("Project/Zetan Studio/ZSESettingsUIElementsSettings", SettingsScope.Project)
            {
                label = EDL.Tr("编辑器"),
                activateHandler = (searchContext, rootElement) =>
                {
                    SerializedObject serializedObject = new SerializedObject(GetOrCreate());

                    Label title = new Label() { text = EDL.Tr("编辑器设置") };
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
#endif
    }
}