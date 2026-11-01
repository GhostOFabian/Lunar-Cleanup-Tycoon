// Flaggen im Bild (Fabian 01.11.): erscheinen auf gereinigten Parzellen, gefundene wehen in der Planetenfarbe und tragen deinen Text.
using System.Collections.Generic;
using LunarCleanup.Core;
using TMPro;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class FlagView { public GameObject go; public Transform cloth; public TextMeshPro label; public FlagSave f; public bool shown, got; public string text; public Transform lid, chest; public float openT = -1; public int look = -1; }
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
            var col = v.f.got ? Game.FlagCloth(ps, v.f) : new Color(.85f, .86f, .9f); var cm = MeshKit.Mat(col, .05f, .3f, v.f.got ? col * .35f : (Color?)null);
            MeshKit.Box(cl.transform, cm, new Vector3(.55f, 0, 0), new Vector3(1.1f, .66f, .03f));
            var tgo = new GameObject("Text"); tgo.transform.SetParent(cl.transform, false); tgo.transform.localPosition = new Vector3(.55f, 0, -.03f);
            var tm = tgo.AddComponent<TextMeshPro>(); tm.fontSize = 2.2f; tm.alignment = TextAlignmentOptions.Center; tm.enableAutoSizing = true; tm.fontSizeMin = .6f; tm.fontSizeMax = 2.4f; tm.rectTransform.sizeDelta = new Vector2(1.0f, .58f);
            tm.color = v.f.got ? Game.FlagInk(v.f) : new Color(.2f, .22f, .3f); var tm2 = Object.Instantiate(tgo, cl.transform); tm2.transform.localPosition = new Vector3(.55f, 0, .03f); tm2.transform.localRotation = Quaternion.Euler(0, 180, 0);
            /* Fabian 01.11.: Schatztruhe neben der Flagge – antippen öffnet sie, danach darfst du die Flagge beschriften */
            var ch = new GameObject("Schatztruhe").transform; ch.SetParent(g.transform, false); ch.localPosition = new Vector3(-.75f, 0, .35f); ch.localRotation = Quaternion.Euler(0, 25, 0); v.chest = ch;
            var wood = MeshKit.Mat(new Color(.45f, .27f, .13f), .05f, .35f); var gold = MeshKit.Mat(new Color(1f, .78f, .3f), .9f, .75f, new Color(1f, .7f, .2f) * (v.f.got ? .15f : .6f));
            MeshKit.Box(ch, wood, new Vector3(0, .18f, 0), new Vector3(.62f, .34f, .4f));
            foreach (float bx in new[] { -.24f, .24f }) MeshKit.Box(ch, gold, new Vector3(bx, .18f, 0), new Vector3(.06f, .36f, .42f));
            MeshKit.Box(ch, gold, new Vector3(0, .02f, 0), new Vector3(.64f, .04f, .42f));
            if (v.f.got) MeshKit.Box(ch, MeshKit.Mat(new Color(.25f, .15f, .08f), 0, .2f), new Vector3(0, .345f, 0), new Vector3(.56f, .01f, .34f)); /* leer, Boden innen */
            var lid = new GameObject("Deckel").transform; lid.SetParent(ch, false); lid.localPosition = new Vector3(0, .35f, -.2f); v.lid = lid;
            MeshKit.Box(lid, wood, new Vector3(0, .07f, .2f), new Vector3(.62f, .14f, .4f)); MeshKit.Box(lid, wood, new Vector3(0, .155f, .2f), new Vector3(.62f, .05f, .32f));
            foreach (float bx in new[] { -.24f, .24f }) MeshKit.Box(lid, gold, new Vector3(bx, .09f, .2f), new Vector3(.06f, .16f, .42f));
            MeshKit.Box(lid, gold, new Vector3(0, .02f, .405f), new Vector3(.1f, .12f, .03f)); /* Schloss */
            if (!v.f.got) { var gl = MeshKit.Part(ch, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .8f, .35f, .5f)), new Vector3(0, .45f, 0), Vector3.one * .9f); NoShadow(gl); gl.AddComponent<Billboard>(); }
            lid.localRotation = Quaternion.Euler(v.f.got && v.openT < 0 ? -105 : 0, 0, 0);
            v.label = tm; v.got = v.f.got; v.text = null; v.look = v.f.fcol * 100 + v.f.tcol; return g;
        }
        void UpdateFlags()
        {
            if (ps == null || pd == null || ps.sv.flags == null) return; if (flagFor != ps.pid || flagViews.Count != ps.sv.flags.Count) BuildFlags();
            flagT -= Time.deltaTime; bool force = flagVer != Game.FlagVer; if (flagT > 0 && !force) { Wave(); return; } flagT = .5f; flagVer = Game.FlagVer;
            int n = 0;
            foreach (var v in flagViews)
            {
                n++; bool vis = Game.FlagVisible(ps, v.f) && !MapMode;
                if (vis && (!v.go || v.got != v.f.got || (v.got && v.look != v.f.fcol * 100 + v.f.tcol))) { bool opening = v.go && !v.got && v.f.got; if (v.go) Destroy(v.go); if (opening) v.openT = 0; v.go = MakeFlag(v); if (opening) ChestBurst(v.chest.position + v.chest.up * .4f, v.chest.up); }
                if (v.go && v.go.activeSelf != vis) v.go.SetActive(vis);
                if (v.go && vis)
                {
                    string t = v.f.got ? (string.IsNullOrEmpty(v.f.text) ? "#" + n : v.f.text) : "?";
                    if (t != v.text) { v.text = t; foreach (var tm in v.go.GetComponentsInChildren<TextMeshPro>()) tm.text = t; }
                }
            }
            Wave();
        }
        /// <summary>Deckel geht auf, goldene Funken steigen auf.</summary>
        void ChestAnim()
        {
            foreach (var v in flagViews) if (v.openT >= 0 && v.lid) { v.openT += Time.deltaTime; float k = Mathf.Clamp01(v.openT / .8f); k = 1 - (1 - k) * (1 - k) * (1 - k); v.lid.localRotation = Quaternion.Euler(-105 * k + Mathf.Sin(k * 9) * 6 * (1 - k), 0, 0); if (v.openT > .8f) v.openT = -1; }
        }
        Material chestFxMat;
        void ChestBurst(Vector3 pos, Vector3 up)
        {
            if (!chestFxMat) { chestFxMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); chestFxMat.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(chestFxMat, true); }
            var go = new GameObject("Truhenfunken"); go.transform.SetParent(transform, false); go.transform.position = pos; go.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            var p = go.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = p.main; m.loop = false; m.duration = .5f; m.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f); m.startSize = new ParticleSystem.MinMaxCurve(.06f, .16f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .85f, .35f), new Color(1f, 1f, .75f)); m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = .15f; m.stopAction = ParticleSystemStopAction.Destroy;
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30; sh.radius = .2f; sh.rotation = new Vector3(-90, 0, 0);
            var em = p.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 60) });
            var col = p.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); col.color = gr;
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial = chestFxMat; p.Play(); Sfx.PlayAt("buy", pos, 1.3f, .7f, 60);
        }
        void Wave() { ChestAnim(); float t = Time.time; for (int i = 0; i < flagViews.Count; i++) { var c = flagViews[i].cloth; if (c && c.gameObject.activeInHierarchy) c.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 1.7f + i) * 14, Mathf.Sin(t * 2.3f + i * 1.3f) * 3); } }
        /// <summary>Für das Antippen: sichtbare Flaggen mit Weltposition (Mitte des Tuchs).</summary>
        public IEnumerable<(FlagSave f, Vector3 pos)> FlagSpots() { foreach (var v in flagViews) if (v.go && v.go.activeInHierarchy) { yield return (v.f, v.go.transform.TransformPoint(new Vector3(.5f, 1.6f, 0))); if (v.chest) yield return (v.f, v.chest.position + v.chest.up * .25f); } }
    }
}
