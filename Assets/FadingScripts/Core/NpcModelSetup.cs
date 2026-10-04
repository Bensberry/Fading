using UnityEngine;
using UnityEngine.AI;

// Goes in: nowhere (ChapterRules calls Run() when a chapter scene loads, before the NPCs start).
// Mom's AI object ('wife', the script GrandmaAI with a NavMeshAgent) and her animated model ('femeie_1') are separate objects
// in the scene, so the AI walks around as an empty cylinder while the model stands still somewhere else.
// This hooks them together when the scene starts, without editing the scene file:
//   - the model becomes a child of the AI object (so it walks with it), standing on the floor under the cylinder
//   - the cylinder is made invisible (its collider stays)
//   - the AI is told which Animator to use (it sets the "Speed" value)
// If the model is already a child of the AI object (set up by hand in the scene), nothing is changed.
public static class NpcModelSetup
{
    public static void Run()
    {
        foreach (GrandmaAI ai in Object.FindObjectsByType<GrandmaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AttachModel(ai);
    }

    static void AttachModel(GrandmaAI ai)
    {
        Transform npc = ai.transform;
        if (npc.GetComponentInChildren<Animator>(true) != null) return;         // a model is already attached

        Animator model = FindLooseModel();
        if (model == null) return;

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        float offset = agent != null ? agent.baseOffset : 0f;                   // the agent hovers this high above the floor

        Transform m = model.transform;
        m.SetParent(npc, true);                                                 // keeps its size in the world
        m.position = new Vector3(npc.position.x, npc.position.y - offset, npc.position.z);
        m.rotation = npc.rotation;

        MeshRenderer cylinder = npc.GetComponent<MeshRenderer>();
        if (cylinder != null) cylinder.enabled = false;

        model.applyRootMotion = false;
        ai.animator = model;
    }

    // The animated Mom model that is standing loose in the scene (not under any object).
    static Animator FindLooseModel()
    {
        foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a.transform.parent == null && a.gameObject.name.IndexOf("femeie", System.StringComparison.OrdinalIgnoreCase) >= 0) return a;
        return null;
    }
}
