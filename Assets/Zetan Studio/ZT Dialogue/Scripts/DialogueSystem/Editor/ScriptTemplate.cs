using UnityEngine;

namespace ZetanStudio.DialogueSystem.Editor
{
    public struct ScriptTemplate
    {
        public string fileName;
        public TextAsset templateFile;

        public static ScriptTemplate Node => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateNode, fileName = "MyNode.cs"};
        public static ScriptTemplate Bifurcation => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateBifurcation, fileName = "MyBifurcation.cs" };
        public static ScriptTemplate Block => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateBlock, fileName = "MyBlock.cs" };
        public static ScriptTemplate Condition => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateCondition, fileName = "MyCondition.cs" };
        public static ScriptTemplate Decorator => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateDecorator, fileName = "MyDecorator.cs"};
        public static ScriptTemplate ExternalOptions => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateExternalOptions, fileName = "MyExternalOptions.cs" };
        public static ScriptTemplate Sentence => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateSentence, fileName = "MySentence.cs" };
        public static ScriptTemplate Suffix => new ScriptTemplate { templateFile = DialogueEditorSettings.GetOrCreate().scriptTemplateSuffix, fileName = "MySuffix.cs" };
    }
}
