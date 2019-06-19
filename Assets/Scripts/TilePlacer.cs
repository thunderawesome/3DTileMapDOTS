using Battlerock;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TilePlacer : MonoBehaviour
{
    [Flags]
    public enum Directions
    {
        NorthWest = 1 << 0,
        North = 1 << 1,
        NorthEast = 1 << 2,
        West = 1 << 3,
        East = 1 << 4,
        SouthWest = 1 << 5,
        South = 1 << 6,
        SouthEast = 1 << 7,
    }

    public TileMapScriptableObject tileMap;
    public GridMap gridMap;

    [SerializeField]
    private GameObject m_currentTile;

    private static Directions CalculateTileFlags(bool east, bool west, bool north, bool south, bool northWest, bool northEast, bool southWest, bool southEast)
    {
        var directions = (east ? Directions.East : 0) | (west ? Directions.West : 0) | (north ? Directions.North : 0) | (south ? Directions.South : 0);
        directions |= ((north && west) && northWest) ? Directions.NorthWest : 0;
        directions |= ((north && east) && northEast) ? Directions.NorthEast : 0;
        directions |= ((south && west) && southWest) ? Directions.SouthWest : 0;
        directions |= ((south && east) && southEast) ? Directions.SouthEast : 0;
        return directions;
    }

    private void Start()
    {
        TestTileDirections();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_currentTile = tileMap.tiles[0];
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            m_currentTile = tileMap.tiles[1];
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            m_currentTile = tileMap.tiles[2];
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            m_currentTile = null;
        }
    }

    private void PlaceTile()
    {
        Vector3Int cellPosition = gridMap.GridLayout.WorldToCell(transform.position);
        transform.position = gridMap.GridLayout.CellToWorld(cellPosition);
    }

    private static void TestTileDirections()
    {
        bool east, west, north, south, northWest, northEast, southWest, southEast;
        var output = new HashSet<Directions>();

        for (var i = 0; i <= 255; i++)
        {
            var directions = (Directions)i;
            east = (directions & Directions.East) == Directions.East;
            west = (directions & Directions.West) == Directions.West;
            south = (directions & Directions.South) == Directions.South;
            north = (directions & Directions.North) == Directions.North;
            northEast = (directions & Directions.NorthEast) == Directions.NorthEast;
            northWest = (directions & Directions.NorthWest) == Directions.NorthWest;
            southEast = (directions & Directions.SouthEast) == Directions.SouthEast;
            southWest = (directions & Directions.SouthWest) == Directions.SouthWest;

            output.Add(CalculateTileFlags(east, west, north, south, northWest, northEast, southWest, southEast));
        }

        var counter = 0;

        Debug.Log($"Total items {output.Count}");

        foreach (var item in output)
        {
            Debug.Log($"Item {counter} directions {(int)item}");
            counter++;
        }
    }
}
