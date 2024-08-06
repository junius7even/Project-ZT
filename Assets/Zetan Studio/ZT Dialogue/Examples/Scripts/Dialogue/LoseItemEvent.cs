using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem;

    [Name("失去道具")]
    public class LoseItemEvent : DialogueEvent
    {
        [field: SerializeField]
        public Item Item { get; private set; }

        public override bool IsValid => Item != null && !string.IsNullOrEmpty(Item.ID) && Item.amount > 0;

        protected override bool Invoke()
        {
            return PlayerManager.player.LoseItem(Item.ID, Item.amount);
        }
    }
}