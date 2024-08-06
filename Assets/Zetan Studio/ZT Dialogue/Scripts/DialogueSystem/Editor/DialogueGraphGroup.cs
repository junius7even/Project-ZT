using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZetanStudio.DialogueSystem.Editor
{
    public class DialogueGraphGroup : Group
    {
        public DialogueGroup Target { get; private set; }

        private readonly Action beforeModify;

        public DialogueGraphGroup(IEnumerable<Node> nodes, DialogueGroup group, Action<DialogueGraphGroup, ContextualMenuPopulateEvent> onRightClick, Action beforeModify)
        {
            this.Q("titleContainer").style.minHeight = 31f;
            var label = this.Q<Label>("titleLabel");
            label.style.fontSize = 16f;
            if (group != null)
            {
                title = group._name;
                Target = group;
                userData = group;
                style.left = group._position.x;
                style.top = group._position.y;
                if (nodes != null) AddElements(nodes);
            }
            this.beforeModify = beforeModify;
            headerContainer.AddManipulator(new ContextualMenuManipulator(evt => onRightClick?.Invoke(this, evt)));
        }

        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            if (Target == null) return;
            beforeModify?.Invoke();
            Target._name = newName;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (Target == null) return;
            beforeModify?.Invoke();
            Target._position.x = newPos.xMin;
            Target._position.y = newPos.yMin;
        }
    }
}
