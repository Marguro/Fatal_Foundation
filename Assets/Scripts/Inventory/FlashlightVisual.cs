using UnityEngine;

namespace Inventory
{
    public class FlashlightVisual : MonoBehaviour
    {
        [SerializeField] private Light[] lightsToToggle;
        [SerializeField] private GameObject[] toggleObjects;

        private void Awake()
        {
            if (lightsToToggle == null || lightsToToggle.Length == 0)
            {
                lightsToToggle = GetComponentsInChildren<Light>(true);
            }
        }

        public void SetOn(bool isOn)
        {
            if (lightsToToggle != null)
            {
                foreach (var lightComp in lightsToToggle)
                {
                    if (lightComp != null)
                        lightComp.enabled = isOn;
                }
            }

            if (toggleObjects != null)
            {
                foreach (var obj in toggleObjects)
                {
                    if (obj != null)
                        obj.SetActive(isOn);
                }
            }
        }
    }
}



