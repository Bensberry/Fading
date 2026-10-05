using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(AudioSource))]
public class RadioClue : MonoBehaviour
{
    [Header("Player Interaction")]
    public Transform player;
    public float interactionRange = 4f;
    public GameObject interactionPrompt;

    [Header("Raycast Interaction")]
    public float raycastDistance = 4f;
    public LayerMask raycastLayers = ~0;

    [Header("Radio Playlist")]
    public AudioClip[] playlistClips;
    public int weddingSongIndex = 3;

    [Header("Playback Limit")]
    [Tooltip("Automatically stop the radio after this many seconds.")]
    public float maxPlaybackSeconds = 20f;

    private AudioSource audioSource;
    private ClueGoal clueGoal;

    private int currentTrackIndex = 0;
    private bool isRadioOn = false;
    private bool weddingClueWasActive = false;

    private Coroutine playbackTimer;

    private Camera playerCamera;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        clueGoal = GetComponent<ClueGoal>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        playerCamera = Camera.main;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (clueGoal == null)
        {
            Debug.LogWarning(
                "[RADIO] ClueGoal component is missing from the radio."
            );
        }

        if (playlistClips == null ||
            playlistClips.Length == 0)
        {
            Debug.LogWarning(
                "[RADIO] No audio clips assigned to the playlist."
            );
        }
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (playerCamera == null)
            return;


        // ========================================================
        // CHECK WHAT THE CAMERA IS LOOKING AT
        // ========================================================

        bool lookingAtRadio = IsLookingAtRadio();


