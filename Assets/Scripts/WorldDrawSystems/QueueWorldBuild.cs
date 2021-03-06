﻿using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;
using Unity.Rendering;

//[UpdateBefore(typeof(QueueBuffer))]
//public class ClearQueueBuffer : ComponentSystem
//{
//    private EntityQuery m_Query;
//    protected override void OnCreate()
//    {
//        m_Query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RemoveUltraChunkEvent>());
//        base.OnCreate();
//    }
//    protected override void OnUpdate()
//    {
//        Entities.WithAll<QueueManager>().ForEach((Entity en, ref QueueManager manager) =>
//        {
//            EntityManager.GetBuffer<EntitiesBuffer>(en).Clear();
//        });
//    }
//}

//[UpdateAfter(typeof(DeleteSystem))]
//public class QueueBuffer : ComponentSystem
//{
//    private Entity queueManagerEntity;
//    protected override void OnStartRunning()
//    {
//        base.OnStartRunning();
//        queueManagerEntity = EntityManager.CreateEntity(typeof(QueueManager));
//        EntityManager.AddBuffer<EntitiesBuffer>(queueManagerEntity);
//    }

//    protected override void OnUpdate()
//    {  
//        Entities.WithAll<UltraChunk>().ForEach((Entity en, ref UltraChunk ultraChunk) =>
//        {
//            NativeArray<Entity> builtEntities = EntityManager.GetBuffer<EntitiesBuffer>(queueManagerEntity).Reinterpret<Entity>().AsNativeArray();
//            if (!builtEntities.Contains(en))
//                EntityManager.GetBuffer<EntitiesBuffer>(queueManagerEntity).Add(en);
//        });
//    }
//}

//public class QueueBuildEvent : ComponentSystem
//{
//    private Entity currentChunkEntity;
//    private EntityQuery m_Group;
//    private EntityArchetype archetype;

//    private float3 GetPosition(float3 position)
//    {
//        float outerRadius = 1f;
//        float innerRadius = outerRadius * 0.866025404f;

//        position.x *= 0.866025404f;
//        position.z *= (outerRadius * 0.75f);

//        return position;
//    }

//    protected override void OnStartRunning()
//    {
//        base.OnStartRunning();
//    }

//    protected override void OnCreate()
//    {
//        m_Group = GetEntityQuery(typeof(QueueManager));
//        archetype = EntityManager.CreateArchetype(typeof(RemoveUltraChunkEvent));
//        base.OnCreate();
//    }

//    protected override void OnDestroy()
//    {
//        base.OnDestroy();
//    }

//    protected override void OnUpdate()
//    {
//        var queueManagerArray = m_Group.ToEntityArray(Allocator.TempJob);
//        Entity queueManager = queueManagerArray[0];
//        queueManagerArray.Dispose();
//        NativeArray<Entity> drawnChunks = EntityManager.GetBuffer<EntitiesBuffer>(queueManager).Reinterpret<Entity>().AsNativeArray();      

//        Entities.WithAll<CurrentUltraChunkFlag, UltraChunk>().ForEach((Entity en, ref CurrentUltraChunkFlag currentChunk, ref UltraChunk ultraChunk) =>
//        {
//            if (!en.Equals(currentChunkEntity))
//            {
//                NativeArray<float3> ultraChunks = new NativeArray<float3>(drawnChunks.Length, Allocator.Temp); //Nested for is fine if its just one currentultrachunk
//                for (int i = 0; i < drawnChunks.Length; i++)
//                {
//                    ultraChunks[i] = EntityManager.GetComponentData<UltraChunk>(drawnChunks[i]).center;

//                    if (math.distancesq(ultraChunks[i], ultraChunk.center) > (MeshComponents.radius * MeshComponents.radius) * 20) //Start of the deleting pipeline
//                    {
//                        Entity entity = PostUpdateCommands.CreateEntity(archetype);
//                        PostUpdateCommands.SetComponent(entity, new RemoveUltraChunkEvent { group = ultraChunks[i] });
//                        PostUpdateCommands.DestroyEntity(drawnChunks[i]);
//                    }

//                }

//                float3 neighbourRight = ultraChunk.center + GetPosition(new float3((MeshComponents.radius * MeshComponents.chunkSize) / 2, 0, 0));
//                float3 neighbourLeft = ultraChunk.center - GetPosition(new float3((MeshComponents.radius * MeshComponents.chunkSize) / 2, 0, 0));
//                float3 neighbourForward = ultraChunk.center + GetPosition(new float3(0, 0, (MeshComponents.radius * MeshComponents.chunkSize) / 2));
//                float3 neighbourBack = ultraChunk.center - GetPosition(new float3(0, 0, (MeshComponents.radius * MeshComponents.chunkSize) / 2));

