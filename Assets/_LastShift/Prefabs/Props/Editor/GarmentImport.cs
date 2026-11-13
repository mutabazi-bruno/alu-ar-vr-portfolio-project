using UnityEditor;

namespace LastShift.EditorTools
{
    // Import settings for Habimana's clothes models: no rig or animation (they only hang on hooks),
    // and no materials from the files, since the prop builder gives each garment its own.
    public class GarmentImport : AssetPostprocessor
    {
        public const string Folder = "Assets/_LastShift/Prefabs/Props/Garments";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder))
                return;
            var model = (ModelImporter)assetImporter;
            model.animationType = ModelImporterAnimationType.None;
            model.importAnimation = false;
            model.importCameras = false;
            model.importLights = false;
            model.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
                return;
            var texture = (TextureImporter)assetImporter;
            if (assetPath.EndsWith("_normal.png"))
                texture.textureType = TextureImporterType.NormalMap;
            texture.maxTextureSize = 2048;
        }
    }
}
