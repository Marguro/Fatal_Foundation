using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem.UI;
#endif

namespace Multiplayer
{
    /// <summary>
    /// Temporary multiplayer test UI for starting host/client.
    /// Client must enter host IP before starting.
    /// </summary>
    public class MultiplayerCenterTestUI : MonoBehaviour
    {
        [FormerlySerializedAs("m_StartHostButton")]
        [SerializeField] private Button mStartHostButton;
        [FormerlySerializedAs("m_StartClientButton")]
        [SerializeField] private Button mStartClientButton;
        [FormerlySerializedAs("m_HostIpInputField")]
        [SerializeField] private TMP_InputField mHostIpInputField;
        [FormerlySerializedAs("m_DefaultHostAddress")]
        [SerializeField] private string mDefaultHostAddress = "127.0.0.1";
        [SerializeField] private string mHostListenAddress = "0.0.0.0";

        private void Awake()
        {
            EnsureEventSystemExists();
            EnsureHostIpInputFieldExists();
        }

        private void Start()
        {
            if (mStartHostButton == null || mStartClientButton == null)
            {
                Debug.LogError("[MultiplayerCenterTestUI] Start buttons are not assigned.");
                return;
            }

            if (mHostIpInputField != null && string.IsNullOrWhiteSpace(mHostIpInputField.text))
            {
                mHostIpInputField.text = mDefaultHostAddress;
            }

            mStartHostButton.onClick.AddListener(StartHost);
            mStartClientButton.onClick.AddListener(StartClient);
        }

        private void OnDestroy()
        {
            if (mStartHostButton != null)
            {
                mStartHostButton.onClick.RemoveListener(StartHost);
            }

            if (mStartClientButton != null)
            {
                mStartClientButton.onClick.RemoveListener(StartClient);
            }
        }

        private void StartClient()
        {
            var address = mDefaultHostAddress;
            if (mHostIpInputField != null && !string.IsNullOrWhiteSpace(mHostIpInputField.text))
            {
                address = mHostIpInputField.text.Trim();
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                address = mDefaultHostAddress;
            }

            if (!IsValidHostAddress(address))
            {
                Debug.LogError($"[MultiplayerCenterTestUI] Invalid host address '{address}'. Please enter a valid IP or hostname.");
                return;
            }

            ConfigureClientAddress(address);

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[MultiplayerCenterTestUI] NetworkManager.Singleton is missing.");
                return;
            }

            var started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                Debug.LogError("[MultiplayerCenterTestUI] StartClient failed. Check host address, port, and firewall.");
                return;
            }

            Debug.Log("[MultiplayerCenterTestUI] Client started successfully.");
            DeactivateButtons();
        }

        private void StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[MultiplayerCenterTestUI] NetworkManager.Singleton is missing.");
                return;
            }

            ConfigureHostListenAddress();

            var started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                Debug.LogError("[MultiplayerCenterTestUI] StartHost failed. Check transport settings and port availability.");
                return;
            }

            Debug.Log("[MultiplayerCenterTestUI] Host started successfully.");
            DeactivateButtons();
        }

        private void DeactivateButtons()
        {
            mStartHostButton.interactable = false;
            mStartClientButton.interactable = false;
            if (mHostIpInputField != null)
            {
                mHostIpInputField.interactable = false;
            }
        }

        private void ConfigureClientAddress(string address)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            if (!(networkManager.NetworkConfig.NetworkTransport is UnityTransport transport))
            {
                Debug.LogWarning("[MultiplayerCenterTestUI] NetworkTransport is not UnityTransport, skipped IP override.");
                return;
            }

            var connection = transport.ConnectionData;
            var listenAddress = string.IsNullOrWhiteSpace(connection.ServerListenAddress)
                ? connection.Address
                : connection.ServerListenAddress;

            transport.SetConnectionData(address, connection.Port, listenAddress);
            Debug.Log($"[MultiplayerCenterTestUI] Client target set to {address}:{connection.Port}");
        }

        private void ConfigureHostListenAddress()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            if (!(networkManager.NetworkConfig.NetworkTransport is UnityTransport transport))
            {
                Debug.LogWarning("[MultiplayerCenterTestUI] NetworkTransport is not UnityTransport, skipped host listen override.");
                return;
            }

            var connection = transport.ConnectionData;
            var hostAddress = string.IsNullOrWhiteSpace(connection.Address) ? mDefaultHostAddress : connection.Address;
            var listenAddress = string.IsNullOrWhiteSpace(mHostListenAddress) ? "0.0.0.0" : mHostListenAddress.Trim();

            transport.SetConnectionData(hostAddress, connection.Port, listenAddress);
            Debug.Log($"[MultiplayerCenterTestUI] Host listen endpoint set to {listenAddress}:{connection.Port}");
        }

        private static bool IsValidHostAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            if (IPAddress.TryParse(address, out _))
            {
                return true;
            }

            // Allow hostnames like machine-name.local for LAN DNS scenarios.
            return address.IndexOf(' ') < 0;
        }

        private void EnsureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var inputType = typeof(StandaloneInputModule);
#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
            inputType = typeof(InputSystemUIInputModule);
#endif
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), inputType);
            eventSystem.transform.SetParent(transform);
        }

        private void EnsureHostIpInputFieldExists()
        {
            if (mHostIpInputField != null)
            {
                return;
            }

            if (mStartClientButton == null)
            {
                return;
            }

            var parent = mStartClientButton.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find("HostIpInputField");
            if (existing != null && existing.TryGetComponent(out TMP_InputField existingInputField))
            {
                mHostIpInputField = existingInputField;
                return;
            }

            var inputRoot = new GameObject("HostIpInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            inputRoot.transform.SetParent(parent, false);
            inputRoot.transform.SetSiblingIndex(Mathf.Max(0, mStartClientButton.transform.GetSiblingIndex()));

            var inputImage = inputRoot.GetComponent<Image>();
            inputImage.color = Color.white;

            var layoutElement = inputRoot.GetComponent<LayoutElement>();
            layoutElement.minHeight = 40f;
            layoutElement.flexibleHeight = 1f;

            var textAreaObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaObject.transform.SetParent(inputRoot.transform, false);
            var textAreaRect = textAreaObject.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10f, 6f);
            textAreaRect.offsetMax = new Vector2(-10f, -7f);

            var fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                Debug.LogWarning("[MultiplayerCenterTestUI] TMP default font asset is missing. Import TMP Essentials if text is invisible.");
            }

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(textAreaObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.font = fontAsset;
            textComponent.fontSize = 24;
            textComponent.richText = false;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(textAreaObject.transform, false);
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            var placeholderText = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderText.font = fontAsset;
            placeholderText.fontSize = 24;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.text = "Host IP Address (e.g. 192.168.1.10)";
            placeholderText.color = new Color(0.45f, 0.45f, 0.45f, 0.75f);

            var inputField = inputRoot.GetComponent<TMP_InputField>();
            inputField.targetGraphic = inputImage;
            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.text = mDefaultHostAddress;

            mHostIpInputField = inputField;
        }
    }
}



