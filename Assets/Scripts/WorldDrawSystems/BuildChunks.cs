using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;
using Unity.Rendering;

[DisableAutoCreation]
public class ContructMegaChunks : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private Camera mainCamera;
    private float3 lastbuildPos;
    private EntityArchetype archetype;

    private struct CreateMegaChunk : IJobParallelFor
    {
        [ReadOnly]
        public int chunkSize;
        [ReadOnly]
        public EntityArchetype archetype;
        [ReadOnly]
        [DeallocateOnJobCompletion]public NativeArray<float3> positionList;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {

            BuildChunkAt(GetPosition(positionList[index]), index);

        }

        private float3 GetPosition(float3 position)
        {
            float outerRadius = 1f;
            float innerRadius = outerRadius * 0.866025404f;

            position.x *= 0.866025404f;
            position.z *= (outerRadius * 0.75f);

            return position;
        }

        private void BuildChunkAt(float3 chunkPos, int index)
        {
            Entity en = commandBuffer.CreateEntity(index, archetype);
            commandBuffer.SetComponent(index, en, new MegaChunk
            {
                center = chunkPos,
                spawnCubes = true,
                entity = en
            });
            commandBuffer.SetComponent(index, en, new Translation { Value = chunkPos });
            commandBuffer.SetComponent(index, en, new Rotation { Value = quaternion.identity });
            commandBuffer.SetComponent(index, en, new LocalToWorld { });
            //commandBuffer.AddComponent(index, en, new PhysicsCollider
            //{
            //    Value = Unity.Physics.BoxCollider.Create(bm)
            //});

            //commandBuffer.AddSharedComponent(index, en, group);
        }
    }

    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        lastbuildPos = mainCamera.transform.position;
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        base.OnCreate();
    }

    private float2 Get2DPositionFromIndex(int index, int radius)
    {

        float x = index % radius;
        float y = index / radius;

        return new float2(x, y);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        archetype = EntityManager.CreateArchetype(typeof(MegaChunk), typeof(Translation), typeof(Rotation), typeof(LocalToWorld));

        NativeArray<float3> positionList = new NativeArray<float3>(MeshComponents.radius * MeshComponents.radius, Allocator.TempJob);

        lastbuildPos = mainCamera.transform.position;

        for (int i = 0; i < MeshComponents.radius * MeshComponents.radius; i++)
        {
            float2 offset = Get2DPositionFromIndex(i, MeshComponents.radius);
            positionList[i] = new float3(mainCamera.transform.position.x + (offset.x) * MeshComponents.chunkSize, 0, mainCamera.transform.position.z + (offset.y) * MeshComponents.chunkSize);
        }

        //positionList[0] = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z);
        //positionList[1] = new float3(mainCamera.transform.position.x + MeshComponents.chunkSize, 0, mainCamera.transform.position.z);
        //positionList[2] = new float3(mainCamera.transform.position.x - MeshComponents.chunkSize, 0, mainCamera.transform.position.z);
        //positionList[3] = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z + MeshComponents.chunkSize);
        //positionList[4] = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z - MeshComponents.chunkSize);

        CreateMegaChunk createMegaChunk = new CreateMegaChunk
        {
            archetype = archetype,
            chunkSize = MeshComponents.chunkSize,
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            positionList = positionList
        };

        JobHandle handle = createMegaChunk.Schedule(positionList.Length, 16);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }
}

public class ConsumeContructQueue : JobComponentSystem
{
    private EntityArchetype archetype;
    private EntityQuery m_Query;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    [BurstCompile]
    private struct BuildUltraChunks : IJob
    {
        [ReadOnly]
        public EntityArchetype archetype;
        [ReadOnly]
        public float3 position;
        [ReadOnly]
        public Entity eventEntity;

        public EntityCommandBuffer commandBuffer;

        public void Execute()
        {
            BuildChunkAt(position);
            commandBuffer.DestroyEntity(eventEntity);
        }

        private void BuildChunkAt(float3 chunkPos)
        {
            Entity en = commandBuffer.CreateEntity(archetype);
            commandBuffer.SetComponent(en, new UltraChunk
            {
                center = chunkPos,
                entity = en,
                startBuild = true
            });
        }
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        archetype = EntityManager.CreateArchetype(typeof(UltraChunk));
    }

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(typeof(BuildUltraChunkEvent));
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var Array = m_Query.ToComponentDataArray<BuildUltraChunkEvent>(Allocator.TempJob);
        var toBuild = Array[0];
        Array.Dispose();

