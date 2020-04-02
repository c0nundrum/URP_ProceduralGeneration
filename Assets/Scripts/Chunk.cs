using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

public struct ChunkValue {
    //Make blocks and chunks in one struct, move mesh generation to outside the system, save only the position and cube type array of the chunk
    
    public float3 position;

    public NativeArray<BlockType> GetBlockArray()
    {
        NativeArray<BlockType> blockArray = new NativeArray<BlockType>((int)math.pow(MeshComponents.chunkSize, 3), Allocator.Temp); // this gets sent to dynamic buffer

        for (int z = 0; z < MeshComponents.chunkSize; z++)
        {
            for (int y = 0; y < MeshComponents.chunkSize; y++)
            {
                for (int x = 0; x < MeshComponents.chunkSize; x++)
                {
                    float3 pos = new float3(x, y, z);
                    int worldX = (int)(x + position.x);
                    int worldY = (int)(y + position.y);
                    int worldZ = (int)(z + position.z);

                    if (Utils.FBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f)
                        blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.AIR;
                    else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
                        if (Utils.FBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.38f && worldY < 40)
                            blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.DIAMOND;
                        else
                            blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.STONE;
                    else if (worldY == Utils.GenerateHeight(worldX, worldZ))
                        blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.GRASS;
                    else if (worldY < Utils.GenerateHeight(worldX, worldZ))
                        blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.DIRT;
                    else
                        blockArray[x + MeshComponents.chunkSize * (y + MeshComponents.chunkSize * z)] = BlockType.AIR;
                }

            }
        }

        return blockArray;
    }

}

public struct Chunk
{
    public Block[,,] chunkData;
    public float3 position;
    //public string chunkName;
    //public int3 concurrentChunkName;

    public Chunk(float3 position)
    {
        this.position = position;
        chunkData = new Block[MeshComponents.chunkSize, MeshComponents.chunkSize, MeshComponents.chunkSize];
        for (int z = 0; z < MeshComponents.chunkSize; z++)
        {
            for (int y = 0; y < MeshComponents.chunkSize; y++)
            {
                for (int x = 0; x < MeshComponents.chunkSize; x++)
                {
                    float3 pos = new float3(x, y, z);
                    int worldX = (int)(x + position.x);
                    int worldY = (int)(y + position.y);
                    int worldZ = (int)(z + position.z);

                    if (Utils.FBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f)
                        chunkData[x, y, z] = new Block(BlockType.AIR, pos, this);
                    else if (worldY <= Utils.GenerateStoneHeight(worldX, worldZ))
                        if (Utils.FBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.38f && worldY < 40)
                            chunkData[x, y, z] = new Block(BlockType.DIAMOND, pos, this);
                        else
                            chunkData[x, y, z] = new Block(BlockType.STONE, pos, this);
                    else if (worldY == Utils.GenerateHeight(worldX, worldZ))
                        chunkData[x, y, z] = new Block(BlockType.GRASS, pos, this);
                    else if (worldY < Utils.GenerateHeight(worldX, worldZ))
                        chunkData[x, y, z] = new Block(BlockType.DIRT, pos, this);
                    else
                        chunkData[x, y, z] = new Block(BlockType.AIR, pos, this);
                }

            }
        }
    }

}
