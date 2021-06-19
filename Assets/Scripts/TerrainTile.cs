using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

[Flags]
public enum Neighbor
{
    North = 1 << 0,
    NorthEast = 1 << 1,
    East = 1 << 2,
    SouthEast = 1 << 3,
    South = 1 << 4,
    SouthWest = 1 << 5,
    West = 1 << 6,
    NorthWest = 1 << 7
}

[Serializable]
[CreateAssetMenu(fileName = "New Terrain Tile", menuName = "Tiles/Terrain Tile")]
public class TerrainTile : ScriptableObject
{
    [SerializeField]
    private GameObject[] m_objects;

    public GameObject[] Objects { get => m_objects; set => m_objects = value; }

    public void RefreshTile(Vector3Int location, ITileGrid tileMap)
    {
        for (int yd = -1; yd <= 1; yd++)
        {
            for (int xd = -1; xd <= 1; xd++)
            {
                Vector3Int position = new Vector3Int(location.x + xd, location.y + yd, location.z);
                if (TileValue(tileMap, position))
                {
                    tileMap.RefreshTile(position);
                }
            }
        }
    }

    public void GetTileData(Vector3Int location, ITileGrid tileMap, ref ThreeDTileData tileData)
    {
        UpdateTile(location, tileMap, ref tileData);
    }

    private void UpdateTile(Vector3Int location, ITileGrid tileMap, ref ThreeDTileData tileData)
    {
        tileData.transform = Matrix4x4.identity;

        int mask = TileValue(tileMap, location + new Vector3Int(0, 1, 0)) ? (int)Neighbor.North : 0;        // North
        mask += TileValue(tileMap, location + new Vector3Int(1, 1, 0)) ? (int)Neighbor.NorthEast : 0;       // NorthEast
        mask += TileValue(tileMap, location + new Vector3Int(1, 0, 0)) ? (int)Neighbor.East : 0;            // East
        mask += TileValue(tileMap, location + new Vector3Int(1, -1, 0)) ? (int)Neighbor.SouthEast : 0;      // SouthEast
        mask += TileValue(tileMap, location + new Vector3Int(0, -1, 0)) ? (int)Neighbor.South : 0;          // South
        mask += TileValue(tileMap, location + new Vector3Int(-1, -1, 0)) ? (int)Neighbor.SouthWest : 0;     // SouthWest
        mask += TileValue(tileMap, location + new Vector3Int(-1, 0, 0)) ? (int)Neighbor.West : 0;           // West
        mask += TileValue(tileMap, location + new Vector3Int(-1, 1, 0)) ? (int)Neighbor.NorthWest : 0;      // NorthWest

        Debug.Log($"{(Neighbor)mask} ---- {mask}");

        byte original = (byte)mask;
        if ((original | 254) < 255) { mask = mask & 125; }
        if ((original | 251) < 255) { mask = mask & 245; }
        if ((original | 239) < 255) { mask = mask & 215; }
        if ((original | 191) < 255) { mask = mask & 95; }

        int index = GetIndex((byte)mask);
        if (index >= 0 && index < Objects.Length && TileValue(tileMap, location))
        {
            tileData.gameObject = Objects[index];
            tileData.transform = GetTransform((byte)mask);
        }
    }

    private bool TileValue(ITileGrid tileMap, Vector3Int position)
    {
        Tile tile = tileMap.GetTile(position);
        return (tile != null);
    }

    private int GetIndex(byte mask)
    {
        switch (mask)
        {
            case 0: return 0;
            case 1:
            case 4:
            case 16:
            case 64: return 1;
            case 5:
            case 20:
            case 80:
            case 65: return 2;
            case 7:
            case 28:
            case 112:
            case 193: return 3;
            case 17:
            case 68: return 4;
            case 21:
            case 84:
            case 81:
            case 69: return 5;
            case 23:
            case 92:
            case 113:
            case 197: return 6;
            case 29:
            case 116:
            case 209:
            case 71: return 7;
            case 31:
            case 124:
            case 241:
            case 199: return 8;
            case 85: return 9;
            case 87:
            case 93:
            case 117:
            case 213: return 10;
            case 95:
            case 125:
            case 245:
            case 215: return 11;
            case 119:
            case 221: return 12;
            case 127:
            case 253:
            case 247:
            case 223: return 13;
            case 255: return 14;
        }
        return -1;
    }

