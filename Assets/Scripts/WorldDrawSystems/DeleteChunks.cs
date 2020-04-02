using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;

public class DeleteSystem : JobComponentSystem
{
    private BeginPresentationEntityCommandBufferSystem beginPresentationEntityCommandBufferSystem;

    private EntityQuery m_Query;

    [BurstCompile]
    private struct ParallellDeleteQueue : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]public NativeArray<DeleteEntityEvent> eventArray;
        [ReadOnly]
        public BufferFromEntity<Child> lookup;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {
            if (lookup.Exists(eventArray[index].entity))
            {
                NativeArray<Entity> array = lookup[eventArray[index].entity].Reinterpret<Entity>().AsNativeArray();
                for (int i = 0; i < array.Length; i++)
                {
                    commandBuffer.AddComponent(index, array[i], new DeleteEntityEvent { entity = array[i] });
                }
            }
            else
            {
                commandBuffer.DestroyEntity(index, eventArray[index].entity);
            }
        }
    }

    [BurstCompile]
    private struct DeleteQueue : IJobForEachWithEntity<DeleteEntityEvent>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref DeleteEntityEvent deleteEvent)
        {
            commandBuffer.DestroyEntity(index, deleteEvent.entity);
        }
    }

    protected override void OnCreate()
    {
        beginPresentationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();

        m_Query = GetEntityQuery(ComponentType.ReadOnly<DeleteEntityEvent>());

        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        NativeArray<DeleteEntityEvent> eventArray = m_Query.ToComponentDataArray<DeleteEntityEvent>(Allocator.TempJob);

        ParallellDeleteQueue parallellDeleteQueue = new ParallellDeleteQueue
        {
            lookup = GetBufferFromEntity<Child>(true),
            eventArray = eventArray,
            commandBuffer = beginPresentationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };

        inputDeps = parallellDeleteQueue.Schedule(eventArray.Length, 32, inputDeps);
        beginPresentationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}

public class EmergencyRemove : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    [BurstCompile]
    private struct Remove : IJobForEachWithEntity<DeleteNowFlag>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly]ref DeleteNowFlag c0)
        {
            commandBuffer.DestroyEntity(index, entity);
        }
    }

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Remove removeJob = new Remove
        {
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };

        inputDeps = removeJob.Schedule(this, inputDeps);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}

public class ConsumeRemoveUltraChunkEvent : JobComponentSystem
{
    private EntityQuery m_Query;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityQuery delete_Query;
    private EntityCommandBuffer commandbuffer;

    [BurstCompile]
    private struct DeleteQueue : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]public NativeArray<Entity> toDelete;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {
            commandBuffer.AddComponent(index, toDelete[index], new QueueRemoveEntityFlag { });
        }
    }

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(typeof(RemoveUltraChunkEvent));
        delete_Query = GetEntityQuery(typeof(MegaChunk), typeof(UltraChunkGroup));
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<RemoveUltraChunkEvent> events = m_Query.ToComponentDataArray<RemoveUltraChunkEvent>(Allocator.TempJob);
        NativeArray<Entity> eventsEntity = m_Query.ToEntityArray(Allocator.TempJob);

        for (int i = 0; i < events.Length; i++)
        {
            delete_Query.SetSharedComponentFilter(new UltraChunkGroup { ultrachunkPosition = math.floor(events[i].group) });
            var deleteArray = delete_Query.ToEntityArray(Allocator.TempJob);
            DeleteQueue deleteQueueJob = new DeleteQueue
            {
                commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                toDelete = deleteArray
            };
            inputDeps = deleteQueueJob.Schedule(deleteArray.Length, 16, inputDeps);
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            commandbuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
            commandbuffer.DestroyEntity(eventsEntity[i]);
        }
        eventsEntity.Dispose();
        events.Dispose();

        return inputDeps;
    }
}

//[DisableAutoCreation]
//public class DeleteUltraChunk : JobComponentSystem //Start of the deleting pipeline
//{
//    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
//    private Camera mainCamera;
//    private EntityArchetype archetype;

//    [BurstCompile]
//    private struct CheckForDeletion : IJobForEachWithEntity<UltraChunk>
//    {
//        [ReadOnly]
//        public float3 currentPosition;
//        [ReadOnly]
//        public int radius;
//        [ReadOnly]
//        public EntityArchetype archetype;

//        public EntityCommandBuffer.Concurrent commandBuffer;

//        public void Execute(Entity en, int index, ref UltraChunk ultraChunk)
//        {
//            if(math.distancesq(currentPosition, ultraChunk.center) > (radius * radius) * 10)
//            {
//                Entity entity = commandBuffer.CreateEntity(index, archetype);
//                commandBuffer.SetComponent(index, entity, new RemoveUltraChunkEvent { group = ultraChunk.center });
//                commandBuffer.DestroyEntity(index, en);
//            }
//        }
//    }

//    protected override void OnCreate()
//    {
//        mainCamera = Camera.main;
//        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//        archetype = EntityManager.CreateArchetype(typeof(RemoveUltraChunkEvent));

//        base.OnCreate();
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        CheckForDeletion deletionJob = new CheckForDeletion
//        {
//            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//            currentPosition = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z),
//            radius = MeshComponents.radius,
//            archetype = archetype
//        };

//        inputDeps = deletionJob.Schedule(this, inputDeps);

//        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

//        return inputDeps;
//    }
//}

public class DeleteMegaChunk : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    private EntityQuery m_Query;

    [BurstCompile]
    private struct ParallellMegachunkJob : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<MegaChunk> megaChunks;
        [ReadOnly]
        public BufferFromEntity<Child> lookup;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {   
            //if (lookup.Exists(megaChunks[index].entity))
            //{
            //    NativeArray<Entity> array = lookup[megaChunks[index].entity].Reinterpret<Entity>().AsNativeArray();
            //    for (int i = 0; i < array.Length; i++)
            //    {
            //        commandBuffer.AddComponent(index, array[i], new DeleteEntityEvent { entity = array[i] });
            //    }
            //}
            //else
            //{
            //    commandBuffer.AddComponent(index, megaChunks[index].entity, new DeleteNowFlag { });
            //}
            commandBuffer.AddComponent(index, megaChunks[index].entity, new DeleteEntityEvent { entity = megaChunks[index].entity });
        }
    }

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_Query = GetEntityQuery(ComponentType.ReadOnly<MegaChunk>(), ComponentType.ReadOnly<QueueRemoveEntityFlag>());

        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<MegaChunk> megaChunks = m_Query.ToComponentDataArray<MegaChunk>(Allocator.TempJob);

        ParallellMegachunkJob parallellMegachunkJob = new ParallellMegachunkJob
        {
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            lookup = GetBufferFromEntity<Child>(true),
            megaChunks = megaChunks
        };

        inputDeps = parallellMegachunkJob.Schedule(megaChunks.Length, 16, inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
        
    }
}