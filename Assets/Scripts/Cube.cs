using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public enum BlockType { GRASS, DIRT, DIAMOND, STONE, AIR };

public struct Block
{

    private enum Cubeside { BOTTOM, TOP, LEFT, FRONT, BACK, RIGHT };

    public bool isSolid;
    private BlockType bType;
    private Chunk owner;
    private Vector3 position;

    private const float ATLAS_SIZE = 0.03125f;

    private Vector2[,] blockUVs;

    public Block(BlockType b, Vector3 pos, Chunk owner)
    {
        Vector2[,] blockUVs = {
            /*GRASS TOP*/
            {
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
        //this.chunkMap = chunkMap;
        this.blockUVs = blockUVs;
        this.owner = owner;
        bType = b;
        position = pos;
        //this.material = material;
        if(bType == BlockType.AIR)
            isSolid = false;
        else
            isSolid = true;
    }

    private Mesh CreateQuads(Cubeside side, BlockType bType)
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
        Vector3 p0 = new Vector3(-.5f, -.5f, .5f) + position;
        Vector3 p1 = new Vector3(.5f, -.5f, .5f) + position;
        Vector3 p2 = new Vector3(.5f, -.5f, -.5f) + position;
        Vector3 p3 = new Vector3(-.5f, -.5f, -.5f) + position;
        Vector3 p4 = new Vector3(-.5f, .5f, .5f) + position;
        Vector3 p5 = new Vector3(.5f, .5f, .5f) + position;
        Vector3 p6 = new Vector3(.5f, .5f, -.5f) + position;
        Vector3 p7 = new Vector3(-.5f, .5f, -.5f) + position;

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

    public bool HasSolidNeighbour(int x, int y, int z, ConcurrentDictionary<int3, Chunk> chunkMap)
    {
        //If this is ever is Jobified, this will be a memory sink
        Block[,,] chunks;

        if(x < 0 || x >= MeshComponents.chunkSize ||
            y < 0 || y >= MeshComponents.chunkSize ||
            z < 0 || z >= MeshComponents.chunkSize)
        {
            //Block in another chunk
            float3 neighbourChunkPos = owner.position + new float3((x - (int)position.x) * MeshComponents.chunkSize,
                                                                     (y - (int)position.y) * MeshComponents.chunkSize,
                                                                     (z - (int)position.z) * MeshComponents.chunkSize);
            string nName = MeshComponents.BuildChunkName(neighbourChunkPos);
            x = ConvertBlockIndexToLocal(x);
            y = ConvertBlockIndexToLocal(y);
            z = ConvertBlockIndexToLocal(z);

            if(chunkMap.TryGetValue(new int3(neighbourChunkPos), out Chunk nChunk))
            {
                chunks = nChunk.chunkData;
            }
            else
            {
                //Edge of the known world
                return false;
            }
        } else
        {
            //Block in this chunk
            chunks = owner.chunkData;        
        }

        if (chunks.Length != 0)
        {
            try
            {
                return chunks[x, y, z].isSolid;
            }
            catch (System.IndexOutOfRangeException ex) { }
            return false;
        }
        else
        {
            return false;
        }
    }

    private int ConvertBlockIndexToLocal(int i)
    {
        if (i == -1)
            i = MeshComponents.chunkSize - 1;
        else if (i == MeshComponents.chunkSize)
            i = 0;
        return i;
    }

    public Mesh CreateCube(ConcurrentDictionary<int3, Chunk> chunkMap)
    {
        if (bType == BlockType.AIR) return null;

        List<Mesh> quads = new List<Mesh>();

        if(!HasSolidNeighbour((int)position.x, (int)position.y ,(int)position.z + 1, chunkMap))
            quads.Add(CreateQuads(Cubeside.FRONT, bType));
        if (!HasSolidNeighbour((int)position.x, (int)position.y, (int)position.z - 1, chunkMap))
            quads.Add(CreateQuads(Cubeside.BACK, bType));
        if (!HasSolidNeighbour((int)position.x, (int)position.y + 1, (int)position.z, chunkMap))
            quads.Add(CreateQuads(Cubeside.TOP, bType));
        if (!HasSolidNeighbour((int)position.x, (int)position.y - 1, (int)position.z, chunkMap))
            quads.Add(CreateQuads(Cubeside.BOTTOM, bType));
        if (!HasSolidNeighbour((int)position.x - 1, (int)position.y, (int)position.z, chunkMap))
            quads.Add(CreateQuads(Cubeside.LEFT, bType));
        if (!HasSolidNeighbour((int)position.x + 1, (int)position.y, (int)position.z, chunkMap))
            quads.Add(CreateQuads(Cubeside.RIGHT, bType));
        

        CombineInstance[] array = new CombineInstance[quads.Count];

        for (int i = 0; i < array.Length; i++)
            array[i].mesh = quads[i];

        Mesh cube = new Mesh();
        //since our cubes are created on the correct spot already, we dont need a matrix, and so far, no light data
        cube.CombineMeshes(array, true, false, false);

        return cube;
    }
}
