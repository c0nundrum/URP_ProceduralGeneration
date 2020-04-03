using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;
using Material = UnityEngine.Material;

public class MeshComponents : MonoBehaviour
{
    public static Dictionary<float3, ChunkTable> _lookupMesh;
    public Camera mainCamera;
    public Mesh tileMeshComp;
    public static MeshComponents instance;
    public static int columnHeight = 8;
    
    public readonly static int chunkSize = 32;
    public readonly static int radius = 8;
    public static int _drawDistance;
    public static int worldSize = 2;

    public static Mesh tileMesh;
    //public static List<string> toRemove = new List<string>();
    public Material material;
    public int drawDistance = 5000;

    public float3 lasbuildPos;

    public static ConcurrentDictionary<string, Chunk> chunks;
    public NativeHashMap<int3, Chunk> chunkMap;

    public static string BuildChunkName(float3 f)
    {
        return (int)f.x + "_" + (int)f.y + "_" + (int)f.z;
    }

    public static int3 ConcurrentChunkName(float3 f)
    {
        return new int3((int)f.x, (int)f.y, (int)f.z);
    }

    public static Material textureAtlas;

    private EntityArchetype archetype;
    private EntityCommandBuffer entityCommandBuffer;
    private EntityManager entityManager;
    private Mesh mesh;
    private bool firstbuild = true;

    private const float ATLAS_SIZE = 0.03125f;
    public Block[,,] chunkData;

    private void Awake()
    {
        _lookupMesh = new Dictionary<float3, ChunkTable>();
        textureAtlas = material;
        tileMesh = tileMeshComp;
        _drawDistance = drawDistance;
        instance = this;
    }

    public void BuildNearPlayer()
    {
        StopCoroutine("BuildRecursiveWorld");
        StartCoroutine(BuildRecursiveWorld((int)(mainCamera.transform.position.x / chunkSize), (int)(mainCamera.transform.position.y / chunkSize), (int)(mainCamera.transform.position.z / chunkSize), radius));
    }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 ppos = Camera.main.transform.position;

        //Camera.main.transform.position = new Vector3(ppos.x, Utils.GenerateHeight(ppos.x, ppos.z) + 10, ppos.z);

        lasbuildPos = mainCamera.transform.position;
        firstbuild = true;

        //entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //archetype = entityManager.CreateArchetype(typeof(RenderMesh), typeof(LocalToWorld), typeof(Translation), typeof(Rotation), typeof(WorldChunk), typeof(PhysicsCollider));
        //archetype = entityManager.CreateArchetype(typeof(WorldChunk), typeof(LocalToWorld), typeof(RenderMesh));

        //chunks = new ConcurrentDictionary<string, Chunk>();
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;

        //Build starting Chunk
        //BuildChunkAt((int)(Camera.main.transform.position.x/chunkSize), (int)(Camera.main.transform.position.y / chunkSize), (int)(Camera.main.transform.position.z / chunkSize));

        //Draw it
        //StartCoroutine(DrawChunks());

        //Build Bigger World

