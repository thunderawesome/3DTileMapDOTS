using Battlerock;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TilePlacer : MonoBehaviour
{
    public GridMap gridMap;

    [SerializeField]
    private GameObject m_currentTile;

    private static Direction CalculateTileFlags(bool east, bool west, bool north, bool south, bool northWest, bool northEast, bool southWest, bool southEast)
    {
        var directions = (east ? Direction.East : 0) | (west ? Direction.West : 0) | (north ? Direction.North : 0) | (south ? Direction.South : 0);
        //directions |= ((north && west) && northWest) ? Direction.NorthWest : 0;
        //directions |= ((north && east) && northEast) ? Direction.NorthEast : 0;
        //directions |= ((south && west) && southWest) ? Direction.SouthWest : 0;
        //directions |= ((south && east) && southEast) ? Direction.SouthEast : 0;
        return directions;
    }  
}
