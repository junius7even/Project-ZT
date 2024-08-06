using UnityEngine;

namespace ZetanStudio.Examples
{
    using DialogueSystem.UI;
    using System.Collections.Generic;
    using UI;

    public class Player : MonoBehaviour, IPlayerNameHolder
    {
        [SerializeField]
        private Rigidbody2D rigidbd;

        [SerializeField]
        private float moveSpeed = 5f;

        private Vector3 movement;

        public int level;
        public string _name;

        public string Name => L.Tr("Common", _name);

        [field: SerializeField]
        public Gender Gender { get; set; }

        public List<Item> items = new List<Item>();

        public void SetLevel(int level)
        {
            this.level = level;
            MessageManager.Push(L.Tr("Message", "等级变为 {0}", level));
        }
        public void SetGender(int gender)
        {
            Gender = (Gender)gender;
            MessageManager.Push(L.Tr("Message", "性别变为 {0}", L.Tr("Common", gender == 0 ? "男" : "女")));
        }
        public void GetItem(string ID, int count)
        {
            var find = items.Find(i => i.ID == ID);
            if (find != null) find.amount += count;
            else items.Add(new Item() { ID = ID, amount = count });
            MessageManager.Push(L.Tr("Message", "获得了 {0} 个 {1} 道具", count, ID));
        }
        public bool LoseItem(string ID, int count)
        {
            var find = items.Find(i => i.ID == ID);
            if (find != null && find.amount >= count)
            {
                find.amount -= count;
                if (find.amount < 1) items.Remove(find);
                MessageManager.Push(L.Tr("Message", "失去了 {0} 个 {1} 道具", count, ID));
                return true;
            }
            else return false;
        }

        private void Awake()
        {
            PlayerManager.player = this;
            IPlayerNameHolder.Instance = this;
        }

        private void Update()
        {
            if (!Command.Instance.IsTyping && !WindowManager.IsWindowOpen<DialogueWindow>())
                movement = Vector3.ClampMagnitude(new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1);
            else movement = Vector3.zero;
        }

        private void FixedUpdate()
        {
            rigidbd.MovePosition(rigidbd.transform.position + moveSpeed * Time.deltaTime * movement);
        }
    }

    [System.Serializable]
    public class Item
    {
        public string ID;
        public int amount;
    }
}
