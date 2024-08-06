using UnityEngine;

namespace ZetanStudio.Examples
{
    public class Character : MonoBehaviour
    {
        [field: SerializeField]
        public string ID { get; protected set; }

        [field: SerializeField]
        public virtual string Name { get; protected set; }
    }
}