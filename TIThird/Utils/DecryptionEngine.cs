using System.Numerics;
using System.IO;

namespace TIThird.Utils;

public class DecryptionEngine
{
    private int _blockSize;

    public async Task DecryptFileAsync(
        string inputPath,
        string outputPath,
        BigInteger p,
        BigInteger x)
    {
        _blockSize = GetPaddedSize(p);

        await using var inputStream = File.OpenRead(inputPath);
        await using var outputStream = File.Create(outputPath);

        byte[] buffer = new byte[_blockSize * 2];
        int bytesRead;

        while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) == buffer.Length)
        {
            byte[] aBytes = new byte[_blockSize];
            byte[] bBytes = new byte[_blockSize];

            Buffer.BlockCopy(buffer, 0, aBytes, 0, _blockSize);
            Buffer.BlockCopy(buffer, _blockSize, bBytes, 0, _blockSize);

            BigInteger a = new BigInteger(aBytes, isUnsigned: true, isBigEndian: false);
            BigInteger b = new BigInteger(bBytes, isUnsigned: true, isBigEndian: false);

            BigInteger decrypted = DecryptBlock(a, b, p, x);

            // Оставляем только младший байт (так как мы шифровали по одному байту)
            byte decryptedByte = (byte)(decrypted & 0xFF);
            await outputStream.WriteAsync(new[] { decryptedByte });
        }
    }

    private BigInteger DecryptBlock(BigInteger a, BigInteger b, BigInteger p, BigInteger x)
    {
        // s = a^x mod p
        BigInteger s = MathEngine.FastPow(a, x, p);

        // s^-1 mod p
        BigInteger sInverse = MathEngine.ModInverse(s, p);

        // m = (b * s^-1) mod p
        return (b * sInverse) % p;
    }

    private int GetPaddedSize(BigInteger p)
    {
        return (int)((p.GetBitLength() + 7) / 8);
    }
}