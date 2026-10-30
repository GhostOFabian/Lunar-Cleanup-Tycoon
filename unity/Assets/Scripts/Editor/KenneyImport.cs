// Kenney-Modelle lesbar importieren: die Förderbänder prüfen per Strahl gegen die echten Dreiecke,
// wo die Gebäudewand ist (geht im fertigen Handy-Spiel nur mit "Read/Write" an).
using UnityEditor;

namespace LunarCleanup.EditorTools
{
    public class KenneyImport : AssetPostprocessor
    {
        public override uint GetVersion() => 2;
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Kenney/")) return;
            var mi = (ModelImporter)assetImporter; mi.isReadable = true;
        }
    }
}
