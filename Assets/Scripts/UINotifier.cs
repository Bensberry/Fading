using System.Collections;
using System.Collections.Generic;
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

    // The [F] Interact prompt shows ONLY while at least one puzzle object (lamp, clock, album) says
    // "the player is next to me AND my step can be done right now", and never during a message or a cutscene.
    private static readonly HashSet<Object> wantingPrompt = new HashSet<Object>();
    private bool showingNotification;
    [HideInInspector] public bool promptsBlocked;          // the final cutscene switches the prompt off for good

    public static void WantPrompt(Object who, bool want)
    {
        if (want) wantingPrompt.Add(who);
        else wantingPrompt.Remove(who);
    }

    private void Update()
    {
        if (interactUI == null) return;
        wantingPrompt.RemoveWhere(o => o == null);
        bool on = wantingPrompt.Count > 0 && !showingNotification && !promptsBlocked && !CutsceneRunner.IsPlaying;
        if (interactUI.activeSelf != on) interactUI.SetActive(on);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        wantingPrompt.Clear();
        if (interactUI != null) interactUI.SetActive(false);

        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        // The [F] Interact prompt is hidden while the message shows (see Update)
        showingNotification = true;
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

        // The [F] Interact prompt comes back only if the player is still next to something they can do (see Update)
        showingNotification = false;
    }
}