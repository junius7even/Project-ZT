using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.Examples
{
    [DisallowMultipleComponent]
    public class MessageItem : MonoBehaviour
    {
        [Label("消息文字")]
        public Text messageText;
    }
}