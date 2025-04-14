using System.Collections.Concurrent;
using System.IO;
using System.Numerics;

namespace TIThird.Utils;

public class EncryptionEngine
{
    private BlockingCollection<(byte[] data, int index)> _encryptionQueue = new();
    private CancellationTokenSource _cts = new();
    private int _activeWorkers;
    private int _blockSize;
    private readonly Lock _lock = new();
    
    public async Task EncryptFileAsync(
        string inputPath,
        string outputPath,
        BigInteger p,
        BigInteger k,
        BigInteger x,
        BigInteger g)
    {
        ResetState();

        _blockSize = GetPaddedSize(p);

        Task writeTask = Task.Run(() => WriteWorker(outputPath, _cts.Token));
        
        try
        {
            await ReadAndEncryptPerByteAsync(inputPath, p, k, x, g, _blockSize, _cts.Token);
            await WaitForCompletionAsync();
        }
        finally
        {
            _encryptionQueue.CompleteAdding();
            await writeTask;
        }
    }

    private Task WaitForCompletionAsync()
    {
        lock (_lock)
        {
            while (_activeWorkers != 0 || _encryptionQueue.Count != 0)
            {
                Thread.Sleep(100); 
            }
        }
        return Task.CompletedTask;
    }

    private void ResetState()
    {
        if (_encryptionQueue.IsAddingCompleted)
        {
            _encryptionQueue.Dispose();
            _cts.Dispose();
            _encryptionQueue = new BlockingCollection<(byte[], int)>();
            _cts = new CancellationTokenSource();
        }
        _activeWorkers = 0;
    }

    private async Task ReadAndEncryptPerByteAsync(
        string inputFilePath,
        BigInteger p,
        BigInteger k,
        BigInteger x,
        BigInteger g,
        int blockSize,
        CancellationToken ct)
    {
        await using var inputStream = File.OpenRead(inputFilePath);
        int index = 0;
        int currentByte;
        while ((currentByte = inputStream.ReadByte()) != -1 && !ct.IsCancellationRequested)
        {
            byte[] data = new byte[] { (byte)currentByte };
            int currentIndex = index++;

            Interlocked.Increment(ref _activeWorkers);

            _ = Task.Run(() =>
            {
                try
                {
                    var encrypted = EncryptBlock(data, p, k, x, g);
                    _encryptionQueue.Add((encrypted, currentIndex), ct);
                }
                finally
                {
                    Interlocked.Decrement(ref _activeWorkers);
                }
            }, ct);
        }
    }

    private void WriteWorker(string outputFilePath, CancellationToken ct)
    {
        using var outputStream = File.Create(outputFilePath);
        SortedDictionary<int, byte[]> orderedBlocks = new SortedDictionary<int, byte[]>();
        int writePosition = 0;

        foreach (var item in _encryptionQueue.GetConsumingEnumerable(ct))
        {
            orderedBlocks[item.index] = item.data;

            while (orderedBlocks.TryGetValue(writePosition, out var data))
            {
                outputStream.Write(data, 0, data.Length);
                orderedBlocks.Remove(writePosition);
                writePosition++;
            }
        }
    }

    public byte[] EncryptBlock(byte[] data, BigInteger p, BigInteger k, BigInteger x, BigInteger g)
    {
        int blockSize = GetPaddedSize(p);

        if (data.Length != 1)
            throw new ArgumentException("Data must be exactly one byte for single-byte encryption.");

        byte[] paddedData = new byte[blockSize];
        paddedData[0] = data[0]; // little-endian: младший байт в начало

        BigInteger m = new BigInteger(paddedData, isUnsigned: true, isBigEndian: false);

        BigInteger y = MathEngine.FastPow(g, x, p);
        BigInteger a = MathEngine.FastPow(g, k, p);
        BigInteger b = (MathEngine.FastPow(y, k, p) * m) % p;

        byte[] aBytes = ToFixedSize(a, blockSize);
        byte[] bBytes = ToFixedSize(b, blockSize);

        byte[] result = new byte[blockSize * 2];
        Buffer.BlockCopy(aBytes, 0, result, 0, blockSize);
        Buffer.BlockCopy(bBytes, 0, result, blockSize, blockSize);

        return result;
    }

    private byte[] ToFixedSize(in BigInteger value, int size)
    {
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);

        if (bytes.Length > size)
            throw new OverflowException("Value exceeds target size");

        if (bytes.Length < size)
        {
            byte[] padded = new byte[size];
            Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
            return padded;
        }

        return bytes;
    }

    public int GetPaddedSize(BigInteger p)
    {
        return (int)((p.GetBitLength() + 7) / 8);
    }
}
