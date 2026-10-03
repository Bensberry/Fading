using UnityEngine;

// Goes in: nowhere. It runs by itself when any scene starts (no need to attach it).
// Gives the player in Chapter0 the F-key "touch" ability without editing his scene or prefab:
// it adds our PlayerInteractor to the main camera if no camera has one yet.
public static class PlayerInteractorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AddInteractor()
    {
        // Only needed in scenes that contain touchable objects.
        if (Object.FindFirstObjectByType<Interactable>(FindObjectsInactive.Include) == null) return;
        if (Object.FindFirstObjectByType<PlayerInteractor>() != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        cam.gameObject.AddComponent<PlayerInteractor>();
    }
}
