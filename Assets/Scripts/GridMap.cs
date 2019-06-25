using Battlerock;
using System;
using System.Collections;
using UnityEngine;

[Flags]
public enum Direction
{
    //NorthWest = 1 << 0,
    //North = 1 << 1,
    //NorthEast = 1 << 2,
    //West = 1 << 3,
    //East = 1 << 4,
    //SouthWest = 1 << 5,
    //South = 1 << 6,
    //SouthEast = 1 << 7,
    North = 1 << 0,
    West = 1 << 3,
    South = 1 << 2,
    East = 1 << 1
}

public class GridMap : MonoBehaviour
{
    #region Private Variables

    [SerializeField]
    private TileMapScriptableObject m_tileMap = null;

    [SerializeField]
    private short m_width = 1;

    [SerializeField]
    private short m_length = 1;

    [SerializeField]
    private float tileWaitTime = .25f;

    [SerializeField]
    private int[,] m_tiles = null;

    private int[,] m_offsets = new int[,]
    {
        { -1, -1 },
        { 0, -1 },
        { 1, -1 },
        { -1, 0 },
        { 1, 0 },
        { -1, 1 },
        { 0, 1 },
        { 1, 1 }
    };

    #endregion

    #region Public Properties

    public TileMapScriptableObject TileMap { get => m_tileMap; }

    #endregion

    #region Private Methods

    private void Start()
    {
        StartCoroutine(GenerateGridOverTime());
    }

    private IEnumerator GenerateGridOverTime()
    {
        var gridSize = m_width * m_length;
        var cellSize = m_tileMap.tiles[0].obj.GetComponent<Renderer>().bounds.size;

        int index = 0;

        m_tiles = new int[m_width, m_length];

        while (index < gridSize)
        {
            int x = index % m_length;
            int z = Mathf.FloorToInt(index / m_length);

            m_tiles[z, x] = 0;
            index++;
        }

        index = 0;

        while (index < gridSize)
        {
            int x = index % m_length;
            int z = Mathf.FloorToInt(index / m_length);

            var tile = m_tiles[z, x];

            var position = new Vector3(x * cellSize.x, 0, -z * cellSize.z);

            var directions = (Direction)index;

            bool east = false;
            bool west = false;
            bool north = false;
            bool south = false;
            bool northWest = false;
            bool northEast = false;
            bool southWest = false;
            bool southEast = false;

            if ((x > 0 && z > 0) && (x < m_length && z < m_width))
            {

                east = (directions & Direction.East) == Direction.East;
                west = (directions & Direction.West) == Direction.West;
                south = (directions & Direction.South) == Direction.South;
                north = (directions & Direction.North) == Direction.North;
                //northEast = (directions & Direction.NorthEast) == Direction.NorthEast;
                //northWest = (directions & Direction.NorthWest) == Direction.NorthWest;
                //southEast = (directions & Direction.SouthEast) == Direction.SouthEast;
                //southWest = (directions & Direction.SouthWest) == Direction.SouthWest;
            }

            var dir = CalculateTileFlags(east, west, north, south, northWest, northEast, southWest, southEast);
            var tileIndex = (int)dir;

            Debug.Log($"{nameof(tileIndex)}: {tileIndex}");

            Instantiate(m_tileMap.tiles[tileIndex].obj, position, Quaternion.identity, this.transform);
            yield return new WaitForSeconds(tileWaitTime);
            index++;
        }
    }

    private static Direction CalculateTileFlags(bool east, bool west, bool north, bool south, bool northWest, bool northEast, bool southWest, bool southEast)
    {
        var direction = (east ? Direction.East : 0) | (west ? Direction.West : 0) | (north ? Direction.North : 0) | (south ? Direction.South : 0);
        //direction |= ((north && west) && northWest) ? Direction.NorthWest : 0;
        //direction |= ((north && east) && northEast) ? Direction.NorthEast : 0;
        //direction |= ((south && west) && southWest) ? Direction.SouthWest : 0;
        //direction |= ((south && east) && southEast) ? Direction.SouthEast : 0;

        Debug.Log($"{nameof(CalculateTileFlags)}: {nameof(direction)}: {direction}");
        return direction;
    }

    #endregion
}