        BuildUltraChunks buildUltraChunks = new BuildUltraChunks
        {
            archetype = archetype,
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
            position = toBuild.positionToBuild,
            eventEntity = toBuild.eventEntity
        };

        inputDeps = buildUltraChunks.Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}

[DisableAutoCreation]
public class BuildUltraChunk : JobComponentSystem
{
    private EntityArchetype archetype;
    private Camera mainCamera;
    private float3 lastbuildPos;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    [BurstCompile]
    private struct BuildUltraChunks : IJob
    {
        [ReadOnly]
        public EntityArchetype archetype;
        [ReadOnly]
        public float3 position;

        public EntityCommandBuffer commandBuffer;

        public void Execute()
        {
            BuildChunkAt(position);
        }

        private void BuildChunkAt(float3 chunkPos)
        {
            Entity en = commandBuffer.CreateEntity(archetype);
            commandBuffer.SetComponent(en, new UltraChunk
            {
                center = chunkPos,
                entity = en,
                startBuild = true
            });
        }
    }

    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        lastbuildPos = mainCamera.transform.position;
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private float3 GetPosition(float3 position)
    {
        float outerRadius = 1f;
        float innerRadius = outerRadius * 0.866025404f;

        position.x *= 0.866025404f;
        position.z *= (outerRadius * 0.75f);

        return position;
    }


    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        archetype = EntityManager.CreateArchetype(typeof(UltraChunk));

        float3 buildPos = math.floor(mainCamera.transform.position / (MeshComponents.radius / 4));
        int offset = MeshComponents.radius / 4;

        NativeList<float3> positionList = new NativeList<float3>(Allocator.Temp);

        positionList.Add(GetPosition(new float3(buildPos.x - offset, 0, buildPos.z - offset) * (MeshComponents.radius / 4)));
        positionList.Add(GetPosition(new float3(buildPos.x + offset, 0, buildPos.z - offset) * (MeshComponents.radius / 4)));
        positionList.Add(GetPosition(new float3(buildPos.x - offset, 0, buildPos.z + offset) * (MeshComponents.radius / 4)));
        positionList.Add(GetPosition(new float3(buildPos.x + offset, 0, buildPos.z + offset) * (MeshComponents.radius / 4)));

        for (int i = 0; i < positionList.Length; i++)
        {
            BuildUltraChunks buildUltraChunkJobLeftDown = new BuildUltraChunks
            {
                archetype = archetype,
                commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                position = positionList[i]
            };

            JobHandle job = buildUltraChunkJobLeftDown.Schedule();

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(job);
        }

        lastbuildPos = mainCamera.transform.position;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }
}

[DisableAutoCreation]
public class BuildMegaChunk : JobComponentSystem
{
    private EntityArchetype archetype;
    private Camera mainCamera;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    private float3 lastbuildPos;

    private BoxGeometry bm;

    //[BurstCompile] //Burst cant work with sharedcomponent data
    private struct BuildParallelChunks : IJobParallelFor
    {
        [ReadOnly]
        public float3 position;
        [ReadOnly]
        public int chunkSize;
        [ReadOnly]
        public EntityArchetype archetype;
        [ReadOnly]
        public BoxGeometry bm;
        [ReadOnly]
        public int radius;
        [ReadOnly]
        public UltraChunkGroup group;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {
            int diameter = (int)math.floor(radius / 2);

            float2 Position2D = GetPositionFromIndex(index, radius);

            float x = ((position.x + Position2D.x) - diameter) * chunkSize;
            float z = ((position.z + Position2D.y) - diameter) * chunkSize;
            //float y = GenerateHeight(x, z);

            BuildChunkAt(new float3(x, 0, z), index);

        }

