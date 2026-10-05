using UnityEngine;

// Goes in: nowhere (CutsceneContext.Glimmers adds it). Makes one small memory spark drift, bob and twinkle in a dream.
public class Glimmer : MonoBehaviour
{
    Vector3 start, drift;
    float seed, size;

    void Start()
    {
        start = transform.position;
        drift = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.02f, 0.1f), Random.Range(-0.15f, 0.15f));
        seed = Random.value * 10f;
        size = transform.localScale.x;
    }

    void Update()
    {
        float t = Time.time + seed;
        transform.position = start + drift * Mathf.Sin(t * 0.3f) * 3f + new Vector3(0f, Mathf.Sin(t * 1.3f) * 0.08f, 0f);
        transform.localScale = Vector3.one * size * (0.75f + 0.35f * Mathf.Sin(t * 4f));
    }
}
