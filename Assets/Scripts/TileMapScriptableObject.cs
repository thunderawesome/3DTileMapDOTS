using UnityEngine;

namespace Battlerock
{
    public class TileMapScriptableObject : ScriptableObject
    {
        public string objectName = "Tilemap";
        public Tile[] tiles;
        public float dimension = 1.0f;
    }
}