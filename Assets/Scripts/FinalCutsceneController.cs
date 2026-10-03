using System.Collections;
using UnityEngine;
using Unity.Cinemachine; 
using TMPro;

public class FinalCutsceneController : MonoBehaviour
{
    public static FinalCutsceneController Instance { get; private set; }

    // Other scripts (tutorial, chapter logic) listen to these.
    public static event System.Action OnCutsceneStarted;
    public static event System.Action OnCutsceneFinished;

    [Header("Cinemachine & Input References")]
    public CinemachineCamera cutsceneVcam; 
    public FirstPersonController playerController; 

    [Header("Player Blocking Objects to Hide")]
    [Tooltip("Drag the Player mesh, Candle, or Player parent object here so it doesn't block the cutscene camera.")]
    public GameObject[] objectsToHideInCutscene; // <--- Drag Player, MemorialCandle, etc. here!

    [Header("Granny References")]
    public Animator grannyAnimator;
    public string grannyTalkAnimTrigger = "PlayGrannyTalk";
    public AudioSource grannyAudioSource;
    public AudioClip grannyDialogueSFX;

    [Header("UI Dialogue (Optional)")]
    public TextMeshProUGUI dialogueText;
    public string dialogueString = "What do you think you're doing...?";
    public float dialogueDisplayDuration = 4.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartCutscene()
    {
        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        if (OnCutsceneStarted != null) OnCutsceneStarted();
        // 1. Freeze Player Movement & Mouse Look
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Hide Interaction UI prompt
        if (UINotifier.Instance != null && UINotifier.Instance.interactUI != null)
        {
            UINotifier.Instance.interactUI.SetActive(false);
        }

        // 2. Hide Player Body Parts / Candle / Head meshes that block the view
        if (objectsToHideInCutscene != null)
        {
            foreach (GameObject obj in objectsToHideInCutscene)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // 3. Raise Cutscene Vcam Priority to trigger Cinemachine smooth camera blend
        if (cutsceneVcam != null)
        {
            cutsceneVcam.Priority.Value = 20; 
        }

        // Wait brief moment for camera blending transition
        yield return new WaitForSeconds(1.0f);

        // 4. Trigger Granny Turn & Talk Animation
        if (grannyAnimator != null)
        {
            grannyAnimator.SetTrigger(grannyTalkAnimTrigger);
        }

        // 5. Play Dialogue Audio & Subtitles
        if (grannyAudioSource != null && grannyDialogueSFX != null)
        {
            grannyAudioSource.PlayOneShot(grannyDialogueSFX);
        }

        if (dialogueText != null)
        {
            dialogueText.text = dialogueString;
            dialogueText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(dialogueDisplayDuration);

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }

        Debug.Log("Cutscene complete.");
        if (OnCutsceneFinished != null) OnCutsceneFinished();
    }
}