using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ThreeDTileMap : MonoBehaviour, ITileGrid
{
    #region Private Variables

    [SerializeField]
    private TerrainTile m_tileMap = null;

    private Dictionary<Vector3Int, Tile> m_tiles = null;

    [SerializeField]
    private short m_width = 1;

    [SerializeField]
    private short m_length = 1;

    [SerializeField]
    private float tileWaitTime = .25f;

    #endregion

    #region Public Properties

    public Vector3Int origin => throw new NotImplementedException();

    public Vector3Int size => throw new NotImplementedException();

    public Bounds localBounds => throw new NotImplementedException();

    public BoundsInt cellBounds => throw new NotImplementedException();

    #endregion

    #region Private Methods

    private void Start()
    {
        m_tiles = new Dictionary<Vector3Int, Tile>();
        StartCoroutine(GenerateGridOverTime());
    }

    private IEnumerator GenerateGridOverTime()
    {
        var gridSize = m_width * m_length;
        var cellSize = m_tileMap.m_tileObjects[0].GetComponentInChildren<Renderer>().bounds.size;

        int index = 0;
        
        while (index < gridSize)
        {
            int z = index % m_length;
            int x = Mathf.FloorToInt(index / m_length);

            //var position = new Vector3Int((int)(x * cellSize.x), 0, (int)(z * cellSize.z));
            var location = new Vector3Int(x, z, 0);

            m_tiles.Add(location, new Tile(null, Matrix4x4.identity));
            //ThreeDTileData threeDTileData = new ThreeDTileData();
            //m_tileMap.GetTileData(location, this, ref threeDTileData);
            //var go = Instantiate(threeDTileData.gameObject, position, Quaternion.identity, this.transform);

            //m_tiles[location].gameObject = go;
            //m_tiles[location].transform = threeDTileData.transform;
            index++;
            //yield return new WaitForSeconds(tileWaitTime);
        }

        index = 0;
        while (index < gridSize)
        {
            int z = index % m_length;
            int x = Mathf.FloorToInt(index / m_length);

            var position = new Vector3Int((int)(-x * cellSize.x), 0, (int)(z * cellSize.z));
            var location = new Vector3Int(x, z, 0);
            ThreeDTileData threeDTileData = new ThreeDTileData();
            m_tileMap.GetTileData(location, this, ref threeDTileData);
            var go = Instantiate(threeDTileData.gameObject, position, threeDTileData.transform.rotation, this.transform);
            m_tiles[location].gameObject = go;
            m_tiles[location].transform = threeDTileData.transform;
            index++;
            yield return new WaitForSeconds(tileWaitTime);
        }
    }

    public GameObject GetGameObject(Vector3Int position)
    {
        return GetTile(position).gameObject;
    }

    public Tile GetTile(Vector3Int position)
    {
        if (m_tiles.TryGetValue(position, out Tile tile))
        {
            return tile;
        }
        return null;
        //throw new Exception($"{nameof(GetTile)}: failed to find a tile at the given position: {position}.");
    }

    public T GetTile<T>(Vector3Int position) where T : Tile
    {
        if (m_tiles.TryGetValue(position, out Tile tile))
        {
            return (T)tile;
        }
        return null;
        //throw new Exception($"{nameof(GetTile)}: failed to find a tile at the given position: {position}.");
    }

    public Matrix4x4 GetTransformMatrix(Vector3Int position)
    {
        return GetTile(position).transform;
    }

    public void RefreshTile(Vector3Int position)
    {
        var tile = GetTile(position);


        Instantiate(tile.gameObject);
    }

    #endregion
}