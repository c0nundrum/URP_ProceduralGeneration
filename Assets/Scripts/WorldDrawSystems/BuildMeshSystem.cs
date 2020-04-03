using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;

public class ChunkTable
{
    public Entity entity;
    public Mesh mesh;
    public bool isDrawn;

    public ChunkTable(Entity entity, Mesh mesh)
    {
        this.entity = entity;
        this.mesh = mesh;
        this.isDrawn = false;
    }
}

[DisableAutoCreation]
[UpdateAfter(typeof(BuildChunkMesh))]
public class DrawMesh : ComponentSystem
{
    private UnityEngine.Material material;
    private float3 lastbuildPos;
    public Camera mainCamera;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

    }

    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        lastbuildPos = mainCamera.transform.position;
        material = MeshComponents.textureAtlas;
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        foreach (var chunkTable in MeshComponents._lookupMesh)
        {
            if (!chunkTable.Value.isDrawn && math.distancesq(mainCamera.transform.position, chunkTable.Key) < MeshComponents._drawDistance)
            {
                EntityManager.AddSharedComponentData(chunkTable.Value.entity, new RenderMesh { mesh = chunkTable.Value.mesh, material = material });
                EntityManager.AddComponentData(chunkTable.Value.entity, new RenderBounds { Value = chunkTable.Value.mesh.bounds.ToAABB() });
                EntityManager.AddComponentData(chunkTable.Value.entity, new LocalToWorld { });
                chunkTable.Value.isDrawn = true;
            } else if (chunkTable.Value.isDrawn && math.distancesq(mainCamera.transform.position, chunkTable.Key) >= MeshComponents._drawDistance)
            {
                EntityManager.RemoveComponent(chunkTable.Value.entity, typeof(RenderMesh));
                EntityManager.RemoveComponent(chunkTable.Value.entity, typeof(RenderBounds));
                EntityManager.RemoveComponent(chunkTable.Value.entity, typeof(LocalToWorld));
                chunkTable.Value.isDrawn = false;
            }
        }
    }
}

[DisableAutoCreation]
public class BuildChunkMesh : ComponentSystem
{
    private Mesh originMesh;
    private UnityEngine.Material material;
    //private Dictionary<float3, Mesh> dict;

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


    private Mesh HexChunk(Vector3 position, MegaChunk chunk)
    {
        List<Mesh> hexChunk = new List<Mesh>();

        for (int i = 0; i < MeshComponents.chunkSize * MeshComponents.chunkSize; i++)
        {

            Mesh hex = new Mesh();

            Vector3[] vertices = originMesh.vertices;

            float2 position2D = Get2DPositionFromIndex(i, MeshComponents.chunkSize);

            float y = GenerateHeight(position2D.x - (MeshComponents.chunkSize / 2) + chunk.center.x, position2D.y - (MeshComponents.chunkSize / 2) + chunk.center.z); //chunk.y is always zero

            Vector3 buildPosition = new Vector3(position2D.x - (MeshComponents.chunkSize / 2), math.floor(y - chunk.center.y), position2D.y - (MeshComponents.chunkSize / 2));

            for (int j = 0; j < vertices.Length; j++)
            {
                vertices[j] += buildPosition;
            }

            hex.vertices = vertices;
            hex.uv = originMesh.uv;
            hex.normals = originMesh.normals;
            hex.triangles = originMesh.triangles;
            hex.RecalculateBounds();

            hexChunk.Add(hex);
        }

        CombineInstance[] array = new CombineInstance[hexChunk.Count];


        for (int i = 0; i < array.Length; i++)
            array[i].mesh = hexChunk[i];

        Mesh hexTile = new Mesh();
        //since our cubes are created on the correct spot already, we dont need a matrix, and so far, no light data
        hexTile.CombineMeshes(array, true, false, false);
        hexTile.Optimize();
        return hexTile;

    }

