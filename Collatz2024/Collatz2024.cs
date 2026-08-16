using BigRationalLibraryNamespace;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Collatz2024Namespace
{
    public class Collatz2024
    {
        #region Public Methods
        public static List<int[]> GenerateExponentPermutations(int length, int order)
        {
            // Returns a materialized list of all int[] of given length with values in [1,order]
            // such that the maximum value present is exactly 'order'.
            // (Previously yielded an IEnumerable<int[]>; List<int[]> still supports LINQ usage.)
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (order <= 0) throw new ArgumentOutOfRangeException(nameof(order));

            // Optional capacity hint (guard against overflow / huge allocations)
            List<int[]> result;
            try
            {
                // count = order^length - (order-1)^length
                double countD = Math.Pow(order, length) - Math.Pow(order - 1, length);
                int capacity = countD > int.MaxValue ? int.MaxValue : (int)countD;
                result = new List<int[]>(capacity);
            }
            catch
            {
                result = new List<int[]>();
            }

            int[] current = new int[length];

            void Recurse(int index, bool hasMax)
            {
                if (index == length)
                {
                    if (hasMax)
                        result.Add((int[])current.Clone());
                    return;
                }

                // If max not yet used and this is the last slot, force it to be 'order'
                if (!hasMax && index == length - 1)
                {
                    current[index] = order;
                    Recurse(index + 1, true);
                    return;
                }

                for (int v = 1; v <= order; v++)
                {
                    current[index] = v;
                    Recurse(index + 1, hasMax || v == order);
                }
            }

            Recurse(0, false);
            return result;
        }
        public static BigInteger NextOdd(BigInteger _Collatz)
        {
            BigInteger two = new BigInteger(2);
            BigInteger result = _Collatz * 3 + 1;
            while (BigInteger.Remainder(result, two) == 0)
            {
                result /= 2;
            }
            return result;
        }
        public static BigInteger CollapseInOne(Int32 _N)
        {
            BigInteger retval = (BigInteger.Pow(4, _N) - 1) / (BigInteger)3;
            return retval;
        }
        public static BigInteger CollapseInOne_ModOneOut(Int32 _N)
        {
            BigInteger retval = (BigInteger.Pow(4, 3 * _N + 1) - 1) / (BigInteger)3;
            return retval;
        }
        public static BigInteger CollapseInOne_ModTwoOut(Int32 _N)
        {
            BigInteger retval = (BigInteger.Pow(4, 3 * _N - 1) - 1) / (BigInteger)3;
            return retval;
        }
        public static BigInteger CollapseInTwo_ModOne(Int32 _N1, Int32 _N2)
        {
            BigInteger retval = CollapseInOne_ModOneOut(_N1);
            retval *= BigInteger.Pow(2, 2 * _N2);
            Debug.Assert(--retval % 3 == 0, "Collatz2024.CollapseInTwo_ModOne");
            return retval / 3;
        }
        public static BigInteger CollapseInTwo_ModTwo(Int32 _N1, Int32 _N2)
        {
            BigInteger retval = CollapseInOne_ModTwoOut(_N1);
            retval *= BigInteger.Pow(2, 2 * _N2 - 1);
            Debug.Assert(--retval % 3 == 0, "Collatz2024.CollapseInTwo_ModTwo");
            return retval / 3;
        }
        public static string toBinaryBigEndianString(BigInteger _BigInt)
        {
            if (_BigInt > long.MaxValue)
                return toBinaryBigEndianStringGtInt64(_BigInt);
            string binaryString = Convert.ToString((long)_BigInt, 2);
            char[] binaryCharArray = binaryString.ToCharArray();
            Array.Reverse(binaryCharArray);
            string retval = new string(binaryCharArray);
            //if (_BigInt <= long.MaxValue)
            //{
            //    string hh = toBinaryBigEndianStringGtInt64(_BigInt);
            //    if (!retval.Equals(hh))
            //        Debug.Assert(true);
            //}
            return retval;
        }
        public static string toBinaryLittleEndianStringGtInt64(BigInteger _BigInt)
        {
            // Special case for zero
            if (_BigInt == 0) return "0";
            if (_BigInt > long.MaxValue)
                return toBinaryLittleEndianStringGtInt64(_BigInt);

            StringBuilder binaryString = new StringBuilder();
            BigInteger tempBigInt = _BigInt;

            // Extract bits from the BigInteger starting from the least significant bit
            while (tempBigInt > 0)
            {
                // Prepend '1' or '0' depending on the current bit
                binaryString.Insert(0, (tempBigInt & 1) == 1 ? '1' : '0');
                tempBigInt >>= 1; // Right shift to get the next bit
            }
            string retval = binaryString.ToString();
            //if (_BigInt <= long.MaxValue)
            //{
            //    string hh = toBinaryLittleEndianStringGtInt64(_BigInt);
            //    if (!retval.Equals(hh))
            //        Debug.Assert(true);
            //}
            return retval;
        }
        public static string toBinaryBigEndianStringGtInt64(BigInteger _BigInt)
        {
            if (_BigInt == 0) return "0";

            StringBuilder binaryString = new StringBuilder();
            while (_BigInt > 0)
            {
                // Append '1' or '0' depending on the least significant bit
                binaryString.Append((_BigInt & 1) == 1 ? '1' : '0');
                _BigInt >>= 1; // Right shift to move to the next bit
            }

            return binaryString.ToString();
        }
        public static string toBinaryLittleEndianString(BigInteger bigInt)
        {
            byte[] bytes = bigInt.ToByteArray();
            Array.Reverse(bytes);
            string retval = string.Join("", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))).TrimStart('0');
            return retval;
        }
        public static BigInteger toBigIntegerFromBinaryLittleEndianString(string _LittleEndianBinaryText)
        {
            BigInteger retval = 0;
            foreach (char c in _LittleEndianBinaryText)
                retval = (retval << 1) | (c == '1' ? 1 : 0);
            return retval;
        }
        public static BigInteger toBigIntegerFromBinaryBigEndianString(string _BigEndianBinaryText)
        {
            BigInteger retval = 0;
            for (int i = _BigEndianBinaryText.Length - 1; i >= 0; i--)
            {
                retval = (retval << 1) | (_BigEndianBinaryText[i] == '1' ? 1 : 0);
            }
            return retval;
        }
        public static IEnumerable<string> GetBinaryBigEndianDecaysInOne()
        {
            StringBuilder sb = new StringBuilder("1");
            while (true)
            {
                yield return sb.ToString();
                sb.Append("01");
            }
        }
        public static UInt64 OddStepCountToOne(BigInteger _N)
        {
            Debug.Assert(_N > 0, "Collatz2024.OddStepCountToOne");
            BigInteger n = _N;
            while (n.IsEven)
                n >>= 1;
            UInt64 retval = 0;
            while (true)
            {
                retval++;
                n = NextOdd(n);
                if (n == 1)
                    break;
            }
            return retval;
        }
        public static UInt64 OddStepCountToSmaller(BigInteger _N)
        {
            Debug.Assert(_N > 0, "Collatz2024.OddStepCountToSmaller");
            BigInteger n = _N;
            while (n.IsEven)
                n >>= 1;
            UInt64 retval = 0;
            while (true)
            {
                retval++;
                n = NextOdd(n);
                if (n < _N)
                    break;
            }
            return retval;
        }
        public static ulong DecayInN_FormulaList(int _C, CollatzDecayFormulaRecursive _RecursiveFormula)
        {
            // f(n) = 2^2 * f(n-1) + 1
            // f(n) = 2^6 * f(n-1) + 35
            // f(n) = 2^6 * f(n-1) + 49

            // f(n) = (2^(2n+2) - 1) / 3^1
            // f(n) = [2^(6n-1) - 5] / 3^2 
            // f(n) = [2^(6n+4) - 7] / 3^2
            if (_RecursiveFormula.IsMember(_C))
                return _RecursiveFormula.StepsToOne;
            return 0;
        }
        public static bool SolveForLoop(int[] _TwosExponentArray, out BigRational _N)
        {
            _N = BigRational.Zero;
            bool isInteger = false;
            try
            {
                if (_TwosExponentArray is null)
                    throw new ArgumentNullException(nameof(_TwosExponentArray));
                if (_TwosExponentArray.Length == 0)
                    throw new ArgumentException("Must supply at least one exponent.", nameof(_TwosExponentArray));

                foreach (var k in _TwosExponentArray)
                    if (k <= 0)
                        throw new ArgumentOutOfRangeException(nameof(_TwosExponentArray), "All exponents must be positive integers (>0).");

                int m = _TwosExponentArray.Length;

                // Unified formula also works for m == 1
                BigInteger K = 0;
                foreach (int k in _TwosExponentArray)
                    K += k;

                if (K > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(_TwosExponentArray), "Sum of exponents too large (exceeds int.MaxValue).");

                BigInteger pow2K = BigInteger.Pow(2, (int)K);
                BigInteger pow3m = BigInteger.Pow(3, m);
                BigInteger denominator = pow2K - pow3m;

                // For positive exponents denominator cannot be zero (2^K = 3^m has no positive solution), but guard anyway.
                if (denominator == 0)
                    throw new InvalidOperationException("Degenerate denominator (unexpected 2^K == 3^m).");

                BigInteger S = BigInteger.Zero;
                BigInteger prefix = BigInteger.Zero;
                for (int i = 0; i < m; i++)
                {
                    BigInteger term3 = BigInteger.Pow(3, m - 1 - i);
                    BigInteger term2 = BigInteger.Pow(2, (int)prefix);
                    S += term3 * term2;
                    prefix += _TwosExponentArray[i];
                }

                _N = BigRational.Create(S, denominator);
                isInteger = _N.IsInteger;
            }
            catch
            {
                _N = BigRational.Zero;
                return false;
            }
            return isInteger && _N > 0;
        }
        #endregion Public Methods
    }
    public interface ICollatzDecayFormula
    {
        UInt32 StepsToOne { get; }
        public bool IsMember(BigInteger _C);
        public BigInteger NthMember(int _N);
        // override string ToString();
    }
    public class CollatzDecayFormulaRecursive : ICollatzDecayFormula
    {
        #region Properties
        public UInt32 StepsToOne { get; }
        public BigInteger PowerOfTwo { get; }
        public Int32 TwosExponent { get; }
        public Int64 AdditiveConstant { get; }
        private List<BigInteger> DecayAnchorList;
        private HashSet<BigInteger> DecayAnchorHashSet;
        #endregion Properties
        #region Constructors
        private CollatzDecayFormulaRecursive()
        {
            DecayAnchorList = new List<BigInteger>();
            DecayAnchorHashSet = new HashSet<BigInteger>();
        }
        public CollatzDecayFormulaRecursive(UInt32 _StepsToOne, Int32 _TwosExponent, Int64 _AdditiveConstant) : this()
        {
            // f(n) = 2^2 * f(n-1) + 1
            // f(n) = 2^6 * f(n-1) + 35
            // f(n) = 2^6 * f(n-1) + 49
            StepsToOne = _StepsToOne;
            TwosExponent = _TwosExponent;
            PowerOfTwo = BigInteger.Pow(2, TwosExponent);
            AdditiveConstant = _AdditiveConstant;
            if (StepsToOne == 1)
            {
                DecayAnchorList.Add(1);
                DecayAnchorHashSet.Add(1);
            }
            if (StepsToOne == 2)
            {
                switch (AdditiveConstant)
                {
                    case 35:
                        DecayAnchorList.Add(3);
                        DecayAnchorHashSet.Add(3);
                        break;
                    case 49:
                        DecayAnchorList.Add(113);
                        DecayAnchorHashSet.Add(113);
                        break;
                    default:
                        Debug.Assert(false, "CollatzDecayFormula.new bad parameters");
                        break;
                }
            }
        }
        public CollatzDecayFormulaRecursive(ICollatzDecayFormula _PredecessorFormula, int _ModThree) : this()
        {
            // f(n) = 2^2 * f(n-1) + 1
            // f(n) = 2^6 * f(n-1) + 35
            // f(n) = 2^6 * f(n-1) + 49
            const int successThreshold = 7;
            Debug.Assert(_ModThree == 1 || _ModThree == 2, "CollatzDecayFormulaRecursive.new()");
            BigInteger prevAnchor, anchor, pow2 = -1;
            Int64 addConst = -1;
            bool isOk = false;
            int n = 0, log2ratio = -1;
            while (true)
            {
                prevAnchor = _PredecessorFormula.NthMember(n++);
                if (prevAnchor == 1)
                    continue;
                if (prevAnchor % 3 == _ModThree)
                {
                    anchor = (_ModThree == 1) ? 4 * prevAnchor - 1 : 2 * prevAnchor - 1;
                    Debug.Assert(anchor % 3 == 0, "CollatzDecayFormulaRecursive.new()");
                    anchor /= 3;
                    DecayAnchorList.Add(anchor);
                    DecayAnchorHashSet.Add(anchor);
                    if (DecayAnchorList.Count > 1)
                    {
                        double ratio = (double)DecayAnchorList[DecayAnchorList.Count - 1] / (double)DecayAnchorList[DecayAnchorList.Count - 2];
                        log2ratio = (int)Math.Truncate(Math.Log(ratio, 2));
                        pow2 = BigInteger.One << log2ratio;
                        addConst = (Int64)(DecayAnchorList[DecayAnchorList.Count - 1] - pow2 * DecayAnchorList[DecayAnchorList.Count - 2]);
                        isOk = true;
                        for (int i = 1; i < DecayAnchorList.Count; i++)
                        {
                            if (DecayAnchorList[i] != pow2 * DecayAnchorList[i - 1] + addConst)
                            {
                                isOk = false;
                                break;
                            }
                        }
                    }
                }
                if (DecayAnchorList.Count > successThreshold)
                    break;
            }
            Debug.Assert(isOk, "CollatzDecayFormulaRecursive.new()");
            PowerOfTwo = pow2;
            TwosExponent = log2ratio;
            AdditiveConstant = addConst;
            StepsToOne = _PredecessorFormula.StepsToOne + 1;
            foreach (BigInteger c in DecayAnchorList)
                if (Collatz2024.OddStepCountToOne(c) != StepsToOne)
                    Debug.Assert(Collatz2024.OddStepCountToOne(c) == StepsToOne, "CollatzDecayFormulaRecursive.new()");
        }
        #endregion Constructors
        #region Public Methods
        public bool IsMember(BigInteger _C)
        {
            // f(n) = 2^2 * f(n-1) + 1
            // f(n) = 2^6 * f(n-1) + 35
            // f(n) = 2^6 * f(n-1) + 49
            if (_C < 1)
                return false;
            //if (_C == 113 && AdditiveConstant == 49)
            //    Debug.Assert(true);
            BigInteger c = _C;
            while (c.IsEven)
                c >>= 1;
            while (c > DecayAnchorList.Last())
            {
                BigInteger nextValue = PowerOfTwo * DecayAnchorList.Last() + AdditiveConstant;
                DecayAnchorList.Add(nextValue);
                DecayAnchorHashSet.Add(nextValue);
            }
            if (DecayAnchorHashSet.Contains(c))
                return true;
            while ((c - 1) % 4 == 0 && c > 4)
            {
                {
                    --c;
                    c >>= 2;
                    if (c.IsEven)
                    {
                        c <<= 2;
                        ++c;
                        break;
                    }
                }
            }
            return DecayAnchorHashSet.Contains(c);
        }
        public BigInteger NthMember(int _N)
        {
            while (_N >= DecayAnchorList.Count)
            {
                BigInteger nextValue = PowerOfTwo * DecayAnchorList.Last() + AdditiveConstant;
                DecayAnchorList.Add(nextValue);
                DecayAnchorHashSet.Add(nextValue);
            }
            return DecayAnchorList[_N];
        }
        public override string ToString()
        {
            return $"f(n) = 2^{TwosExponent} * f(n-1) + {AdditiveConstant}";
        }
        #endregion Public Methods
    }
    public class CollatzDecayFormula : ICollatzDecayFormula
    {
        #region Properties
        public UInt32 StepsToOne { get; }
        public int N_Factor { get; }
        public int N_Constant { get; }
        public Int32 ThreesExponent { get; }
        public BigInteger PowerOfThree { get; }
        public Int64 SubtractiveConstant { get; }
        #endregion Properties
        #region Constructors
        private CollatzDecayFormula()
        {
        }
        public CollatzDecayFormula(UInt32 _StepsToOne, int _nFactor, int _nConst, Int64 _SubtractiveConstant, Int32 _ThreesExponent) : this()
        {
            StepsToOne = _StepsToOne;
            N_Factor = _nFactor;
            N_Constant = _nConst;
            ThreesExponent = _ThreesExponent;
            PowerOfThree = BigInteger.Pow(3, ThreesExponent);
            SubtractiveConstant = _SubtractiveConstant;
        }
        #endregion Constructors
        #region Public Methods
        public bool IsMember(BigInteger _fn)
        {
            // f(n) = (2^(2n+2) - 1) / 3^1
            // f(n) = [2^(6n-1) - 5] / 3^2 
            // f(n) = [2^(6n+4) - 7] / 3^2
            if (_fn < 1)
                return false;
            if (_fn == 113 && SubtractiveConstant == 7)
                Debug.Assert(true);
            BigInteger c = _fn;
            while (c.IsEven)
                c >>= 1;
            while ((c - 1) % 4 == 0 && c > 4)
            {
                {
                    --c;
                    c >>= 2;
                    if (c.IsEven)
                    {
                        c <<= 2;
                        ++c;
                        break;
                    }
                }
            }

            BigInteger pow2 = c * PowerOfThree + SubtractiveConstant;
            if (!pow2.IsEven)
                return false;
            // Is a power of 2?
            if ((pow2 & (pow2 - 1)) != 0)
                return false;
            int log2 = 0;
            while (pow2 > 1)
            {
                pow2 >>= 1;
                log2++;
            }
            log2 -= N_Constant;
            if (log2 == 0)
                return StepsToOne == 1;
            return log2 % N_Factor == 0;
        }
        public override string ToString()
        {
            // f(n) = [2^(6n+4) - 7] / 3^2
            return $"f(n) = [2^({N_Factor}n+{N_Constant}) - {SubtractiveConstant}] / 3^{ThreesExponent}";
        }
        public BigInteger NthMember(int _N)
        {
            return 0;
        }
        #endregion Public Methods
    }
    public class CollatzDecayFormulaBitManipulation : ICollatzDecayFormula
    {
        #region Properties
        public UInt32 StepsToOne { get; }
        #endregion Properties
        #region Constructors
        private CollatzDecayFormulaBitManipulation()
        {
        }
        public CollatzDecayFormulaBitManipulation(UInt32 _StepsToOne) : this()
        {
            StepsToOne = _StepsToOne;
        }
        #endregion Constructors

        public bool IsMember(BigInteger _C)
        {
            if (_C < 1)
                return false;
            bool retval = false;
            BigInteger c = _C;
            while (c.IsEven)
                c >>= 1;
            if (StepsToOne > 1)
            {
                while ((c & 3) == 1) // Equivalent to c % 4 == 1
                    c = (c - 1) >> 2; // Equivalent to (c - 1) / 4
                if (c.IsEven)
                    c = (c << 2) + 1;
            }
            string bitString = Collatz2024.toBinaryBigEndianString(c);
            switch (StepsToOne)
            {
                case 1:
                    // Must be of the form 1010101...
                    retval = IsPatternMatch(bitString, 1, "1", "01", "");
                    break;
                case 2:
                    // Must be of the form 1000111000111... or 11000111000111...
                    retval = IsPatternMatch(bitString, 2, "1", "000111", "");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "");
                    break;
                case 3:
                    // To 5  1.000111.000111.000111.0001 or 11.000111.000111.000111.0001
                    retval = IsPatternMatch(bitString, 2, "1", "000111", "0001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "0001");
                    // To 85  1.000111.000111.000111.01001 or 11.000111.000111.000111.01001
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "01001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "01001");
                    // To 341 1.000111.000111.000111.101001 or 11.000111.000111.000111.101001
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "101001");
                    // To 21845 1.000111.00110111.101001 11.000111.00110111.101001
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "00110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "00110111101001");
                    // To 5461 
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "000110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "000110111101001");
                    // To 349525 
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "0000010110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "0000010110111101001");
                    // To 1398101 
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "0001000010110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "0001000010110111101001");
                    // To 22369621
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "01001000010110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "01001000010110111101001");
                    // To 89478485
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "1", "000111", "101001000010110111101001");
                    if (!retval)
                        retval = IsPatternMatch(bitString, 2, "11", "000111", "101001000010110111101001");
                    break;

                default:
                    throw new NotImplementedException();
            }
            return retval;
        }
        public BigInteger NthMember(int _N)
        {
            BigInteger retval = -1;
            StringBuilder sb = new('1');
            switch (StepsToOne)
            {
                case 1:
                    for (int i = 1; i <= _N; i++)
                        sb.Append("01");
                    break;
                default:
                    throw new NotImplementedException();
            }
            retval = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb.ToString());
            return retval;
        }
        private bool IsPatternMatch(string _BinaryBE, int _MinBits, string _Prefix, string _Repeat, string _Suffix)
        {
            if (_BinaryBE.Length < _MinBits || _BinaryBE.Length < _Prefix.Length + _Suffix.Length)
                return false;
            for (int i = 0; i < _Prefix.Length; i++)
            {
                if (_Prefix[i] != _BinaryBE[i])
                    return false;
            }
            int suffixStartIndex = _BinaryBE.Length - _Suffix.Length;
            for (int i = 0; i < _Suffix.Length; i++)
            {
                if (_Suffix[i] != _BinaryBE[suffixStartIndex + i])
                    return false;
            }
            if ((_BinaryBE.Length - _Prefix.Length - _Suffix.Length) % _Repeat.Length != 0)
                return false;
            int baseIndex = _BinaryBE.Length - _Suffix.Length - _Repeat.Length;
            while (baseIndex > _Prefix.Length - 1)
            {
                for (int i = _Repeat.Length - 1; i >= 0; i--)
                {
                    if (_Repeat[i] != _BinaryBE[baseIndex + i])
                        return false;
                }
                baseIndex -= _Repeat.Length;
            }
            return true;
        }
    }
}
