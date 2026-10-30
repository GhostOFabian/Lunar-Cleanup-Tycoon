// Hält die Oberfläche innerhalb der sicheren Bildschirmfläche (Notch, abgerundete Ecken, Home-Leiste).
using UnityEngine;

namespace LunarCleanup.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect last;
        Vector2Int lastSize;

        void OnEnable() { Apply(); }
        void Update() { if (Screen.safeArea != last || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply(); }

        void Apply()
        {
            last = Screen.safeArea; lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            rt.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
