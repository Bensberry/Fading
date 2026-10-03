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
        if (isPlayerInProximity && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && !isInteracting)
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
            StartCoroutine(FlipPageThenLookRoutine());
        }
        else
        {
            if (UINotifier.Instance != null)
            {
                UINotifier.Instance.ShowNotification("granny is not distracted enough");
            }
        }
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