        private void BuildChunkAt(float3 chunkPos, int index)
        {
            Entity en = commandBuffer.CreateEntity(index, archetype);
            commandBuffer.SetComponent(index, en, new MegaChunk
            {
                center = chunkPos,
                spawnCubes = true,
                entity = en
            });
            commandBuffer.SetComponent(index, en, new Translation { Value = chunkPos });
            commandBuffer.SetComponent(index, en, new Rotation { Value = quaternion.identity });
            commandBuffer.SetComponent(index, en, new LocalToWorld { });
            //commandBuffer.AddComponent(index, en, new PhysicsCollider
            //{
            //    Value = Unity.Physics.BoxCollider.Create(bm)
            //});

            commandBuffer.AddSharedComponent(index, en, group);
        }

        private float2 GetPositionFromIndex(int index, int radius)
        {

            float outerRadius = 1f;
            float innerRadius = outerRadius * 0.866025404f;

            float x = index % radius;
            float y = index / radius;

            if (y % 2 == 1)
            {
                x += 0.5f;
            }

            x = x * innerRadius;
            y = y * (outerRadius * 0.75f);

            return new float2(x, y);
        }

        private int GenerateHeight(float x, float z)
        {
            int maxHeight = 150;
            float smooth = 0.01f;
            int octaves = 4;
            float persistence = 0.5f;
            //Parameters should come in from the chunk
            float height = Map(0, maxHeight, 0, 1, FBM(x * smooth, z * smooth, octaves, persistence));
            return (int)height;
        }

        private float Map(float newmin, float newmax, float originalMin, float originalMax, float value)
        {
            return math.lerp(newmin, newmax, math.unlerp(originalMin, originalMax, value));
        }

        private float FBM(float x, float z, int oct, float pers)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            float offset = 32000f;

            for (int i = 0; i < oct; i++)
            {
                total += noise.cnoise(new float2(x + offset * frequency, z + offset * frequency)) * amplitude;

                maxValue += amplitude;

                amplitude *= pers;
                frequency *= 2;
            }

            return total / maxValue;
        }

    }

    private EntityQuery m_Query;

    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        lastbuildPos = mainCamera.transform.position;
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_Query = GetEntityQuery(typeof(UltraChunk));

        bm = new BoxGeometry
        {
            BevelRadius = 0f,
            Center = new float3(float3.zero - 0.5f),
            Orientation = quaternion.identity,
            Size = new float3(MeshComponents.chunkSize)
        };

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        archetype = EntityManager.CreateArchetype(typeof(MegaChunk), typeof(Translation), typeof(Rotation), typeof(LocalToWorld));

        lastbuildPos = mainCamera.transform.position;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<UltraChunk> ultraChunks = m_Query.ToComponentDataArray<UltraChunk>(Allocator.TempJob);
        
        for (int i = 0; i < ultraChunks.Length; i++)
        {
            UltraChunk chunk = ultraChunks[i];
            if (chunk.startBuild)
            {
                UltraChunkGroup group = new UltraChunkGroup { ultrachunkPosition = math.floor(chunk.center) };
                BuildParallelChunks buildParallelChunks = new BuildParallelChunks
                {
                    archetype = archetype,
                    chunkSize = MeshComponents.chunkSize,
                    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    bm = bm,
                    position = chunk.center / MeshComponents.chunkSize,
                    radius = MeshComponents.radius / 2,
                    group = group
                };

                inputDeps = buildParallelChunks.Schedule((MeshComponents.radius / 2) * (MeshComponents.radius / 2), 8, inputDeps);

                endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

                chunk.startBuild = false;

                EntityManager.SetComponentData(chunk.entity, chunk);
            }
        }

        ultraChunks.Dispose();

        return inputDeps;
    }
}

