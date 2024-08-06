using UnityEngine;

namespace ZetanStudio.Examples
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset;
        public float speed = 1;

        private Camera m_Camera;
        private float cameraMovingTime;

        private void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }

        void Update()
        {
            if (target)
            {
                cameraMovingTime += Time.deltaTime * speed;
                var dest = target.position - (Vector3)offset;
                dest = new Vector3(dest.x, dest.y, m_Camera.transform.position.z);
                if (dest == m_Camera.transform.position) cameraMovingTime = 0;
                else m_Camera.transform.position = Vector3.Lerp(m_Camera.transform.position, dest, cameraMovingTime);
            }

        }
    }
}
