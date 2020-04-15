using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;

public class BuildBiomes : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private List<BuildBiome> biomeList;

    [ExcludeComponent(typeof(BuildBiome))]
    private struct Biomes : IJobForEachWithEntity<MegaChunk>
    {
        [ReadOnly]
        public int seed;
        [ReadOnly]
        public BuildBiome forest;
        [ReadOnly]
        public BuildBiome plain;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index,[ReadOnly] ref MegaChunk c0)
        {
            if (seed > 7)
                commandBuffer.AddSharedComponent(index, entity, forest);
            else
                commandBuffer.AddSharedComponent(index, entity, plain);
        }
    }

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        biomeList = new List<BuildBiome>
        {
            new BuildBiome { biomeType = BiomeType.FOREST },
            new BuildBiome { biomeType = BiomeType.PLAIN }
        };

        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Biomes biomes = new Biomes
        {
            seed = UnityEngine.Random.Range(0, 10),
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            forest = biomeList[0],
             plain = biomeList[1]
        };

        inputDeps = biomes.Schedule(this, inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}
