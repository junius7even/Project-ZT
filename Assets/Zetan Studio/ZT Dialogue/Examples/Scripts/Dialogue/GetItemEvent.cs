using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;

    [Name("获得道具")]
    public class GetItemEvent : DialogueEvent
    {
        [field: SerializeField]
        public Item Item { get; private set; }

        public override bool IsValid => Item != null && !string.IsNullOrEmpty(Item.ID) && Item.amount > 0;

        protected override bool Invoke()
        {
            PlayerManager.player.GetItem(Item.ID, Item.amount);
            return true;
        }
    }
}