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

            GenerateMap_Normal();
        }
    }

    #endregion

    #region Private Methods   

    private void GenerateMap_Normal()
    {
        m_mapHolder = new GameObject("MapHolder");

        m_tiles = new Dictionary<Vector3Int, Tile>();
        //StartCoroutine(GenerateGridOverTime());

        var map = MapFunctions.GenerateCellularAutomata(m_width, m_length, UnityEngine.Random.Range(0, 99999), m_fillPercent, m_edgesAreWalls);
        map = MapFunctions.SmoothMooreCellularAutomata(map, m_edgesAreWalls, m_smoothCount);

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
        m_mapHolder = new GameObject("MapHolder");

        m_tiles = new Dictionary<Vector3Int, Tile>();

        var map = MapFunctions.GenerateCellularAutomata(m_width, m_length, UnityEngine.Random.Range(0, 99999), m_fillPercent, m_edgesAreWalls);
        map = MapFunctions.SmoothMooreCellularAutomata(map, m_edgesAreWalls, m_smoothCount);

        EntityManager entityManager = World.Active.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(Scale),
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
                    entityManager.SetComponentData(entity,
                        new Scale
                        {
                            Value = 1
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