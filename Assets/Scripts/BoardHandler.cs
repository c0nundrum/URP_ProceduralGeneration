using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using System.Collections;
using Unity.Jobs;

[DisableAutoCreation]
public class BoardHandler : ComponentSystem
{
    public Mesh mesh;
    public int worldSize = 2;
    public Material material;

    private EntityArchetype archetype;
    private EntityCommandBuffer entityCommandBuffer;

    protected override void OnCreate()
    {
        //mesh = MeshComponents.instance.mesh;

        //material = MeshComponents.instance.material;

        //archetype = EntityManager.CreateArchetype(typeof(RenderMesh), typeof(LocalToWorld), typeof(Translation));

        //entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

        //BuildWorld();

        base.OnCreate();
    }

    protected override void OnUpdate()
    {

    }

    private void BuildWorld()
    {
        for (int z =0; z < worldSize; z++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    float3 pos = new float3(x, y, z);
                    Entity entity = EntityManager.CreateEntity(archetype);

                    EntityManager.SetSharedComponentData(entity, new RenderMesh
                    {
                        mesh = mesh,
                        material = material
                    });

                    EntityManager.SetComponentData(entity, new Translation
                    {
                        Value = pos
                    });
                }
                //yield return null;
            }
        }
    }
}
