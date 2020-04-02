//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Jobs;
//using Unity.Entities;
//using Unity.Transforms;
//using Unity.Rendering;
//using Unity.Mathematics;
//using Unity.Physics;
//using Unity.Collections;
//using MeshCollider = Unity.Physics.MeshCollider;
//using Collider = Unity.Physics.Collider;

//public class Drawchunk : JobComponentSystem
//{
//    [RequireComponentTag(typeof(DrawChunkFlag))]
//    private struct DrawJob : IJobForEachWithEntity<WorldChunk>
//    {
//        public EntityCommandBuffer.Concurrent commandBuffer;       
//        public void Execute(Entity entity, int index, ref WorldChunk worldChunk)
//        {

//            commandBuffer.AddComponent(index, entity, new Translation
//            {
//                Value = worldChunk.position
//            });

//            commandBuffer.AddComponent(index, entity, new Rotation { Value = quaternion.identity });

//            commandBuffer.RemoveComponent(index, entity, typeof(DrawChunkFlag));
//        }
//    }

//    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

//    protected override void OnCreate()
//    {
//        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

//        base.OnCreate();
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        DrawJob drawJob = new DrawJob { commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent() };

//        inputDeps = drawJob.Schedule(this, inputDeps);

//        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

//        return inputDeps;
//    }
//}

////[DisableAutoCreation]
//public class BuildCollision : ComponentSystem
//{
//    //[RequireComponentTag(typeof(BuildCollisionMeshFlag))]
//    //[ExcludeComponent(typeof(PhysicsCollider))]
//    //private struct DrawJob : IJobForEachWithEntity<WorldChunk>
//    //{
//    //    public EntityCommandBuffer.Concurrent commandBuffer;
//    //    [ReadOnly]
//    //    public RenderMesh renderMesh;
//    //    //[ReadOnly]
//    //    //public BufferFromEntity<VerticesBuffer> lookupVerticesBuffer;
//    //    //[ReadOnly]
//    //    //public BufferFromEntity<TrianglesBuffer> lookupTrianglesBuffer;

//    //    public void Execute(Entity entity, int index, ref WorldChunk worldChunk)
//    //    {
//    //        //BlobAssetReference<Collider> sourceCollider = MeshCollider.Create(lookupVerticesBuffer[entity].Reinterpret<float3>().AsNativeArray(), lookupTrianglesBuffer[entity].Reinterpret<int3>().AsNativeArray());
//    //        NativeArray<Vector3> vertices = new NativeArray<Vector3>(renderMesh.mesh.vertices, Allocator.Temp);
//    //        NativeArray<int> triangles = new NativeArray<int>(renderMesh.mesh.triangles, Allocator.Temp);

//    //        BlobAssetReference<Collider> sourceCollider = MeshCollider.Create(vertices.Reinterpret<float3>(), triangles);

//    //        commandBuffer.AddComponent(index, entity, new PhysicsCollider
//    //        {
//    //            Value = sourceCollider
//    //        });

//    //        commandBuffer.RemoveComponent(index, entity, typeof(BuildCollisionMeshFlag));
//    //    }
//    //}


//    //private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

//    //protected override void OnCreate()
//    //{
//    //    endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

//    //    base.OnCreate();
//    //}

//    //protected override JobHandle OnUpdate(JobHandle inputDeps)
//    //{
//    //    EntityCommandBuffer commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

//    //    //Entities.WithAll<RenderMesh, BuildCollisionMeshFlag>().ForEach((Entity entity, in RenderMesh renderMesh) => {
//    //    //    NativeArray<Vector3> vertices = new NativeArray<Vector3>(renderMesh.mesh.vertices, Allocator.Temp);
//    //    //    NativeArray<int> triangles = new NativeArray<int>(renderMesh.mesh.triangles, Allocator.Temp);

//    //    //    BlobAssetReference<Collider> sourceCollider = MeshCollider.Create(vertices.Reinterpret<float3>(), triangles);

//    //    //    commandBuffer.AddComponent(entity, new PhysicsCollider
//    //    //    {
//    //    //        Value = sourceCollider
//    //    //    });

//    //    //    commandBuffer.RemoveComponent(entity, typeof(BuildCollisionMeshFlag));

//    //    //}).WithoutBurst().Run();

//    //    //DrawJob drawJob = new DrawJob
//    //    //{
//    //    //    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//    //    //    renderMesh = EntityManager.GetSharedComponentData<RenderMesh>()
//    //    //};

//    //    //inputDeps = drawJob.Schedule(this, inputDeps);

//    //    endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

//    //    return inputDeps;
//    //}

//    protected override void OnUpdate()
//    {
//        Entities.WithAll<BuildCollisionMeshFlag>().ForEach((Entity entity) =>
//        {
//            RenderMesh renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(entity);
//            NativeArray<Vector3> vertices = new NativeArray<Vector3>(renderMesh.mesh.vertices, Allocator.Temp);
//            NativeArray<int> triangles = new NativeArray<int>(renderMesh.mesh.triangles, Allocator.Temp);

//            BlobAssetReference<Collider> sourceCollider = MeshCollider.Create(vertices.Reinterpret<float3>(), triangles);

//            PostUpdateCommands.AddComponent(entity, new PhysicsCollider
//            {
//                Value = sourceCollider
//            });

//            PostUpdateCommands.RemoveComponent(entity, typeof(BuildCollisionMeshFlag));

//        });
//    }
//}
