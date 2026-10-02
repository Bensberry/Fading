using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Base class for anything the ghost can touch.
// Add one of the subclasses (SlideAndReturn, MusicBoxInteraction, RockingChairInteraction) to an INT_ object.
public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Text shown at the bottom of the screen when the player looks at this object.")]
    public string prompt = "Click to touch";

    [Tooltip("Hook game logic here (count a sign, raise fear, play a voice line...).")]
    public UnityEvent onInteract = new UnityEvent();

    public bool IsBusy { get; private set; }

    protected virtual void Awake()
    {
        // The player's raycast needs a collider. Add a box that fits the object if there isn't one.
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            if (GetComponent<MeshFilter>() == null)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
                    box.center = transform.InverseTransformPoint(b.center);
                    Vector3 s = transform.InverseTransformVector(b.size);
                    box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
            }
        }
    }

    // Called by PlayerInteractor. Ignored while the object is still moving.
    public void TryInteract()
    {
        if (IsBusy) return;
        StartCoroutine(RunWrapped());
    }

    IEnumerator RunWrapped()
    {
        IsBusy = true;
        onInteract.Invoke();
        yield return StartCoroutine(Run());
        IsBusy = false;
    }

    // What the object actually does. Runs as a coroutine.
    protected abstract IEnumerator Run();
}