[DisableAutoCreation] //This should be on in the non chunk pipeline
public class FillChunks : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityArchetype archetype;

    [BurstCompile]
    public struct SetUpdateCubes : IJobForEachWithEntity<MegaChunk>
    {
        public bool shouldDraw;

        public void Execute(Entity entity, int index, ref MegaChunk megaChunk)
        {
            megaChunk.spawnCubes = shouldDraw;
        }

    }

    [BurstCompile]
    public struct SpawnCubesParallel : IJobParallelFor
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]public MegaChunk input;

        [ReadOnly]
        public EntityArchetype archetype;
        [ReadOnly]
        public int chunk;
        [ReadOnly]
        public int chunkSize;

        [WriteOnly]
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {
            if (input.spawnCubes)
            {
                
                float2 position = Get2DPositionFromIndex(index, chunkSize);
                float z = position.y - (chunkSize / 2) + input.center.z;
                float x = position.x - (chunkSize / 2) + input.center.x;

                float y = GenerateHeight(position.x - (chunkSize / 2) + input.center.x, position.y - (chunkSize / 2) + input.center.z);
                //float y = 0;

                //BuildCubeAt(new float3(position.x, y - input.center.y, position.y), index, input.entity); //position.y = z // Debug, Always build
                BuildCubeAt(new float3(position.x, math.floor(y - input.center.y), position.y), index, input.entity); //position.y = z

                ////if (GetBlock(new float3(x, y, z)) <= BlockType.STONE && ShouldDraw((position - (chunkSize / 2)) + input.center)) //Need the worldspace               
                //if (GetBlock(new float3(x, y, z)) < BlockType.AIR)//Need the worldspace      
                //{
                //    BuildCubeAt(new float3(position.x, y - input.center.y, position.y), index, input.entity); //position.y = z

                //    ////Build Cliffs
                //    //NativeArray<float> neighboursHeight = GenerateNeighbourHeight(new float3(x, y, z));
                //    //for (int i = 0; i < neighboursHeight.Length; i++)
                //    //{
                //    //    if( y - neighboursHeight[i]> 1)
                //    //        for (int j = 0; j <= y - neighboursHeight[i]; j++)
                //    //        {
                //    //            BuildCubeAt(new float3(position.x, (y - j) - input.center.y, position.y), index, input.entity); //position.y = z
                //    //        }
                //    //}
                //}                                         
            }
        }

        private NativeArray<float> GenerateNeighbourHeight(float3 worldSpacePosition)
        {
            NativeArray<float> neighboursHeight = new NativeArray<float>(4, Allocator.Temp);
            neighboursHeight[0] = GenerateHeight(worldSpacePosition.x + 1, worldSpacePosition.z);
            neighboursHeight[1] = GenerateHeight(worldSpacePosition.x - 1, worldSpacePosition.z);
            neighboursHeight[2] = GenerateHeight(worldSpacePosition.x, worldSpacePosition.z + 1);
            neighboursHeight[3] = GenerateHeight(worldSpacePosition.x, worldSpacePosition.z - 1);

            return neighboursHeight;
        }

        private int3 Get3DPositionFromIndex(int index)
        {
            int z = index / (chunkSize * chunkSize);
            index -= (z * chunkSize * chunkSize);
            int y = index / chunkSize;
            int x = index % chunkSize;

            return new int3(x, y, z);
        }

        private float2 Get2DPositionFromIndex(int index, int radius)
        {
            float outerRadius = 1f;
            float innerRadius = outerRadius * 0.866025404f;

            float x = index % radius;
            float y = index / radius;

            if (y % 2 == 1)
            {
                x += 0.5f;
            }

            x = x * innerRadius;
            y = y * (outerRadius * 0.75f);

            return new float2(x, y);
        }

        private Entity BuildCubeAt(float3 position, int index, Entity parent)
        {
            Entity en = commandBuffer.CreateEntity(index, archetype);
                
            commandBuffer.SetComponent(index, en, new CubePosition { position = position - (chunkSize / 2), type = BlockType.DIRT, HasCube = false, owner = en, parent = parent });

            return en;
        }

        private BlockType GetBlock(float3 position)
        {

            BlockType block;

            int worldX = (int)(position.x);
            int worldY = (int)(position.y);
            int worldZ = (int)(position.z);

            ////Caves
            //if (FBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.48f)
            //{
            //    block = BlockType.AIR;
            //    //shouldCreate = false;
            //}
            //else
            if (worldY <= GenerateStoneHeight(worldX, worldZ))
                if (FBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.38f && worldY < 40)
                    block = BlockType.DIAMOND;
                else
                    block = BlockType.STONE;
            else if (worldY == GenerateHeight(worldX, worldZ))
                block = BlockType.GRASS;
            else if (worldY < GenerateHeight(worldX, worldZ))
                block = BlockType.DIRT;
            else
            {
                block = BlockType.AIR;
            }

            return block;
        }

        private bool ShouldDraw(float3 position)
        {
            if (GetBlock(new float3(position.x, position.y, position.z + 1)) == BlockType.AIR || GetBlock(new float3(position.x, position.y, position.z - 1)) == BlockType.AIR)
                return true;
            if (GetBlock(new float3(position.x, position.y + 1, position.z)) == BlockType.AIR || GetBlock(new float3(position.x, position.y - 1, position.z)) == BlockType.AIR)
                return true;
            if (GetBlock(new float3(position.x + 1, position.y, position.z)) == BlockType.AIR || GetBlock(new float3(position.x - 1, position.y, position.z)) == BlockType.AIR)
                return true;

            return false;
        }

        private float Map(float newmin, float newmax, float originalMin, float originalMax, float value)
        {
            return math.lerp(newmin, newmax, math.unlerp(originalMin, originalMax, value));
        }

        private float GenerateHeight(float x, float z)
        {
            int maxHeight = 150;
            float smooth = 0.01f;
            int octaves = 4;
            float persistence = 0.5f;
            //Parameters should come in from the chunk
            float height = Map(0, maxHeight, 0, 1, FBM(x * smooth, z * smooth, octaves, persistence));
            //return height * 0.1159f; //Weird number is the mesh height, cant import it because its a mesh
            return height * .5f;
            //return 0;
        }

        private int GenerateStoneHeight(float x, float z)
        {
            int maxHeight = 150;
            float smooth = 0.01f;
            int octaves = 4;
            float persistence = 0.5f;
            //Parameters should come in from the chunk
            float height = Map(0, maxHeight - 5, 0, 1, FBM(x * smooth * 2, z * smooth * 2, octaves + 1, persistence));
            return (int)height;
        }

        private float FBM(float x, float z, int oct, float pers)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            float offset = 16000f;

            for (int i = 0; i < oct; i++)
            {
                total += noise.cnoise(new float2(x + offset * frequency, z + offset * frequency)) * amplitude;

                maxValue += amplitude;

                amplitude *= pers;
                frequency *= 2;
            }

            return total / maxValue;
        }

        private float FBM3D(float x, float y, float z, float sm, int oct)
        {
            float XY = FBM(x * sm, y * sm, oct, 0.5f);
            float YZ = FBM(y * sm, z * sm, oct, 0.5f);
            float XZ = FBM(x * sm, z * sm, oct, 0.5f);

            float YX = FBM(y * sm, x * sm, oct, 0.5f);
            float ZY = FBM(z * sm, y * sm, oct, 0.5f);
            float ZX = FBM(z * sm, x * sm, oct, 0.5f);

            return math.unlerp(-1, 1, ((XY + YZ + XZ + YX + ZY + ZX) / 6.0f));
        }

    }

    private EntityQuery m_Query;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        archetype = EntityManager.CreateArchetype(typeof(CubePosition), typeof(Parent), typeof(Translation), typeof(Rotation), typeof(LocalToWorld),
            typeof(LocalToParent), typeof(RenderBounds), typeof(PerInstanceCullingTag));
    }

    protected override void OnCreate()
    {

        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_Query = GetEntityQuery(typeof(MegaChunk));

        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //SpawnCubes spawncubesJob = new SpawnCubes
        //{
        //    archetype = archetype,
        //    chunk = MeshComponents.chunkSize,
        //    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        //    output = 
        //};
        //inputDeps = spawncubesJob.Schedule(this, inputDeps);

        NativeArray<MegaChunk> megaChunks = m_Query.ToComponentDataArray< MegaChunk>(Allocator.TempJob);

        for (int i = 0; i < megaChunks.Length; i++)
        {
            MegaChunk chunk = megaChunks[i];
            if (megaChunks[i].spawnCubes)
            {
                SpawnCubesParallel spawnCubesParallel = new SpawnCubesParallel
                {
                    archetype = archetype,
                    chunk = MeshComponents.chunkSize,
                    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    input = chunk,
                    chunkSize = MeshComponents.chunkSize
                };

                inputDeps = spawnCubesParallel.Schedule(MeshComponents.chunkSize * MeshComponents.chunkSize, 8, inputDeps);

                endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);
            }
  
        }
        SetUpdateCubes setCubeUpdateJob = new SetUpdateCubes
        {
            shouldDraw = false
        };

        inputDeps = setCubeUpdateJob.Schedule(this, inputDeps);

        megaChunks.Dispose();

        return inputDeps;
    }
}