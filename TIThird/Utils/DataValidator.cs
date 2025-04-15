using System.Numerics;
using TIThird.Exceptions;

namespace TIThird.Utils;

public class DataValidator
{
    private DataValidator() { }
    
    
    public static bool IsPValid(string p, out BigInteger pNumberValue)
    {
        const int minPNumberValue = 256;
        if (!BigInteger.TryParse(p, out BigInteger pNumber))
        {
            pNumberValue = BigInteger.MinusOne;
            return false;
        }
        pNumberValue = pNumber;

        if (pNumberValue < minPNumberValue)
        {
            throw new OutOfBoundsException();
        }
        
        if (!MathEngine.IsPrime(pNumberValue))
        {
            throw new ValueNotPrimeException();
        }
        
        return true;
    }

    public static bool IsKValid(string k, BigInteger p, out BigInteger kNumberValue)
    {
        if (!BigInteger.TryParse(k, out BigInteger kNumber))
        {
            kNumberValue = BigInteger.MinusOne;
            return false;
        }

        // k должно быть в диапазоне [1, p-1] и взаимно просто с p-1!!!!!!!!!!!!!!
        // P - число из IsPValid
        kNumberValue = kNumber;

        BigInteger upperBound = p - 1;
        
        if (kNumberValue < 1 || kNumberValue > upperBound)
        {
            throw new OutOfBoundsException();
        }

        if (MathEngine.Gcd(kNumber, upperBound) != 1)
        {
            throw new NotRelativeException();
        }
        
        return true;
    }

    public static bool IsXValid(string x, BigInteger p, out BigInteger xNumberValue)
    {
        if (!BigInteger.TryParse(x, out BigInteger xNumber))
        {
            xNumberValue = BigInteger.MinusOne;
            return false;
        }

        // x ∈ [2, p-2] для выбора секретного ключа
        // p - число из IsPValid
        xNumberValue = xNumber;
        
        BigInteger upperBound = p - 2;
        
        if (xNumberValue < 2 || xNumberValue > upperBound)
        {
            throw new OutOfBoundsException();
        }
        
        return true;
    }
}