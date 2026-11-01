// Flaggen im Bild (Fabian 01.11.): erscheinen auf gereinigten Parzellen, gefundene wehen in der Planetenfarbe und tragen deinen Text.
using System.Collections.Generic;
using LunarCleanup.Core;
using TMPro;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class FlagView { public GameObject go; public Transform cloth; public TextMeshPro label; public FlagSave f; public bool shown, got; public string text; }
        readonly List<FlagView> flagViews = new List<FlagView>(); string flagFor; int flagVer = -1; float flagT;

        void BuildFlags()
        {
            flagFor = ps.pid; foreach (var v in flagViews) if (v.go) Destroy(v.go); flagViews.Clear(); flagVer = -1;
            foreach (var f in ps.sv.flags) flagViews.Add(new FlagView { f = f });
        }
        GameObject MakeFlag(FlagView v)
        {
            var dir = Game.FlagDir(ps, v.f); var g = new GameObject("Flagge"); g.transform.SetParent(transform, false);
            g.transform.position = pd.Surf(dir, 0, v.f.cell); g.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(pd.cells[v.f.cell].e1, dir).normalized, dir);
            var pole = MeshKit.Mat(new Color(.82f, .84f, .88f), .8f, .6f); MeshKit.Cyl(g.transform, pole, new Vector3(0, 1.1f, 0), .04f, .05f, 2.2f, 8); MeshKit.Ball(g.transform, pole, new Vector3(0, 2.24f, 0), .09f);
            MeshKit.Cyl(g.transform, MeshKit.Mat(new Color(.25f, .26f, .3f), .4f, .3f), new Vector3(0, .03f, 0), .22f, .26f, .06f, 10);
            var cl = new GameObject("Tuch"); cl.transform.SetParent(g.transform, false); cl.transform.localPosition = new Vector3(.03f, 1.82f, 0); v.cloth = cl.transform;
            var col = v.f.got ? ps.P.col0 : new Color(.85f, .86f, .9f); var cm = MeshKit.Mat(col, .05f, .3f, v.f.got ? col * .35f : (Color?)null);
            MeshKit.Box(cl.transform, cm, new Vector3(.55f, 0, 0), new Vector3(1.1f, .66f, .03f));
            var tgo = new GameObject("Text"); tgo.transform.SetParent(cl.transform, false); tgo.transform.localPosition = new Vector3(.55f, 0, -.03f);
            var tm = tgo.AddComponent<TextMeshPro>(); tm.fontSize = 2.2f; tm.alignment = TextAlignmentOptions.Center; tm.enableAutoSizing = true; tm.fontSizeMin = .6f; tm.fontSizeMax = 2.4f; tm.rectTransform.sizeDelta = new Vector2(1.0f, .58f);
            tm.color = v.f.got ? Color.white : new Color(.2f, .22f, .3f); var tm2 = Object.Instantiate(tgo, cl.transform); tm2.transform.localPosition = new Vector3(.55f, 0, .03f); tm2.transform.localRotation = Quaternion.Euler(0, 180, 0);
            v.label = tm; v.got = v.f.got; v.text = null; return g;
        }
        void UpdateFlags()
        {
            if (ps == null || pd == null || ps.sv.flags == null) return; if (flagFor != ps.pid || flagViews.Count != ps.sv.flags.Count) BuildFlags();
            flagT -= Time.deltaTime; bool force = flagVer != Game.FlagVer; if (flagT > 0 && !force) { Wave(); return; } flagT = .5f; flagVer = Game.FlagVer;
            int n = 0;
            foreach (var v in flagViews)
            {
                n++; bool vis = Game.FlagVisible(ps, v.f) && !MapMode;
                if (vis && (!v.go || v.got != v.f.got)) { if (v.go) Destroy(v.go); v.go = MakeFlag(v); }
                if (v.go && v.go.activeSelf != vis) v.go.SetActive(vis);
                if (v.go && vis)
                {
                    string t = v.f.got ? (string.IsNullOrEmpty(v.f.text) ? "#" + n : v.f.text) : "?";
                    if (t != v.text) { v.text = t; foreach (var tm in v.go.GetComponentsInChildren<TextMeshPro>()) tm.text = t; }
                }
            }
            Wave();
        }
        void Wave() { float t = Time.time; for (int i = 0; i < flagViews.Count; i++) { var c = flagViews[i].cloth; if (c && c.gameObject.activeInHierarchy) c.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 1.7f + i) * 14, Mathf.Sin(t * 2.3f + i * 1.3f) * 3); } }
        /// <summary>Für das Antippen: sichtbare Flaggen mit Weltposition (Mitte des Tuchs).</summary>
        public IEnumerable<(FlagSave f, Vector3 pos)> FlagSpots() { foreach (var v in flagViews) if (v.go && v.go.activeInHierarchy) yield return (v.f, v.go.transform.TransformPoint(new Vector3(.5f, 1.6f, 0))); }
    }
}
