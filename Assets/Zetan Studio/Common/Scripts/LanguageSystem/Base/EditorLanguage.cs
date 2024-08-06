namespace ZetanStudio.Editor
{
    public sealed class EditorLanguage
    {
#if UNITY_EDITOR
        static EditorLanguage()
        {
            settings = EditorMiscSettings.GetOrCreate();
            UnityEditor.EditorApplication.projectChanged += () =>
            {
                if (!UnityEditor.EditorApplication.isCompiling && !settings) settings = EditorMiscSettings.GetOrCreate();
            };
        }
#endif

        private static EditorMiscSettings settings;

        public static string Tr(string text) => L.Tr(settings ? settings.Translation : null, text);

        public static string Tr(string text, params object[] args) => L.Tr(settings ? settings.Translation : null, text, args);
    }

    public sealed class EDL
    {
        public static string Tr(string text) => EditorLanguage.Tr(text);

        public static string Tr(string text, params object[] args) => EditorLanguage.Tr(text, args);
    }
}