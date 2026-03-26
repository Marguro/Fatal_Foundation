using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DeactivateGameObjectOnButtonClick : MonoBehaviour
{
    [SerializeField] private GameObject targetToDeactivate;
    [SerializeField] private KeyCode deactivateKey = KeyCode.L;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleButtonClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleButtonClick);
    }

    private void Update()
    {
        if (Input.GetKeyDown(deactivateKey))
        {
            HandleButtonClick();
        }
    }

    private void HandleButtonClick()
    {
        if (targetToDeactivate == null)
        {
            Debug.LogWarning($"{nameof(DeactivateGameObjectOnButtonClick)} on {name} has no target assigned.", this);
            return;
        }

        targetToDeactivate.SetActive(false);
    }
}


