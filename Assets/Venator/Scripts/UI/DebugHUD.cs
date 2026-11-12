using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary test HUD drawn with OnGUI (the real Canvas HUD replaces it in Week 03).
/// Shows the crosshair, Vitality, Tonics, bolts, movement state, and the controls.
/// F1 hides the controls list. F10 quits a built game.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    HealthSystem health;
    WeaponController weapons;
    FirstPersonController mover;
    bool showHelp = true;
    GUIStyle label, big;

    void Awake()
    {
        health = GetComponent<HealthSystem>();
        weapons = GetComponent<WeaponController>();
        mover = GetComponent<FirstPersonController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f1Key.wasPressedThisFrame) showHelp = !showHelp;
        if (kb.f10Key.wasPressedThisFrame) Application.Quit();
    }

    void Bar(Rect r, float fill, Color c)
    {
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fill), r.height - 4), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            label.normal.textColor = new Color(0.92f, 0.89f, 0.78f);
            big = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
        float w = Screen.width, h = Screen.height;

        // crosshair
        GUI.color = new Color(1f, 0.95f, 0.85f, 0.9f);
        GUI.DrawTexture(new Rect(w / 2 - 8, h / 2 - 1, 16, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(w / 2 - 1, h / 2 - 8, 2, 16), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (health != null)
        {
            GUI.Label(new Rect(24, h - 78, 200, 20), "VITALITY", label);
            Bar(new Rect(24, h - 58, 280, 18), health.Current / health.maxVitality, new Color(0.56f, 0.17f, 0.17f));
            GUI.Label(new Rect(24, h - 36, 300, 20), $"Tonics: {health.Tonics}/{health.maxTonics}  (H to drink)", label);
            if (Time.time - health.LastHitTime < 0.25f)
            {
                GUI.color = new Color(0.6f, 0f, 0f, 0.25f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (health.IsDead) GUI.Label(new Rect(0, h / 2 - 60, w, 40), "THE NIGHT PREVAILS... returning to the last light", big);
        }

        if (weapons != null)
        {
            string ammo = weapons.IsReloading ? "RELOADING..." : $"BOLTS  {weapons.Loaded} / {weapons.Reserve}";
            GUI.Label(new Rect(w - 224, h - 58, 200, 24), ammo, big);
        }

        if (mover != null)
        {
            string state = mover.IsSliding ? "SLIDING" : mover.IsCrouched ? "CROUCHED" : mover.IsSprinting ? "SPRINT" : "";
            GUI.Label(new Rect(w / 2 - 100, h - 40, 200, 24), state, label);
        }

        GUI.Label(new Rect(24, 16, 400, 24), "HARBOR GATE  -  District 1 (greybox)", label);

        if (showHelp)
        {
            GUI.Label(new Rect(w - 300, 16, 290, 210),
                "WASD move   Mouse look\nShift sprint   Space jump\nSprint, then tap Ctrl = slide (gold beams)\n" +
                "LMB crossbow (hold)   R reload\nRMB whip lash (interrupts telegraphs)\nH tonic   K test damage\n" +
                "Esc free cursor   F1 hide help   F10 quit", label);
        }
    }
}
