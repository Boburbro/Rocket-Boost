using UnityEngine;
using UnityEngine.UI;

// HUD'dagi vaqt hisoblagichi. Raketa urilganda yoki finishga yetganda (Movement o'chganda) to'xtaydi.
public class HudTimer : MonoBehaviour
{
    Text label;
    Movement movement;
    float elapsed;

    public void Init(Text target)
    {
        label = target;
        movement = FindFirstObjectByType<Movement>();
        Refresh();
    }

    void Update()
    {
        if (label == null || movement == null || !movement.enabled) return;
        elapsed += Time.deltaTime;
        Refresh();
    }

    void Refresh()
    {
        int tenths = (int)(elapsed * 10f);
        label.text = (tenths / 600) + ":" + (tenths % 600 / 10).ToString("00") + "." + (tenths % 10);
    }
}
