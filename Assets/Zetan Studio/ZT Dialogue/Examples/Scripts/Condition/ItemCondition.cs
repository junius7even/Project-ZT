using UnityEngine;

namespace ZetanStudio.Examples
{
    using ConditionSystem;

    [Name("拥有道具")]
    public class ItemCondition : Condition
    {
        [field: SerializeField]
        public Item Item { get; private set; }

        public override bool IsValid => Item != null && !string.IsNullOrEmpty(Item.ID) && Item.amount > 0;

        public override bool IsMet()
        {
            return PlayerManager.player.items.Find(i => i.ID == Item.ID) is Item item && item.amount >= Item.amount;
        }
    }
}