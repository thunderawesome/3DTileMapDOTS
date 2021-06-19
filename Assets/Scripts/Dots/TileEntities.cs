using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class TileEntities : MonoBehaviour
{

    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;


    // Start is called before the first frame update
    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld));

        NativeArray<Entity> entityArray = new NativeArray<Entity>(50000, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);

        for (int i = 0; i < entityArray.Length; i++)
        {
            Entity entity = entityArray[i];
           
            entityManager.SetComponentData(entity,
                new Translation
                {
                    Value = new float3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-5f, 5f), 0)
                });
            entityManager.SetSharedComponentData(entity,
                new RenderMesh
                {
                    mesh = mesh,
                    material = material
                });
        }

        entityArray.Dispose();
    }
}