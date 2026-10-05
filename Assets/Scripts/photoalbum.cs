using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotoAlbumInteraction : MonoBehaviour
{
    [Header("UI & Character References")]
    public GameObject interactUI;
    public Animator characterAnimator;
    public string photoAlbumAnimTrigger = "PlayPhotoAlbumAnim";

    [Header("Book Animation References")]
    public Animator bookAnimator;
    public string bookFlipStateName = "book_page_flip";
    public float pageFlipDuration = 1.5f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip pageTurningSFX;

    private bool isPlayerInProximity = false;
    private bool isInteracting = false;

    void Update()
    {
        ShowPromptIfUseful();
        if (isPlayerInProximity &&((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || MobileControls.InteractPressed) && !isInteracting)
        {
            TryStartSequence();
        }
    }

    private void TryStartSequence()
    {
        if (characterAnimator == null) return;

        bool hasLookedAtClock = characterAnimator.GetBool("hasLookedAtClock");

        if (hasLookedAtClock)
        {
            StartCoroutine(StartWhenGrannyIsReady(FlipPageThenLookRoutine()));
        }
        else
        {
            if (UINotifier.Instance != null)
            {
                UINotifier.Instance.ShowNotification("granny is not distracted enough");
            }
        }
    }

    // Pressing F early must not desync Granny's animation: wait (at most 1.5 s) until she is really in her looking pose,
    // then start. The player still succeeds, just a moment later, and the timing windows start when the animation does.
    private IEnumerator StartWhenGrannyIsReady(IEnumerator routine)
    {
        isInteracting = true;
        float waited = 0f;
        while (waited < 1.5f && !GrannyIsInLookingPose())
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return StartCoroutine(routine);
    }

    private bool GrannyIsInLookingPose()
    {
        AnimatorStateInfo state = characterAnimator.GetCurrentAnimatorStateInfo(0);
        return !characterAnimator.IsInTransition(0) && !state.IsName("New State");        // "New State" is her idle pose
    }

    private IEnumerator FlipPageThenLookRoutine()
    {
        isInteracting = true;

        if (characterAnimator != null)
        {
            characterAnimator.SetBool("hasLookedAtPhotoAlbum", true);
            characterAnimator.SetTrigger(photoAlbumAnimTrigger);
        }

        if (bookAnimator != null)
        {
            bookAnimator.Play(bookFlipStateName, 0, 0f);
        }

        if (audioSource != null && pageTurningSFX != null)
        {
            audioSource.PlayOneShot(pageTurningSFX);
        }

        yield return new WaitForSeconds(pageFlipDuration);

        isInteracting = false;

        // Trigger Final Cutscene
        if (FinalCutsceneController.Instance != null)
        {
            FinalCutsceneController.Instance.StartCutscene();
        }
    }

    // The [F] Interact prompt shows only while the player is here AND this step of Granny's puzzle can be done now.
    private void ShowPromptIfUseful()
    {
        bool want = isPlayerInProximity && !isInteracting && characterAnimator != null && (characterAnimator.GetBool("hasLookedAtClock") && !characterAnimator.GetBool("hasLookedAtPhotoAlbum"));
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
}   