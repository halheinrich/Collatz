using HalHeinrich.Numerics;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HalHeinrich.Numerics.Collatz;

/// <summary>
/// Static helpers for the Collatz decay experiments: exponent tuples, odd-step counting,
/// base-2 string conversions, and loop solving over <see cref="BigInteger"/>.
/// </summary>
public static class CollatzMath
{
    #region Private Constants
    /// <summary>
    /// The largest argument <see cref="CollapseInOneModOneOut"/> and
    /// <see cref="CollapseInOneModTwoOut"/> accept, and so the largest first argument the two
    /// <c>CollapseInTwo</c> methods accept: beyond it the <see cref="CollapseInOne"/> index
    /// 3n&#160;&#177;&#160;1 those methods form does not fit in an <see cref="int"/>, and the
    /// unchecked arithmetic wraps to a small exponent instead of failing.
    /// </summary>
    private const Int32 MaxCollapseInOneIndex = (Int32.MaxValue - 1) / 3;
    /// <summary>
    /// The largest second argument <see cref="CollapseInTwoModOne"/> and
    /// <see cref="CollapseInTwoModTwo"/> accept: beyond it the exponent 2n2 or
    /// 2n2&#160;&#8722;&#160;1 those methods form does not fit in an <see cref="int"/>. One bound
    /// covers both, because the two differ by one and the slack below <see cref="int.MaxValue"/>
    /// is odd.
    /// </summary>
    private const Int32 MaxCollapseInTwoIndex = (Int32.MaxValue - 1) / 2;
    #endregion Private Constants
    #region Public Methods
    /// <summary>
    /// Returns every <see cref="int"/> array of length <paramref name="length"/> whose entries lie in
    /// [1, <paramref name="maxExponent"/>] and whose largest entry is exactly
    /// <paramref name="maxExponent"/>, so the set returned has
    /// <paramref name="maxExponent"/>^<paramref name="length"/>&#160;&#8722;&#160;(<paramref name="maxExponent"/>&#160;&#8722;&#160;1)^<paramref name="length"/>
    /// members.
    /// </summary>
    /// <remarks>
    /// These are tuples drawn with repetition, not permutations: at length two over [1,&#160;3] the
    /// result is {1,3}, {2,3}, {3,1}, {3,2} and {3,3}, and {3,3} repeats an entry no permutation
    /// can. Nor is it every tuple over that range - {1,1}, {1,2}, {2,1} and {2,2} are absent,
    /// because <paramref name="maxExponent"/> must actually appear. The old name asserted the
    /// first of those and a name naming only the range would drop the second; see
    /// halheinrich/Math#32.
    /// </remarks>
    /// <param name="length">Array length; must be positive.</param>
    /// <param name="maxExponent">Largest permitted entry, and the value that must appear at least once; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> or <paramref name="maxExponent"/> is not positive.</exception>
    public static IReadOnlyList<int[]> GenerateExponentTuplesWithMax(int length, int maxExponent)
    {
        // (Previously yielded an IEnumerable<int[]>; List<int[]> still supports LINQ usage.)
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExponent);

        // Capacity hint: count = maxExponent^length - (maxExponent-1)^length, computed exactly
        // and clamped to int. This is not a computational path - a capacity hint cannot change
        // what the method returns - so the Math.Pow this replaces was not an exactness violation
        // the way the log2ratio derivation in CollatzDecayFormulaRecursive was. It is exact
        // anyway, because an apparent violation costs a reader's attention every time they meet
        // it.
        BigInteger count = BigInteger.Pow(maxExponent, length) - BigInteger.Pow(maxExponent - 1, length);
        int capacity = count > int.MaxValue ? int.MaxValue : (int)count;
        List<int[]> result = new List<int[]>(capacity);

        int[] current = new int[length];

