using System;
using TMPro;
using UnityEngine;

namespace J7.Extension
{
    public class Interactable: MonoBehaviour
    {
        public InteractableScriptableObject so;
        private SpriteRenderer renderer;
        private bool pressed;
        public TextMeshPro nameTag;
        public Transform tagObject;
        private void Start()
        {
            renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = so.normalSprite;
            nameTag.text = so.interactableName;
            tagObject.localPosition = new Vector3(so.horizontalOffset, so.verticalOffset, 0);
        }

        private void OnMouseOver()
        {
            if (!pressed)
                renderer.sprite = so.hoveredSprite;
        }

        private void OnMouseExit()
        {
            if (!pressed)
                renderer.sprite = so.normalSprite;
        }

        private void OnMouseDown()
        {
            if (!pressed)
            {
                renderer.sprite = so.pressedSprite;
                pressed = true;
            }
        }
    }
}