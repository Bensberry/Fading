using System.Collections;
using UnityEngine;

// INT_Mom_FogWindow: a nickname is traced letter by letter in the fogged glass,
// stays for 5 seconds, then the fog slowly covers it again.
public class FogWriting : Interactable
{
    public string nickname = "Sunny";
    public Color letterColor = new Color(0.25f, 0.3f, 0.35f, 1f);
    public float traceTime = 1.5f;
    public float fadeTime = 2f;
    public int fontSize = 60;
    public float characterSize = 0.03f;

    TextMesh text;

    void Start()
    {
        GameObject g = new GameObject("FogLetters");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0f, 0f, 0.005f);       // just in front of the fog, room side
        g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);     // readable from inside the kitchen
        text = g.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = fontSize;
        text.characterSize = characterSize;
        Font font = LoadFont();
        if (font != null)
        {
            text.font = font;
            g.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        text.text = "";
    }

    static Font LoadFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        return f;
    }

    void SetAlpha(float a) { Color c = letterColor; c.a = a; text.color = c; }

    protected override IEnumerator Apply()
    {
        SetAlpha(1f);
        for (float t = 0f; t < traceTime; t += Time.deltaTime)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(nickname.Length * t / traceTime), 0, nickname.Length);
            text.text = nickname.Substring(0, n);
            yield return null;
        }
        text.text = nickname;
    }

    protected override IEnumerator Revert()
    {
        for (float t = 0f; t < fadeTime; t += Time.deltaTime) { SetAlpha(1f - t / fadeTime); yield return null; }
        RestoreInstant();
    }

    protected override void RestoreInstant() { if (text != null) { text.text = ""; SetAlpha(1f); } }
}
