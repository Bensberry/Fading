using System.Collections;
using UnityEngine;
using TMPro;

public class UINotifier : MonoBehaviour
{
    public static UINotifier Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI notificationText;
    public GameObject interactUI; // Drag 'Interact Panel' or 'Interact Prompt' here
    public float displayDuration = 2.5f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        // Hide the [F] Interact prompt while displaying notification
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        // Hide notification text
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }

        // Re-enable [F] Interact prompt if player is still standing in a trigger
        if (interactUI != null)
        {
            interactUI.SetActive(true);
        }
    }
}