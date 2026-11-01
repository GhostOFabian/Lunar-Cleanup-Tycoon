// Text, der sich selbst übersetzt (Loc). Merkt sich den deutschen Text und stellt bei Sprachwechsel sofort um.
using LunarCleanup.Core;
using TMPro;

namespace LunarCleanup.UI
{
    public class LText : TextMeshProUGUI
    {
        string raw;
        public override string text
        {
            get => raw ?? base.text;
            set { if (value == raw && raw != null) return; raw = value; base.text = Loc.Tr(value); }
        }
        protected override void OnEnable() { base.OnEnable(); Loc.Changed += Refresh; if (raw != null) base.text = Loc.Tr(raw); }
        protected override void OnDisable() { Loc.Changed -= Refresh; base.OnDisable(); }
        void Refresh() { if (raw != null) base.text = Loc.Tr(raw); }
    }
}
