using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClockInteraction : MonoBehaviour
{
    [Header("UI & Character References")]
    public GameObject interactUI;
    public Animator characterAnimator;
    public string clockAnimTrigger = "PlayClockLookAnim";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip clockChimeSFX;

    [Header("Timing Settings")]
    [Tooltip("Exact length of the clock look animation. The player must trigger the album within this time.")]
    public float clockLookAnimDuration = 2.5f;

    [Tooltip("Extra time (in seconds) to wait for transition back to idle before showing reset notification.")]
    public float resetDelay = 1.0f; // <--- TWEAK THIS IN THE INSPECTOR

    private bool isPlayerInProximity = false;
    private bool isInteracting = false;

    void Update()
    {
        if (isPlayerInProximity && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && !isInteracting)
        {
            TryStartSequence();
        }
    }

    private void TryStartSequence()
    {
        if (characterAnimator == null) return;

        bool hasFlickered = characterAnimator.GetBool("hasFlickered");

        if (hasFlickered)
        {
            StartCoroutine(ClockLookRoutine());
        }
        else
        {
            if (UINotifier.Instance != null)
            {
                UINotifier.Instance.ShowNotification("granny isn't looking here");
            }
        }
    }

    private IEnumerator ClockLookRoutine()
    {
        isInteracting = true;

        if (audioSource != null && clockChimeSFX != null)
        {
            audioSource.PlayOneShot(clockChimeSFX);
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(clockAnimTrigger);
            characterAnimator.SetBool("hasLookedAtClock", true);
        }

        // 1. Wait for the exact duration of the clock look animation window
        float windowTimer = 0f;
        while (windowTimer < clockLookAnimDuration)
        {
            // If the player successfully interacted with the Photo Album, exit cleanly without resetting!
            if (characterAnimator != null && characterAnimator.GetBool("hasLookedAtPhotoAlbum"))
            {
                isInteracting = false;
                yield break;
            }

            windowTimer += Time.deltaTime;
            yield return null;
        }

        // 2. Wait for Granny to fully transition back to Idle
        if (resetDelay > 0f)
        {
            float delayTimer = 0f;
            while (delayTimer < resetDelay)
            {
                if (characterAnimator != null && characterAnimator.GetBool("hasLookedAtPhotoAlbum"))
                {
                    isInteracting = false;
                    yield break;
                }
                delayTimer += Time.deltaTime;
                yield return null;
            }
        }

        // 3. If animation & delay finished without album interaction: reset instantly
        if (characterAnimator != null && !characterAnimator.GetBool("hasLookedAtPhotoAlbum"))
        {
            characterAnimator.SetBool("hasFlickered", false);
            characterAnimator.SetBool("hasLookedAtClock", false);
            characterAnimator.SetBool("hasLookedAtPhotoAlbum", false);

            if (UINotifier.Instance != null)
            {
                UINotifier.Instance.ShowNotification("You took too long! Granny turned back around.");
            }
        }

        isInteracting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInProximity = true;
            if (interactUI != null) interactUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInProximity = false;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}