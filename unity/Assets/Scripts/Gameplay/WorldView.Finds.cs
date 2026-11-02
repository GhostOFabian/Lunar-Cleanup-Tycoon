// Fabian 02.11.: Sichtbarer Fund zum Freiwischen – Truhe (eigenes Modell, sonst Kiste) unter einem Staubhügel, goldenes Leuchten.
// Beim Wischen schrumpft der Staubhügel, das Leuchten wird heller; geborgen = Funkenregen.
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        Transform findRoot, findDust, findGlow; Material findGlowMat, findDustMat; HandFind findShown;
        void UpdateFind()
        {
            var f = Game.Find; if (f != null && (f.pid != ps.pid)) f = null;
            if (f != findShown) { ClearFind(); findShown = f; if (f != null) BuildFind(f); }
            else if (f != null && !findRoot) BuildFind(f); /* Basis wurde neu aufgebaut */
            if (f == null || !findRoot) return;
            float p = Game.FindProgress, pulse = .75f + .25f * Mathf.Sin(Time.time * 5f);
            findDust.localScale = Vector3.one * Mathf.Lerp(2.6f, .2f, p) ; findDust.gameObject.SetActive(p < .98f);
            var cam = Camera.main; if (cam) { findGlow.rotation = Quaternion.LookRotation(findGlow.position - cam.transform.position, cam.transform.up); findGlow.localScale = Vector3.one * (1.6f + 3f * p) * pulse; }
            if (findGlowMat) findGlowMat.SetColor("_BaseColor", new Color(1f, .82f, .35f, .35f + .55f * p));
            if (dust && Random.value < .15f + p) dust.Emit(new ParticleSystem.EmitParams { position = findRoot.position + findRoot.up * .6f + Random.insideUnitSphere * .8f, velocity = findRoot.up * Random.Range(.5f, 2f), startColor = new Color(1f, .85f, .4f, .9f), startSize = Random.Range(.15f, .35f), startLifetime = 1f }, 1);
        }
        void BuildFind(HandFind f)
        {
            var at = pd.Surf(f.dir, 0); findRoot = New("Fund"); findRoot.position = at; findRoot.rotation = Quaternion.FromToRotation(Vector3.up, f.dir);
            var model = LcKit.Has("Welt/truhe") ? LcKit.Spawn("Welt/truhe", findRoot, Vector3.zero, Random.Range(0f, 360f), GameData.Hex("#ffbf4d")) : null;
            if (!model) { var box = MeshKit.Part(findRoot, MeshKit.Cube, MeshKit.Mat(new Color(.55f, .38f, .2f), .1f, .3f, new Color(.3f, .2f, .05f)), new Vector3(0, .4f, 0), new Vector3(1.2f, .8f, .8f)); NoShadow(box); }
            findDustMat = MeshKit.Mat(new Color(.42f, .36f, .3f), 0, .1f, new Color(.25f, .18f, .05f)); /* leicht glimmender Staubhügel */ var d = MeshKit.Part(findRoot, MeshKit.Sphere, findDustMat, new Vector3(0, .1f, 0), Vector3.one * 2.6f); d.transform.localScale = new Vector3(2.6f, 1.2f, 2.6f); findDust = d.transform;
            findGlowMat = GlowMat(MeshKit.GlowTex(), new Color(1f, .82f, .35f, .5f)); var g = MeshKit.Part(findRoot, MeshKit.Quad, findGlowMat, new Vector3(0, 1f, 0), Vector3.one * 3); NoShadow(g); findGlow = g.transform;
            Sfx.PlayAt("find", at, 1, 1, 200);
        }
        void ClearFind()
        {
            if (findRoot)
            {
                if (dust && findShown != null && findShown.prog >= 1) for (int q = 0; q < 60; q++) dust.Emit(new ParticleSystem.EmitParams { position = findRoot.position + findRoot.up, velocity = (findRoot.up * 2 + Random.insideUnitSphere) * Random.Range(2f, 6f), startColor = new Color(1f, .85f, .4f), startSize = Random.Range(.2f, .5f), startLifetime = 1.4f }, 1);
                Destroy(findRoot.gameObject);
            }
            if (findGlowMat) Destroy(findGlowMat); if (findDustMat) Destroy(findDustMat); findRoot = null; findShown = null;
        }
    }
}