    protected override void OnCreate()
    {
        //OriginCube = MakeCubeAtZero();
        originMesh = MeshComponents.tileMesh;
        material = MeshComponents.textureAtlas;
        //dict = new Dictionary<float3, Mesh>();

        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        //EntityArchetype arch = EntityManager.CreateArchetype(typeof(MegaChunk), typeof(Translation), typeof(Rotation), typeof(LocalToWorld));
        //Entity megaChunk = EntityManager.CreateEntity(arch);

        //EntityManager.SetComponentData(megaChunk, new MegaChunk {
        //    center = float3.zero,
        //    entity = megaChunk,
        //    spawnCubes = false
        //});

        //EntityManager.SetComponentData(megaChunk, new Translation { Value = float3.zero });
        //EntityManager.SetComponentData(megaChunk, new Rotation { Value = quaternion.identity });

    }

    protected override void OnUpdate()
    {
        //There is an option to do culling per chunk by leaving out the PerInstanceCulling tag component. Meaning for each chunk we use the combined bounding volume.For grass likely a good choice.
        Entities.WithAll<MegaChunk>().WithNone<RenderMesh>().ForEach((Entity en, ref MegaChunk chunk) => {

            if (!MeshComponents._lookupMesh.ContainsKey(chunk.center))
            {
                Mesh hex = HexChunk(chunk.center, chunk);
                ChunkTable chunkTable = new ChunkTable(en, hex);
                ////EntityManager.AddSharedComponentData(en, new RenderMesh { mesh = hex, material = material, castShadows = UnityEngine.Rendering.ShadowCastingMode.On });
                //EntityManager.AddSharedComponentData(en, new RenderMesh { mesh = hex, material = material });
                //EntityManager.AddComponentData(en, new RenderBounds { Value = hex.bounds.ToAABB() });
                ////EntityManager.AddComponentData(en, new PerInstanceCullingTag { });
                //EntityManager.AddComponentData(en, new LocalToWorld { });
                //chunkTable.isDrawn = true;
                MeshComponents._lookupMesh.Add(chunk.center, chunkTable);
            }

        });
    }
}

[DisableAutoCreation]
public class BuildMeshSystem_Legacy : JobComponentSystem
{
    private Entity prefabEntity;

    private EntityQuery m_Query;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    [BurstCompile]
    private struct BuildScene : IJobForEachWithEntity<CubePosition>
    {
        [ReadOnly]
        public Entity prefabEntity;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref CubePosition cube)
        {
            Entity hexTile = commandBuffer.Instantiate(index, prefabEntity);
            commandBuffer.AddComponent(index, hexTile, new Parent { Value = cube.parent });
            commandBuffer.SetComponent(index, hexTile, new Translation { Value = cube.position });
            commandBuffer.SetComponent(index, hexTile, new Rotation { Value = quaternion.identity });
            commandBuffer.AddComponent(index, hexTile, new LocalToParent { });

            commandBuffer.DestroyEntity(index, entity);
        }
    }

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_Query = EntityManager.CreateEntityQuery(typeof(PrefabEntityComponent));
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {

        base.OnStartRunning();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var arrayEntity = m_Query.ToEntityArray(Allocator.TempJob);
        prefabEntity = arrayEntity[0];
        
        BuildScene buildScene = new BuildScene
        {
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            prefabEntity = prefabEntity
        };

        arrayEntity.Dispose();

        inputDeps = buildScene.Schedule(this, inputDeps);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}

[DisableAutoCreation]
//[UpdateAfter(typeof(CreateMeshJobSystem))]
public class BuildMeshSystem : ComponentSystem
{
    private enum Cubeside { BOTTOM, TOP, LEFT, FRONT, BACK, RIGHT };

    private const float ATLAS_SIZE = 0.03125f;

