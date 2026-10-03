using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Base class for everything the ghost can touch.
// THE 5-SECOND RULE: every touch follows the same fixed sequence (doors are the only exception):
//   Apply()  ->  hold for exactly 5 seconds (WhileHeld runs every frame)  ->  Revert()  (drifts back)
public abstract class Interactable : MonoBehaviour
{
    public const float TouchDuration = 5f;     // mandatory, deliberately not editable

    [Tooltip("Text shown under the crosshair when the player looks at this object.")]
    public string prompt = "Touch";
    [Tooltip("Optional sound played at the moment of the touch.")]
    public AudioClip sound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Tooltip("Fires when the touch starts. Hook signs / glow / fear logic here.")]
    public UnityEvent onInteract = new UnityEvent();
    [Tooltip("Fires when the object has fully drifted back.")]
    public UnityEvent onReturned = new UnityEvent();

    public bool IsBusy { get; private set; }

    protected virtual void Awake() { EnsureCollider(); }

    // Each object's own script (MomCoffeeMug, GrandmaClock, ...) fills in its settings here.
    // Runs when the script is added in the Editor, and from FadingInteractablesSetup at runtime.
    public virtual void ApplyDefaults() { }
    protected virtual void Reset() { ApplyDefaults(); }

    // Doors override this (they are the one exception to the 5-second rule).
    public virtual void TryInteract()
    {
        if (IsBusy || !isActiveAndEnabled) return;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        IsBusy = true;
        onInteract.Invoke();
        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, soundVolume);

        yield return StartCoroutine(Apply());
        for (float t = 0f; t < TouchDuration; t += Time.deltaTime)
        {
            WhileHeld(t);
            yield return null;
        }
        yield return StartCoroutine(Revert());

        IsBusy = false;
        onReturned.Invoke();
    }

    // If the object is switched off mid-touch (e.g. packed into a box), snap it back instantly.
    protected virtual void OnDisable()
    {
        if (!IsBusy) return;
        StopAllCoroutines();
        IsBusy = false;
        RestoreInstant();
    }

    protected abstract IEnumerator Apply();            // the touch itself
    protected virtual void WhileHeld(float t) { }      // runs every frame during the 5 seconds
    protected abstract IEnumerator Revert();           // drifting back to how it was
    protected abstract void RestoreInstant();          // snap back with no animation

    // ---------- helpers for subclasses
    protected Transform FindPart(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return transform;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t != transform && t.name.EndsWith(suffix)) return t;
        return transform;
    }

    protected static float Ease(float x) { return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x)); }

    // The player's raycast needs a collider. Add a box around the whole object (children included).
    void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        box.center = transform.InverseTransformPoint(b.center);
        Vector3 s = transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Max(Mathf.Abs(s.x), 0.05f), Mathf.Max(Mathf.Abs(s.y), 0.05f), Mathf.Max(Mathf.Abs(s.z), 0.05f));
    }
}