        void Recurse(int index, bool hasMax)
        {
            if (index == length)
            {
                if (hasMax)
                    result.Add((int[])current.Clone());
                return;
            }

            // If max not yet used and this is the last slot, force it to be 'maxExponent'
            if (!hasMax && index == length - 1)
            {
                current[index] = maxExponent;
                Recurse(index + 1, true);
                return;
            }

            for (int v = 1; v <= maxExponent; v++)
            {
                current[index] = v;
                Recurse(index + 1, hasMax || v == maxExponent);
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
    /// Returns the zero-based position of <paramref name="odd"/> in the sequence of odd
    /// integers 3, 5, 7, 9, &#8230;
    /// </summary>
    /// <param name="odd">An odd value of three or more.</param>
    /// <returns>(<paramref name="odd"/> &#8722; 3) / 2.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="odd"/> is even, or is less than three.
    /// </exception>
    /// <remarks>
    /// The inverse of <see cref="OddOfIndex"/> over that domain. The domain is checked rather
    /// than assumed, because the arithmetic alone fails quietly outside it: measured on SDK
    /// 10.0.400, the unguarded expression maps an even argument to the index of the odd value
    /// below it - 4 to 0 and 6 to 1, with no error - and an argument below three to a negative
    /// quotient whose conversion throws <see cref="OverflowException"/>, which tells a caller
    /// nothing about what it did wrong.
    /// </remarks>
    public static BigInteger IndexOfOdd(BigInteger odd)
    {
        if (odd.IsEven || odd < 3)
            throw new ArgumentOutOfRangeException(nameof(odd), odd, "Must be an odd value of three or more.");
        return (odd - 3) / 2;
    }
    /// <summary>
    /// Returns the odd integer at zero-based position <paramref name="index"/> in the sequence
    /// 3, 5, 7, 9, &#8230;
    /// </summary>
    /// <param name="index">A position of zero or more.</param>
    /// <returns>2 &#215; <paramref name="index"/> + 3.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <remarks>
    /// The inverse of <see cref="IndexOfOdd"/>. The parameter is a <see cref="BigInteger"/>
    /// deliberately: the test-local version this replaces took an <see cref="int"/> and
    /// evaluated the arithmetic in <see cref="int"/> before widening, so index 1073741823
    /// returned -2147483647 instead of 2147483649, silently.
    /// </remarks>
    public static BigInteger OddOfIndex(BigInteger index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return 2 * index + 3;
    }
    /// <summary>
    /// Returns (4^<paramref name="n"/> &#8722; 1) / 3 &#8212; the sequence 1, 5, 21, 85, 341, &#8230; for n = 1, 2, 3, &#8230;
    /// </summary>
    /// <param name="n">A positive index into that sequence.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is zero or negative.</exception>
    /// <remarks>
    /// The lower bound is one, not zero, and until halheinrich/Math#27 nothing enforced it or said
    /// so. <see cref="BigInteger.Pow(BigInteger, int)"/> accepts an exponent of zero, so n = 0
    /// returned 0 without complaint - a value outside the sequence this summary describes, because
    /// 3c&#160;+&#160;1 = 4^n with c a positive odd integer forces n &#8805; 1. Below zero the
    /// exponent went negative and <see cref="BigInteger.Pow(BigInteger, int)"/> threw an
    /// <see cref="ArgumentOutOfRangeException"/> naming <c>exponent</c> - a parameter of an internal
    /// call that no caller of this method passed and could not see.
    /// </remarks>
    public static BigInteger CollapseInOne(Int32 n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        BigInteger retval = (BigInteger.Pow(4, n) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns (4^(3<paramref name="n"/>&#160;+&#160;1) &#8722; 1) / 3 &#8212; the members of the
    /// <see cref="CollapseInOne"/> sequence congruent to 1 modulo 3.
    /// </summary>
    /// <param name="n">
    /// An index of zero or more - <see cref="CollapseInOne"/> index 3<paramref name="n"/>&#160;+&#160;1 -
    /// and at most (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3, beyond which that
    /// index does not fit in an <see cref="int"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="n"/> is negative, or exceeds (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3.
    /// </exception>
    /// <remarks>
    /// Zero is inside the domain, unlike <see cref="CollapseInOne"/>'s and
    /// <see cref="CollapseInOneModTwoOut"/>'s: it selects <see cref="CollapseInOne"/>(1), which is 1.
    /// The upper bound guards silent wraparound rather than a limit a caller reaches -
    /// 3<paramref name="n"/>&#160;+&#160;1 is unchecked <see cref="int"/> arithmetic, so measured on
    /// SDK 10.0.400 n = 1431655766 wrapped to an exponent of 3 and returned 21 with no error, and
    /// n = 1431655765 wrapped to an exponent of 0 and returned 0 (halheinrich/Math#27).
    /// </remarks>
    public static BigInteger CollapseInOneModOneOut(Int32 n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, MaxCollapseInOneIndex);
        BigInteger retval = (BigInteger.Pow(4, 3 * n + 1) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns (4^(3<paramref name="n"/>&#160;&#8722;&#160;1) &#8722; 1) / 3 &#8212; the members of the
    /// <see cref="CollapseInOne"/> sequence congruent to 2 modulo 3.
    /// </summary>
    /// <param name="n">
    /// A positive index - <see cref="CollapseInOne"/> index 3<paramref name="n"/>&#160;&#8722;&#160;1 -
    /// and at most (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3, beyond which that
    /// index does not fit in an <see cref="int"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="n"/> is zero or negative, or exceeds (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3.
    /// </exception>
    /// <remarks>
    /// The lower bound is one where <see cref="CollapseInOneModOneOut"/>'s is zero, because
    /// 3<paramref name="n"/>&#160;&#8722;&#160;1 reaches the first usable <see cref="CollapseInOne"/>
    /// index one argument later than 3<paramref name="n"/>&#160;+&#160;1 does. It was already
    /// enforced, but by accident: the exponent went negative and
    /// <see cref="BigInteger.Pow(BigInteger, int)"/> threw naming <c>exponent</c>, a parameter of an
    /// internal call that no caller of this method passed. The upper bound was not enforced at all -
    /// measured on SDK 10.0.400, n = 1431655766 wrapped to an exponent of 1 and returned 1
    /// (halheinrich/Math#27).
    /// </remarks>
    public static BigInteger CollapseInOneModTwoOut(Int32 n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n, MaxCollapseInOneIndex);
        BigInteger retval = (BigInteger.Pow(4, 3 * n - 1) - 1) / (BigInteger)3;
        return retval;
    }
    /// <summary>
    /// Returns (v &#8722; 1) / 3 where v = <see cref="CollapseInOneModOneOut"/>(<paramref name="n1"/>) &#215; 2^(2&#215;<paramref name="n2"/>).
    /// </summary>
    /// <param name="n1">
    /// A positive <see cref="CollapseInOneModOneOut"/> index, at most
    /// (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3.
    /// </param>
    /// <param name="n2">
    /// A positive half-exponent, at most (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;2,
    /// beyond which 2&#215;<paramref name="n2"/> does not fit in an <see cref="int"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="n1"/> or <paramref name="n2"/> is outside the bounds above.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// v is not congruent to 1 modulo 3, so the quotient does not represent what this method computes.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Both lower bounds are one, and this is the member of the family whose accepted domain was
    /// wider than its correct one in <em>both</em> arguments while saying nothing about either
    /// (halheinrich/Math#27). Measured on SDK 10.0.400: <paramref name="n2"/> = 0 left the exponent
    /// at zero, which <see cref="BigInteger.Pow(BigInteger, int)"/> accepts, and returned an even
    /// value that decays in neither two odd steps nor any small count - 28 for
    /// <paramref name="n1"/> = 1, which takes five; 1820 for 2, which takes twelve; 116508 for 3,
    /// which takes thirty-four. An odd c makes 3c&#160;+&#160;1 even, so the power of two an odd step
    /// strips is at least 2^1 and the even exponent 2&#215;<paramref name="n2"/> is at least 2.
    /// <paramref name="n1"/> = 0 selects v = 1, the fixed point, so the values returned were one-step
    /// decayers rather than two-step ones - 1 and 5 for <paramref name="n2"/> = 1 and 2.
    /// <see cref="CollapseInTwoModTwo"/> rejected both cases already, but only as a side effect of
    /// its exponent 2&#215;<paramref name="n2"/>&#160;&#8722;&#160;1 going negative; this one's stayed
    /// non-negative and answered.
    /// </para>
    /// <para>
    /// The congruence used to be asserted with <see cref="Debug.Assert(bool, string)"/>, and the
    /// assertion's own argument carried the subtraction - <c>--retval % 3 == 0</c> - so in a
    /// Release build, where a conditional call and its arguments are both removed, the check and
    /// the decrement vanished together. The returned value differed between configurations only
    /// for v a multiple of three, because 3k/3 is k while (3k&#8722;1)/3 is k&#8722;1, whereas for
    /// v &#8801; 1 and v &#8801; 2 (mod 3) the integer division absorbs the decrement. The check itself,
    /// though, was absent from every Release build. It throws now, in both.
    /// </para>
    /// </remarks>
    public static BigInteger CollapseInTwoModOne(Int32 n1, Int32 n2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n1, MaxCollapseInOneIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n2, MaxCollapseInTwoIndex);
        BigInteger value = CollapseInOneModOneOut(n1) * BigInteger.Pow(2, 2 * n2);
        if ((value - 1) % 3 != 0)
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"CollapseInTwoModOne({n1}, {n2}) produced {value}, which is congruent to {value % 3} modulo 3 rather than 1."));
        return (value - 1) / 3;
    }
    /// <summary>
    /// Returns (v &#8722; 1) / 3 where v = <see cref="CollapseInOneModTwoOut"/>(<paramref name="n1"/>) &#215; 2^(2&#215;<paramref name="n2"/>&#160;&#8722;&#160;1).
    /// </summary>
    /// <param name="n1">
    /// A positive <see cref="CollapseInOneModTwoOut"/> index, at most
    /// (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;3.
    /// </param>
    /// <param name="n2">
    /// A positive half-exponent, at most (<see cref="int.MaxValue"/>&#160;&#8722;&#160;1)&#160;/&#160;2,
    /// beyond which 2&#215;<paramref name="n2"/>&#160;&#8722;&#160;1 does not fit in an <see cref="int"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="n1"/> or <paramref name="n2"/> is outside the bounds above.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// v is not congruent to 1 modulo 3, so the quotient does not represent what this method computes.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Both lower bounds were enforced before halheinrich/Math#27, but by accident rather than by
    /// intent: each drives the exponent negative, and <see cref="BigInteger.Pow(BigInteger, int)"/>
    /// rejects that with an <see cref="ArgumentOutOfRangeException"/> naming <c>exponent</c> - a
    /// parameter of an internal call that no caller of this method passed and could not see. They
    /// are checked here now, and the upper bounds, which nothing checked, with them. This is the
    /// sibling <see cref="CollapseInTwoModOne"/> was measured against: that accidental rejection is
    /// the only reason the silently wrong answers found there have no counterpart here.
    /// </para>
    /// <para>
    /// The congruence used to be asserted with <see cref="Debug.Assert(bool, string)"/>, and the
    /// assertion's own argument carried the subtraction - <c>--retval % 3 == 0</c> - so in a
    /// Release build, where a conditional call and its arguments are both removed, the check and
    /// the decrement vanished together. The returned value differed between configurations only
    /// for v a multiple of three, because 3k/3 is k while (3k&#8722;1)/3 is k&#8722;1, whereas for
    /// v &#8801; 1 and v &#8801; 2 (mod 3) the integer division absorbs the decrement. The check itself,
    /// though, was absent from every Release build. It throws now, in both.
    /// </para>
    /// </remarks>
    public static BigInteger CollapseInTwoModTwo(Int32 n1, Int32 n2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n1, MaxCollapseInOneIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(n2, MaxCollapseInTwoIndex);
        BigInteger value = CollapseInOneModTwoOut(n1) * BigInteger.Pow(2, 2 * n2 - 1);
        if ((value - 1) % 3 != 0)
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"CollapseInTwoModTwo({n1}, {n2}) produced {value}, which is congruent to {value % 3} modulo 3 rather than 1."));
        return (value - 1) / 3;
    }
    /// <summary>
    /// Returns the base-2 digits of <paramref name="bigInt"/>, least-significant digit first.
    /// </summary>
    /// <remarks>
    /// Values above <see cref="long.MaxValue"/> are delegated to
    /// <see cref="ToBinaryLittleEndianStringGtInt64"/>; below that the conversion runs through
    /// <see cref="long"/>, so a negative value yields a 64-digit two's-complement form rather
    /// than a sign-magnitude one. This method was called <c>toBinaryBigEndianString</c> until
    /// halheinrich/Math#25, and produced least-significant-first digits under that name too -
    /// only the name changed.
    /// </remarks>
    public static string ToBinaryLittleEndianString(BigInteger bigInt)
    {
        if (bigInt > long.MaxValue)
            return ToBinaryLittleEndianStringGtInt64(bigInt);
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
    /// The endianness half of this name was wrong until halheinrich/Math#25 and is right now.
    /// The <c>GtInt64</c> half is still misleading and was left alone: the loop below handles any
    /// non-negative <see cref="BigInteger"/> rather than only values above
    /// <see cref="long.MaxValue"/>, which is why halheinrich/Math#1's guard could be deleted
    /// outright rather than repaired - there was nothing for it to delegate to.
    /// </remarks>
    public static string ToBinaryBigEndianStringGtInt64(BigInteger bigInt)
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
    public static string ToBinaryLittleEndianStringGtInt64(BigInteger bigInt)
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
    /// <remarks>
    /// Trimming rather than special-casing makes zero the one argument on which this method and
    /// <see cref="ToBinaryBigEndianStringGtInt64"/> disagree: <see cref="BigInteger.ToByteArray()"/>
    /// gives a single zero byte, so the trim leaves the empty string where the loop-based sibling
    /// returns <c>"0"</c>. Both still round-trip, because
    /// <see cref="ToBigIntegerFromBinaryBigEndianString"/> reads an empty string as zero.
    /// </remarks>
    public static string ToBinaryBigEndianString(BigInteger bigInt)
    {
        byte[] bytes = bigInt.ToByteArray();
        Array.Reverse(bytes);
        string retval = string.Join("", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))).TrimStart('0');
        return retval;
    }
    /// <summary>
    /// Reads <paramref name="bigEndianBinaryText"/> left to right, treating the first character as
    /// the most significant base-2 digit.
    /// </summary>
    /// <remarks>Any character other than <c>1</c> contributes a zero digit.</remarks>
    public static BigInteger ToBigIntegerFromBinaryBigEndianString(string bigEndianBinaryText)
    {
        BigInteger retval = 0;
        foreach (char c in bigEndianBinaryText)
            retval = (retval << 1) | (c == '1' ? 1 : 0);
        return retval;
    }
    /// <summary>
    /// Reads <paramref name="littleEndianBinaryText"/> right to left, treating the last character as the
    /// most significant base-2 digit.
    /// </summary>
    /// <remarks>Any character other than <c>1</c> contributes a zero digit.</remarks>
    public static BigInteger ToBigIntegerFromBinaryLittleEndianString(string littleEndianBinaryText)
    {
        BigInteger retval = 0;
        for (int i = littleEndianBinaryText.Length - 1; i >= 0; i--)
        {
            retval = (retval << 1) | (littleEndianBinaryText[i] == '1' ? 1 : 0);
        }
        return retval;
    }
    /// <summary>
    /// Yields the unbounded sequence <c>1</c>, <c>101</c>, <c>10101</c>, &#8230; &#8212; each element the
    /// previous one with <c>01</c> appended.
    /// </summary>
    /// <remarks>
    /// The sequence never ends; callers must bound their own enumeration.
    /// <para>
    /// This was called <c>GetBinaryBigEndianDecaysInOne</c>, and the endianness half of that name
    /// was dropped rather than flipped, because <em>every element it yields is a palindrome</em>.
    /// The sequence is 1, 101, 10101, 1010101 and on, which is self-reverse by construction, so
    /// both readers return the same value for every element and nothing this method emits could
    /// falsify a claim about digit order either way. The name was not inverted; it was
    /// undetermined, and a name that asserts a convention its outputs cannot exhibit invites a
    /// caller to infer a guarantee that is not there. What the elements are is not in doubt: the
    /// nth is <see cref="CollapseInOne"/>(n + 1), the odd integers reaching one in a single odd
    /// step. That is what the name says now.
    /// </para>
    /// </remarks>
    public static IEnumerable<string> GetDecayInOneBitPatterns()
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is zero or negative.</exception>
    /// <remarks>
    /// Positivity used to be a <see cref="Debug.Assert(bool, string)"/>, so a Release build had no
    /// check at all and the method simply did not return: measured on SDK 10.0.400, zero drives
    /// the strip loop forever because 0 &gt;&gt; 1 is 0 and 0 is even, and a negative value reaches
    /// -1, which <see cref="NextOdd"/> maps back to -1. Does not return for a positive value that
    /// never reaches one - that one is the Collatz conjecture, not a missing guard.
    /// </remarks>
    public static UInt64 OddStepCountToOne(BigInteger n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is zero or negative.</exception>
    /// <remarks>
    /// Positivity used to be a <see cref="Debug.Assert(bool, string)"/>, so Release had no check.
    /// What that hid is not what it hid in <see cref="OddStepCountToOne"/>, measured on SDK
    /// 10.0.400: zero hangs the same way, in the strip loop, but a negative argument usually
    /// hangs in the step loop instead, its orbit settling into a negative cycle that never drops
    /// below the argument - and sometimes it does not hang at all. -17 returned 1, because -25
    /// genuinely is less than -17. A plausible number for a nonsensical argument is worse than a
    /// hang, which is why the guard is on the argument rather than on the loop.
    /// Does not return for a positive value whose orbit never drops below <paramref name="n"/>;
    /// that one is the conjecture, not a missing guard.
    /// </remarks>
    public static UInt64 OddStepCountToSmaller(BigInteger n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
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
    #region Internal Methods
    /// <summary>
    /// Returns the exact floor of log2(<paramref name="numerator"/> / <paramref name="denominator"/>)
    /// for two positive values, without leaving integer arithmetic.
    /// </summary>
    /// <param name="numerator">Must be positive.</param>
    /// <param name="denominator">Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either argument is zero or negative.</exception>
    /// <remarks>
    /// The bit-length difference alone is not the answer. For positive a and b, a lies in
    /// [2^(la-1), 2^la) and b in [2^(lb-1), 2^lb), so a/b lies in (2^(la-lb-1), 2^(la-lb+1)) and
    /// the floor is either la-lb or one less. Which of the two it is cannot be read off the
    /// lengths; it is settled below by the exact comparison a &#8805; b&#183;2^(la-lb), written as a
    /// shift so that no division and no rounding takes place.
    /// </remarks>
    internal static int FloorLog2Ratio(BigInteger numerator, BigInteger denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numerator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);

        // A BigInteger cannot hold more than int.MaxValue bits, so the difference fits an int.
        int bitDelta = (int)(numerator.GetBitLength() - denominator.GetBitLength());
        bool reachesBitDelta = bitDelta >= 0
            ? numerator >= denominator << bitDelta
            : numerator << -bitDelta >= denominator;
        return reachesBitDelta ? bitDelta : bitDelta - 1;
    }
    #endregion Internal Methods
}
/// <summary>
/// The text of the formula shapes this library prints, so that each shape and the rule for signing
/// its terms are stated once rather than at every site that prints one.
/// </summary>
/// <remarks>
/// Six occurrences across five sites used to concatenate a sign character with a signed value, so a
/// negative constant printed <c>+ -5</c> and a zero constant printed a term that is not there. Four
/// of those sites also spelled the recurrence shape out for themselves. Both are the one defect: a
/// rule held in N places is corrected in some of them and survives to diverge in the rest &#8212;
/// see halheinrich/Math#31.
/// <para>
/// Terms are taken as <see cref="BigInteger"/> rather than <see cref="Int64"/> so that
/// <see cref="Int64.MinValue"/> renders rather than overflowing when <see cref="SubtractedTerm"/>
/// negates it; the callers' constants are <see cref="Int64"/> and <see cref="Int32"/>, both of
/// which widen to it implicitly.
/// </para>
/// </remarks>
internal static class FormulaText
{
    /// <summary>
    /// Renders the recurrence <c>f(n) = 2^E * f(n-1) + A</c>, with the additive term carrying the
    /// operator its own sign calls for and dropped entirely when it is zero.
    /// </summary>
    /// <param name="twosExponent">The base-two logarithm of the recurrence's multiplier.</param>
    /// <param name="additiveConstant">The recurrence's additive term.</param>
    internal static string Recurrence(Int32 twosExponent, BigInteger additiveConstant)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"f(n) = 2^{twosExponent} * f(n-1){AddedTerm(additiveConstant)}");
    }
    /// <summary>
    /// Renders <paramref name="value"/> as a term added to the expression it follows, its operator
    /// spaced: <c> + 5</c>, <c> - 5</c> when it is negative, and nothing at all when it is zero.
    /// </summary>
    internal static string AddedTerm(BigInteger value) => SignedTerm(value, " ");
    /// <summary>
    /// Renders <paramref name="value"/> as a term subtracted from the expression it follows, its
    /// operator spaced: <c> - 5</c>, <c> + 5</c> when it is negative, and nothing at all when it is
    /// zero.
    /// </summary>
    internal static string SubtractedTerm(BigInteger value) => SignedTerm(-value, " ");
    /// <summary>
    /// Renders <paramref name="value"/> as a term added to the expression it follows, unspaced for
    /// use inside a compound expression such as an exponent: <c>+5</c>, <c>-5</c> when it is
    /// negative, and nothing at all when it is zero.
    /// </summary>
    internal static string AddedTermCompact(BigInteger value) => SignedTerm(value, "");
    /// <summary>
    /// Renders one signed term: the operator its sign calls for, spaced by <paramref name="spacing"/>
    /// on each side, then its magnitude.
    /// </summary>
    /// <remarks>
    /// A zero term renders as nothing, because a shape printing <c>2^(6n+0)</c> misstates its own
    /// arity as surely as one printing <c>2^(6n+-1)</c> misstates the sign. The magnitude is taken
    /// with <see cref="BigInteger.Abs"/> rather than by trimming a rendered minus sign, so that the
    /// operator and the digits are decided by the same value.
    /// </remarks>
    private static string SignedTerm(BigInteger value, string spacing)
    {
        if (value.IsZero)
            return string.Empty;
        return string.Create(CultureInfo.InvariantCulture,
            $"{spacing}{(value.Sign < 0 ? '-' : '+')}{spacing}{BigInteger.Abs(value)}");
    }
}
/// <summary>
/// A family of odd integers that reach one in a fixed number of odd Collatz steps.
/// </summary>
/// <remarks>
/// An implementation is expected to override <see cref="object.ToString"/> with the definition
/// it describes, so that a family prints as the recurrence, closed form or bit pattern it is.
/// Nothing enforces that: <see cref="object.ToString"/> already satisfies any interface, so an
/// implementation that omits it prints its type name and still compiles.
/// </remarks>
public interface ICollatzDecayFormula
{
    /// <summary>The number of odd Collatz steps every member of this family takes to reach one.</summary>
    public UInt32 StepsToOne { get; }
    /// <summary>Reports whether <paramref name="c"/> belongs to this family.</summary>
    public bool IsMember(BigInteger c);
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> that can also produce its members one index at a time.
/// </summary>
/// <remarks>
/// Enumeration is separated from membership because it is not something every family here can do.
/// <see cref="CollatzDecayFormulaBitManipulation"/> decides membership by matching a candidate's
/// base-2 digits against fixed patterns, which runs in one direction only: nothing in it generates
/// the values those patterns accept. While <c>NthMember</c> sat on <see cref="ICollatzDecayFormula"/>
/// that type declared an operation it could not perform, and the compiler accepted every call to it
/// &#8212; see halheinrich/Math#24.
/// </remarks>
public interface IIndexedCollatzDecayFormula : ICollatzDecayFormula
{
    /// <summary>Returns this family's member at zero-based index <paramref name="n"/>.</summary>
    /// <remarks>
    /// The order is the implementation's own, and index zero is not guaranteed to be the family's
    /// least member. <see cref="CollatzDecayFormula"/> indexes by the n of its closed form, which at
    /// index zero can fall outside the family or outside the integers altogether. A consumer walking
    /// the indices must therefore filter what it collects rather than trust the position &#8212;
    /// <see cref="CollatzDecayFormulaRecursive(IIndexedCollatzDecayFormula, int)"/> does exactly that.
    /// </remarks>
    public BigInteger NthMember(int n);
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> whose members satisfy the recurrence
/// f(i) = <see cref="PowerOfTwo"/> &#215; f(i&#8722;1) + <see cref="AdditiveConstant"/> from a seed anchor.
/// </summary>
public class CollatzDecayFormulaRecursive : IIndexedCollatzDecayFormula
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
    /// <summary>
    /// Every recurrence this class can seed, with the anchor that seeds it.
    /// </summary>
    /// <remarks>
    /// The table is data rather than a switch so that the constructor's guard, its exception message
    /// and this documentation cannot drift apart: adding a row is the only edit a new seeded family
    /// needs. It is not an enumeration of the families that exist at these depths - see
    /// halheinrich/Math#2 - only of the ones this constructor holds an anchor for.
    /// </remarks>
    private static readonly (UInt32 StepsToOne, Int32 TwosExponent, Int64 AdditiveConstant, BigInteger SeedAnchor)[] SeededRecurrences =
    [
        (1, 2, 1, 1),
        (2, 6, 35, 3),
        (2, 6, 49, 113),
    ];
    /// <summary>Renders <see cref="SeededRecurrences"/> for an exception message.</summary>
    private static string DescribeSeededRecurrences()
    {
        return string.Join("; ", SeededRecurrences.Select(recurrence => string.Create(CultureInfo.InvariantCulture,
            $"depth {recurrence.StepsToOne} {FormulaText.Recurrence(recurrence.TwosExponent, recurrence.AdditiveConstant)} anchored at {recurrence.SeedAnchor}")));
    }
    #endregion Properties
    #region Constructors
    private CollatzDecayFormulaRecursive()
    {
        DecayAnchorList = new List<BigInteger>();
        DecayAnchorHashSet = new HashSet<BigInteger>();
    }
    /// <summary>
    /// Creates a formula from an explicit recurrence, seeding the anchor list with that recurrence's
    /// known first member.
    /// </summary>
    /// <param name="stepsToOne">The number of odd steps this family's members take to reach one.</param>
    /// <param name="twosExponent">The base-two logarithm of the recurrence's multiplier.</param>
    /// <param name="additiveConstant">The recurrence's additive term.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// No recurrence in <see cref="SeededRecurrences"/> has that <paramref name="stepsToOne"/>, so no
    /// seed anchor is known at that depth and there is nothing to build the object from.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The depth is seeded but <paramref name="twosExponent"/> and <paramref name="additiveConstant"/>
    /// name no recurrence seeded at it.
    /// </exception>
    /// <remarks>
    /// The constructor accepts exactly the triples in <see cref="SeededRecurrences"/>. That set is
    /// what this constructor can seed and not a claim about how many families exist &#8212; what the
    /// depth-<i>d</i> family count is, and whether this recursion reaches all of it, is open in
    /// halheinrich/Math#2. Rejecting what cannot be seeded is not asserting the enumeration is closed.
    /// <para>
    /// Validation used to be one <see cref="Debug.Assert(bool, string)"/> covering one case of one
    /// depth, so Release accepted every bad triple silently and Debug killed the process on the
    /// single case it caught. Two whole regions went unchecked: depth 1 ignored both recurrence
    /// parameters, and depth 3 upward fell through to an object with an empty anchor list, which no
    /// method here can use. See halheinrich/Math#28.
    /// </para>
    /// </remarks>
    public CollatzDecayFormulaRecursive(UInt32 stepsToOne, Int32 twosExponent, Int64 additiveConstant) : this()
    {
        if (!SeededRecurrences.Any(recurrence => recurrence.StepsToOne == stepsToOne))
            throw new ArgumentOutOfRangeException(nameof(stepsToOne), stepsToOne, string.Create(CultureInfo.InvariantCulture,
                $"No seed anchor is known at depth {stepsToOne}. Seeded here: {DescribeSeededRecurrences()}. Depths beyond those are open - see halheinrich/Math#2 - so no anchor can be supplied for them."));
        int index = Array.FindIndex(SeededRecurrences, recurrence =>
            recurrence.StepsToOne == stepsToOne
            && recurrence.TwosExponent == twosExponent
            && recurrence.AdditiveConstant == additiveConstant);
        if (index < 0)
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture,
                $"{FormulaText.Recurrence(twosExponent, additiveConstant)} has no seed anchor at depth {stepsToOne}. Seeded here: {DescribeSeededRecurrences()}. That set is what this constructor can seed, not a claim that no other family exists at these depths - see halheinrich/Math#2."),
                nameof(additiveConstant));
        StepsToOne = stepsToOne;
        TwosExponent = twosExponent;
        PowerOfTwo = BigInteger.Pow(2, TwosExponent);
        AdditiveConstant = additiveConstant;
        DecayAnchorList.Add(SeededRecurrences[index].SeedAnchor);
        DecayAnchorHashSet.Add(SeededRecurrences[index].SeedAnchor);
    }
    /// <summary>
    /// Derives a formula from <paramref name="predecessorFormula"/>: it walks that formula's members,
    /// keeps those congruent to <paramref name="modThree"/> modulo three, maps each such member p to
    /// (4p&#160;&#8722;&#160;1)/3 when <paramref name="modThree"/> is 1 or (2p&#160;&#8722;&#160;1)/3 when it is 2, and infers
    /// <see cref="PowerOfTwo"/> and <see cref="AdditiveConstant"/> once more than seven anchors agree.
    /// </summary>
    /// <param name="predecessorFormula">
    /// The formula whose members seed the derivation. It is an
    /// <see cref="IIndexedCollatzDecayFormula"/> rather than an <see cref="ICollatzDecayFormula"/>
    /// because this constructor walks <see cref="IIndexedCollatzDecayFormula.NthMember"/> and never
    /// calls <see cref="ICollatzDecayFormula.IsMember"/>; a family that can only test membership
    /// cannot seed it.
    /// </param>
    /// <param name="modThree">Selects the residue class to keep; must be 1 or 2.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="modThree"/> is neither 1 nor 2.</exception>
    /// <exception cref="InvalidOperationException">
    /// The derivation did not produce a family: the anchors it collected do not satisfy a single
    /// recurrence, or one of them does not reach one in <see cref="StepsToOne"/> odd steps.
    /// </exception>
    /// <remarks>
    /// The walk is unbounded: a predecessor that never yields enough qualifying members does not
    /// return.
    /// <para>
    /// Both derivation failures used to be a <see cref="Debug.Assert(bool, string)"/>, which is absent
    /// from Release builds, so a Release build returned the object built from the failed derivation
    /// and a Debug build killed the process instead of failing a test. They are the caller's
    /// <paramref name="predecessorFormula"/> reaching a place this recursion cannot follow, not the
    /// caller mis-spelling a parameter, so they throw <see cref="InvalidOperationException"/> rather
    /// than an argument exception. See halheinrich/Math#28.
    /// </para>
    /// </remarks>
    public CollatzDecayFormulaRecursive(IIndexedCollatzDecayFormula predecessorFormula, int modThree) : this()
    {
        // f(n) = 2^2 * f(n-1) + 1
        // f(n) = 2^6 * f(n-1) + 35
        // f(n) = 2^6 * f(n-1) + 49
        if (modThree is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(modThree), modThree,
                "Must be 1 or 2: the derivation keeps one of the two residue classes modulo three that can yield an anchor.");
        const int successThreshold = 7;
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
                // The division below is exact, so nothing checks it. Working modulo three: the
                // guard above leaves modThree at 1 or 2, and this branch is reached only when
                // prevAnchor % 3 == modThree - which under C#'s truncated remainder also rules out a
                // negative prevAnchor, whose remainder can only be 0, -1 or -2. So 4 * prevAnchor is
                // 4 * 1 = 1 when modThree is 1, and 2 * prevAnchor is 2 * 2 = 4 = 1 when it is 2:
                // either product is 1, and one less than it is divisible by three.
                anchor /= 3;
                DecayAnchorList.Add(anchor);
                DecayAnchorHashSet.Add(anchor);
                if (DecayAnchorList.Count > 1)
                {
                    log2ratio = CollatzMath.FloorLog2Ratio(
                        DecayAnchorList[DecayAnchorList.Count - 1],
                        DecayAnchorList[DecayAnchorList.Count - 2]);
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
        if (!isOk)
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"The {DecayAnchorList.Count} anchors derived from {predecessorFormula} keeping residue {modThree} do not satisfy one recurrence {FormulaText.Recurrence(log2ratio, addConst)}, so they are not a family this class can represent."));
        PowerOfTwo = pow2;
        TwosExponent = log2ratio;
        AdditiveConstant = addConst;
        StepsToOne = predecessorFormula.StepsToOne + 1;
        foreach (BigInteger c in DecayAnchorList)
            if (CollatzMath.OddStepCountToOne(c) != StepsToOne)
                throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                    $"Anchor {c} derived from {predecessorFormula} reaches one in {CollatzMath.OddStepCountToOne(c)} odd steps, not the {StepsToOne} this family claims."));
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
    /// <summary>
    /// Returns the member at zero-based index <paramref name="n"/>, extending the anchor list by the
    /// recurrence as far as needed.
    /// </summary>
    /// <remarks>
    /// Index zero is the seed anchor, so unlike <see cref="CollatzDecayFormula.NthMember"/> this
    /// implementation's order does begin at the family's least member.
    /// </remarks>
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
    /// <summary>
    /// Returns the recurrence in the form <c>f(n) = 2^E * f(n-1) + A</c>, where the additive term
    /// carries the operator its own sign calls for &#8212; <c>+ 35</c>, <c>- 5</c> &#8212; and is
    /// absent altogether when <see cref="AdditiveConstant"/> is zero, leaving
    /// <c>f(n) = 2^E * f(n-1)</c>.
    /// </summary>
    public override string ToString()
    {
        return FormulaText.Recurrence(TwosExponent, AdditiveConstant);
    }
    #endregion Public Methods
}
/// <summary>
/// An <see cref="ICollatzDecayFormula"/> whose members are given in closed form as
/// [2^(<see cref="NFactor"/>n&#160;+&#160;<see cref="NConstant"/>) &#8722; <see cref="SubtractiveConstant"/>] / <see cref="PowerOfThree"/>.
/// </summary>
public class CollatzDecayFormula : IIndexedCollatzDecayFormula
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
    /// <summary>
    /// Returns the closed form as <c>f(n) = [2^(Fn+C) - S] / 3^E</c>, where each of the two
    /// constants carries the operator its own sign calls for and is absent altogether when it is
    /// zero.
    /// </summary>
    /// <remarks>
    /// So a negative <see cref="NConstant"/> prints <c>2^(6n-1)</c> and a zero one <c>2^(6n)</c>,
    /// and a negative <see cref="SubtractiveConstant"/> prints <c>+ 5</c>, because subtracting a
    /// negative adds it. The bracketed numerator is what remains when both are zero:
    /// <c>f(n) = [2^(6n)] / 3^2</c>.
    /// </remarks>
    public override string ToString()
    {
        // f(n) = [2^(6n+4) - 7] / 3^2
        return $"f(n) = [2^({NFactor}n{FormulaText.AddedTermCompact(NConstant)}){FormulaText.SubtractedTerm(SubtractiveConstant)}] / 3^{ThreesExponent}";
    }
    /// <summary>
    /// Returns [2^(<see cref="NFactor"/><paramref name="n"/>&#160;+&#160;<see cref="NConstant"/>) &#8722; <see cref="SubtractiveConstant"/>]&#160;/&#160;<see cref="PowerOfThree"/>,
    /// this family's closed form evaluated at <paramref name="n"/>.
    /// </summary>
    /// <remarks>
    /// The index is the n of the closed form, not a position in the family's ascending order, and the
    /// two need not agree: [2^(6n&#160;+&#160;4)&#160;&#8722;&#160;7]&#160;/&#160;3^2 is 1 at index zero,
    /// which reaches one in a single odd step and so belongs to no <see cref="StepsToOne"/>-of-two
    /// family. <see cref="IsMember"/> rejects it for the same reason, by the log2-of-zero case it
    /// carries. Compare <see cref="CollatzDecayFormulaRecursive.NthMember"/>, whose index zero is the
    /// seed anchor.
    /// </remarks>
    /// <param name="n">A zero-based index into the closed form.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="n"/> is negative, or the exponent it forms is negative or exceeds
    /// <see cref="int.MaxValue"/>. A negative exponent is reachable from a well-formed instance rather
    /// than only from a malformed one: [2^(6n&#160;&#8722;&#160;1)&#160;&#8722;&#160;5]&#160;/&#160;3^2
    /// has its first member at n of one, so index zero is outside its domain.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The division is not exact, which is a statement about the instance's constants rather than about
    /// <paramref name="n"/>. Letting <see cref="BigInteger"/> division truncate instead would return a
    /// value indistinguishable from a member and carry the error onward untracked, which AGENTS.md
    /// &#167;&#160;Exactness discipline exists to prevent.
    /// </exception>
    public BigInteger NthMember(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        Int64 exponent = (Int64)NFactor * n + NConstant;
        if (exponent < 0)
            throw new ArgumentOutOfRangeException(nameof(n), n, string.Create(CultureInfo.InvariantCulture,
                $"CollatzDecayFormula.NthMember({n}) needs exponent {exponent}, and 2 raised to a negative exponent is not an integer."));
        if (exponent > Int32.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(n), n, string.Create(CultureInfo.InvariantCulture,
                $"CollatzDecayFormula.NthMember({n}) needs exponent {exponent}, which exceeds the largest exponent BigInteger.Pow accepts."));
        BigInteger numerator = BigInteger.Pow(2, (Int32)exponent) - SubtractiveConstant;
        BigInteger quotient = BigInteger.DivRem(numerator, PowerOfThree, out BigInteger remainder);
        if (!remainder.IsZero)
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"CollatzDecayFormula.NthMember({n}) produced numerator {numerator}, which leaves remainder {remainder} on division by 3^{ThreesExponent}."));
        return quotient;
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
    /// <summary>The shallowest depth <see cref="IsMember"/> holds a pattern for.</summary>
    private const UInt32 MinStepsToOne = 1;
    /// <summary>The deepest depth <see cref="IsMember"/> holds a pattern for.</summary>
    private const UInt32 MaxStepsToOne = 3;
    /// <inheritdoc/>
    public UInt32 StepsToOne { get; }
    #endregion Properties
    #region Constructors
    private CollatzDecayFormulaBitManipulation()
    {
    }
    /// <summary>Creates a formula for the given number of odd steps to one.</summary>
    /// <param name="stepsToOne">The depth this family decides; 1, 2 or 3.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="stepsToOne"/> is outside 1 to 3. This type's whole definition is the pattern
    /// table in <see cref="IsMember"/>, which holds those three depths, so an instance for any other
    /// depth is one that cannot answer the only question it exists to answer.
    /// </exception>
    /// <remarks>
    /// <see cref="StepsToOne"/> is validated here and never changes afterwards, which is what makes
    /// <see cref="IsMember"/>'s switch total without a default case. Before halheinrich/Math#36 the
    /// constructor accepted any depth: a depth-4 instance built successfully, threw
    /// <see cref="NotImplementedException"/> from <see cref="IsMember"/>, and - after
    /// halheinrich/Math#24 gave the type a <see cref="ToString"/> - printed itself as a family it
    /// could not decide. Nothing in the repo constructed one, so the object was well-formed and
    /// wrong rather than actively breaking anything; AGENTS.md &#167;&#160;Writing code asks for that
    /// shape to be made unrepresentable rather than documented.
    /// </remarks>
    public CollatzDecayFormulaBitManipulation(UInt32 stepsToOne) : this()
    {
        if (stepsToOne is < MinStepsToOne or > MaxStepsToOne)
            throw new ArgumentOutOfRangeException(nameof(stepsToOne), stepsToOne, string.Create(CultureInfo.InvariantCulture,
                $"This family is decided by a base-2 digit pattern, and patterns are held for depths {MinStepsToOne} to {MaxStepsToOne} only."));
        StepsToOne = stepsToOne;
    }
    #endregion Constructors

    /// <summary>
    /// Reports whether <paramref name="c"/> belongs to this family by pattern-matching its base-2
    /// digits.
    /// </summary>
    /// <remarks>
    /// The switch below has no default case because the constructor rejects every depth this table
    /// does not cover, so <see cref="StepsToOne"/> is 1, 2 or 3 for the life of the object. Until
    /// halheinrich/Math#36 the default threw <see cref="NotImplementedException"/>, which was the
    /// only thing standing between a depth-4 instance and a wrong answer.
    /// </remarks>
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
        string bitString = CollatzMath.ToBinaryLittleEndianString(candidate);
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
        }
        return retval;
    }
    /// <summary>
    /// Returns the depth this family decides and the means it decides it by, as
    /// <c>decay in N odd steps, decided by base-2 digit pattern</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two siblings print a recurrence and a closed form because each holds one. This type holds
    /// only <see cref="StepsToOne"/>. Its definition is the table of prefix / repeat / suffix triples
    /// written into <see cref="IsMember"/>, one set per depth, and restating any of it here - the
    /// patterns themselves, or merely how many there are - would encode the same rule in a second
    /// place, where a pattern added to the switch would leave this text quietly wrong. So the line
    /// says the depth and the method and claims nothing about the patterns: AGENTS.md
    /// &#167;&#160;Testing discipline forbids a name or doc asserting what the output does not hold,
    /// and a count this type cannot keep in step with is exactly that.
    /// </para>
    /// <para>
    /// Printing the table honestly would mean lifting it out of the switch into data both this method
    /// and <see cref="IsMember"/> read, which is a change to the one implementation that gets depth
    /// three right and is not made here.
    /// </para>
    /// </remarks>
    public override string ToString()
    {
        return $"decay in {StepsToOne} odd steps, decided by base-2 digit pattern";
    }
    private static bool IsPatternMatch(string littleEndianBinaryText, int minBits, string prefix, string repeat, string suffix)
    {
        if (littleEndianBinaryText.Length < minBits || littleEndianBinaryText.Length < prefix.Length + suffix.Length)
            return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (prefix[i] != littleEndianBinaryText[i])
                return false;
        }
        int suffixStartIndex = littleEndianBinaryText.Length - suffix.Length;
        for (int i = 0; i < suffix.Length; i++)
        {
            if (suffix[i] != littleEndianBinaryText[suffixStartIndex + i])
                return false;
        }
        if ((littleEndianBinaryText.Length - prefix.Length - suffix.Length) % repeat.Length != 0)
            return false;
        int baseIndex = littleEndianBinaryText.Length - suffix.Length - repeat.Length;
        while (baseIndex > prefix.Length - 1)
        {
            for (int i = repeat.Length - 1; i >= 0; i--)
            {
                if (repeat[i] != littleEndianBinaryText[baseIndex + i])
                    return false;
            }
            baseIndex -= repeat.Length;
        }
        return true;
    }
}