    private readonly Vector2[,] blockUVs = {
        /*GRASS TOP*/ {
                new Vector2(19 * ATLAS_SIZE, 29 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 29 * ATLAS_SIZE),
                                    new Vector2(19 * ATLAS_SIZE, 28 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 28 * ATLAS_SIZE)},
        /*GRASS SIDE*/ {
                new Vector2(19 * ATLAS_SIZE, 28 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 28 * ATLAS_SIZE),
                                new Vector2(19 * ATLAS_SIZE, 27 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 27 * ATLAS_SIZE)},
        /*DIRT*/ {
                new Vector2(18 * ATLAS_SIZE, 32 * ATLAS_SIZE), new Vector2(19 * ATLAS_SIZE, 32 * ATLAS_SIZE),
                                new Vector2(18 * ATLAS_SIZE, 31 * ATLAS_SIZE), new Vector2(19 * ATLAS_SIZE, 31 * ATLAS_SIZE)},
        /*STONE*/ {
                new Vector2(20 * ATLAS_SIZE, 26 * ATLAS_SIZE), new Vector2(21 * ATLAS_SIZE, 26 * ATLAS_SIZE),
                                new Vector2(20 * ATLAS_SIZE, 25 * ATLAS_SIZE), new Vector2(21 * ATLAS_SIZE, 25 * ATLAS_SIZE)},
        /*DIAMOND*/ {
                new Vector2(0 * ATLAS_SIZE, 1 * ATLAS_SIZE), new Vector2(1 * ATLAS_SIZE, 1 * ATLAS_SIZE),
                                new Vector2(1 * ATLAS_SIZE, 0 * ATLAS_SIZE), new Vector2(1 * ATLAS_SIZE, 1 * ATLAS_SIZE)}
        };

