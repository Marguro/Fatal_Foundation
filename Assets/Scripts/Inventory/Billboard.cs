using UnityEngine;

namespace Inventory
{
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private bool lockY = false;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            if (lockY)
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
                Vector3 euler = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0, euler.y, 0);
            }
            else
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}

