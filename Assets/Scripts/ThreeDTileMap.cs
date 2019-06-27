using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


public class ThreeDTileMap : MonoBehaviour, ITileGrid
{
    #region Private Variables

    [SerializeField]
    private TerrainTile m_tileMap = null;

    private GameObject m_mapHolder = null;

    [SerializeField]
    private Mesh m_mesh = null;

    [SerializeField]
    private Material m_material = null;

    private Dictionary<Vector3Int, Tile> m_tiles = null;

    [SerializeField]
    private short m_width = 1;

    [SerializeField]
    private short m_length = 1;

    [SerializeField]
    private float tileWaitTime = .25f;

    [SerializeField]
    private bool m_edgesAreWalls = false;

    [SerializeField]
    private int m_smoothCount = 1;

    [SerializeField]
    private int m_fillPercent = 75;

    [SerializeField]
    private bool m_flipXAxis = false;

    [SerializeField]
    private bool m_flipZAxis = false;

    #endregion

    #region Public Properties

    public Vector3Int origin => throw new NotImplementedException();

    public Vector3Int size => throw new NotImplementedException();

    public Bounds localBounds => throw new NotImplementedException();

    public BoundsInt cellBounds => throw new NotImplementedException();

    #endregion

    #region Private Methods

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (m_mapHolder != null)
            {
                Destroy(m_mapHolder);
            }

            GenerateMap();
        }
    }

    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        m_mapHolder = new GameObject("MapHolder");

        m_tiles = new Dictionary<Vector3Int, Tile>();

        var map = GenerateCellularAutomata(m_width, m_length, UnityEngine.Random.Range(0, 99999), m_fillPercent, m_edgesAreWalls);
        map = SmoothMooreCellularAutomata(map, m_edgesAreWalls, m_smoothCount);

        EntityManager entityManager = World.Active.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(LocalToWorld));

        NativeArray<Entity> entityArray = new NativeArray<Entity>(map.Length, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);

        var cellSize = m_tileMap.m_tileObjects[0].GetComponentInChildren<Renderer>().bounds.size;

        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            for (int y = 0; y < map.GetUpperBound(1); y++)
            {
                if (map[x, y] != 0)
                {
                    var location = new Vector3Int(x, y, 0);
                    m_tiles.Add(location, new Tile(null, Matrix4x4.identity));
                }
            }
        }

        int index = 0;
        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            for (int y = 0; y < map.GetUpperBound(1); y++)
            {
                if (map[x, y] != 0)
                {
                    var location = new Vector3Int(x, y, 0);

                    int xDir = (int)(x * cellSize.x);
                    int zDir = (int)(y * cellSize.z);
                    if (m_flipXAxis == true)
                    {
                        xDir *= -1;
                    }

                    if (m_flipZAxis == true)
                    {
                        zDir *= -1;
                    }

                    var position = new Vector3Int(xDir, 0, zDir);

                    ThreeDTileData threeDTileData = new ThreeDTileData();
                    m_tileMap.GetTileData(location, this, ref threeDTileData);

                    m_tiles[location].transform = threeDTileData.transform;

                    Entity entity = entityArray[index];

                    entityManager.SetComponentData(entity,
                        new Translation
                        {
                            Value = new float3(position.x, 0, position.z)
                        });
                    entityManager.SetComponentData(entity,
                        new Rotation
                        {
                            Value = new quaternion(threeDTileData.transform)
                        });
                    entityManager.SetSharedComponentData(entity,
                        new RenderMesh
                        {
                            mesh = threeDTileData.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh,
                            material = m_material
                        });

                    index++;
                }
            }
        }

        entityArray.Dispose();
    }

    public static int[,] GenerateCellularAutomata(int width, int height, float seed, int fillPercent, bool edgesAreWalls)
    {
        //Seed our random number generator
        System.Random rand = new System.Random(seed.GetHashCode());

        //Initialise the map
        int[,] map = new int[width, height];

        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            for (int y = 0; y < map.GetUpperBound(1); y++)
            {
                //If we have the edges set to be walls, ensure the cell is set to on (1)
                if (edgesAreWalls && (x == 0 || x == map.GetUpperBound(0) - 1 || y == 0 || y == map.GetUpperBound(1) - 1))
                {
                    map[x, y] = 1;
                }
                else
                {
                    //Randomly generate the grid
                    map[x, y] = (rand.Next(0, 100) < fillPercent) ? 1 : 0;
                }
            }
        }
        return map;
    }

    public static int[,] SmoothMooreCellularAutomata(int[,] map, bool edgesAreWalls, int smoothCount)
    {
        for (int i = 0; i < smoothCount; i++)
        {
            for (int x = 0; x < map.GetUpperBound(0); x++)
            {
                for (int y = 0; y < map.GetUpperBound(1); y++)
                {
                    int surroundingTiles = GetMooreSurroundingTiles(map, x, y, edgesAreWalls);

                    if (edgesAreWalls && (x == 0 || x == (map.GetUpperBound(0) - 1) || y == 0 || y == (map.GetUpperBound(1) - 1)))
                    {
                        //Set the edge to be a wall if we have edgesAreWalls to be true
                        map[x, y] = 1;
                    }
                    //The default moore rule requires more than 4 neighbours
                    else if (surroundingTiles > 4)
                    {
                        map[x, y] = 1;
                    }
                    else if (surroundingTiles < 4)
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }
        //Return the modified map
        return map;
    }

    static int GetMooreSurroundingTiles(int[,] map, int x, int y, bool edgesAreWalls)
    {
        /* Moore Neighbourhood looks like this ('T' is our tile, 'N' is our neighbours)
         * 
         * N N N
         * N T N
         * N N N
         * 
         */

        int tileCount = 0;

        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < map.GetUpperBound(0) && neighbourY >= 0 && neighbourY < map.GetUpperBound(1))
                {
                    //We don't want to count the tile we are checking the surroundings of
                    if (neighbourX != x || neighbourY != y)
                    {
                        tileCount += map[neighbourX, neighbourY];
                    }
                }
            }
        }
        return tileCount;
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