    private Matrix4x4 GetTransform(byte mask)
    {
        switch (mask)
        {
            case 4:
            case 20:
            case 28:
            case 68:
            case 84:
            case 92:
            case 116:
            case 124:
            case 93:
            case 125:
            case 221:
            case 253:
                return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            case 16:
            case 80:
            case 112:
            case 81:
            case 113:
            case 209:
            case 241:
            case 117:
            case 245:
            case 247:
                return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, -180f, 0f), Vector3.one);
            case 64:
            case 65:
            case 193:
            case 69:
            case 197:
            case 71:
            case 199:
            case 213:
            case 215:
            case 223:
                return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, -270f, 0f), Vector3.one);
        }
        return Matrix4x4.identity;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainTile))]
public class TerrainTileEditor : Editor
{
    private TerrainTile tile { get { return (target as TerrainTile); } }

    public void OnEnable()
    {
        if (tile.Objects == null || tile.Objects.Length != 15)
        {
            tile.Objects = new GameObject[15];
            EditorUtility.SetDirty(tile);
        }
    }


    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Place GameObjects shown based on the contents of the prefab.");
        EditorGUILayout.Space();

        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 210;

        EditorGUI.BeginChangeCheck();
        tile.Objects[0] = (GameObject)EditorGUILayout.ObjectField("Filled", tile.Objects[0], typeof(GameObject), false, null);
        tile.Objects[1] = (GameObject)EditorGUILayout.ObjectField("Three Sides", tile.Objects[1], typeof(GameObject), false, null);
        tile.Objects[2] = (GameObject)EditorGUILayout.ObjectField("Two Sides and One Corner", tile.Objects[2], typeof(GameObject), false, null);
        tile.Objects[3] = (GameObject)EditorGUILayout.ObjectField("Two Adjacent Sides", tile.Objects[3], typeof(GameObject), false, null);
        tile.Objects[4] = (GameObject)EditorGUILayout.ObjectField("Two Opposite Sides", tile.Objects[4], typeof(GameObject), false, null);
        tile.Objects[5] = (GameObject)EditorGUILayout.ObjectField("One Side and Two Corners", tile.Objects[5], typeof(GameObject), false, null);
        tile.Objects[6] = (GameObject)EditorGUILayout.ObjectField("One Side and One Lower Corner", tile.Objects[6], typeof(GameObject), false, null);
        tile.Objects[7] = (GameObject)EditorGUILayout.ObjectField("One Side and One Upper Corner", tile.Objects[7], typeof(GameObject), false, null);
        tile.Objects[8] = (GameObject)EditorGUILayout.ObjectField("One Side", tile.Objects[8], typeof(GameObject), false, null);
        tile.Objects[9] = (GameObject)EditorGUILayout.ObjectField("Four Corners", tile.Objects[9], typeof(GameObject), false, null);
        tile.Objects[10] = (GameObject)EditorGUILayout.ObjectField("Three Corners", tile.Objects[10], typeof(GameObject), false, null);
        tile.Objects[11] = (GameObject)EditorGUILayout.ObjectField("Two Adjacent Corners", tile.Objects[11], typeof(GameObject), false, null);
        tile.Objects[12] = (GameObject)EditorGUILayout.ObjectField("Two Opposite Corners", tile.Objects[12], typeof(GameObject), false, null);
        tile.Objects[13] = (GameObject)EditorGUILayout.ObjectField("One Corner", tile.Objects[13], typeof(GameObject), false, null);
        tile.Objects[14] = (GameObject)EditorGUILayout.ObjectField("Empty", tile.Objects[14], typeof(GameObject), false, null);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(tile);

        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}
#endif