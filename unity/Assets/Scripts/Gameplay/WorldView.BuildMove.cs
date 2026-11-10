// Fabian 03.11.: Gebäude frei verschieben – Gebäude antippen → „Verschieben“, dann mit dem Finger ziehen.
// Grüner Ring = passt, roter Ring = geht nicht (außerhalb des Basisgeländes oder zu nah an einem anderen Gebäude). „Fertig“ verlegt die Bänder neu.
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        public string MoveId; public bool Moving => MoveId != null; Vector2 movePos; string moveWhy = ""; Transform moveRing; Material moveRingMat;
        public string MoveWhyText => moveWhy; public bool MoveValid => Moving && moveWhy == "";
        string MoveGroup => MoveId != null && Game.IsUnit(MoveId) ? "u1" : MoveId;
        float MoveYaw() { float y = ps.lay.yaw.TryGetValue(MoveGroup, out var yy) ? yy : 0; return Game.IsUnit(MoveId) ? y : Mathf.Round(y / (Mathf.PI / 2)) * (Mathf.PI / 2); }
        public void BeginMove(string id)
        {
            EndMove(); if (!ps.lay.pos.ContainsKey(id) || !blds.ContainsKey(Game.IsUnit(id) ? "u1" : id)) return; MoveId = id; movePos = ps.lay.pos[id]; moveWhy = "";
            moveRingMat = GlowMat(MeshKit.GlowTex(), new Color(.4f, 1f, .6f, .8f)); var g = MeshKit.Part(transform, MeshKit.Quad, moveRingMat, Vector3.zero, Vector3.one); NoShadow(g); moveRing = g.transform; MoveTo(movePos);
            Sfx.Play("click", 1.1f, .5f);
        }
        public void MoveTo(Vector2 pl)
        {
            if (!Moving) return; pl.x = Mathf.Clamp(pl.x, -GOFF + 2, GOFF - 2); pl.y = Mathf.Clamp(pl.y, -GOFF + 2, GOFF - 2); movePos = pl; moveWhy = Game.MoveWhy(ps, MoveId, pl);
            if (blds.TryGetValue(MoveGroup, out var bp) && bp.root) Place(bp.root.transform, pl, MoveYaw(), .25f); /* leicht angehoben, solange es „in der Hand“ ist */
            if (moveRing) { var d = pd.PlanDir(pl); moveRing.position = pd.Surf(d, .15f); moveRing.rotation = Quaternion.LookRotation(-d) ; moveRing.localScale = Vector3.one * GameData.Bld[MoveId].fp * 3.2f; }
            if (moveRingMat) moveRingMat.SetColor("_BaseColor", moveWhy == "" ? new Color(.4f, 1f, .6f, .85f) : new Color(1f, .35f, .3f, .9f));
        }
        public bool CommitMove() { if (!MoveValid) return false; var id = MoveId; var p = movePos; EndMove(false); return Game.MoveBuilding(ps, id, p); }
        public void ResetMove() { var id = MoveId; EndMove(); if (id != null) Game.ResetBuilding(ps, id); }
        public void EndMove(bool restore = true)
        {
            if (Moving && restore && blds.TryGetValue(MoveGroup, out var bp) && bp.root && ps.lay.pos.ContainsKey(MoveGroup)) Place(bp.root.transform, ps.lay.pos[MoveGroup], MoveYaw());
            if (moveRing) Destroy(moveRing.gameObject); if (moveRingMat) Destroy(moveRingMat); moveRing = null; moveRingMat = null; MoveId = null;
        }
    }
}
