using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System;

public enum ChunkStatus { DRAW, DONE, KEEP };

public struct WorldChunk : IComponentData
{
    public int3 position;
    public ChunkStatus status;
}

public struct CurrentChunkFlag : IComponentData { };
public struct DrawChunkFlag : IComponentData { };
public struct BuildCollisionMeshFlag : IComponentData { };
public struct DoneChunkFlag : IComponentData { };
public struct BuildMeshFlag : IComponentData { };
public struct CubeFlag : IComponentData { };

public struct CubePosition : IComponentData {
    public float3 position;
    public BlockType type;
    public bool HasCube;
    public Entity owner;
    public Entity parent;
};

public struct MegaChunk : IComponentData {
    public float3 center;
    public bool spawnCubes;
    public Entity entity;
}

public struct MoveChunkEvent : IComponentData
{
    public Entity megaChunk;
    public float3 originalPosition;
}

public struct BuildUltraChunkEvent : IComponentData
{
    public Entity eventEntity;
    public float3 positionToBuild;
}

public struct QueueRemoveUltraChunkFlag : IComponentData { };

public struct DeleteNowFlag : IComponentData { };

public struct RemoveUltraChunkEvent : IComponentData {
    public float3 group;
};

public struct QueueRemoveEntityFlag : IComponentData { };

public struct DeleteEntityEvent : IComponentData
{
    public Entity entity;
}

struct BuildCubes : ISharedComponentData
{
    public bool spawnCubes;
}

struct UltraChunk : IComponentData, IEquatable<UltraChunk>
{
    public Entity entity;
    public float3 center;
    public bool startBuild;

    public bool Equals(UltraChunk obj)
    {
        return center.Equals(obj.center);
    }
}

struct UltraChunkGroup : ISharedComponentData
{
    public float3 ultrachunkPosition;
}

struct CurrentUltraChunkFlag : IComponentData { }

struct QueueManager : IComponentData { }