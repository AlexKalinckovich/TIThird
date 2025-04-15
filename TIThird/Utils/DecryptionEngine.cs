using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace TIThird.Utils
{
    public class DecryptionEngine
    {
        public async Task DecryptFileAsync(
            string inputPath,
            string outputPath,
            BigInteger p,
            BigInteger x) 
        {
            // Всегда вычисляем размер блока через GetPaddedSize(p)
            int blockSize = GetPaddedSize(p);
            int totalBlockSize = blockSize * 2;
            await using var inputStream = File.OpenRead(inputPath);
            await using var outputStream = File.Create(outputPath);
            
            byte[] buffer = new byte[totalBlockSize];
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, totalBlockSize))) == totalBlockSize)
            {
                if (bytesRead < buffer.Length)
                {
                    Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);
                }
                
                byte[] aBytes = new byte[blockSize];
                byte[] bBytes = new byte[blockSize];
                
                Buffer.BlockCopy(buffer, 0, aBytes, 0, blockSize);
                Buffer.BlockCopy(buffer, blockSize, bBytes, 0, blockSize);

                BigInteger a = new BigInteger(aBytes, isUnsigned: true, isBigEndian: false);
                BigInteger b = new BigInteger(bBytes, isUnsigned: true, isBigEndian: false);

                BigInteger decrypted = DecryptBlock(a, b, p, x);
                byte decryptedByte = (byte)(decrypted & 0xFF);
                await outputStream.WriteAsync(new[] { decryptedByte });
            }
        }

        private int GetPaddedSize(BigInteger p)
        {
            return (int)((p.GetBitLength() + 7) / 8);
        }

        private BigInteger DecryptBlock(BigInteger a, BigInteger b, BigInteger p, BigInteger x)
        {
            BigInteger s = MathEngine.FastPow(a, x, p);
            BigInteger sInverse = MathEngine.ModInverse(s, p);
            return (b * sInverse) % p;
        }
    }
}
