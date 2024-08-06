using System;

namespace ZetanStudio
{
    [Serializable]
    public class SaveData : GenericData
    {
        public SaveData(string version)
        {
            this["saveDate"] = DateTime.Now.ToString();
            this["version"] = version;
            this["sceneName"] = UtilityZT.GetActiveScene().name;
        }
    }
}