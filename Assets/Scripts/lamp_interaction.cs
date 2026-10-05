using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LampInteraction : MonoBehaviour
{
    [Header("UI & Light References")]
    public GameObject interactUI;
    public Light lampLight;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip lampSwitchSFX;

    [Header("Animation References")]
    public Animator characterAnimator;
    public string animationTriggerName = "PlayFlickerAnim";

    [Header("Timing Settings")]
    public float flickerDuration = 2.0f;
    public float minFlickerInterval = 0.05f;
    public float maxFlickerInterval = 0.15f;
    [Tooltip("Exact length of the lamp look animation. The player must trigger the clock within this time.")]
    public float lampLookAnimDuration = 5.8f; 
    
    [Tooltip("Extra time (in seconds) to wait for transition back to idle before showing reset notification.")]
    public float resetDelay = 1.0f; // <--- TWEAK THIS IN THE INSPECTOR

    private bool isPlayerInProximity = false;
    private bool isFlickering = false;

    void Update()
    {
        ShowPromptIfUseful();
        if (isPlayerInProximity &&((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || MobileControls.InteractPressed) && !isFlickering)
        {
            StartCoroutine(FlickerAndAnimateRoutine());
        }
    }

    // The [F] Interact prompt shows only while the player is here AND this step of Granny's puzzle can be done now.
    private void ShowPromptIfUseful()
    {
        bool want = isPlayerInProximity && !isFlickering && characterAnimator != null && (!characterAnimator.GetBool("hasFlickered"));
        if (UINotifier.Instance != null) UINotifier.WantPrompt(this, want);
        else if (interactUI != null) interactUI.SetActive(want);
    }

    private void OnDisable()
    {
        UINotifier.WantPrompt(this, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInProximity = true;

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInProximity = false;

        }
    }

    private IEnumerator FlickerAndAnimateRoutine()
    {
        isFlickering = true;

        // 1. Play Switch Sound
        if (audioSource != null && lampSwitchSFX != null)
        {
            audioSource.PlayOneShot(lampSwitchSFX);
        }

        // 2. Play Character Lamp Animation & Open Clock Window
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(animationTriggerName); 
            characterAnimator.SetBool("hasFlickered", true); 
        }

        // 3. Light Flicker Loop
        float timer = 0f;
        while (timer < flickerDuration)
        {
            if (lampLight != null) lampLight.enabled = !lampLight.enabled;
            float randomTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            timer += randomTime;
            yield return new WaitForSeconds(randomTime);
        }
        if (lampLight != null) lampLight.enabled = true;

        // 4. Wait for the remaining duration of the animation window
        float remainingTime = Mathf.Max(0f, lampLookAnimDuration - flickerDuration);
        
        float windowTimer = 0f;
        while (windowTimer < remainingTime)
        {
            // If the player successfully interacted with the clock during the animation, break immediately!
            if (characterAnimator != null && characterAnimator.GetBool("hasLookedAtClock"))
            {
                isFlickering = false;
                yield break;
            }

            windowTimer += Time.deltaTime;
            yield return null;
        }

        // --- NEW: Wait for Granny to fully settle back into Idle ---
        if (resetDelay > 0f)
        {
            float delayTimer = 0f;
            while (delayTimer < resetDelay)
            {
                // Still allow the player to trigger clock during the transition if needed
                if (characterAnimator != null && characterAnimator.GetBool("hasLookedAtClock"))
                {
                    isFlickering = false;
                    yield break;
                }
                delayTimer += Time.deltaTime;
                yield return null;
            }
        }

        // 5. If time ran out and clock was NOT interacted with: reset instantly
        if (characterAnimator != null && !characterAnimator.GetBool("hasLookedAtClock"))
        {
            characterAnimator.SetBool("hasFlickered", false);
            characterAnimator.SetBool("hasLookedAtClock", false);
            characterAnimator.SetBool("hasLookedAtPhotoAlbum", false);

            if (UINotifier.Instance != null)
            {
                UINotifier.Instance.ShowNotification("You took too long! Granny looked back.");
            }
        }

        isFlickering = false;
    }
}