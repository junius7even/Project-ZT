using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace J7.Extension
{
    public class AnimationManager : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }

    [System.Serializable]
    public class AnimEvent
    {
        public string objectName;
        public string animTrigger;
    }
}