using UnityEngine;

namespace DerivTycoon.City
{
    public class CityCamera : MonoBehaviour
    {
        [Header("Isometric Settings")]
        public float PanSpeed = 0.5f;
        public float ZoomSpeed = 2f;
        public float MinZoom = 4f;
        public float MaxZoom = 20f;

        [Header("Bounds")]
        public float PanLimit = 12f;

        private Camera _camera;
        private Vector3 _lastMousePos;
        private bool _isPanning;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            SetIsometricView();
        }

        private void SetIsometricView()
        {
            // 45 yaw, 45 pitch isometric. Camera placed at (-10,14,-10) to look at grid center (0,0,0)
            transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            transform.position = new Vector3(-10f, 14f, -10f);

            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = 10f;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
            }
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _lastMousePos = Input.mousePosition;
                _isPanning = true;
            }

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                _isPanning = false;

            if (_isPanning)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                _lastMousePos = Input.mousePosition;

                Vector3 move = new Vector3(-delta.x, 0, -delta.y) * PanSpeed * Time.deltaTime * 10f;
                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();
                Vector3 forward = Vector3.Cross(right, Vector3.up);

                Vector3 newPos = transform.position + right * move.x + forward * move.z;
                newPos.x = Mathf.Clamp(newPos.x, -PanLimit, PanLimit);
                newPos.z = Mathf.Clamp(newPos.z, -PanLimit, PanLimit);
                newPos.y = transform.position.y;

                transform.position = newPos;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - scroll * ZoomSpeed * 10f,
                    MinZoom, MaxZoom);
            }
        }
    }
}
