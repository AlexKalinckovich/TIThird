using System.Numerics;
using System.Security.Cryptography;

namespace TIThird.Utils;

public class MathEngine
{

    public static bool IsPrime(BigInteger number, int iterations = 5)
    {
        // NEGATIVE NUMBERS AND ONE IS NOT PRIME
        if (number <= 1) return false;
        // 2,3 IS PRIME
        if (number <= 3) return true;
        // IF NUMBER IS EVEN => IT IS NOT PRIME
        if (number.IsEven) return false;

        // WE NEED TO DECOMPOSE VALUES IN => n - 1 = d * (2 ^ s)
        (BigInteger d, int s) = Decompose(number - 1);
    
        // IT IS CRYPTOGRAPHY SAVE RANDOM GENERATOR
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[number.GetByteCount()];
    
        for (int i = 0; i < iterations; i++)
        {
            // FILL ARRAY OF BYTES WITH STRONG RANDOM BYTES
            rng.GetBytes(bytes);
            // CREATING A RANDOM NUMBER (+ 2 IS FOR BOUND [1,N - 2])
            BigInteger a = BigInteger.Abs(new BigInteger(bytes)) % (number - 3) + 2;
        
            // NOW CALCULATING (a ^ b mod n)
            BigInteger x = FastPow(a, d, number);
            
            // JUST RULE CONDITION, IF X IS THAT => SKIP
            if (x == 1 || x == number - 1) continue;

            bool isComposite = true;
            for (int j = 0; j < s - 1; j++)
            {
                // x = x^2 mod n
                x = FastPow(x, 2, number);
                if (x == number - 1)
                {
                    isComposite = false;
                    break;
                }
            }
            if (isComposite) return false;
        }
        return true;
    }

    
    private static (BigInteger d, int s) Decompose(BigInteger numMinusOne)
    {
        BigInteger d = numMinusOne;
        int s = 0;
        // WHILE d IS EVEN
        while (d % 2 == 0)
        {
            // DIVISION BY TWO
            d /= 2;
            // INCREMENT POWER
            s += 1;
        }
        return (d, s);
    }
    
    public static BigInteger FastPow(BigInteger baseVal, BigInteger exponent, BigInteger mod)
    {
        // IF MOD IS ONE => ALWAYS ZERO
        if (mod == BigInteger.One) return 0;
        
        BigInteger result = BigInteger.One;
        
        // NORMALIZING baseVal (mod is number from IsPrime function) 
        // TO BOUND [0, mod-1]
        // FOR RULE a^k ≡ (a mod n)^k (mod n)
        baseVal %= mod;
    
        while (exponent > 0)
        {
            if (exponent % 2 == 1)
                result = (result * baseVal) % mod;
        
            // ((baseVal % mod) * (baseVal % mod)) % mod 
            // BUT NORMALIZATION WAS IN (baseVal % mod)
            baseVal = (baseVal * baseVal) % mod;
            exponent >>= 1;
        }
        return result < 0 ? result + mod : result;
    }


    public static List<BigInteger> FindPrimitivesRoots(BigInteger p, bool isCheckedForPrime = false)
    {
        if (!isCheckedForPrime && !IsPrime(p)) return [];
    
        // EULER FUNCTION PARAMETER
        BigInteger phi = p - 1;
    
        // UNIQUE PRIMES DIVISORS OF NUMBER PHI
        List<BigInteger> factors = GetUniquePrimes(phi);
    
        List<BigInteger> roots = [];
        Parallel.For(2, (int)Math.Min((decimal)(p - 1), int.MaxValue), g =>
        {
        
            bool isRoot = true;
            
            // CHECKING ALL FACTORS 
            foreach (var factor in factors)
            {
                /*
                 *  Проверяем g=5:

                    5^(22/2) mod 23 = 5^11 mod 23 = 22 ≠ 1

                    5^(22/11) mod 23 = 5^2 mod 23 = 2 ≠ 1
                    ⇒ 5 — первообразный корень
                 */
                if (FastPow(g, phi / factor, p) == 1)
                {
                    isRoot = false;
                    break;
                }
            }
        
            if (isRoot)
                lock (roots) { roots.Add(g); }
        });
        return roots.OrderByDescending(x => x).Take(100_000).ToList();
    }


    public static BigInteger Gcd(BigInteger a, BigInteger b)
    {
        a = BigInteger.Abs(a);
        b = BigInteger.Abs(b);

        while (b != 0)
        {
            BigInteger temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    
    private static List<BigInteger> GetUniquePrimes(BigInteger n)
    {
        List<BigInteger> factors = [];
        if (n == 1) return factors;
    
        if (n % 2 == 0)
        {
            factors.Add(2);
            while (n % 2 == 0) n >>= 1;
        }
    
        for (BigInteger i = 3; i * i <= n; i += 2)
        {
            if (n % i == 0)
            {
                factors.Add(i);
                while (n % i == 0) n /= i;
            }
        }
    
        if (n > 2) factors.Add(n);
        return factors;
    }
    
    public static BigInteger ModInverse(BigInteger a, BigInteger mod)
    {
        BigInteger gcd = BigInteger.GreatestCommonDivisor(a, mod);
        if (gcd != 1)
            throw new ArgumentException("Обратный элемент не существует.");
    
        BigInteger x, y;
        ExtendedEuclidean(a, mod, out x, out y);
        return (x % mod + mod) % mod;
    }

    private static void ExtendedEuclidean(BigInteger a, BigInteger b, out BigInteger x, out BigInteger y)
    {
        if (b == 0)
        {
            x = 1;
            y = 0;
            return;
        }
        ExtendedEuclidean(b, a % b, out y, out x);
        y -= (a / b) * x;
    }
}