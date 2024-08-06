using System;
using UnityEngine;

namespace J7.Extension
{
    public class Interactable: MonoBehaviour
    {
        public InteractableScriptableObject so;
        private SpriteRenderer renderer;
        
        private void Start()
        {
            renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = so.normalSprite;
        }

        private void OnMouseOver()
        {
            renderer.sprite = so.hoveredSprite;
        }

        private void OnMouseExit()
        {
            renderer.sprite = so.normalSprite;
        }
    }
}