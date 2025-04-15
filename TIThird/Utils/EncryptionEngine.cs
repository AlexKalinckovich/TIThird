using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;
using TIThird.View;

namespace TIThird.Utils
{
    public class EncryptionEngine
    {
        private const int MaxViewEntries = 1000;
        private int _viewEntryCount = 0;
        private BlockingCollection<(byte[] data, int index)> _encryptionQueue = new();
        private CancellationTokenSource _cts = new();
        private int _activeWorkers;
        private readonly Lock _lock = new();
        private int _blockSize; 
        private BigInteger _a; // вычисляемое заранее значение a = g^k mod p

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

            _a = MathEngine.FastPow(g, k, p);

            Task writeTask = Task.Run(() => WriteWorker(outputPath, _cts.Token));
            try
            {
                await ReadAndEncryptPerByteAsync(inputPath, p, k, x, g, _cts.Token);
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
            _viewEntryCount = 0;
        }

        private async Task ReadAndEncryptPerByteAsync(
            string inputFilePath,
            BigInteger p,
            BigInteger k,
            BigInteger x,
            BigInteger g,
            CancellationToken ct)
        {
            await using var inputStream = File.OpenRead(inputFilePath);
            int index = 0;
            int currentByte;
            while ((currentByte = inputStream.ReadByte()) != -1 && !ct.IsCancellationRequested)
            {
                byte[] data = [(byte)currentByte];
                int currentIndex = index++;

                Interlocked.Increment(ref _activeWorkers);
                _ = Task.Run(() =>
                {
                    try
                    {
                        byte[] encrypted = EncryptBlock(data, p, k, x, g, currentIndex, _blockSize);
                        _encryptionQueue.Add((encrypted, currentIndex), ct);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _activeWorkers);
                    }
                }, ct);
            }
        }

        /// <summary>
        /// Шифрует один байт входных данных.
        /// Числа a и b сериализуются в массив размерности blockSize (полученной через GetPaddedSize(p)).
        /// Результат – массив длины blockSize*2, содержащий представление a и b.
        /// </summary>
        private byte[] EncryptBlock(byte[] data,
                                   in BigInteger p,
                                   in BigInteger k,
                                   in BigInteger x,
                                   in BigInteger g,
                                   int index,
                                   int blockSize)
        {
            if (data.Length != 1)
                throw new ArgumentException("Data must be exactly one byte for single-byte encryption.");

            // Представляем входной байт как число
            BigInteger m = data[0];

            // Вычисляем y = g^x mod p, затем b = (y^k * m) mod p.
            BigInteger y = MathEngine.FastPow(g, x, p);
            BigInteger b = (MathEngine.FastPow(y, k, p) * m) % p;

            // Добавляем значение в представление (например, для UI-отображения)

            byte[] result = new byte[blockSize * 2];
            // Сериализуем a и b в массив фиксированной длины blockSize.
            byte[] aBytes = ToFixedSize(_a, blockSize);
            byte[] bBytes = ToFixedSize(b, blockSize);
            Buffer.BlockCopy(aBytes, 0, result, 0, blockSize);
            Buffer.BlockCopy(bBytes, 0, result, blockSize, blockSize);
            return result;
        }

        /// <summary>
        /// Функция ToFixedSize приводит значение BigInteger к массиву байт фиксированной длины.
        /// Если полученный массив короче требуемого размера, дополняет его нулями (младшие байты остаются первыми).
        /// Если длиннее – генерируется исключение.
        /// </summary>
        public byte[] ToFixedSize(in BigInteger value, int size)
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

        /// <summary>
        /// Вычисляет размер в байтах, необходимый для представления p.
        /// </summary>
        public int GetPaddedSize(BigInteger p)
        {
            int size = (int)((p.GetBitLength() + 7) / 8);
            return size;
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
                    UpdateValueView(writePosition, data);
                    outputStream.Write(data, 0, data.Length);
                    orderedBlocks.Remove(writePosition);
                    writePosition++;
                }
            }
        }

        private void UpdateValueView(int writePosition, byte[] data)
        {
            if (_viewEntryCount < MaxViewEntries)
            {
                int size = data.Length / 2;
                byte[] aBytes = new byte[size];
                byte[] bBytes = new byte[size];
                Buffer.BlockCopy(data, 0, aBytes, 0, size);
                Buffer.BlockCopy(data, size, bBytes, 0, size);
                
                BigInteger a = new BigInteger(aBytes, isUnsigned: true);
                BigInteger b = new BigInteger(bBytes, isUnsigned: true);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_viewEntryCount < MaxViewEntries)
                    {
                        MainWindow.AbEntries.Add(new AbEntry { Index = writePosition, A = a, B = b });
                        _viewEntryCount++;
                    }
                }, DispatcherPriority.Background);
            }
        }
    }
}
