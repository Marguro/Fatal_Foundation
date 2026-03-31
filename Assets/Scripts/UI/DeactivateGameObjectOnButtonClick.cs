using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class DeactivateGameObjectOnButtonClick : MonoBehaviour
{
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var activeSceneName = SceneManager.GetActiveScene().name;
            NetworkManager.Singleton.SceneManager.LoadScene(activeSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Only the Host/Server can reset the game.");
        }
    }
}
