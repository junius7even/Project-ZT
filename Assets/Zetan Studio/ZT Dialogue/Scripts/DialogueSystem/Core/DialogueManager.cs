using System.Collections.Generic;

namespace ZetanStudio.DialogueSystem
{
    using SaveSystem;

    public static class DialogueManager
    {
        private static readonly Dictionary<string, DialogueData> data = new Dictionary<string, DialogueData>();

        public static DialogueData GetOrCreateData(EntryNode entry)
        {
            if (!entry) return null;
            if (!data.TryGetValue(entry.ID, out var find))
                data.Add(entry.ID, find = new DialogueData(entry));
            else find.Refresh(entry);
            return find;
        }

        public static void RemoveData(EntryNode entry)
        {
            if (!entry) return;
            data.Remove(entry.ID);
        }

        public static void RemoveData(string entryID)
        {
            if (string.IsNullOrEmpty(entryID)) return;
            data.Remove(entryID);
        }

        [SaveMethod]
        public static void SaveData(SaveData saveData)
        {

            var dialog = new GenericData();
            saveData["dialogueData"] = dialog;
            foreach (var d in data.Values)
            {
                dialog[d.ID] = d.GenerateSaveData();
            }
        }

        [LoadMethod]
        public static void LoadData(SaveData saveData)
        {
            data.Clear();
            if (saveData.TryReadData("dialogueData", out var dialog))
                foreach (var kvp in dialog.ReadDataDict())
                {
                    data[kvp.Key] = new DialogueData(kvp.Value);
                }
        }
    }
}