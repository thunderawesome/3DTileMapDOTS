using UnityEngine;

namespace Battlerock
{
    public class TileMapScriptableObject : ScriptableObject
    {
        public string objectName = "Tilemap";
        public GameObject[] tiles;
    }
}