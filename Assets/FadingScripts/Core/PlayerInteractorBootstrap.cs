using UnityEngine;
using UnityEngine.SceneManagement;

// Goes in: nowhere. It runs by itself when any scene starts (no need to attach it).
// Gives the player in Chapter0 the F-key "touch" ability without editing his scene or prefab:
// it adds our PlayerInteractor to the main camera if no camera has one yet.
public static class PlayerInteractorBootstrap
{
    // Unity runs this start-up hook only ONCE (for the first scene), so we listen for every scene load instead.
    // That way it also works when the game is started from the main menu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Also run once for the very first scene (when you press Play directly in a chapter). Safe to run twice.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RunForFirstScene()
    {
        AddInteractor();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddInteractor();
    }

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
