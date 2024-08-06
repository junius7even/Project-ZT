using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ZetanStudio.Examples
{
    [RequireComponent(typeof(InputField))]
    public class Command : SingletonMonoBehaviour<Command>
    {
        private InputField input;

        public bool IsTyping => input.isFocused;

        private void Awake()
        {
            input = GetComponent<InputField>();
            input.onSubmit.AddListener(Handle);
        }

        public void Send()
        {
            Handle(input.text);
        }

        public void Handle(string command)
        {
            if (Regex.Match(command, @"^level +(\d+)$", RegexOptions.IgnoreCase) is Match match && match.Success)
                PlayerManager.player.SetLevel(int.Parse(match.Groups[1].Value));
            else if (Regex.Match(command, @"^get +(\w+) +(\d+)$", RegexOptions.IgnoreCase) is Match match2 && match2.Success)
                PlayerManager.player.GetItem(match2.Groups[1].Value, int.Parse(match2.Groups[2].Value));
            else if (Regex.Match(command, @"^lose +(\w+) +(\d+)$", RegexOptions.IgnoreCase) is Match match3 && match3.Success)
                PlayerManager.player.LoseItem(match3.Groups[1].Value, int.Parse(match3.Groups[2].Value));
            else if (Regex.Match(command, @"^gender +(\d+)$", RegexOptions.IgnoreCase) is Match match4 && match4.Success)
                PlayerManager.player.SetGender(int.Parse(match4.Groups[1].Value));
            else MessageManager.Push(L.Tr("Message", "指令不存在或格式不正确"));
        }
    }
}
