using UnityEngine;

namespace ZetanStudio.Examples
{
    public class AutoRotation : MonoBehaviour
    {
        [SerializeField]
        private float rotateSpeed = 10;

        void Update()
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z + Time.deltaTime * rotateSpeed);
        }
    }
}
