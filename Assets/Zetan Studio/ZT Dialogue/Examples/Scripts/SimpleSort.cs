using System.Collections.Generic;
using UnityEngine;

namespace ZetanStudio.Examples
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSort : MonoBehaviour
    {
        private readonly static HashSet<SimpleSort> instances = new HashSet<SimpleSort>();

        private SpriteRenderer spriteRenderer;

        private void OnEnable()
        {
            instances.Add(this);
        }

        private void OnDisable()
        {
            instances.Remove(this);
        }


        void Update()
        {
            try
            {
                if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
                var max = transform.position.y;
                var min = transform.position.y;
                foreach (var instance in instances)
                {
                    if (instance.spriteRenderer.sortingLayerID == spriteRenderer.sortingLayerID)
                    {
                        if (instance.transform.position.y < min)
                            min = instance.transform.position.y;
                        if (instance.transform.position.y > max)
                            max = instance.transform.position.y;
                    }
                }
                spriteRenderer.sortingOrder = Mathf.CeilToInt(max - transform.position.y);
            }
            catch { }
        }
    }
}
