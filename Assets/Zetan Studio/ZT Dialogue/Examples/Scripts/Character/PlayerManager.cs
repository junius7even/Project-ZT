using UnityEngine;

namespace ZetanStudio.Examples
{
    using SaveSystem;

    public static class PlayerManager
    {
        public const string playerLevelChanged = "PlayerLevelChanged";
        public const string playerGenderChanged = "PlayerGenderChanged";

        public static Player player;

        [SaveMethod]
        public static void SaveData(SaveData saveData)
        {
            var data = saveData.Write("player", new GenericData());
            data["name"] = player._name;
            data["level"] = player.level;
            data["gender"] = (int)player.Gender;
            data["posX"] = player.transform.position.x;
            data["posY"] = player.transform.position.y;
            data["posZ"] = player.transform.position.z;
            if (player.items.Count > 0)
            {
                var items = data.Write("items", new GenericData());
                foreach (var item in player.items)
                {
                    items[item.ID] = item.amount;
                }
            }
        }

        [LoadMethod]
        public static void LoadData(SaveData saveData)
        {
            if (saveData.TryReadData("player", out var data))
            {
                if (player)
                {
                    player._name = (string)data["name"];
                    player.level = (int)data["level"];
                    player.Gender = (Gender)(int)data["gender"];
                    player.transform.position = new Vector3(data.ReadFloat("posX"), data.ReadFloat("posY"), data.ReadFloat("posZ"));
                    Camera.main.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, Camera.main.transform.position.z);
                    if(data.TryReadData("items", out var items))
                    {
                        player.items.Clear();
                        foreach (var kvp in items.ReadIntDict())
                        {
                            player.items.Add(new Item() { ID = kvp.Key, amount = kvp.Value });
                        }
                    }    
                }
            }
        }
    }
}