        StartChunkPipeline(false);
        //StartNonChunkPipeline(true);

    }

    private void StartChunkPipeline(bool continuousWorldBuild)
    {
        //StartBuildMegaChunksJob();
        //StartBuildUltraChunksJob();
        StartConstructChunksJob();
        StartBuildChunkMesh();
        StartDrawMesh();
        if(continuousWorldBuild)
            StartBuildQueue();
    }

    private void StartNonChunkPipeline(bool continuousWorldBuild)
    {
        StartBuildMesh();
        StartBuildMegaChunksJob();
        StartBuildUltraChunksJob();
        StartFillChunks();
        if (continuousWorldBuild)
            StartBuildQueue();
    }

    // Update is called once per frame
    void Update()
    {
        float3 movement = lasbuildPos - new float3(mainCamera.transform.position);

        //if(math.length(movement) > chunkSize)
        //{
        //    lasbuildPos = mainCamera.transform.position;
        //    BuildNearPlayer();
        //}

        //if (firstbuild)
        //{
        //    StartCamera();
        //    firstbuild = false;
        //}
        //StartCoroutine(DrawChunks());
    }

    private void CreateChunkController()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity en = entityManager.CreateEntity();
        entityManager.AddBuffer<WorldChunksBuffer>(en);

    }

    private void StartCamera()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<CameraControlSystem>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }


    private void StartBuildQueue()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<CurrentChunkSystem>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartFillChunks()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<FillChunks>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartBuildMegaChunksJob()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<BuildMegaChunk>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartBuildUltraChunksJob()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<BuildUltraChunk>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartConstructChunksJob()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<ContructMegaChunks>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartBuildMesh()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<BuildMeshSystem>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartBuildChunkMesh()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<BuildChunkMesh>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartDrawMesh()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<DrawMesh>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    private void StartDeleteChunksJob()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

        var countSystem = world.GetOrCreateSystem<DeleteMegaChunk>();

        simulationSystemGroup.AddSystemToUpdateList(countSystem);

        simulationSystemGroup.SortSystemUpdateList();

        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    //private void StartDeleteUltraChunksJob()
    //{
    //    var world = World.DefaultGameObjectInjectionWorld;
    //    var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

    //    var countSystem = world.GetOrCreateSystem<DeleteUltraChunk>();

    //    simulationSystemGroup.AddSystemToUpdateList(countSystem);

    //    simulationSystemGroup.SortSystemUpdateList();

    //    ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    //}

    private void BuildChunkAt(int x, int y, int z)
    {
        Vector3 chunkPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
        string n = BuildChunkName(chunkPosition);
        if (!chunks.TryGetValue(n, out Chunk c))
        {
            //c = new Chunk(chunkPosition, textureAtlas, entityManager, archetype);
            //chunks.TryAdd(c.chunkName, c);
            //chunkMap.TryAdd(c.concurrentChunkName, c);
            //c.DrawChunk();
        }
    }

    private IEnumerator BuildRecursiveWorld(int x, int y, int z, int rad)
    {
        rad--;
        if (rad <= 0) yield break;

        //Build Chunk Front
        BuildChunkAt(x, y, z + 1);
        StartCoroutine(BuildRecursiveWorld(x, y, z + 1, rad));
        yield return null;

        //Build Chunk Back
        BuildChunkAt(x, y, z - 1);
        StartCoroutine(BuildRecursiveWorld(x, y, z - 1, rad));
        yield return null;

        //Build Chunk left
        BuildChunkAt(x -1, y, z);
        StartCoroutine(BuildRecursiveWorld(x - 1, y, z, rad));
        yield return null;

        //Build Chunk right
        BuildChunkAt(x + 1, y, z);
        StartCoroutine(BuildRecursiveWorld(x + 1, y, z, rad));
        yield return null;

        //Build Chunk up
        BuildChunkAt(x, y + 1, z);
        StartCoroutine(BuildRecursiveWorld(x, y + 1, z, rad));
        yield return null;

        //Build Chunk Down
        BuildChunkAt(x, y - 1, z);
        StartCoroutine(BuildRecursiveWorld(x, y - 1, z, rad));
        yield return null;
    }

    private IEnumerator DrawChunks()
    {
        foreach(KeyValuePair<string, Chunk> c in chunks)
        {
            //if(c.Value.status == ChunkStatus.DRAW)
            //{
            //    Chunk value = c.Value;
            //    value.DrawChunk();
            //    value.status = ChunkStatus.KEEP;
            //    chunks[c.Key] = value;
            //}
            yield return null;
        }
    }

    //private IEnumerator BuildWorld()
    //{
    //    int posx = (int)math.floor(mainCamera.transform.position.x / chunkSize);
    //    int posz = (int)math.floor(mainCamera.transform.position.z / chunkSize);

    //    float totalChunks = (math.pow(radius * 2 + 1, 2) * columnHeight) * 2;

    //    for (int z = -radius; z <= radius; z++)
    //        for (int x = -radius; x <= radius; x++)
    //            for (int y = 0; y < columnHeight; y++)
    //            {
    //                float3 chunkPosition = new float3((x+posx) * chunkSize, y * chunkSize, (z+posz) * chunkSize);
    //                Chunk c;
    //                string n = BuildChunkName(chunkPosition);
    //                if(chunks.TryGetValue(n, out c))
    //                {
    //                    //c.status = ChunkStatus.KEEP;
    //                    break;
    //                }
    //                else
    //                {
    //                    //c = new Chunk(chunkPosition, textureAtlas, entityManager, archetype);
    //                    chunks.TryAdd(c.chunkName, c);
    //                }

    //                yield return null;
    //            }

    //    if (firstbuild)
    //    {
    //        var world = World.DefaultGameObjectInjectionWorld;
    //        var simulationSystemGroup = world.GetOrCreateSystem<SimulationSystemGroup>();

    //        var countSystem = world.GetOrCreateSystem<CameraControlSystem>();

    //        simulationSystemGroup.AddSystemToUpdateList(countSystem);

    //        simulationSystemGroup.SortSystemUpdateList();

    //        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);

    //        firstbuild = false;
    //    }

    //}

    //public void StartBuild()
    //{
    //    StartCoroutine(BuildWorld());
    //}

}
