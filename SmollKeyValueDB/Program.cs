using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SmollKeyValueDB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}

internal sealed class StaticKeyValueSizeDataStore<TKey, TValue> : IStaticKeyValueStore
    where TKey : unmanaged
    where TValue : unmanaged
{
    private readonly Dictionary<TKey, TValue> _DataStore = new Dictionary<TKey, TValue>();

    public bool IsKeyValueSizeCorrect(Span<byte> keyBytes, Span<byte> valueBytes)
    {
        return keyBytes.Length == Unsafe.SizeOf<TKey>() &&
               valueBytes.Length == Unsafe.SizeOf<TValue>();
    }

    public bool TryAdd(Span<byte> keyBytes, Span<byte> valueBytes)
    {
        TKey key = MemoryMarshal.Read<TKey>(keyBytes);
        TValue value = MemoryMarshal.Read<TValue>(valueBytes);

        _DataStore[key] = value;
        return true;
    }

    public bool Exists(Span<byte> keyBytes)
    {
        TKey key = MemoryMarshal.Read<TKey>(keyBytes);

        return _DataStore.ContainsKey(key);
    }

    public bool TryGet(Span<byte> keyBytes, Span<byte> valueBuffer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(valueBuffer.Length, Unsafe.SizeOf<TValue>());

        TKey key = MemoryMarshal.Read<TKey>(keyBytes);
        ref readonly TValue value = ref CollectionsMarshal.GetValueRefOrNullRef(_DataStore, key);
        if (Unsafe.IsNullRef(in value))
        {
            return false;
        }

        MemoryMarshal.Write(valueBuffer, in value);
        return true;
    }

    public bool TryRemove(Span<byte> keyBytes)
    {
        TKey key = MemoryMarshal.Read<TKey>(keyBytes);

        return _DataStore.Remove(key);
    }
}

internal interface IStaticKeyValueStore : IDataStore
{
    bool IsKeyValueSizeCorrect(Span<byte> keyBytes, Span<byte> valueBytes);
}

internal interface IDataStore
{
    bool TryAdd(Span<byte> keyBytes, Span<byte> valueBytes);
    bool Exists(Span<byte> keyBytes);
    bool TryGet(Span<byte> keyBytes, Span<byte> valueBuffer);
    bool TryRemove(Span<byte> keyBytes);
}



internal sealed class RootDataStore
{
    private readonly Dictionary<string, IStaticKeyValueStore> _StaticHashKeyValueDataStores = new();
}