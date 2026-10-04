using UnityEngine;

public class GhostClueObject : MonoBehaviour
{
    [Header("Glow Settings")]
    public float glowDuration = 1.5f;      // Total duration is 1.5 seconds
    public float breathCycleTime = 0.75f;  // One full breath cycle takes 0.75 seconds

    private Renderer objRenderer;
    private Material mat;
    
    private bool isGlowing = false;
    private float glowTimer = 0f;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            mat = objRenderer.material;
            mat.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        // Whenever 'E' is pressed, trigger the breathing glow instantly
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartGlow();
        }

        // Handle the breathing glow over 1.5 seconds
        if (isGlowing)
        {
            glowTimer -= Time.deltaTime;
            
            if (glowTimer > 0f)
            {
                // Calculate elapsed time since the glow started
                float elapsedTime = glowDuration - glowTimer;
                
                // Smooth sine-wave breathing pulse (goes from 0 to 1 and back every 0.75 seconds)
                float pulse = (Mathf.Sin(elapsedTime * (Mathf.PI * 2f / breathCycleTime)) + 1f) * 0.5f;
                
                // High-to-low breathing intensity multiplier
                Color baseColor = new Color(3.5f, 1.7f, 0.2f); 
                Color glowColor = baseColor * (pulse * 2.5f + 0.5f); 
                
                if (mat != null)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", glowColor);
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", new Color(1f, 0.6f, 0.2f) * (pulse * 0.5f + 0.5f));
                    }
                }
            }
            else
            {
                // Turn glow completely off when the 1.5 seconds expire
                isGlowing = false;
                if (mat != null)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", Color.black);
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", Color.white);
                    }
                }
            }
        }
    }

    void StartGlow()
    {
        isGlowing = true;
        glowTimer = glowDuration;
        Debug.Log("Breathing glow triggered on: " + gameObject.name);
    }
}