//                if (!ultraChunks.Contains(neighbourRight))
//                {
//                    Entity eventEntity = EntityManager.CreateEntity(typeof(BuildUltraChunkEvent));
//                    EntityManager.SetComponentData(eventEntity, new BuildUltraChunkEvent {
//                        eventEntity = eventEntity,
//                        positionToBuild = neighbourRight
//                    });
//                }                   
//                if (!ultraChunks.Contains(neighbourLeft))
//                {
//                    Entity eventEntity = EntityManager.CreateEntity(typeof(BuildUltraChunkEvent));
//                    EntityManager.SetComponentData(eventEntity, new BuildUltraChunkEvent
//                    {
//                        eventEntity = eventEntity,
//                        positionToBuild = neighbourLeft
//                    });
//                }
//                if (!ultraChunks.Contains(neighbourForward))
//                {
//                    Entity eventEntity = EntityManager.CreateEntity(typeof(BuildUltraChunkEvent));
//                    EntityManager.SetComponentData(eventEntity, new BuildUltraChunkEvent
//                    {
//                        eventEntity = eventEntity,
//                        positionToBuild = neighbourForward
//                    });
//                }
//                if (!ultraChunks.Contains(neighbourBack))
//                {
//                    Entity eventEntity = EntityManager.CreateEntity(typeof(BuildUltraChunkEvent));
//                    EntityManager.SetComponentData(eventEntity, new BuildUltraChunkEvent
//                    {
//                        eventEntity = eventEntity,
//                        positionToBuild = neighbourBack
//                    });
//                }
//                currentChunkEntity = en;
//            }
//        });
//    }
//}

//[DisableAutoCreation]
//public class CurrentChunkSystem : JobComponentSystem
//{
//    private Camera mainCamera;
//    private Entity currentChunkEntity;
//    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

//    protected override void OnStartRunning()
//    {
//        base.OnStartRunning();
//    }

//    protected override void OnCreate()
//    {
//        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//        currentChunkEntity = Entity.Null;
//        mainCamera = Camera.main;
//        base.OnCreate();
//    }

//    //[BurstCompile]
//    [RequireComponentTag(typeof(RenderMesh))]
//    private struct FindCurrentChunk : IJobForEachWithEntity<MegaChunk>
//    {
//        [ReadOnly]
//        public float3 position;
//        [ReadOnly]
//        public int chunkSize;
//        [ReadOnly]
//        public Entity currentChunkEntity;

//        [WriteOnly]
//        public NativeArray<Entity> output;

//        public EntityCommandBuffer.Concurrent commandBuffer;

//        public void Execute(Entity en, int index, ref MegaChunk megaChunk)
//        {
//            if (math.distancesq(new float3(position.x, 0, position.z), megaChunk.center) < chunkSize )
//            {
//                if (!en.Equals(currentChunkEntity))
//                {
//                    commandBuffer.AddComponent(index, en, new CurrentUltraChunkFlag { });
//                    if (!currentChunkEntity.Equals(Entity.Null))
//                        commandBuffer.RemoveComponent(index, currentChunkEntity, typeof(CurrentUltraChunkFlag));
//                    output[0] = en;
//                }
//            }
//        }
//    }

//    //protected override void OnUpdate()
//    //{
//    //    Entities.WithAll<MegaChunk, RenderMesh>().ForEach((Entity en, ref UltraChunk ultraChunk) =>
//    //    {
//    //        if (math.distance(new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z), ultraChunk.center) < MeshComponents.chunkSize * 2)
//    //        {
//    //            if (!en.Equals(currentChunkEntity))
//    //            {
//    //                EntityManager.AddComponentData(en, new CurrentUltraChunkFlag { });
//    //                if (!currentChunkEntity.Equals(Entity.Null))
//    //                    EntityManager.RemoveComponent(currentChunkEntity, typeof(CurrentUltraChunkFlag));
//    //                currentChunkEntity = en;
//    //            }
//    //        }
//    //    });
//    //}

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {       
//        NativeArray<Entity> output = new NativeArray<Entity>(1, Allocator.TempJob);
//        FindCurrentChunk findCurrentChunk = new FindCurrentChunk
//        {
//            chunkSize = MeshComponents.chunkSize,
//            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//            currentChunkEntity = currentChunkEntity,
//            position = mainCamera.transform.position,
//            output = output
//        };

//        inputDeps = findCurrentChunk.ScheduleSingle(this, inputDeps);
//        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

//        inputDeps.Complete();

//        if(!output[0].Equals(currentChunkEntity))
//            currentChunkEntity = output[0];

//        output.Dispose();

//        return inputDeps;
//    }
//}
