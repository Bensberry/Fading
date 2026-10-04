using System.Collections;
using UnityEngine;

// INT_Child_MusicBox
// The lid opens, the key turns and the tune plays.
// While the music is playing, its ClueGoal is active.
public class MusicBoxInteraction : Interactable
{
    [Header("Music")]
    public AudioClip lullaby;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    public float lidOpenAngle = 70f;
    public float openTime = 0.8f;
    public float closeTime = 1.2f;
    public float keyTurnsPerSecond = 0.5f;

    [Header("Clue")]
    public ClueGoal clueGoal;

    private Transform lid;
    private Transform key;

    private Quaternion lidClosed;
    private AudioSource music;

    private void Start()
    {
        lid = FindPart("_Lid");
        key = FindPart("_Key");

        if (lid != null)
            lidClosed = lid.localRotation;

        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.spatialBlend = 1f;
        music.maxDistance = 12f;

        if (clueGoal == null)
            clueGoal = GetComponent<ClueGoal>();
    }

    protected override IEnumerator Apply()
    {
        // Music box has been activated.
        if (clueGoal != null)
            clueGoal.ActivateClue();

        if (lullaby != null)
        {
            music.clip = lullaby;
            music.volume = volume;
            music.Play();
        }

        if (lid != null)
        {
            yield return TurnLid(
                lidClosed,
                lidClosed * Quaternion.Euler(-lidOpenAngle, 0f, 0f),
                openTime
            );
        }
    }

    protected override void WhileHeld(float t)
    {
        if (key != null && key != transform)
        {
            key.Rotate(
                360f * keyTurnsPerSecond * Time.deltaTime,
                0f,
                0f,
                Space.Self
            );
        }
    }

    protected override IEnumerator Revert()
    {
        if (lid != null)
        {
            Quaternion open = lid.localRotation;

            for (float t = 0f; t < closeTime; t += Time.deltaTime)
            {
                float progress = t / closeTime;

                lid.localRotation = Quaternion.Slerp(
                    open,
                    lidClosed,
                    Ease(progress)
                );

                if (music != null)
                    music.volume = volume * (1f - progress);

                yield return null;
            }

            lid.localRotation = lidClosed;
        }

        if (music != null)
            music.Stop();

        // Music box has stopped.
        if (clueGoal != null)
            clueGoal.DeactivateClue();
    }

    protected override void RestoreInstant()
    {
        if (lid != null)
            lid.localRotation = lidClosed;

        if (music != null)
            music.Stop();

        if (clueGoal != null)
            clueGoal.DeactivateClue();
    }

    private IEnumerator TurnLid(
        Quaternion from,
        Quaternion to,
        float time
    )
    {
        if (lid == null)
            yield break;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            lid.localRotation = Quaternion.Slerp(
                from,
                to,
                Ease(t / time)
            );

            yield return null;
        }

        lid.localRotation = to;
    }
}