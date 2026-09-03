using HalHeinrich.Numerics;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HalHeinrich.Numerics.Collatz;

/// <summary>
/// Static helpers for the Collatz decay experiments: exponent permutations, odd-step counting,
/// base-2 string conversions, and loop solving over <see cref="BigInteger"/>.
/// </summary>
public static class CollatzMath
{
    #region Public Methods
    /// <summary>
    /// Returns every <see cref="int"/> array of length <paramref name="length"/> whose entries lie in
    /// [1, <paramref name="order"/>] and whose maximum entry is exactly <paramref name="order"/>.
    /// </summary>
    /// <param name="length">Array length; must be positive.</param>
    /// <param name="order">Largest permitted entry, and the value that must appear at least once; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> or <paramref name="order"/> is not positive.</exception>
    public static IReadOnlyList<int[]> GenerateExponentPermutations(int length, int order)
    {
        // Returns a materialized list of all int[] of given length with values in [1,order]
        // such that the maximum value present is exactly 'order'.
        // (Previously yielded an IEnumerable<int[]>; List<int[]> still supports LINQ usage.)
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);

        // Optional capacity hint (guard against overflow / huge allocations)
        List<int[]> result;
        try
        {
            // count = order^length - (order-1)^length
            double countD = Math.Pow(order, length) - Math.Pow(order - 1, length);
            int capacity = countD > int.MaxValue ? int.MaxValue : (int)countD;
            result = new List<int[]>(capacity);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Negative capacity from an overflowed cast: fall back to the default.
            result = new List<int[]>();
        }
        catch (OutOfMemoryException)
        {
            // The hint asked for more than the heap can give: fall back to the default.
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
    /// <summary>
    /// Returns 3&#215;<paramref name="collatz"/>&#160;+&#160;1 divided by two until the result is odd.
    /// </summary>
    /// <param name="collatz">The value to step from.</param>
    public static BigInteger NextOdd(BigInteger collatz)
    {
        BigInteger two = new BigInteger(2);
        BigInteger result = collatz * 3 + 1;
        while (BigInteger.Remainder(result, two) == 0)
        {
            result /= 2;
        }
        return result;
    }
    /// <summary>
    /// Returns (4^<paramref name="n"/> &#8722; 1) / 3 &#8212; the sequence 1, 5, 21, 85, 341, &#8230; for n = 1, 2, 3, &#8230;
    /// </summary>
    public static BigInteger CollapseInOne(Int32 n)
    {
        BigInteger retval = (BigInteger.Pow(4, n) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns (4^(3<paramref name="n"/>&#160;+&#160;1) &#8722; 1) / 3.
    /// </summary>
    public static BigInteger CollapseInOneModOneOut(Int32 n)
    {
        BigInteger retval = (BigInteger.Pow(4, 3 * n + 1) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns (4^(3<paramref name="n"/>&#160;&#8722;&#160;1) &#8722; 1) / 3.
    /// </summary>
    public static BigInteger CollapseInOneModTwoOut(Int32 n)
    {
        BigInteger retval = (BigInteger.Pow(4, 3 * n - 1) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns v / 3 where v = <see cref="CollapseInOneModOneOut"/>(<paramref name="n1"/>) &#215; 2^(2&#215;<paramref name="n2"/>).
    /// </summary>
    /// <remarks>
    /// The code asserts v &#8801; 1 (mod 3); under that invariant v / 3 equals (v &#8722; 1) / 3, which is the
    /// form the expression is written in. The assertion is a <see cref="Debug.Assert(bool, string)"/>
    /// and so is absent from Release builds - see halheinrich/Math#6.
    /// </remarks>
    public static BigInteger CollapseInTwoModOne(Int32 n1, Int32 n2)
    {
        BigInteger retval = CollapseInOneModOneOut(n1);
        retval *= BigInteger.Pow(2, 2 * n2);
        Debug.Assert(--retval % 3 == 0, "CollatzMath.CollapseInTwoModOne");
        return retval / 3;
    }
    /// <summary>
    /// Returns v / 3 where v = <see cref="CollapseInOneModTwoOut"/>(<paramref name="n1"/>) &#215; 2^(2&#215;<paramref name="n2"/>&#160;&#8722;&#160;1).
    /// </summary>
    /// <remarks>
    /// The code asserts v &#8801; 1 (mod 3); under that invariant v / 3 equals (v &#8722; 1) / 3, which is the
    /// form the expression is written in. The assertion is a <see cref="Debug.Assert(bool, string)"/>
    /// and so is absent from Release builds - see halheinrich/Math#6.
    /// </remarks>
    public static BigInteger CollapseInTwoModTwo(Int32 n1, Int32 n2)
    {
        BigInteger retval = CollapseInOneModTwoOut(n1);
        retval *= BigInteger.Pow(2, 2 * n2 - 1);
        Debug.Assert(--retval % 3 == 0, "CollatzMath.CollapseInTwoModTwo");
        return retval / 3;
    }
    /// <summary>
    /// Returns the base-2 digits of <paramref name="bigInt"/>, least-significant digit first.
    /// </summary>
    /// <remarks>
    /// Despite the name, the digit order produced is least-significant-first. Values above
    /// <see cref="long.MaxValue"/> are delegated to <see cref="toBinaryBigEndianStringGtInt64"/>;
    /// below that the conversion runs through <see cref="long"/>, so a negative value yields a
    /// 64-digit two's-complement form rather than a sign-magnitude one.
    /// </remarks>
    public static string toBinaryBigEndianString(BigInteger bigInt)
    {
        if (bigInt > long.MaxValue)
            return toBinaryBigEndianStringGtInt64(bigInt);
        string binaryString = Convert.ToString((long)bigInt, 2);
        char[] binaryCharArray = binaryString.ToCharArray();
        Array.Reverse(binaryCharArray);
        return new string(binaryCharArray);
    }
    /// <summary>
    /// Returns the base-2 digits of <paramref name="bigInt"/>, most-significant digit first, or
    /// <c>"0"</c> when <paramref name="bigInt"/> is zero.
    /// </summary>
    /// <remarks>
    /// The name is wrong twice over and is left alone for now: the digit order produced is
    /// most-significant-first, and the loop below handles any non-negative
    /// <see cref="BigInteger"/> rather than only values above <see cref="long.MaxValue"/>.
    /// That second point is why halheinrich/Math#1's guard could be deleted outright rather
    /// than repaired - there was nothing for it to delegate to.
    /// </remarks>
    public static string toBinaryLittleEndianStringGtInt64(BigInteger bigInt)
    {
        // Special case for zero
        if (bigInt == 0) return "0";

        StringBuilder binaryString = new StringBuilder();
        BigInteger tempBigInt = bigInt;

        // Extract bits from the BigInteger starting from the least significant bit
        while (tempBigInt > 0)
        {
            // Prepend '1' or '0' depending on the current bit
            binaryString.Insert(0, (tempBigInt & 1) == 1 ? '1' : '0');
            tempBigInt >>= 1; // Right shift to get the next bit
        }
        return binaryString.ToString();
    }
    /// <summary>
    /// Returns the base-2 digits of <paramref name="bigInt"/>, least-significant digit first, or
    /// <c>"0"</c> when <paramref name="bigInt"/> is zero.
    /// </summary>
    public static string toBinaryBigEndianStringGtInt64(BigInteger bigInt)
    {
        if (bigInt == 0) return "0";

        StringBuilder binaryString = new StringBuilder();
        while (bigInt > 0)
        {
            // Append '1' or '0' depending on the least significant bit
            binaryString.Append((bigInt & 1) == 1 ? '1' : '0');
            bigInt >>= 1; // Right shift to move to the next bit
        }

        return binaryString.ToString();
    }
    /// <summary>
    /// Returns the base-2 digits of <paramref name="bigInt"/>, most-significant digit first, with
    /// leading zeros trimmed.
    /// </summary>
    /// <remarks>Despite the name, the digit order produced is most-significant-first.</remarks>
    public static string toBinaryLittleEndianString(BigInteger bigInt)
    {
        byte[] bytes = bigInt.ToByteArray();
        Array.Reverse(bytes);
        string retval = string.Join("", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))).TrimStart('0');
        return retval;
    }
    /// <summary>
    /// Reads <paramref name="littleEndianBinaryText"/> left to right, treating the first character as
    /// the most significant base-2 digit.
    /// </summary>
    /// <remarks>Any character other than <c>1</c> contributes a zero digit.</remarks>
    public static BigInteger toBigIntegerFromBinaryLittleEndianString(string littleEndianBinaryText)
    {
        BigInteger retval = 0;
        foreach (char c in littleEndianBinaryText)
            retval = (retval << 1) | (c == '1' ? 1 : 0);
        return retval;
    }
    /// <summary>
    /// Reads <paramref name="bigEndianBinaryText"/> right to left, treating the last character as the
    /// most significant base-2 digit.
    /// </summary>
    /// <remarks>Any character other than <c>1</c> contributes a zero digit.</remarks>
    public static BigInteger toBigIntegerFromBinaryBigEndianString(string bigEndianBinaryText)
    {
        BigInteger retval = 0;
        for (int i = bigEndianBinaryText.Length - 1; i >= 0; i--)
        {
            retval = (retval << 1) | (bigEndianBinaryText[i] == '1' ? 1 : 0);
        }
        return retval;
    }
    /// <summary>
    /// Yields the unbounded sequence <c>1</c>, <c>101</c>, <c>10101</c>, &#8230; &#8212; each element the
    /// previous one with <c>01</c> appended.
    /// </summary>
    /// <remarks>The sequence never ends; callers must bound their own enumeration.</remarks>
    public static IEnumerable<string> GetBinaryBigEndianDecaysInOne()
    {
        StringBuilder sb = new StringBuilder("1");
        while (true)
        {
            yield return sb.ToString();
            sb.Append("01");
        }
    }
    /// <summary>
    /// Strips factors of two from <paramref name="n"/>, then counts applications of
    /// <see cref="NextOdd"/> until the value reaches one.
    /// </summary>
    /// <remarks>Does not return for a value that never reaches one.</remarks>
    public static UInt64 OddStepCountToOne(BigInteger n)
    {
        Debug.Assert(n > 0, "CollatzMath.OddStepCountToOne");
        BigInteger odd = n;
        while (odd.IsEven)
            odd >>= 1;
        UInt64 retval = 0;
        while (true)
        {
            retval++;
            odd = NextOdd(odd);
            if (odd == 1)
                break;
        }
        return retval;
    }
    /// <summary>
    /// Strips factors of two from <paramref name="n"/>, then counts applications of
    /// <see cref="NextOdd"/> until the value is less than <paramref name="n"/> itself.
    /// </summary>
    /// <remarks>Does not return for a value whose orbit never drops below <paramref name="n"/>.</remarks>
    public static UInt64 OddStepCountToSmaller(BigInteger n)
    {
        Debug.Assert(n > 0, "CollatzMath.OddStepCountToSmaller");
        BigInteger odd = n;
        while (odd.IsEven)
            odd >>= 1;
        UInt64 retval = 0;
        while (true)
        {
            retval++;
            odd = NextOdd(odd);
            if (odd < n)
                break;
        }
        return retval;
    }
    /// <summary>
    /// Returns <see cref="CollatzDecayFormulaRecursive.StepsToOne"/> when <paramref name="recursiveFormula"/>
    /// reports <paramref name="c"/> as a member, and zero otherwise.
    /// </summary>
    public static ulong DecayInNFormulaList(int c, CollatzDecayFormulaRecursive recursiveFormula)
    {
        // f(n) = 2^2 * f(n-1) + 1
        // f(n) = 2^6 * f(n-1) + 35
        // f(n) = 2^6 * f(n-1) + 49

        // f(n) = (2^(2n+2) - 1) / 3^1
        // f(n) = [2^(6n-1) - 5] / 3^2 
        // f(n) = [2^(6n+4) - 7] / 3^2
        if (recursiveFormula.IsMember(c))
            return recursiveFormula.StepsToOne;
        return 0;
    }
    /// <summary>
    /// Solves for the rational N implied by a cycle whose successive odd steps divide out 2^k for each
    /// k in <paramref name="twosExponentArray"/>, and reports whether that N is a positive integer.
    /// </summary>
    /// <param name="twosExponentArray">The per-step exponents of two; every entry must be positive.</param>
    /// <param name="n">
    /// Receives S / (2^K &#8722; 3^m), where m is the array length, K the sum of its entries, and
    /// S = &#8721; 3^(m&#8722;1&#8722;i) &#215; 2^(k_0&#160;+&#160;&#8230;&#160;+&#160;k_(i&#8722;1)). Set to zero when the computation is rejected.
    /// </param>
    /// <returns><see langword="true"/> when N is a positive integer; otherwise <see langword="false"/>.</returns>
    /// <remarks>Invalid input is reported through the return value rather than by throwing.</remarks>
    public static bool SolveForLoop(int[] twosExponentArray, out BigRational n)
    {
        n = BigRational.Zero;
        bool isInteger = false;
        try
        {
            ArgumentNullException.ThrowIfNull(twosExponentArray);
            if (twosExponentArray.Length == 0)
                throw new ArgumentException("Must supply at least one exponent.", nameof(twosExponentArray));

            foreach (var k in twosExponentArray)
                if (k <= 0)
                    throw new ArgumentOutOfRangeException(nameof(twosExponentArray), "All exponents must be positive integers (>0).");

            int m = twosExponentArray.Length;

            // Unified formula also works for m == 1
            BigInteger K = 0;
            foreach (int k in twosExponentArray)
                K += k;

            if (K > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(twosExponentArray), "Sum of exponents too large (exceeds int.MaxValue).");

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
                prefix += twosExponentArray[i];
            }

            n = BigRational.Create(S, denominator);
            isInteger = n.IsInteger;
        }
        catch (ArgumentException)
        {
            n = BigRational.Zero;
            return false;
        }
        catch (InvalidOperationException)
        {
            n = BigRational.Zero;
            return false;
        }
        return isInteger && n > 0;
    }
    #endregion Public Methods
}
/// <summary>
/// A family of odd integers that reach one in a fixed number of odd Collatz steps.
/// </summary>
public interface ICollatzDecayFormula
{
    /// <summary>The number of odd Collatz steps every member of this family takes to reach one.</summary>
    public UInt32 StepsToOne { get; }
    /// <summary>Reports whether <paramref name="c"/> belongs to this family.</summary>
    public bool IsMember(BigInteger c);
    /// <summary>Returns the member at zero-based index <paramref name="n"/>.</summary>
    public BigInteger NthMember(int n);
    // override string ToString();
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> whose members satisfy the recurrence
/// f(i) = <see cref="PowerOfTwo"/> &#215; f(i&#8722;1) + <see cref="AdditiveConstant"/> from a seed anchor.
/// </summary>
public class CollatzDecayFormulaRecursive : ICollatzDecayFormula
{
    #region Properties
    /// <inheritdoc/>
    public UInt32 StepsToOne { get; }
    /// <summary>The recurrence's multiplier, 2^<see cref="TwosExponent"/>.</summary>
    public BigInteger PowerOfTwo { get; }
    /// <summary>The base-two logarithm of <see cref="PowerOfTwo"/>.</summary>
    public Int32 TwosExponent { get; }
    /// <summary>The recurrence's additive term.</summary>
    public Int64 AdditiveConstant { get; }
    private readonly List<BigInteger> DecayAnchorList;
    private readonly HashSet<BigInteger> DecayAnchorHashSet;
    #endregion Properties
    #region Constructors
    private CollatzDecayFormulaRecursive()
    {
        DecayAnchorList = new List<BigInteger>();
        DecayAnchorHashSet = new HashSet<BigInteger>();
    }
    /// <summary>
    /// Creates a formula from an explicit recurrence, seeding the anchor list with the known first
    /// member: 1 for <paramref name="stepsToOne"/> 1, and 3 or 113 for <paramref name="stepsToOne"/> 2
    /// with <paramref name="additiveConstant"/> 35 or 49 respectively.
    /// </summary>
    /// <remarks>
    /// Any other combination leaves the anchor list empty and trips a
    /// <see cref="Debug.Assert(bool, string)"/>, which is absent from Release builds &#8212;
    /// see halheinrich/Math#6.
    /// </remarks>
    public CollatzDecayFormulaRecursive(UInt32 stepsToOne, Int32 twosExponent, Int64 additiveConstant) : this()
    {
        // f(n) = 2^2 * f(n-1) + 1
        // f(n) = 2^6 * f(n-1) + 35
        // f(n) = 2^6 * f(n-1) + 49
        StepsToOne = stepsToOne;
        TwosExponent = twosExponent;
        PowerOfTwo = BigInteger.Pow(2, TwosExponent);
        AdditiveConstant = additiveConstant;
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
    /// <summary>
    /// Derives a formula from <paramref name="predecessorFormula"/>: it walks that formula's members,
    /// keeps those congruent to <paramref name="modThree"/> modulo three, maps each such member p to
    /// (4p&#160;&#8722;&#160;1)/3 when <paramref name="modThree"/> is 1 or (2p&#160;&#8722;&#160;1)/3 when it is 2, and infers
    /// <see cref="PowerOfTwo"/> and <see cref="AdditiveConstant"/> once more than seven anchors agree.
    /// </summary>
    /// <param name="predecessorFormula">The formula whose members seed the derivation.</param>
    /// <param name="modThree">Selects the residue class to keep; must be 1 or 2.</param>
    /// <remarks>
    /// The walk is unbounded: a predecessor that never yields enough qualifying members does not
    /// return. Every check here is a <see cref="Debug.Assert(bool, string)"/> and so is absent from
    /// Release builds &#8212; see halheinrich/Math#6.
    /// </remarks>
    public CollatzDecayFormulaRecursive(ICollatzDecayFormula predecessorFormula, int modThree) : this()
    {
        // f(n) = 2^2 * f(n-1) + 1
        // f(n) = 2^6 * f(n-1) + 35
        // f(n) = 2^6 * f(n-1) + 49
        const int successThreshold = 7;
        Debug.Assert(modThree == 1 || modThree == 2, "CollatzDecayFormulaRecursive.new()");
        BigInteger prevAnchor, anchor, pow2 = -1;
        Int64 addConst = -1;
        bool isOk = false;
        int n = 0, log2ratio = -1;
        while (true)
        {
            prevAnchor = predecessorFormula.NthMember(n++);
            if (prevAnchor == 1)
                continue;
            if (prevAnchor % 3 == modThree)
            {
                anchor = (modThree == 1) ? 4 * prevAnchor - 1 : 2 * prevAnchor - 1;
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
        StepsToOne = predecessorFormula.StepsToOne + 1;
        foreach (BigInteger c in DecayAnchorList)
            if (CollatzMath.OddStepCountToOne(c) != StepsToOne)
                Debug.Assert(CollatzMath.OddStepCountToOne(c) == StepsToOne, "CollatzDecayFormulaRecursive.new()");
    }
    #endregion Constructors
    #region Public Methods
    /// <summary>
    /// Reports whether <paramref name="c"/> belongs to this family, extending the anchor list as far as
    /// needed and, failing a direct hit, reversing the 4x&#160;+&#160;1 map until the candidate leaves that
    /// form.
    /// </summary>
    public bool IsMember(BigInteger c)
    {
        // f(n) = 2^2 * f(n-1) + 1
        // f(n) = 2^6 * f(n-1) + 35
        // f(n) = 2^6 * f(n-1) + 49
        if (c < 1)
            return false;
        //if (c == 113 && AdditiveConstant == 49)
        //    Debug.Assert(true);
        BigInteger candidate = c;
        while (candidate.IsEven)
            candidate >>= 1;
        while (candidate > DecayAnchorList.Last())
        {
            BigInteger nextValue = PowerOfTwo * DecayAnchorList.Last() + AdditiveConstant;
            DecayAnchorList.Add(nextValue);
            DecayAnchorHashSet.Add(nextValue);
        }
        if (DecayAnchorHashSet.Contains(candidate))
            return true;
        while ((candidate - 1) % 4 == 0 && candidate > 4)
        {
            {
                --candidate;
                candidate >>= 2;
                if (candidate.IsEven)
                {
                    candidate <<= 2;
                    ++candidate;
                    break;
                }
            }
        }
        return DecayAnchorHashSet.Contains(candidate);
    }
    /// <inheritdoc/>
    public BigInteger NthMember(int n)
    {
        while (n >= DecayAnchorList.Count)
        {
            BigInteger nextValue = PowerOfTwo * DecayAnchorList.Last() + AdditiveConstant;
            DecayAnchorList.Add(nextValue);
            DecayAnchorHashSet.Add(nextValue);
        }
        return DecayAnchorList[n];
    }
    /// <summary>Returns the recurrence in the form <c>f(n) = 2^E * f(n-1) + A</c>.</summary>
    public override string ToString()
    {
        return $"f(n) = 2^{TwosExponent} * f(n-1) + {AdditiveConstant}";
    }
    #endregion Public Methods
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> whose members are given in closed form as
/// [2^(<see cref="NFactor"/>n&#160;+&#160;<see cref="NConstant"/>) &#8722; <see cref="SubtractiveConstant"/>] / <see cref="PowerOfThree"/>.
/// </summary>
public class CollatzDecayFormula : ICollatzDecayFormula
{
    #region Properties
    /// <inheritdoc/>
    public UInt32 StepsToOne { get; }
    /// <summary>The coefficient of n in the exponent of two.</summary>
    public int NFactor { get; }
    /// <summary>The constant term in the exponent of two.</summary>
    public int NConstant { get; }
    /// <summary>The base-three logarithm of <see cref="PowerOfThree"/>.</summary>
    public Int32 ThreesExponent { get; }
    /// <summary>The divisor, 3^<see cref="ThreesExponent"/>.</summary>
    public BigInteger PowerOfThree { get; }
    /// <summary>The term subtracted from the power of two before dividing.</summary>
    public Int64 SubtractiveConstant { get; }
    #endregion Properties
    #region Constructors
    private CollatzDecayFormula()
    {
    }
    /// <summary>Creates a formula from an explicit closed form.</summary>
    public CollatzDecayFormula(UInt32 stepsToOne, int nFactor, int nConst, Int64 subtractiveConstant, Int32 threesExponent) : this()
    {
        StepsToOne = stepsToOne;
        NFactor = nFactor;
        NConstant = nConst;
        ThreesExponent = threesExponent;
        PowerOfThree = BigInteger.Pow(3, ThreesExponent);
        SubtractiveConstant = subtractiveConstant;
    }
    #endregion Constructors
    #region Public Methods
    /// <summary>
    /// Reports whether <paramref name="c"/> belongs to this family: it strips factors of two, reverses
    /// the 4x&#160;+&#160;1 map, and accepts when c&#160;&#215;&#160;<see cref="PowerOfThree"/>&#160;+&#160;<see cref="SubtractiveConstant"/>
    /// is an even power of two whose exponent, less <see cref="NConstant"/>, is a multiple of
    /// <see cref="NFactor"/>.
    /// </summary>
    public bool IsMember(BigInteger c)
    {
        // f(n) = (2^(2n+2) - 1) / 3^1
        // f(n) = [2^(6n-1) - 5] / 3^2 
        // f(n) = [2^(6n+4) - 7] / 3^2
        if (c < 1)
            return false;
        if (c == 113 && SubtractiveConstant == 7)
            Debug.Assert(true);
        BigInteger candidate = c;
        while (candidate.IsEven)
            candidate >>= 1;
        while ((candidate - 1) % 4 == 0 && candidate > 4)
        {
            {
                --candidate;
                candidate >>= 2;
                if (candidate.IsEven)
                {
                    candidate <<= 2;
                    ++candidate;
                    break;
                }
            }
        }

        BigInteger pow2 = candidate * PowerOfThree + SubtractiveConstant;
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
        log2 -= NConstant;
        if (log2 == 0)
            return StepsToOne == 1;
        return log2 % NFactor == 0;
    }
    /// <summary>Returns the closed form as <c>f(n) = [2^(Fn+C) - S] / 3^E</c>.</summary>
    public override string ToString()
    {
        // f(n) = [2^(6n+4) - 7] / 3^2
        return $"f(n) = [2^({NFactor}n+{NConstant}) - {SubtractiveConstant}] / 3^{ThreesExponent}";
    }
    /// <summary>Not implemented; always returns zero.</summary>
    public BigInteger NthMember(int n)
    {
        return 0;
    }
    #endregion Public Methods
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> that decides membership by matching the candidate's base-2
/// digits against fixed prefix / repeat / suffix patterns.
/// </summary>
public class CollatzDecayFormulaBitManipulation : ICollatzDecayFormula
{
    #region Properties
    /// <inheritdoc/>
    public UInt32 StepsToOne { get; }
    #endregion Properties
    #region Constructors
    private CollatzDecayFormulaBitManipulation()
    {
    }
    /// <summary>Creates a formula for the given number of odd steps to one.</summary>
    public CollatzDecayFormulaBitManipulation(UInt32 stepsToOne) : this()
    {
        StepsToOne = stepsToOne;
    }
    #endregion Constructors

    /// <summary>
    /// Reports whether <paramref name="c"/> belongs to this family by pattern-matching its base-2
    /// digits.
    /// </summary>
    /// <exception cref="NotImplementedException"><see cref="StepsToOne"/> is greater than three.</exception>
    public bool IsMember(BigInteger c)
    {
        if (c < 1)
            return false;
        bool retval = false;
        BigInteger candidate = c;
        while (candidate.IsEven)
            candidate >>= 1;
        if (StepsToOne > 1)
        {
            while ((candidate & 3) == 1) // Equivalent to candidate % 4 == 1
                candidate = (candidate - 1) >> 2; // Equivalent to (candidate - 1) / 4
            if (candidate.IsEven)
                candidate = (candidate << 2) + 1;
        }
        string bitString = CollatzMath.toBinaryBigEndianString(candidate);
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
    /// <summary>
    /// Returns the member at zero-based index <paramref name="n"/> for <see cref="StepsToOne"/> equal to
    /// one, built from <c>01</c> repeated <paramref name="n"/> times.
    /// </summary>
    /// <remarks>
    /// The leading <c>1</c> that the <see cref="StepsToOne"/>-of-one pattern carries is missing: the
    /// builder is written <c>new(char)</c>, which binds to the <see cref="StringBuilder"/> capacity
    /// overload rather than seeding the content. Verified by probe on SDK 10.0.400.
    /// </remarks>
    /// <exception cref="NotImplementedException"><see cref="StepsToOne"/> is not one.</exception>
    public BigInteger NthMember(int n)
    {
        BigInteger retval = -1;
        StringBuilder sb = new('1');
        switch (StepsToOne)
        {
            case 1:
                for (int i = 1; i <= n; i++)
                    sb.Append("01");
                break;
            default:
                throw new NotImplementedException();
        }
        retval = CollatzMath.toBigIntegerFromBinaryBigEndianString(sb.ToString());
        return retval;
    }
    private static bool IsPatternMatch(string binaryBE, int minBits, string prefix, string repeat, string suffix)
    {
        if (binaryBE.Length < minBits || binaryBE.Length < prefix.Length + suffix.Length)
            return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (prefix[i] != binaryBE[i])
                return false;
        }
        int suffixStartIndex = binaryBE.Length - suffix.Length;
        for (int i = 0; i < suffix.Length; i++)
        {
            if (suffix[i] != binaryBE[suffixStartIndex + i])
                return false;
        }
        if ((binaryBE.Length - prefix.Length - suffix.Length) % repeat.Length != 0)
            return false;
        int baseIndex = binaryBE.Length - suffix.Length - repeat.Length;
        while (baseIndex > prefix.Length - 1)
        {
            for (int i = repeat.Length - 1; i >= 0; i--)
            {
                if (repeat[i] != binaryBE[baseIndex + i])
                    return false;
            }
            baseIndex -= repeat.Length;
        }
        return true;
    }
}
