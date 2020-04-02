using Unity.Entities;
using Unity.Mathematics;


[InternalBufferCapacity(16)]
public struct EntitiesBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Entity(EntitiesBuffer e) { return e.Value; }
    public static implicit operator EntitiesBuffer(Entity e) { return new EntitiesBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Entity Value;
}

[InternalBufferCapacity(64)]
public struct VerticesBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator float3(VerticesBuffer e) { return e.Value; }
    public static implicit operator VerticesBuffer(float3 e) { return new VerticesBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public float3 Value;
}

[InternalBufferCapacity(64)]
public struct TrianglesBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int3(TrianglesBuffer e) { return e.Value; }
    public static implicit operator TrianglesBuffer(int3 e) { return new TrianglesBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public int3 Value;
}

[InternalBufferCapacity(512)]
public struct BlockTypeBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator BlockType(BlockTypeBuffer e) { return e.Value; }
    public static implicit operator BlockTypeBuffer(BlockType e) { return new BlockTypeBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public BlockType Value;
}

[InternalBufferCapacity(512)]
public struct WorldChunksBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int3(WorldChunksBuffer e) { return e.Value; }
    public static implicit operator WorldChunksBuffer(int3 e) { return new WorldChunksBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public int3 Value;
}

[InternalBufferCapacity(64)]
public struct TestBuffer : IBufferElementData
{
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int(TestBuffer e) { return e.Value; }
    public static implicit operator TestBuffer(int e) { return new TestBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public int Value;
}