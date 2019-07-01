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

    [Header("Map Visuals")]
    [SerializeField]
    private TerrainTile m_tileMap = null;

    private GameObject m_mapHolder = null;

    [SerializeField]
    private Mesh m_mesh = null;

    [SerializeField]
    private Material m_material = null;

    private Dictionary<Vector3Int, Tile> m_tiles = null;

    [Header("Map Settings")]
    [SerializeField]
    private short m_width = 1;

    [SerializeField]
    private short m_length = 1;

    [SerializeField]
    private bool m_flipXAxis = false;

    [SerializeField]
    private bool m_flipZAxis = false;

    [SerializeField]
    private bool m_edgesAreWalls = false;

    [SerializeField]
    private int m_smoothCount = 1;

    [SerializeField]
    private int m_fillPercent = 75;

    [Header("Random Walk")]
    [SerializeField]
    private int m_minSectionWidth = 4;

    [Header("Tunnel/River")]
    [SerializeField]
    private int m_numberOfRivers = 1;

    [Range(0, 10)]
    [SerializeField]
    private int m_minPathWidth = 2;

    [Range(0, 10)]
    [SerializeField]
    private int m_maxPathWidth = 4;

    [SerializeField]
    private int m_maxPathChange = 4;

    [SerializeField]
    private int m_roughness = 4;

    [SerializeField]
    private int m_windyness = 4;

    [Header("Generate Over Time")]
    [SerializeField]
    private float m_tileWaitTime = .25f;

    [Header("Algorithms")]
    [SerializeField]
    private bool m_randomWalkTopEnabled = false;

    [SerializeField]
    private bool m_randomWalkTopSmoothedEnabled = false;

    [SerializeField]
    private bool m_directionTunnelEnabled = false;

    private EntityManager m_entityManager;

    private NativeArray<Entity> m_entityArray;

    #endregion

    #region Public Properties

    public Vector3Int origin => throw new NotImplementedException();

    public Vector3Int size => throw new NotImplementedException();

    public Bounds localBounds => throw new NotImplementedException();

    public BoundsInt cellBounds => throw new NotImplementedException();

    #endregion

    #region Unity Methods

    private void Start()
    {
        //GenerateMap_Normal();
        GenerateMap_DOTS();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (m_mapHolder != null)
            {
                Destroy(m_mapHolder);
            }

            //GenerateMap_Normal();
            GenerateMap_DOTS();
        }
    }

    #endregion

    #region Private Methods   

    private void GenerateMap_Normal()
    {
        m_mapHolder = new GameObject("MapHolder");
        m_tiles = new Dictionary<Vector3Int, Tile>();

        var seed = UnityEngine.Random.Range(0, 99999);
        var map = MapFunctions.GenerateCellularAutomata(m_width, m_length, seed, m_fillPercent, m_edgesAreWalls);
        map = MapFunctions.SmoothMooreCellularAutomata(map, m_edgesAreWalls, m_smoothCount);
        if (m_randomWalkTopEnabled == true)
        {
            map = MapFunctions.RandomWalkTop(map, seed);
            if (m_randomWalkTopSmoothedEnabled == true)
            {
                map = MapFunctions.RandomWalkTopSmoothed(map, seed, m_minSectionWidth);
            }
        }

        for (int i = 0; i < m_numberOfRivers; i++)
        {
            if (m_directionTunnelEnabled == true)
            {
                var rand = UnityEngine.Random.Range(0, 2) * 2 - 1;
                map = MapFunctions.DirectionalTunnel(map, m_minPathWidth, m_maxPathWidth, m_maxPathChange * rand, m_roughness, m_windyness, UnityEngine.Random.Range(m_width/4, m_width-(m_width / 4 )- 1));
            }
        }        

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

                    var go = Instantiate(threeDTileData.gameObject, position, threeDTileData.transform.rotation, m_mapHolder.transform);
                    m_tiles[location].gameObject = go;

                    m_tiles[location].transform = threeDTileData.transform;
                }
            }
        }
    }

    private void GenerateMap_DOTS()
    {
        m_tiles = new Dictionary<Vector3Int, Tile>();

        var seed = UnityEngine.Random.Range(0, 99999);
        var map = MapFunctions.GenerateCellularAutomata(m_width, m_length, seed, m_fillPercent, m_edgesAreWalls);
        map = MapFunctions.SmoothMooreCellularAutomata(map, m_edgesAreWalls, m_smoothCount);

        if (m_randomWalkTopEnabled == true)
        {
            map = MapFunctions.RandomWalkTop(map, seed);
            if (m_randomWalkTopSmoothedEnabled == true)
            {
                map = MapFunctions.RandomWalkTopSmoothed(map, seed, m_minSectionWidth);
            }
        }

        for (int i = 0; i < m_numberOfRivers; i++)
        {
            if (m_directionTunnelEnabled == true)
            {
                var rand = UnityEngine.Random.Range(0, 2) * 2 - 1;
                map = MapFunctions.DirectionalTunnel(map, m_minPathWidth, m_maxPathWidth, m_maxPathChange * rand, m_roughness, m_windyness, UnityEngine.Random.Range(m_width / 4, m_width - (m_width / 4) - 1));
            }
        }

        if (m_entityArray != null && m_entityArray.Length > 0)
        {
            for (int i = 0; i < m_entityArray.Length; i++)
            {
                var entity = m_entityArray[i];
            }
        }

        CreateEntities(map.Length, out m_entityManager, out m_entityArray);

        var cellSize = m_tileMap.m_tileObjects[0].GetComponentInChildren<Renderer>().bounds.size;
        InitializeMapLayout(map);
        UpdateMapLayout(map, cellSize);
        m_entityArray.Dispose();
    }

    private void InitializeMapLayout(int[,] map)
    {
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
    }

    private void UpdateMapLayout(int[,] map, Vector3 cellSize)
    {
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
                    UpdateEntities(index, position, threeDTileData);

                    index++;
                }
            }
        }
    }

    private void UpdateEntities(int index, Vector3Int position, ThreeDTileData threeDTileData)
    {
        Entity entity = m_entityArray[index];

        m_entityManager.SetComponentData(entity,
            new Translation
            {
                Value = new float3(position.x, 0, position.z)
            });
        m_entityManager.SetComponentData(entity,
            new Rotation
            {
                Value = new quaternion(threeDTileData.transform)
            });
        m_entityManager.SetComponentData(entity,
            new Scale
            {
                Value = 1
            });
        m_entityManager.SetSharedComponentData(entity,
            new RenderMesh
            {
                mesh = threeDTileData.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh,
                material = m_material
            });
    }

    private static void CreateEntities(int size, out EntityManager entityManager, out NativeArray<Entity> entityArray)
    {
        entityManager = World.Active.EntityManager;
        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(Scale),
            typeof(RenderMesh),
            typeof(LocalToWorld));

        entityArray = new NativeArray<Entity>(size, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);
    }

    #endregion

    #region Interface Methods

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