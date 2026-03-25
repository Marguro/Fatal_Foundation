using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Quits the app when Escape is pressed.
/// In the Unity Editor, Escape stops Play Mode.
/// </summary>
public class QuitOnEscape : MonoBehaviour
{
    [SerializeField] private bool allowQuitInEditor = true;

    private void Update()
    {
        if (!WasEscapePressedThisFrame())
        {
            return;
        }

        QuitGame();
    }

    private static bool WasEscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        if (allowQuitInEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
        Application.Quit();
    }
}

