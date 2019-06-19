using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Battlerock
{
    public class CreateScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Battlerock/Create/ScriptableObject")]
        public static void CreateMyAsset()
        {
            TileMapScriptableObject asset = ScriptableObject.CreateInstance<TileMapScriptableObject>();

            var directory = "Assets/ScriptableAssets/";
            System.IO.Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(asset, directory + asset.GetType().ToString() + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }

#endif
    }
}