    private Mesh CreateQuads(Cubeside side, BlockType bType, Vector3 worldChunkPosition)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ScriptedMesh" + side.ToString();

        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];
        Vector2[] uvs = new Vector2[4];

        int[] triangles = new int[6];

        Vector2 uv00;
        Vector2 uv10;
        Vector2 uv01;
        Vector2 uv11;

        if (bType == BlockType.GRASS && side == Cubeside.TOP)
        {
            uv00 = blockUVs[0, 0];
            uv10 = blockUVs[0, 1];
            uv01 = blockUVs[0, 2];
            uv11 = blockUVs[0, 3];

        }
        else if (bType == BlockType.GRASS && side == Cubeside.BOTTOM)
        {
            uv00 = blockUVs[(int)(BlockType.DIRT + 1), 0];
            uv10 = blockUVs[(int)(BlockType.DIRT + 1), 1];
            uv01 = blockUVs[(int)(BlockType.DIRT + 1), 2];
            uv11 = blockUVs[(int)(BlockType.DIRT + 1), 3];
        }
        else
        {
            uv00 = blockUVs[(int)(bType + 1), 0];
            uv10 = blockUVs[(int)(bType + 1), 1];
            uv01 = blockUVs[(int)(bType + 1), 2];
            uv11 = blockUVs[(int)(bType + 1), 3];
        }

        //All vertices
        Vector3 p0 = new Vector3(-.5f, -.5f, .5f) + worldChunkPosition;
        Vector3 p1 = new Vector3(.5f, -.5f, .5f) + worldChunkPosition;
        Vector3 p2 = new Vector3(.5f, -.5f, -.5f) + worldChunkPosition;
        Vector3 p3 = new Vector3(-.5f, -.5f, -.5f) + worldChunkPosition;
        Vector3 p4 = new Vector3(-.5f, .5f, .5f) + worldChunkPosition;
        Vector3 p5 = new Vector3(.5f, .5f, .5f) + worldChunkPosition;
        Vector3 p6 = new Vector3(.5f, .5f, -.5f) + worldChunkPosition;
        Vector3 p7 = new Vector3(-.5f, .5f, -.5f) + worldChunkPosition;

        switch (side)
        {
            case Cubeside.BOTTOM:
                vertices = new Vector3[] { p0, p1, p2, p3 };
                normals = new Vector3[]
                {
                    Vector3.down,
                    Vector3.down,
                    Vector3.down,
                    Vector3.down
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.TOP:
                vertices = new Vector3[] { p7, p6, p5, p4 };
                normals = new Vector3[]
                {
                    Vector3.up,
                    Vector3.up,
                    Vector3.up,
                    Vector3.up
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.LEFT:
                vertices = new Vector3[] { p7, p4, p0, p3 };
                normals = new Vector3[]
                {
                    Vector3.left,
                    Vector3.left,
                    Vector3.left,
                    Vector3.left
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.RIGHT:
                vertices = new Vector3[] { p5, p6, p2, p1 };
                normals = new Vector3[]
                {
                    Vector3.right,
                    Vector3.right,
                    Vector3.right,
                    Vector3.right
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.FRONT:
                vertices = new Vector3[] { p4, p5, p1, p0 };
                normals = new Vector3[]
                {
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.BACK:
                vertices = new Vector3[] { p6, p7, p3, p2 };
                normals = new Vector3[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        return mesh;

    }

    private RenderMesh SingleCube;
    private Mesh OriginCube;

    public Mesh MakeCubeAtZero()
    {

        List<Mesh> quads = new List<Mesh>();

        BlockType bType = BlockType.DIRT;
        Vector3 blockPosition = new Vector3(0, 0, 0);

        quads.Add(CreateQuads(Cubeside.FRONT, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.BACK, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.TOP, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.BOTTOM, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.LEFT, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.RIGHT, bType, blockPosition));


        CombineInstance[] array = new CombineInstance[quads.Count];

        for (int i = 0; i < array.Length; i++)
            array[i].mesh = quads[i];

        Mesh cube = new Mesh();
        //since our cubes are created on the correct spot already, we dont need a matrix, and so far, no light data
        cube.CombineMeshes(array, true, false, false);
        cube.Optimize();

        return cube;
    }


    protected override void OnCreate()
    {
        //OriginCube = MakeCubeAtZero();
        OriginCube = MeshComponents.tileMesh;

        base.OnCreate();
    }

    private Entity prefabEntity;

    protected override void OnStartRunning()
    {
        SingleCube = new RenderMesh
        {
            mesh = OriginCube,
            material = MeshComponents.textureAtlas
        };
        base.OnStartRunning();

        Entities.ForEach((ref PrefabEntityComponent prefabEntityComponent) => {
            prefabEntity = prefabEntityComponent.prefabEntity;
        });

    }

    protected override void OnUpdate()
    {

        Entities.WithNone<RenderMesh>().WithAll<CubePosition>().ForEach((Entity en, ref CubePosition cube) =>
        {
            if (!cube.HasCube)
            {
                Entity hexTile = EntityManager.Instantiate(prefabEntity);
                EntityManager.AddComponentData(hexTile, new Parent { Value = cube.parent });
                EntityManager.SetComponentData(hexTile, new Translation { Value = cube.position });
                EntityManager.SetComponentData(hexTile, new Rotation { Value = quaternion.identity });
                EntityManager.AddComponentData(hexTile, new LocalToParent { });
                //EntityManager.AddSharedComponentData(en, SingleCube);
                //EntityManager.SetComponentData(en, new RenderBounds { Value = OriginCube.bounds.ToAABB() });

                PostUpdateCommands.DestroyEntity(en);

                //cube.HasCube = true;
            }
        });
    }
}

[DisableAutoCreation]
public class CreateMeshJobSystem : JobComponentSystem
{
    private EntityQuery m_Group;

    private EntityArchetype ZeroCube;
    private RenderMesh SingleCube;
    private Mesh OriginCube;

    private EndSimulationEntityCommandBufferSystem beginPresentationEntityCommandBufferSystem;

    private enum Cubeside { BOTTOM, TOP, LEFT, FRONT, BACK, RIGHT };

    private const float ATLAS_SIZE = 0.03125f;

    private readonly Vector2[,] blockUVs = {
        /*GRASS TOP*/ {
                new Vector2(19 * ATLAS_SIZE, 29 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 29 * ATLAS_SIZE),
                                    new Vector2(19 * ATLAS_SIZE, 28 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 28 * ATLAS_SIZE)},
        /*GRASS SIDE*/ {
                new Vector2(19 * ATLAS_SIZE, 28 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 28 * ATLAS_SIZE),
                                new Vector2(19 * ATLAS_SIZE, 27 * ATLAS_SIZE), new Vector2(20 * ATLAS_SIZE, 27 * ATLAS_SIZE)},
        /*DIRT*/ {
                new Vector2(18 * ATLAS_SIZE, 32 * ATLAS_SIZE), new Vector2(19 * ATLAS_SIZE, 32 * ATLAS_SIZE),
                                new Vector2(18 * ATLAS_SIZE, 31 * ATLAS_SIZE), new Vector2(19 * ATLAS_SIZE, 31 * ATLAS_SIZE)},
        /*STONE*/ {
                new Vector2(20 * ATLAS_SIZE, 26 * ATLAS_SIZE), new Vector2(21 * ATLAS_SIZE, 26 * ATLAS_SIZE),
                                new Vector2(20 * ATLAS_SIZE, 25 * ATLAS_SIZE), new Vector2(21 * ATLAS_SIZE, 25 * ATLAS_SIZE)},
        /*DIAMOND*/ {
                new Vector2(0 * ATLAS_SIZE, 1 * ATLAS_SIZE), new Vector2(1 * ATLAS_SIZE, 1 * ATLAS_SIZE),
                                new Vector2(1 * ATLAS_SIZE, 0 * ATLAS_SIZE), new Vector2(1 * ATLAS_SIZE, 1 * ATLAS_SIZE)}
        };


    private Mesh MakeCubeAtZero()
    {

        List<Mesh> quads = new List<Mesh>();

        BlockType bType = BlockType.DIRT;
        Vector3 blockPosition = new Vector3(0, 0, 0);

        quads.Add(CreateQuads(Cubeside.FRONT, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.BACK, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.TOP, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.BOTTOM, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.LEFT, bType, blockPosition));
        quads.Add(CreateQuads(Cubeside.RIGHT, bType, blockPosition));


        CombineInstance[] array = new CombineInstance[quads.Count];

        for (int i = 0; i < array.Length; i++)
            array[i].mesh = quads[i];

        Mesh cube = new Mesh();
        //since our cubes are created on the correct spot already, we dont need a matrix, and so far, no light data
        cube.CombineMeshes(array, true, false, false);
        cube.Optimize();

        return cube;
    }

    private Mesh CreateQuads(Cubeside side, BlockType bType, Vector3 worldChunkPosition)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ScriptedMesh" + side.ToString();

        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];
        Vector2[] uvs = new Vector2[4];

        int[] triangles = new int[6];

        Vector2 uv00;
        Vector2 uv10;
        Vector2 uv01;
        Vector2 uv11;

        if (bType == BlockType.GRASS && side == Cubeside.TOP)
        {
            uv00 = blockUVs[0, 0];
            uv10 = blockUVs[0, 1];
            uv01 = blockUVs[0, 2];
            uv11 = blockUVs[0, 3];

        }
        else if (bType == BlockType.GRASS && side == Cubeside.BOTTOM)
        {
            uv00 = blockUVs[(int)(BlockType.DIRT + 1), 0];
            uv10 = blockUVs[(int)(BlockType.DIRT + 1), 1];
            uv01 = blockUVs[(int)(BlockType.DIRT + 1), 2];
            uv11 = blockUVs[(int)(BlockType.DIRT + 1), 3];
        }
        else
        {
            uv00 = blockUVs[(int)(bType + 1), 0];
            uv10 = blockUVs[(int)(bType + 1), 1];
            uv01 = blockUVs[(int)(bType + 1), 2];
            uv11 = blockUVs[(int)(bType + 1), 3];
        }

        //All vertices
        Vector3 p0 = new Vector3(-.5f, -.5f, .5f) + worldChunkPosition;
        Vector3 p1 = new Vector3(.5f, -.5f, .5f) + worldChunkPosition;
        Vector3 p2 = new Vector3(.5f, -.5f, -.5f) + worldChunkPosition;
        Vector3 p3 = new Vector3(-.5f, -.5f, -.5f) + worldChunkPosition;
        Vector3 p4 = new Vector3(-.5f, .5f, .5f) + worldChunkPosition;
        Vector3 p5 = new Vector3(.5f, .5f, .5f) + worldChunkPosition;
        Vector3 p6 = new Vector3(.5f, .5f, -.5f) + worldChunkPosition;
        Vector3 p7 = new Vector3(-.5f, .5f, -.5f) + worldChunkPosition;

        switch (side)
        {
            case Cubeside.BOTTOM:
                vertices = new Vector3[] { p0, p1, p2, p3 };
                normals = new Vector3[]
                {
                    Vector3.down,
                    Vector3.down,
                    Vector3.down,
                    Vector3.down
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.TOP:
                vertices = new Vector3[] { p7, p6, p5, p4 };
                normals = new Vector3[]
                {
                    Vector3.up,
                    Vector3.up,
                    Vector3.up,
                    Vector3.up
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.LEFT:
                vertices = new Vector3[] { p7, p4, p0, p3 };
                normals = new Vector3[]
                {
                    Vector3.left,
                    Vector3.left,
                    Vector3.left,
                    Vector3.left
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.RIGHT:
                vertices = new Vector3[] { p5, p6, p2, p1 };
                normals = new Vector3[]
                {
                    Vector3.right,
                    Vector3.right,
                    Vector3.right,
                    Vector3.right
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.FRONT:
                vertices = new Vector3[] { p4, p5, p1, p0 };
                normals = new Vector3[]
                {
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
            case Cubeside.BACK:
                vertices = new Vector3[] { p6, p7, p3, p2 };
                normals = new Vector3[]
                {
                    Vector3.back,
                    Vector3.back,
                    Vector3.back,
                    Vector3.back
                };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                triangles = new int[] { 3, 1, 0, 3, 2, 1 };
                break;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();

        return mesh;

    }

    [BurstCompile]
    private struct CreateCubeJob : IJobChunk
    {
        //[ReadOnly]
        //public RenderMesh renderMesh;
        //[ReadOnly]
        //public AABB value;
        [ReadOnly]
        public ArchetypeChunkComponentType<CubePosition> cubePosition;

        public EntityCommandBuffer.Concurrent commandBuffer;

        private void CreateCubeAt(float3 position, Entity en, Entity parent, int chunkIndex)
        {

            commandBuffer.SetComponent(chunkIndex, en, new Parent { Value = parent });
            commandBuffer.SetComponent(chunkIndex, en, new Translation { Value = position });
            commandBuffer.SetComponent(chunkIndex, en, new Rotation { Value = quaternion.identity });
            commandBuffer.SetComponent(chunkIndex, en, new LocalToParent { });
            //commandBuffer.SetComponent(chunkIndex, en, new LocalToWorld { });
            //commandBuffer.AddSharedComponent(chunkIndex, en, renderMesh);
            //commandBuffer.AddComponent(chunkIndex, en, new RenderBounds { Value = value });
            //commandBuffer.SetComponent(chunkIndex, en, new PerInstanceCullingTag { });
            //commandBuffer.SetComponent(chunkIndex, en, new LocalToParent { });

        }

        public  void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkBuild = chunk.GetNativeArray(cubePosition);

            for (var i = 0; i < chunk.Count; i++)
            {
                CreateCubeAt(chunkBuild[i].position, chunkBuild[i].owner, chunkBuild[i].parent, chunkIndex);
            }

        }
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        //SingleCube = new RenderMesh
        //{
        //    mesh = OriginCube,
        //    material = MeshComponents.textureAtlas
        //};
        ZeroCube = EntityManager.CreateArchetype(typeof(Translation), typeof(Rotation), typeof(LocalToWorld));

    }

    protected override void OnCreate()
    {
        beginPresentationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        //OriginCube = MakeCubeAtZero();
        m_Group = GetEntityQuery(ComponentType.ReadOnly<CubePosition>(), ComponentType.ReadOnly<Parent>());
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var cubePosition = GetArchetypeChunkComponentType<CubePosition>(true);
        CreateCubeJob createCubeJob = new CreateCubeJob
        {
            commandBuffer = beginPresentationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            //value = OriginCube.bounds.ToAABB(),
            cubePosition = cubePosition
        };

        return createCubeJob.Schedule(m_Group, inputDeps);
    }
}