        // ========================================================
        // PROMPT
        // ========================================================

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(
                lookingAtRadio
            );
        }


        // ========================================================
        // F INTERACTION
        // ========================================================

        if (lookingAtRadio && FKeyPressedThisFrame())
        {
            if (isRadioOn)
            {
                StopRadio();
            }
            else
            {
                StartRadio();
            }

            return;
        }


        // ========================================================
        // LEFT CLICK = NEXT TRACK
        // ========================================================

        if (
            lookingAtRadio &&
            isRadioOn &&
            audioSource.isPlaying &&
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            PlayNextTrack();
        }
    }

    // Check F key across both Input Systems
    private bool FKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || MobileControls.InteractPressed;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }


    // ============================================================
    // RADIO RAYCAST
    // ============================================================

    private bool IsLookingAtRadio()
    {
        if (playerCamera == null)
            return false;


        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );


        RaycastHit hit;


        bool hitSomething = Physics.Raycast(
            ray,
            out hit,
            raycastDistance,
            raycastLayers,
            QueryTriggerInteraction.Collide
        );


        if (!hitSomething)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * raycastDistance,
                Color.red
            );

            return false;
        }


        // --------------------------------------------------------
        // Check the object that was actually hit.
        // --------------------------------------------------------

        bool belongsToThisRadio =
            hit.transform == transform ||
            hit.transform.IsChildOf(transform);


        if (belongsToThisRadio)
        {
            Debug.DrawLine(
                ray.origin,
                hit.point,
                Color.green
            );

            return true;
        }


        // Something else was hit.
        Debug.DrawLine(
            ray.origin,
            hit.point,
            Color.red
        );

        return false;
    }


    // ============================================================
    // START RADIO
    // ============================================================

    private void StartRadio()
    {
        if (
            playlistClips == null ||
            playlistClips.Length == 0
        )
        {
            Debug.LogWarning(
                "[RADIO] Cannot play: playlist is empty."
            );

            return;
        }

        isRadioOn = true;


        if (
            currentTrackIndex >=
            playlistClips.Length
        )
        {
            currentTrackIndex = 0;
        }


        PlayCurrentTrack();
    }


    // ============================================================
    // STOP RADIO
    // ============================================================

    private void StopRadio()
    {
        isRadioOn = false;


        if (playbackTimer != null)
        {
            StopCoroutine(playbackTimer);
            playbackTimer = null;
        }


        audioSource.Stop();


        DeactivateWeddingClue();


        Debug.Log(
            "[RADIO] Radio turned off."
        );
    }


    // ============================================================
    // NEXT TRACK
    // ============================================================

    private void PlayNextTrack()
    {
        if (
            playlistClips == null ||
            playlistClips.Length == 0
        )
        {
            return;
        }


        currentTrackIndex++;


        if (
            currentTrackIndex >=
            playlistClips.Length
        )
        {
            currentTrackIndex = 0;
        }


        PlayCurrentTrack();
    }


    // ============================================================
    // PLAY CURRENT TRACK
    // ============================================================

    private void PlayCurrentTrack()
    {
        if (
            playlistClips == null ||
            currentTrackIndex < 0 ||
            currentTrackIndex >= playlistClips.Length
        )
        {
            Debug.LogWarning(
                "[RADIO] Invalid playlist track."
            );

            StopRadio();

            return;
        }


        AudioClip clip =
            playlistClips[currentTrackIndex];


        if (clip == null)
        {
            Debug.LogWarning(
                $"[RADIO] Track at index " +
                $"{currentTrackIndex} is empty."
            );

            StopRadio();

            return;
        }


        // --------------------------------------------------------
        // Stop previous playback timer.
        // --------------------------------------------------------

        if (playbackTimer != null)
        {
            StopCoroutine(playbackTimer);
            playbackTimer = null;
        }


        audioSource.Stop();


        // --------------------------------------------------------
        // Clear previous clue.
        // --------------------------------------------------------

        DeactivateWeddingClue();


        // --------------------------------------------------------
        // Play new track.
        // --------------------------------------------------------

        audioSource.clip = clip;
        audioSource.Play();

        isRadioOn = true;


        Debug.Log(
            $"[RADIO] Now playing: {clip.name}. " +
            $"Track index: {currentTrackIndex}"
        );


        // --------------------------------------------------------
        // Wedding song clue.
        // --------------------------------------------------------

        if (
            currentTrackIndex ==
            weddingSongIndex
        )
        {
            if (clueGoal != null)
            {
                clueGoal.ActivateClue();

                weddingClueWasActive = true;


                Debug.Log(
                    "[RADIO CLUE] Wedding song is playing. " +
                    "goalAchieved = TRUE"
                );
            }
        }
        else
        {
            Debug.Log(
                "[RADIO] Regular track playing. " +
                "No clue active."
            );
        }


        // --------------------------------------------------------
        // Automatic stop timer.
        // --------------------------------------------------------

        playbackTimer =
            StartCoroutine(
                StopAfterDuration()
            );
    }


    // ============================================================
    // AUTOMATIC STOP
    // ============================================================

    private IEnumerator StopAfterDuration()
    {
        yield return new WaitForSeconds(
            maxPlaybackSeconds
        );


        if (
            audioSource != null &&
            audioSource.isPlaying
        )
        {
            audioSource.Stop();


            Debug.Log(
                $"[RADIO] Automatically stopped after " +
                $"{maxPlaybackSeconds} seconds."
            );
        }


        DeactivateWeddingClue();


        isRadioOn = false;

        playbackTimer = null;
    }


    // ============================================================
    // DEACTIVATE WEDDING CLUE
    // ============================================================

    private void DeactivateWeddingClue()
    {
        if (
            weddingClueWasActive &&
            clueGoal != null
        )
        {
            clueGoal.DeactivateClue();


            Debug.Log(
                "[RADIO CLUE] Wedding song ended or radio stopped. " +
                "goalAchieved = FALSE"
            );
        }


        weddingClueWasActive = false;
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (playbackTimer != null)
        {
            StopCoroutine(playbackTimer);
            playbackTimer = null;
        }


        if (audioSource != null)
            audioSource.Stop();


        DeactivateWeddingClue();


        isRadioOn = false;
    }
}