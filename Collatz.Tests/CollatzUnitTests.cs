using HalHeinrich.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HalHeinrich.Numerics.Collatz;

public class CollatzUnitTests
{
    // CA1861: hoisted so each exponent array is allocated once rather than per call.
    private static readonly int[] Exp_1 = [1];
    private static readonly int[] Exp_2 = [2];
    private static readonly int[] Exp_3 = [3];
    private static readonly int[] Exp_4 = [4];
    private static readonly int[] Exp_1_1 = [1, 1];
    private static readonly int[] Exp_1_2 = [1, 2];
    private static readonly int[] Exp_2_1 = [2, 1];
    private static readonly int[] Exp_2_2 = [2, 2];
    private static readonly int[] Exp_1_3 = [1, 3];
    private static readonly int[] Exp_3_1 = [3, 1];
    private static readonly int[] Exp_2_3 = [2, 3];
    private static readonly int[] Exp_3_2 = [3, 2];
    private static readonly int[] Exp_3_3 = [3, 3];
    private static readonly int[] Exp_1_1_1 = [1, 1, 1];
    private static readonly int[] Exp_1_1_2 = [1, 1, 2];
    private static readonly int[] Exp_1_2_1 = [1, 2, 1];
    private static readonly int[] Exp_2_1_1 = [2, 1, 1];
    private static readonly int[] Exp_1_2_2 = [1, 2, 2];
    private static readonly int[] Exp_2_1_2 = [2, 1, 2];
    private static readonly int[] Exp_2_2_1 = [2, 2, 1];
    private static readonly int[] Exp_2_2_2 = [2, 2, 2];

    // Gating bound. These scans used to run to 10,000,000, 100,000,000 or int.MaxValue, which
    // put the suite past an hour and so stopped it gating anything. What each test claims is
    // unchanged; only the range it is checked over is. Measured coverage below this limit:
    // 9 values decaying to one in a single odd step, 22 in two, 48 in three, and 8,338
    // reducible by the 4n+1 map - enough of each that the property is exercised rather than
    // merely named. The unbounded sweep each of these came from lives in Collatz.Experiments,
    // where it may take an hour; run it there when the range itself is the point.
    private const int GatedOddScanLimit = 100_000;

    // Idea2026 walks odd *indices* rather than odd values, so it carries its own bound.
    // Its full run is Collatz.Experiments' SeedIndexSweep.
    private const int GatedSeedIndexLimit = 10_000;

    // Straddles long.MaxValue and mixes symmetric with asymmetric bit patterns, so a
    // most-significant-first result cannot be mistaken for a least-significant-first one.
    private static readonly BigInteger[] BinaryRoundTripValues =
    [
        BigInteger.Zero,
        BigInteger.One,
        (BigInteger)long.MaxValue - 1,
        long.MaxValue,
        (BigInteger)long.MaxValue + 1,
        (BigInteger.One << 64) + 1,
        BigInteger.One << 100,
        BigInteger.Parse("818446744073709551615", CultureInfo.InvariantCulture),
    ];

    #region Fact Methods
    [Fact]
    public void TextSolveForLoop()
    {
        bool isCollatzLoop;
        BigRational n;

        // Loop length 1
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(1, -1));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2, out n);
        Assert.True(isCollatzLoop && n == new BigRational(1, 1));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_3, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(1, 5));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_4, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(1, 13));

        // Loop length 2
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(5, -5));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_2, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(5, -1));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(7, -1));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_2, out n);
        Assert.True(isCollatzLoop && n == new BigRational(7, 7));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_3, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(5, 7));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_3_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(11, 7));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_3, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(7, 23));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_3_2, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(11, 23));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_3_3, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(11, 55));

        // Loop length 3
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_1_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(19, -19));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_1_2, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(19, -11));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_2_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(23, -11));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_1_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(29, -11));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_1_2_2, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(23, 5));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_1_2, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(29, 5));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_2_1, out n);
        Assert.True(!isCollatzLoop && n == new BigRational(37, 5));
        isCollatzLoop = CollatzMath.SolveForLoop(Exp_2_2_2, out n);
        Assert.True(isCollatzLoop && n == new BigRational(37, 37));

        // All 2s
        for (int i = 1; i < 10; i++)
        {
            int[] twosExponentArray = new int[i];
            for (int j = 0; j < twosExponentArray.Length; j++)
                twosExponentArray[j] = 2;
            isCollatzLoop = CollatzMath.SolveForLoop(twosExponentArray, out n);
            Assert.True(isCollatzLoop && n == 1);
        }

        // Length 2, order 2: expect {1,2},{2,1},{2,2}
        IReadOnlyList<int[]> permsOrder2Len2List = CollatzMath.GenerateExponentPermutations(2, 2);
        List<string> permsOrder2Len2 = permsOrder2Len2List
            .Select(a => string.Join(",", a))
            .OrderBy(s => s)
            .ToList();
        List<string> expected2 = new List<string> { "1,2", "2,1", "2,2" };
        Assert.True(permsOrder2Len2.Count == expected2.Count);
        foreach (string e in expected2) Assert.Contains(e, permsOrder2Len2);

        // Length 2, order 3: expect {1,3},{2,3},{3,1},{3,2},{3,3}
        IReadOnlyList<int[]> permsOrder3Len2List = CollatzMath.GenerateExponentPermutations(2, 3);
        List<string> permsOrder3Len2 = permsOrder3Len2List
            .Select(a => string.Join(",", a))
            .OrderBy(s => s)
            .ToList();
        List<string> expected3 = new List<string> { "1,3", "2,3", "3,1", "3,2", "3,3" };
        Assert.True(permsOrder3Len2.Count == expected3.Count);
        foreach (string e in expected3) Assert.Contains(e, permsOrder3Len2);

        // Length 1, order k: only [k]
        IReadOnlyList<int[]> single = CollatzMath.GenerateExponentPermutations(1, 5);
        Assert.Single(single);
        Assert.True(single[0][0] == 5);

        // Sanity: count formula order^len - (order-1)^len
        int len = 4, ord = 3;
        IReadOnlyList<int[]> permsOrder3Len4List = CollatzMath.GenerateExponentPermutations(len, ord);
        int all = permsOrder3Len4List.Count;
        int expectedCount = (int)(BigInteger.Pow(ord, len) - BigInteger.Pow(ord - 1, len));
        Assert.True(all == expectedCount);

        // Build one summary string per length (1..5)
        List<string> loopSummaryByLength = new();
        for (int length = 1; length <= 5; length++)
        {
            StringBuilder sbLen = new();
            // The trailing Double column is presentation: it is written to a string and never
            // read back, so no result depends on it. That is what separates it from the log2ratio
            // derivation halheinrich/Math#5 removed, where a double decided pow2 and thence
            // addConst. § Exactness discipline bans floating point upstream of presentation, not
            // in it.
            sbLen.AppendLine("Order,Permutation,N,IsLoop,Numerator,Denominator,Double");
            for (int order = 1; order <= 5; order++)
            {
                foreach (int[] perm in CollatzMath.GenerateExponentPermutations(length, order))
                {
                    bool isLoop = CollatzMath.SolveForLoop(perm, out n);
                    sbLen.Append(order)
                         .Append(",[")
                         .Append(string.Join(' ', perm))
                         .Append("],")
                         .Append(n.ToString())
                         .Append(',')
                         .Append(isLoop ? '1' : '0')
                         .Append(',')
                         .Append(n.Numerator.ToString(CultureInfo.InvariantCulture))
                         .Append(',')
                         .Append(n.Denominator.ToString(CultureInfo.InvariantCulture))
                         .Append(',')
                         .Append(((double)n).ToString(CultureInfo.InvariantCulture))
                         .AppendLine();
                }
            }
            loopSummaryByLength.Add(sbLen.ToString());
        }
    }
    [Fact]
    public void TestNextOdd()
    {
        BigInteger clltz = CollatzMath.NextOdd(3);
        Assert.True(clltz == 5);
        Assert.Equal("101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.NextOdd(5);
        Assert.True(clltz == 1);
        Assert.Equal("1", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.NextOdd(7);
        Assert.True(clltz == 11);
        Assert.Equal("1011", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.NextOdd(9);
        Assert.True(clltz == 7);
        Assert.Equal("111", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.NextOdd(11);
        Assert.True(clltz == 17);
        Assert.Equal("10001", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10001", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.NextOdd(3) == 5);
        Assert.True(CollatzMath.NextOdd(5) == 1);
        Assert.True(CollatzMath.NextOdd(7) == 11);
        Assert.True(CollatzMath.NextOdd(9) == 7);
        Assert.True(CollatzMath.NextOdd(11) == 17);

    }
    [Fact]
    public void TestCollapseInOne()
    {
        BigInteger clltz = CollatzMath.CollapseInOne(1);
        Assert.True(clltz == 1);
        Assert.Equal("1", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(2);
        Assert.True(clltz == (BigInteger)5);
        Assert.Equal("101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(3);
        Assert.True(clltz == (BigInteger)21);
        Assert.Equal("10101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(4);
        Assert.True(clltz == (BigInteger)85);
        Assert.Equal("1010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(5);
        Assert.True(clltz == (BigInteger)341);
        Assert.Equal("101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(6);
        Assert.True(clltz == (BigInteger)1365);
        Assert.Equal("10101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(7);
        Assert.True(clltz == (BigInteger)5461);
        Assert.Equal("1010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(8);
        Assert.True(clltz == (BigInteger)21845);
        Assert.Equal("101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(9);
        Assert.True(clltz == (BigInteger)87381);
        Assert.Equal("10101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOne(10);
        Assert.True(clltz == (BigInteger)349525);
        Assert.Equal("1010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInOne(1) == (BigInteger)1);
        Assert.True(CollatzMath.CollapseInOne(2) == (BigInteger)5);
        Assert.True(CollatzMath.CollapseInOne(3) == (BigInteger)21);
        Assert.True(CollatzMath.CollapseInOne(4) == (BigInteger)85);
        Assert.True(CollatzMath.CollapseInOne(5) == (BigInteger)341);
        Assert.True(CollatzMath.CollapseInOne(6) == (BigInteger)1365);
        Assert.True(CollatzMath.CollapseInOne(7) == (BigInteger)5461);
        Assert.True(CollatzMath.CollapseInOne(8) == (BigInteger)21845);
        Assert.True(CollatzMath.CollapseInOne(9) == (BigInteger)87381);
        Assert.True(CollatzMath.CollapseInOne(10) == (BigInteger)349525);
    }
    [Fact]
    public void TestCollapseInOne_ModOneOut()
    {
        BigInteger clltz = CollatzMath.CollapseInOneModOneOut(1);
        Assert.True(clltz == (BigInteger)85);
        Assert.Equal("1010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModOneOut(2);
        Assert.True(clltz == (BigInteger)5461);
        Assert.Equal("1010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModOneOut(3);
        Assert.True(clltz == (BigInteger)349525);
        Assert.Equal("1010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModOneOut(4);
        Assert.True(clltz == (BigInteger)22369621);
        Assert.Equal("1010101010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModOneOut(5);
        Assert.True(clltz == (BigInteger)1431655765);
        Assert.Equal("1010101010101010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101010101010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInOneModOneOut(1) == (BigInteger)85);
        Assert.True(CollatzMath.CollapseInOneModOneOut(2) == (BigInteger)5461);
        Assert.True(CollatzMath.CollapseInOneModOneOut(3) == (BigInteger)349525);
        Assert.True(CollatzMath.CollapseInOneModOneOut(4) == (BigInteger)22369621);
        Assert.True(CollatzMath.CollapseInOneModOneOut(5) == (BigInteger)1431655765);
    }
    [Fact]
    public void TestCollapseInOne_ModTwoOut()
    {
        BigInteger clltz = CollatzMath.CollapseInOneModTwoOut(1);
        Assert.True(clltz == (BigInteger)5);
        Assert.Equal("101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModTwoOut(2);
        Assert.True(clltz == (BigInteger)341);
        Assert.Equal("101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModTwoOut(3);
        Assert.True(clltz == (BigInteger)21845);
        Assert.Equal("101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModTwoOut(4);
        Assert.True(clltz == (BigInteger)1398101);
        Assert.Equal("101010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInOneModTwoOut(5);
        Assert.True(clltz == (BigInteger)89478485);
        Assert.Equal("101010101010101010101010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101010101010101010101010101", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInOneModTwoOut(1) == (BigInteger)5);
        Assert.True(CollatzMath.CollapseInOneModTwoOut(2) == (BigInteger)341);
        Assert.True(CollatzMath.CollapseInOneModTwoOut(3) == (BigInteger)21845);
        Assert.True(CollatzMath.CollapseInOneModTwoOut(4) == (BigInteger)1398101);
        Assert.True(CollatzMath.CollapseInOneModTwoOut(5) == (BigInteger)89478485);
    }
    [Fact]
    public void TestCollapseInTwo_ModOne()
    {
        BigInteger clltz = CollatzMath.CollapseInTwoModOne(1, 1);
        Assert.True(clltz == (BigInteger)113);
        Assert.Equal("1110001", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModOne(1, 1) == (BigInteger)113);

        clltz = CollatzMath.CollapseInTwoModOne(1, 2);
        Assert.True(clltz == (BigInteger)453);
        Assert.Equal("111000101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(2, 1);
        Assert.True(clltz == (BigInteger)7281);
        Assert.Equal("1110001110001", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(2, 2);
        Assert.True(clltz == (BigInteger)29125);
        Assert.Equal("111000111000101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModOne(1, 2) == (BigInteger)453);
        Assert.True(CollatzMath.CollapseInTwoModOne(2, 1) == (BigInteger)7281);
        Assert.True(CollatzMath.CollapseInTwoModOne(2, 2) == (BigInteger)29125);

        clltz = CollatzMath.CollapseInTwoModOne(1, 3);
        Assert.True(clltz == (BigInteger)1813);
        Assert.Equal("11100010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(2, 3);
        Assert.True(clltz == (BigInteger)116501);
        Assert.Equal("11100011100010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(3, 1);
        Assert.True(clltz == (BigInteger)466033);
        Assert.Equal("1110001110001110001", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(3, 2);
        Assert.True(clltz == (BigInteger)1864133);
        Assert.Equal("111000111000111000101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(3, 3);
        Assert.True(clltz == (BigInteger)7456533);
        Assert.Equal("11100011100011100010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModOne(1, 3) == (BigInteger)1813);
        Assert.True(CollatzMath.CollapseInTwoModOne(2, 3) == (BigInteger)116501);
        Assert.True(CollatzMath.CollapseInTwoModOne(3, 1) == (BigInteger)466033);
        Assert.True(CollatzMath.CollapseInTwoModOne(3, 2) == (BigInteger)1864133);
        Assert.True(CollatzMath.CollapseInTwoModOne(3, 3) == (BigInteger)7456533);

        clltz = CollatzMath.CollapseInTwoModOne(1, 4);
        Assert.True(clltz == (BigInteger)7253);
        Assert.Equal("1110001010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(2, 4);
        Assert.True(clltz == (BigInteger)466005);
        Assert.Equal("1110001110001010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(3, 4);
        Assert.True(clltz == (BigInteger)29826133);
        Assert.Equal("1110001110001110001010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(4, 1);
        Assert.True(clltz == (BigInteger)29826161);
        Assert.Equal("1110001110001110001110001", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1000111000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(4, 2);
        Assert.True(clltz == (BigInteger)119304645);
        Assert.Equal("111000111000111000111000101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101000111000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(4, 3);
        Assert.True(clltz == (BigInteger)477218581);
        Assert.Equal("11100011100011100011100010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101000111000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModOne(4, 4);
        Assert.True(clltz == (BigInteger)1908874325);
        Assert.Equal("1110001110001110001110001010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1010101000111000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModOne(1, 4) == (BigInteger)7253);
        Assert.True(CollatzMath.CollapseInTwoModOne(2, 4) == (BigInteger)466005);
        Assert.True(CollatzMath.CollapseInTwoModOne(3, 4) == (BigInteger)29826133);
        Assert.True(CollatzMath.CollapseInTwoModOne(4, 1) == (BigInteger)29826161);
        Assert.True(CollatzMath.CollapseInTwoModOne(4, 2) == (BigInteger)119304645);
        Assert.True(CollatzMath.CollapseInTwoModOne(4, 3) == (BigInteger)477218581);
        Assert.True(CollatzMath.CollapseInTwoModOne(4, 4) == (BigInteger)1908874325);
    }
    [Fact]
    public void TestCollapseInTwo_ModTwo()
    {
        BigInteger clltz = CollatzMath.CollapseInTwoModTwo(1, 1);
        Assert.True(clltz == (BigInteger)3);
        Assert.Equal("11", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("11", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModTwo(1, 1) == (BigInteger)3);

        clltz = CollatzMath.CollapseInTwoModTwo(1, 2);
        Assert.True(clltz == (BigInteger)13);
        Assert.Equal("1101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1011", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(2, 1);
        Assert.True(clltz == (BigInteger)227);
        Assert.Equal("11100011", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("11000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(2, 2);
        Assert.True(clltz == (BigInteger)909);
        Assert.Equal("1110001101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1011000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModTwo(1, 2) == (BigInteger)13);
        Assert.True(CollatzMath.CollapseInTwoModTwo(2, 1) == (BigInteger)227);
        Assert.True(CollatzMath.CollapseInTwoModTwo(2, 2) == (BigInteger)909);

        clltz = CollatzMath.CollapseInTwoModTwo(1, 3);
        Assert.True(clltz == (BigInteger)53);
        Assert.Equal("110101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101011", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(2, 3);
        Assert.True(clltz == (BigInteger)3637);
        Assert.Equal("111000110101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101011000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(3, 1);
        Assert.True(clltz == (BigInteger)14563);
        Assert.Equal("11100011100011", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("11000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(3, 2);
        Assert.True(clltz == (BigInteger)58253);
        Assert.Equal("1110001110001101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1011000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(3, 3);
        Assert.True(clltz == (BigInteger)233013);
        Assert.Equal("111000111000110101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101011000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModTwo(1, 3) == (BigInteger)53);
        Assert.True(CollatzMath.CollapseInTwoModTwo(2, 3) == (BigInteger)3637);
        Assert.True(CollatzMath.CollapseInTwoModTwo(3, 1) == (BigInteger)14563);
        Assert.True(CollatzMath.CollapseInTwoModTwo(3, 2) == (BigInteger)58253);
        Assert.True(CollatzMath.CollapseInTwoModTwo(3, 3) == (BigInteger)233013);

        clltz = CollatzMath.CollapseInTwoModTwo(1, 4);
        Assert.True(clltz == (BigInteger)213);
        Assert.Equal("11010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101011", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(2, 4);
        Assert.True(clltz == (BigInteger)14549);
        Assert.Equal("11100011010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101011000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(3, 4);
        Assert.True(clltz == (BigInteger)932053);
        Assert.Equal("11100011100011010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101011000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(4, 1);
        Assert.True(clltz == (BigInteger)932067);
        Assert.Equal("11100011100011100011", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("11000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(4, 2);
        Assert.True(clltz == (BigInteger)3728269);
        Assert.Equal("1110001110001110001101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("1011000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(4, 3);
        Assert.True(clltz == (BigInteger)14913077);
        Assert.Equal("111000111000111000110101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("101011000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        clltz = CollatzMath.CollapseInTwoModTwo(4, 4);
        Assert.True(clltz == (BigInteger)59652309);
        Assert.Equal("11100011100011100011010101", CollatzMath.toBinaryLittleEndianString(clltz));
        Assert.Equal("10101011000111000111000111", CollatzMath.toBinaryBigEndianString(clltz));

        Assert.True(CollatzMath.CollapseInTwoModTwo(1, 4) == (BigInteger)213);
        Assert.True(CollatzMath.CollapseInTwoModTwo(2, 4) == (BigInteger)14549);
        Assert.True(CollatzMath.CollapseInTwoModTwo(3, 4) == (BigInteger)932053);
        Assert.True(CollatzMath.CollapseInTwoModTwo(4, 1) == (BigInteger)932067);
        Assert.True(CollatzMath.CollapseInTwoModTwo(4, 2) == (BigInteger)3728269);
        Assert.True(CollatzMath.CollapseInTwoModTwo(4, 3) == (BigInteger)14913077);
        Assert.True(CollatzMath.CollapseInTwoModTwo(4, 4) == (BigInteger)59652309);
    }
    [Fact]
    public void TestCollapseFamily_RejectsArgumentsOutsideItsDomain()
    {
        // halheinrich/Math#27. Every rejection below used to reach the caller as an
        // ArgumentOutOfRangeException whose ParamName was "exponent" - a parameter of
        // BigInteger.Pow that no caller of these methods passed and none could see - or as no
        // exception at all. Each assertion here names the parameter the caller did pass, so a
        // regression to the internal name fails rather than merely reading badly.

        // Below the lower bound, where the exponent was still non-negative and the method
        // answered. CollapseInOne(0) returned 0, which is outside the sequence its own summary
        // describes.
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOne(0)).ParamName);
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOne(-1)).ParamName);
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOneModOneOut(-1)).ParamName);
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOneModTwoOut(0)).ParamName);

        // CollapseInTwoModOne accepted both of these and answered, measured on SDK 10.0.400.
        // n2 = 0 leaves the exponent at zero and returns an even value that is not a two-odd-step
        // decayer at all - 28 for n1 = 1, which takes five odd steps; 1820 for n1 = 2, twelve;
        // 116508 for n1 = 3, thirty-four. n1 = 0 selects v = 1, the fixed point, so what came back
        // were one-step decayers: 1 for n2 = 1 and 5 for n2 = 2.
        Assert.Equal("n2", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModOne(1, 0)).ParamName);
        Assert.Equal("n1", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModOne(0, 1)).ParamName);
        Assert.Equal("n2", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModOne(1, -1)).ParamName);

        // CollapseInTwoModTwo rejected the same two cases already, but only as a side effect of
        // its exponent 2*n2 - 1 going negative there. That accident is the whole difference
        // between the two siblings, and it is why only one of them was silently wrong.
        Assert.Equal("n1", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModTwo(0, 1)).ParamName);
        Assert.Equal("n2", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModTwo(1, 0)).ParamName);

        // Above the upper bound, which nothing checked at either end of the family. 3n + 1,
        // 3n - 1 and 2*n2 are unchecked int arithmetic, so a large argument wrapped to a tiny
        // exponent and a small wrong answer came back. Measured on SDK 10.0.400 before the guard:
        // CollapseInOneModOneOut(1431655765) returned 0 and (1431655766) returned 21;
        // CollapseInOneModTwoOut(1431655766) returned 1.
        const int firstOverflowingIndex = (int.MaxValue - 1) / 3 + 1;
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOneModOneOut(firstOverflowingIndex)).ParamName);
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOneModOneOut(1_431_655_766)).ParamName);
        Assert.Equal("n", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInOneModTwoOut(1_431_655_766)).ParamName);
        Assert.Equal("n1", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModOne(firstOverflowingIndex, 1)).ParamName);
        Assert.Equal("n1", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModTwo(firstOverflowingIndex, 1)).ParamName);

        const int firstOverflowingHalfExponent = (int.MaxValue - 1) / 2 + 1;
        Assert.Equal("n2", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModOne(1, firstOverflowingHalfExponent)).ParamName);
        Assert.Equal("n2", Assert.Throws<ArgumentOutOfRangeException>(
            () => CollatzMath.CollapseInTwoModTwo(1, firstOverflowingHalfExponent)).ParamName);
    }
    [Fact]
    public void TestCollapseFamily_AcceptsTheLowestArgumentInEachDomain()
    {
        // The control for the rejections above: a guard one too strict fails here rather than
        // passing quietly. The lower bounds are not the same number across the family, which is
        // the part halheinrich/Math#27 said no caller could discover.
        Assert.Equal(BigInteger.One, CollatzMath.CollapseInOne(1));
        Assert.Equal(BigInteger.One, CollatzMath.CollapseInOneModOneOut(0));
        Assert.Equal((BigInteger)5, CollatzMath.CollapseInOneModTwoOut(1));
        Assert.Equal((BigInteger)113, CollatzMath.CollapseInTwoModOne(1, 1));
        Assert.Equal((BigInteger)3, CollatzMath.CollapseInTwoModTwo(1, 1));

        // And each value is a member of the family its method names, which is what the old lower
        // bounds failed to guarantee. Hand-derived: 1 -> 4 -> 1; 5 -> 16 -> 1;
        // 113 -> 340 -> 85 -> 256 -> 1; 3 -> 10 -> 5 -> 16 -> 1.
        Assert.Equal(1ul, CollatzMath.OddStepCountToOne(CollatzMath.CollapseInOne(1)));
        Assert.Equal(1ul, CollatzMath.OddStepCountToOne(CollatzMath.CollapseInOneModOneOut(0)));
        Assert.Equal(1ul, CollatzMath.OddStepCountToOne(CollatzMath.CollapseInOneModTwoOut(1)));
        Assert.Equal(2ul, CollatzMath.OddStepCountToOne(CollatzMath.CollapseInTwoModOne(1, 1)));
        Assert.Equal(2ul, CollatzMath.OddStepCountToOne(CollatzMath.CollapseInTwoModTwo(1, 1)));
    }
    [Fact]
    public void TestBinaryStringToBigInt()
    {
        BigInteger gtUint64max = BigInteger.Parse("818446744073709551615", CultureInfo.InvariantCulture); // Larger than UInt64.MaxValue
        string czTxt = CollatzMath.toBinaryLittleEndianString(gtUint64max);
        BigInteger clltz = CollatzMath.toBigIntegerFromBinaryLittleEndianString(czTxt);
        Assert.True(clltz == gtUint64max);

        clltz = CollatzMath.toBigIntegerFromBinaryLittleEndianString("1");
        Assert.True(clltz == 1);
        clltz = CollatzMath.toBigIntegerFromBinaryLittleEndianString("11100011100011100011010101");
        Assert.True(clltz == 59652309);
        clltz = CollatzMath.toBigIntegerFromBinaryBigEndianString("10101011000111000111000111");
        Assert.True(clltz == 59652309);
    }

    [Fact]
    public void TestToBinaryLittleEndianStringGtInt64_AboveInt64()
    {
        // halheinrich/Math#1: this method guarded values above long.MaxValue by calling itself
        // with the argument unchanged, so every such value recursed until the stack ran out.
        // Measured on SDK 10.0.400 before the fix: "Stack overflow." and a dead process, in both
        // Debug and Release - no tail call saves it. A stack overflow cannot be caught, so the
        // failure this test now prevents was the test host dying, not a red test.
        BigInteger twoPow63 = (BigInteger)long.MaxValue + 1;
        Assert.Equal("1" + new string('0', 63), CollatzMath.toBinaryLittleEndianStringGtInt64(twoPow63));

        BigInteger twoPow64Plus1 = (BigInteger.One << 64) + 1;
        Assert.Equal("1" + new string('0', 63) + "1", CollatzMath.toBinaryLittleEndianStringGtInt64(twoPow64Plus1));

        // Cross-check against the sibling implementation, which appends where this one prepends,
        // over values on both sides of the boundary the deleted guard claimed to police.
        foreach (BigInteger value in BinaryRoundTripValues)
        {
            char[] reversed = CollatzMath.toBinaryBigEndianStringGtInt64(value).ToCharArray();
            Array.Reverse(reversed);
            Assert.Equal(new string(reversed), CollatzMath.toBinaryLittleEndianStringGtInt64(value));
        }
    }

    [Fact]
    public void TestFloorLog2Ratio_MatchesExactReference()
    {
        // halheinrich/Math#5(a). Exhaustive over small values, against a reference that shares no
        // machinery with the implementation: it doubles and compares and never reads a bit length.
        for (int a = 1; a <= 64; a++)
            for (int b = 1; b <= 64; b++)
                Assert.Equal(ReferenceFloorLog2Ratio(a, b), CollatzMath.FloorLog2Ratio(a, b));
    }

    [Fact]
    public void TestFloorLog2Ratio_ExactPowersOfTwo()
    {
        // Ratios sitting exactly on a power of two are the boundary either side of which the
        // bit-length difference alone gives a different answer from the truth.
        Assert.Equal(0, CollatzMath.FloorLog2Ratio(1, 1));
        Assert.Equal(1, CollatzMath.FloorLog2Ratio(2, 1));
        Assert.Equal(-1, CollatzMath.FloorLog2Ratio(1, 2));
        Assert.Equal(3, CollatzMath.FloorLog2Ratio(24, 3));
        Assert.Equal(-3, CollatzMath.FloorLog2Ratio(3, 24));
        Assert.Equal(100, CollatzMath.FloorLog2Ratio(BigInteger.One << 100, BigInteger.One));
        Assert.Equal(63, CollatzMath.FloorLog2Ratio(BigInteger.One << 200, BigInteger.One << 137));
    }

    [Fact]
    public void TestFloorLog2Ratio_CasesTheDoubleDerivationGotWrong()
    {
        // Each expectation below is the exact answer; the value in the comment is what the
        // deleted derivation - (int)Math.Truncate(Math.Log((double)a / (double)b, 2)) - actually
        // returned, measured on SDK 10.0.400.

        // 2^60 - 1 rounds up to 2^60 in a 53-bit mantissa. Old answer: 60.
        Assert.Equal(59, CollatzMath.FloorLog2Ratio((BigInteger.One << 60) - 1, BigInteger.One));

        // The ratio overflows a double to infinity and the cast saturates. Old answer:
        // int.MaxValue, which the caller would then have fed to BigInteger.One << log2ratio.
        Assert.Equal(1000, CollatzMath.FloorLog2Ratio(BigInteger.One << 2000, BigInteger.One << 1000));

        // The bit-length difference is 63 but the true floor is 62 - the case the exact
        // comparison exists to settle. Old answer: 63.
        Assert.Equal(62, CollatzMath.FloorLog2Ratio(BigInteger.One << 200, (BigInteger.One << 137) + 1));
    }

    [Fact]
    public void TestOddStepCountToOne_RejectsNonPositiveArguments()
    {
        // halheinrich/Math#6. These were guarded by Debug.Assert, which meant the guard existed
        // only in Debug - and there it did not fail the test, it killed the process: a failed
        // Debug.Assert terminates the host, measured at exit code 35 on SDK 10.0.400. In Release
        // there was no guard at all and the method did not return, because 0 >> 1 is 0 and 0 is
        // even, so the strip loop spins forever. Either way this test could not have been
        // written before the change.
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToOne(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToOne(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToOne(-6));
    }

    [Fact]
    public void TestOddStepCountToSmaller_RejectsNonPositiveArgumentsAndCountsSteps()
    {
        // The sibling guard halheinrich/Math#6's list did not name. Same Debug.Assert, but not
        // the same failure behind it, measured on SDK 10.0.400 with the guard removed: 0 hung in
        // the strip loop as OddStepCountToOne does, most negatives hung in the step loop with the
        // orbit settling into a negative cycle - and -17 returned 1, because -25 really is less
        // than -17. A plausible answer to a nonsensical argument, which is worse than a hang.
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToSmaller(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToSmaller(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddStepCountToSmaller(-17));

        // This method had no test of any kind before now; its only live callers are experiments.
        // Both expectations are hand-derived rather than read off the implementation:
        //   3 -> 10 -> 5, and 5 is not below 3; 5 -> 16 -> 8 -> 4 -> 2 -> 1, and 1 is. Two steps.
        Assert.Equal(2ul, CollatzMath.OddStepCountToSmaller(3));
        //   7 -> 11 -> 17 -> 13 -> 5, none below 7 until 5. Four steps.
        Assert.Equal(4ul, CollatzMath.OddStepCountToSmaller(7));
    }

    [Fact]
    public void TestOddIndexBijection_RoundTrips()
    {
        // CollatzMath.OddOfIndex / IndexOfOdd index the odd integers 3, 5, 7, ... from zero.
        for (int index = 0; index < 1000; index++)
        {
            BigInteger odd = CollatzMath.OddOfIndex(index);
            Assert.Equal((BigInteger)(2 * index + 3), odd);
            Assert.Equal((BigInteger)index, CollatzMath.IndexOfOdd(odd));
        }

        // Past the int boundary where the test-local version this replaces used to wrap:
        // it evaluated index * 2 + 3 in int, so 1073741823 came back as -2147483647.
        Assert.Equal(BigInteger.Parse("2147483649", CultureInfo.InvariantCulture),
            CollatzMath.OddOfIndex(1_073_741_823));

        BigInteger farOut = (BigInteger.One << 200) + 7;
        Assert.Equal(farOut, CollatzMath.IndexOfOdd(CollatzMath.OddOfIndex(farOut)));
    }

    [Fact]
    public void TestOddIndexBijection_RejectsValuesOutsideItsDomain()
    {
        // An even argument used to return the index of the odd value below it, silently:
        // 4 gave 0, the index of 3, and 6 gave 1.
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.IndexOfOdd(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.IndexOfOdd(6));

        // Below three the quotient went negative and the conversion threw OverflowException,
        // which said nothing about the argument. It is now the argument's own exception.
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.IndexOfOdd(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.IndexOfOdd(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.IndexOfOdd(-3));

        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.OddOfIndex(-1));
    }

    [Fact]
    public void TestFloorLog2Ratio_RejectsNonPositiveArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.FloorLog2Ratio(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.FloorLog2Ratio(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.FloorLog2Ratio(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CollatzMath.FloorLog2Ratio(1, -1));
    }

    // Deliberately unlike the implementation: no bit lengths and no division. It doubles the
    // numerator until the ratio reaches one, then doubles the denominator until it drops below
    // two, so the exponent it counts is floor(log2(a / b)) by construction.
    private static int ReferenceFloorLog2Ratio(BigInteger a, BigInteger b)
    {
        int exponent = 0;
        BigInteger numerator = a, denominator = b;
        while (numerator < denominator) { numerator *= 2; exponent--; }
        while (numerator >= denominator * 2) { denominator *= 2; exponent++; }
        return exponent;
    }
    [Fact]
    public void TestDecayInOneViaBinaryBigendianText()
    {
        int trials = 100;
        bool[] isCase = new bool[8];
        {
            for (int j = 0; j < isCase.Length; j++)
                isCase[j] = false;
            StringBuilder sb = new StringBuilder("1");
            string seedBinaryBE;
            BigInteger seed, pow4 = 1, pow4sum = 1;
            int mod3 = 1;
            for (int i = 0; i < trials; i++)
            {
                seedBinaryBE = sb.ToString();
                seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
                Assert.True(seed == pow4sum);
                Assert.True(CollatzMath.NextOdd(seed) == 1);
                Assert.True(seed % 3 == mod3);
                if (++mod3 == 3)
                    mod3 = 0;
                sb.Append("01");
                pow4 <<= 2;
                pow4sum += pow4;
                switch (i)
                {
                    case 0:
                        Assert.True(seed == 1);
                        Assert.Equal("1", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 1:
                        Assert.True(seed == 5);
                        Assert.Equal("101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 2:
                        Assert.True(seed == 21);
                        Assert.Equal("10101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 3:
                        Assert.True(seed == 85);
                        Assert.Equal("1010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 4:
                        Assert.True(seed == 341);
                        Assert.Equal("101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 5:
                        Assert.True(seed == 1365);
                        Assert.Equal("10101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 6:
                        Assert.True(seed == 5461);
                        Assert.Equal("1010101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 7:
                        Assert.True(seed == 21845);
                        Assert.Equal("101010101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    default:
                        break;
                }
            }
            for (int j = 0; j < isCase.Length; j++)
                Assert.True(isCase[j]);
        }
        {
            for (int j = 0; j < isCase.Length; j++)
                isCase[j] = false;
            BigInteger seed, pow4 = 1, pow4sum = 1;
            int mod3 = 1, i = 0;
            foreach (string seedBinaryBE in CollatzMath.GetBinaryBigEndianDecaysInOne().Take(100))
            {
                seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
                Assert.True(seed == pow4sum);
                Assert.True(CollatzMath.NextOdd(seed) == 1);
                Assert.True(seed % 3 == mod3);
                if (++mod3 == 3)
                    mod3 = 0;
                pow4 <<= 2;
                pow4sum += pow4;
                switch (i)
                {
                    case 0:
                        Assert.True(seed == 1);
                        Assert.Equal("1", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 1:
                        Assert.True(seed == 5);
                        Assert.Equal("101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 2:
                        Assert.True(seed == 21);
                        Assert.Equal("10101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 3:
                        Assert.True(seed == 85);
                        Assert.Equal("1010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 4:
                        Assert.True(seed == 341);
                        Assert.Equal("101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 5:
                        Assert.True(seed == 1365);
                        Assert.Equal("10101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 6:
                        Assert.True(seed == 5461);
                        Assert.Equal("1010101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    case 7:
                        Assert.True(seed == 21845);
                        Assert.Equal("101010101010101", seedBinaryBE);
                        Assert.False(isCase[i]);
                        isCase[i] = true;
                        break;
                    default:
                        break;
                }
                ++i;
            }
            for (int j = 0; j < isCase.Length; j++)
                Assert.True(isCase[j]);
        }
        StringBuilder sb1 = new StringBuilder("1");
        for (int i = 0; i < trials; i++)
        {
            AssertDecayIn1(sb1.ToString());
            sb1.Insert(0, "10");
        }
    }
    [Fact]
    public void Idea2026()
    {
        const int maxLoopIdx = GatedSeedIndexLimit;
        BigInteger nxtOdd, nxtIdx, currIdx;
        int loopIdx = 0;
        List<ulong> seedList =
        [
            0, // Index 0 corresponds to Odd 3
        ];
        List<BigInteger> thisOddList = [];
        while (true)
        {
            thisOddList.Clear();

            nxtOdd = CollatzMath.OddOfIndex(loopIdx);
            thisOddList.Add(nxtOdd);
            currIdx = loopIdx;
            while (true)
            {
                nxtOdd = CollatzMath.NextOdd(nxtOdd);
                if (nxtOdd == 1)
                {
                    for (int i = 0; i < thisOddList.Count; i++)
                    {
                        seedList[(int)CollatzMath.IndexOfOdd(thisOddList[i])] = (ulong)(thisOddList.Count - i);
                    }
                    break;
                }
                thisOddList.Add(nxtOdd);
                nxtIdx = CollatzMath.IndexOfOdd(nxtOdd);
                while (nxtIdx >= seedList.Count)
                {
                    seedList.Add(0);
                }
                currIdx = CollatzMath.IndexOfOdd(nxtOdd);
            }
            if (loopIdx > maxLoopIdx)
            {
                break;
            }
            while (true)
            {
                if (++loopIdx == seedList.Count)
                {
                    seedList.Add(0);
                    break;
                }
                if (seedList[loopIdx] == 0)
                    break;
            }
        }
        for (int i = 0; i < seedList.Count; i++)
        {
            if (seedList[i] != 0)
            {
                BigInteger bi = CollatzMath.OddOfIndex(i);
                Assert.True(seedList[i] == CollatzMath.OddStepCountToOne(bi));
            }
        }
        // The table this method used to build here and drop is now
        // Collatz.Experiments' SeedIndexSweep, which runs the full index range.
    }
    // Remove optional leading "10" blocks and optional trailing "000111" block.

    //[Fact]
    //public void TestDecayInTwoViaBinaryBigendianText()
    //{
    //    int trials = 100;
    //    string echoPrefix = "10";
    //    bool[] isCaseAnchor = new bool[8];
    //    bool[,] isCaseEcho = new bool[3, 3];
    //    StringBuilder BigEndDecayIn2 = new("1");
    //    StringBuilder BigEndEchoIn2 = new();
    //    for (int i = 0; i < isCaseAnchor.Length; i++)
    //        isCaseAnchor[i] = false;
    //    for (int i = 0; i < isCaseEcho.GetLength(0); i++)
    //        for (int j = 0; j < isCaseEcho.GetLength(1); j++)
    //            isCaseEcho[i, j] = false;
    //    BigInteger decayIn1, decayIn2;
    //    //for (int i = 0; i < trials; i++)
    //    //{
    //    //    BigEndDecayIn2.Insert(1, "1");
    //    //    decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndDecayIn2.ToString());
    //    //    decayIn1 = CollatzMath.NextOdd(decayIn2);
    //    //    Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //    //    BigEndEchoIn2.Clear().Append(BigEndDecayIn2);
    //    //    for (int j = 0; j < trials; j++)
    //    //    {
    //    //        BigEndEchoIn2.Insert(0, echoPrefix);
    //    //        decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
    //    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //    //    }
    //    //    if (i < 32)
    //    //        BigEndDecayIn2.Insert(0, "10");
    //    //    else
    //    //        BigEndDecayIn2.Insert(0, "1");
    //    //}
    //    BigEndDecayIn2.Clear().Append("1");
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndDecayIn2.ToString());
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        BigEndEchoIn2.Clear().Append(BigEndDecayIn2);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEchoIn2.Insert(0, echoPrefix);
    //            decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        }
    //        if (i < 8)
    //            BigEndDecayIn2.Insert(1, "1");
    //        else
    //            BigEndDecayIn2.Insert(1, '0');
    //    }
    //    foreach (string seedBinaryBE in CollatzMath.GetBinaryBigEndianDecaysInTwo())
    //    {
    //        decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        BigEndEchoIn2.Clear().Append(seedBinaryBE);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEchoIn2.Insert(0, echoPrefix);
    //            decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        }
    //    }
    //    StringBuilder sb = new();
    //    sb.AppendLine("Anchor,DecayIn1,DecayIn2,Steps");
    //    foreach (string ln in CollatzMath.GetBinaryBigEndianDecaysInTwo().Take(256))
    //    {
    //        decayIn2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(ln);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        sb.AppendLine(ln + ',' + decayIn1.ToString() + ',' + decayIn2.ToString() + ',' + CollatzMath.OddStepCountToSmaller(decayIn2).ToString());
    //    }
    //    string csv = sb.ToString();
    //    Assert.True(true);
    //}
    //[Fact]
    //public void xxxTestDecayInTwoViaBinaryBigendianText()
    //{
    //    string mod1SeedTxt = "1";
    //    string mod2SeedTxt = "11";
    //    string echoPrefix = "10";
    //    string seedSuffix = "000111";
    //    StringBuilder sbSeed;
    //    StringBuilder sbEcho = new();
    //    BigInteger seed, decayOnce;
    //    //for (int b = 0; b < 2; b++)
    //    //{
    //    //    sbSeed = new StringBuilder();
    //    //    for (int c = 0; c <= b; c++)
    //    //        sbSeed.Append('1');
    //    //    for (int i = 0; i < 100; i++)
    //    //    {
    //    //        seedTxt = sbSeed.ToString();
    //    //        seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(seedTxt);
    //    //        decayOnce = CollatzMath.NextOdd(seed);
    //    //        Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //    //        sbEcho = new StringBuilder(sbEcho.ToString());
    //    //        for (int j = 0; j < 100; j++)
    //    //        {
    //    //            sbSeed.Insert(0, echoPrefix);
    //    //            seedTxt = sbSeed.ToString();
    //    //            seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(seedTxt);
    //    //            decayOnce = CollatzMath.NextOdd(seed);
    //    //            Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //    //        }
    //    //        sbSeed.Append(seedSuffix);
    //    //    }
    //    //}
    //    sbSeed = new StringBuilder(mod1SeedTxt);
    //    for (int i = 0; i < 100; i++)
    //    {
    //        seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(sbSeed.ToString());
    //        decayOnce = CollatzMath.NextOdd(seed);
    //        Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //        sbEcho = new StringBuilder(sbEcho.ToString());
    //        for (int j = 0; j < 100; j++)
    //        {
    //            sbEcho.Insert(0, echoPrefix);
    //            decayOnce = CollatzMath.NextOdd(seed);
    //            Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //        }
    //        sbSeed.Append(seedSuffix);
    //    }
    //    sbSeed = new StringBuilder(mod2SeedTxt);
    //    for (int i = 0; i < 100; i++)
    //    {
    //        seed = CollatzMath.toBigIntegerFromBinaryBigEndianString(sbSeed.ToString());
    //        decayOnce = CollatzMath.NextOdd(seed);
    //        Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //        sbEcho = new StringBuilder(sbEcho.ToString());
    //        for (int j = 0; j < 100; j++)
    //        {
    //            sbEcho.Insert(0, echoPrefix);
    //            decayOnce = CollatzMath.NextOdd(seed);
    //            Assert.True(CollatzMath.NextOdd(decayOnce) == 1);
    //        }
    //        sbSeed.Append(seedSuffix);
    //    }
    //}
    //[Fact]
    //public void TestDecayInThreeViaBinaryBigendianText()
    //{
    //    int trials = 100;
    //    string echoPrefix = "10", targetDecayInThreeBigEnd;
    //    StringBuilder BigEndAnchor, BigEndEcho = new();
    //    BigInteger decayIn1, decayIn2, decayIn3, targetDecayIn1;
    //    int trialParity;
    //    #region One by each
    //    // 5 anchor
    //    targetDecayIn1 = 5;
    //    targetDecayInThreeBigEnd = "10001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 17);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001") == 35);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110001") == 1137);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110001") == 2275);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110001110001") == 72817);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110001110001") == 145635);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110001110001110001") == 4660337);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110001110001110001") == 9320675);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110001110001110001110001") == 298261617);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110001110001110001110001") == 596523235);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110001110001110001110001110001") == 19088743537);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110001110001110001110001110001") == 38177487075);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }
    //    // 5 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010001") == 69);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010001") == 277);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010001") == 1109);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101010001") == 4437);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10110001") == 141);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010110001") == 565);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010110001") == 2261);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010110001") == 9045);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010001110001") == 4549);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010001110001") == 18197);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010001110001") == 72789);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101010001110001") == 291157);

    //    // 85 anchor
    //    targetDecayIn1 = 85;
    //    targetDecayInThreeBigEnd = "1101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 75);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("100011101001") == 2417);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1100011101001") == 4835);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }

    //    // 85 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101101001") == 301);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101101001") == 1205);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101101001") == 4821);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010110001") == 19285);
    //    Assert.True(CollatzMath.toBinaryLittleEndianString(191).Equals("10111111"));
    //    Assert.True(CollatzMath.toBinaryBigEndianString(191).Equals("11111010"));

    //    // 341 anchor
    //    targetDecayIn1 = 341;
    //    targetDecayInThreeBigEnd = "11101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 151);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111101001") == 4849);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111101001") == 9699);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111101001") == 310385);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111101001") == 620771);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }

    //    // 341 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1011101001") == 605);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101011101001") == 2421);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101011101001") == 9685);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101011101001") == 38741);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101000111101001") == 19397);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101000111101001") == 77589);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101000111101001") == 310357);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010101000111101001") == 1241429);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1011000111101001") == 38797);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101100011101001") == 155189);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101011000111101001") == 620757);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101011000111101001") == 2483029);

    //    // 5461 anchor
    //    targetDecayIn1 = 5461;
    //    targetDecayInThreeBigEnd = "1000110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 38833);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000110111101001") == 77667);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000110111101001") == 2485361);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111101001") == 497073);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }
    //    // 5461 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101000110111101001") == 155333);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101000110111101001") == 621365);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101000110111101001") == 2485333);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010101000110111101001") == 9941333);
    //    Assert.True(CollatzMath.toBinaryLittleEndianString(191).Equals("10111111"));
    //    Assert.True(CollatzMath.toBinaryBigEndianString(191).Equals("11111010"));

    //    // 21845 anchor
    //    targetDecayIn1 = 21845;
    //    targetDecayInThreeBigEnd = "100110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 19417);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1100110111101001") == 38835);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("100011100110111101001") == 1242737);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1100011100110111101001") == 2485475);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }
    //    // 21845 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10100110111101001") == 77669);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010100110111101001") == 310677);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010100110111101001") == 1242709);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010100110111101001") == 4970837);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101100110111101001") == 155341);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101100110111101001") == 621365);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101100110111101001") == 2485461);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010101100110111101001") == 9941845);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10100011100110111101001") == 4970949);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010100011100110111101001") == 19883797);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010100011100110111101001") == 79535189);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010100011100110111101001") == 318140757);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101100011100110111101001") == 9941901);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101100011100110111101001") == 39767605);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101100011100110111101001") == 159070421);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010101100011100110111101001") == 636281685);

    //    // 349525 anchor
    //    targetDecayIn1 = 349525;
    //    targetDecayInThreeBigEnd = "10000010110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 621377);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110000010110111101001") == 1242755);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10001110000010110111101001") == 39768177);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("110001110000010110111101001") == 79536355);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }
    //    // 349525 echo
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010000010110111101001") == 2485509);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010000010110111101001") == 9942037);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010000010110111101001") == 39768149);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101010000010110111101001") == 159072597);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10110000010110111101001") == 4971021);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010110000010110111101001") == 19884085);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010110000010110111101001") == 79536341);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010110000010110111101001") == 318145365);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010001110000010110111101001") == 159072709);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010001110000010110111101001") == 636290837);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010001110000010110111101001") == 2545163349);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010101010001110000010110111101001") == 10180653397);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10110001110000010110111101001") == 318145421);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1010110001110000010110111101001") == 1272581685);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("101010110001110000010110111101001") == 5090326741);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("10101010110001110000010110111101001") == 20361306965);

    //    // 1398101 anchor
    //    targetDecayIn1 = 1398101;
    //    targetDecayInThreeBigEnd = "10001000010110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 4971025);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }

    //    // 22369621 anchor
    //    targetDecayIn1 = 22369621;
    //    targetDecayInThreeBigEnd = "1101001000010110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 19884107);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }

    //    // 89478485 anchor
    //    targetDecayIn1 = 89478485;
    //    targetDecayInThreeBigEnd = "11101001000010110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 39768215);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111101001000010110111101001") == 1272582897);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000111101001000010110111101001") == 2545165795);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("100011000111101001000010110111101001") == 40722652721);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        string BigEndTxt = BigEndAnchor.ToString();
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        string LittleEndTxt = CollatzMath.toBinaryLittleEndianString(decayIn3);
    //        Assert.True(CollatzMath.toBigIntegerFromBinaryLittleEndianString(LittleEndTxt) == decayIn3);
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }

    //    // 1431655765 anchor
    //    targetDecayIn1 = 1431655765;
    //    targetDecayInThreeBigEnd = "1000110111101001000010110111101001";
    //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 10180663217);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("11000110111101001000010110111101001") == 20361326435);
    //    Assert.True(CollatzMath.toBigIntegerFromBinaryBigEndianString("1000111000110111101001000010110111101001") == 651562445937);
    //    BigEndAnchor = new(targetDecayInThreeBigEnd);
    //    for (int i = 0; i < trials; i++)
    //    {
    //        string BigEndTxt = BigEndAnchor.ToString();
    //        decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
    //        string LittleEndTxt = CollatzMath.toBinaryLittleEndianString(decayIn3);
    //        Assert.True(CollatzMath.toBigIntegerFromBinaryLittleEndianString(LittleEndTxt) == decayIn3);
    //        decayIn2 = CollatzMath.NextOdd(decayIn3);
    //        decayIn1 = CollatzMath.NextOdd(decayIn2);
    //        Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //        Assert.True(decayIn1 == targetDecayIn1);
    //        BigEndEcho.Clear().Append(BigEndAnchor);
    //        for (int j = 0; j < trials; j++)
    //        {
    //            BigEndEcho.Insert(0, echoPrefix);
    //            decayIn3 = CollatzMath.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
    //            decayIn2 = CollatzMath.NextOdd(decayIn3);
    //            decayIn1 = CollatzMath.NextOdd(decayIn2);
    //            Assert.True(CollatzMath.NextOdd(decayIn1) == 1);
    //            Assert.True(decayIn1 == targetDecayIn1);
    //        }
    //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
    //        BigEndAnchor.Insert(1, insertTxt);
    //    }
    //    StringBuilder sb = new();
    //    sb.AppendLine("DecayIn3,DecayIn2,DecayIn1,Binary");
    //    foreach ((BigInteger decayIn3, BigInteger decayIn2, BigInteger decayIn1) in CollatzMath.GetBinaryBigEndianDecaysInThree().Take(1024))
    //    {
    //        sb.AppendLine(decayIn3 + "," + decayIn2 + "," + decayIn1 + ',' + CollatzMath.toBinaryBigEndianString(decayIn3));
    //    }
    //    string txt = sb.ToString();
    //    Assert.True(true);
    //}
    [Fact]
    public void TestDecayAsExpected()
    {
        HashSet<BigInteger> ToOneInOneSet = new HashSet<BigInteger>();
        HashSet<BigInteger> ToOneInTwoSet = new HashSet<BigInteger>();
        ToOneInOneSet.Add(fnDecayToOneInOne(0));
        // The closed-form sets only have to cover the range this test scans. Building a fixed
        // 1000 terms of each cost 3.8 of this test's 4 seconds, nearly all of it on values
        // orders of magnitude above GatedOddScanLimit - and each generator verifies its own
        // closed form with a full Collatz descent, so a term thousands of bits wide is not
        // cheap. All three generators are strictly increasing, so stopping one term past the
        // limit covers everything the scan can reach.
        for (int n = 1; ; n++)
        {
            BigInteger inOne = fnDecayToOneInOne(n);
            BigInteger inTwoMod2 = fnDecayToOneInTwo_Mod2(n);
            BigInteger inTwoMod1 = fnDecayToOneInTwo_Mod1(n);
            ToOneInOneSet.Add(inOne);
            ToOneInTwoSet.Add(inTwoMod2);
            ToOneInTwoSet.Add(inTwoMod1);
            if (inOne > GatedOddScanLimit && inTwoMod2 > GatedOddScanLimit && inTwoMod1 > GatedOddScanLimit)
                break;
        }
        ulong oddDecaySteps = 0;
        ulong trials = GatedOddScanLimit;
        for (ulong odd = 1; odd < trials; odd += 2)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            switch (oddDecaySteps)
            {
                case 1:
                    // This branch used to assign the membership result and throw it away, so
                    // the decay-in-one half of this test verified nothing at all -
                    // halheinrich/Math#4's family, inside a test that does assert elsewhere.
                    Assert.True(ToOneInOneSet.Contains(odd),
                        $"{odd} decays to one in a single odd step but is not in the closed-form decay-in-one set");
                    break;
                case 2:
                    ulong decaysTo = odd;
                    while (!ToOneInTwoSet.Contains(decaysTo))
                    {
                        if ((decaysTo - 1) % 4 != 0)
                            Assert.True(false);
                        decaysTo = (decaysTo - 1) / 4;
                        Assert.True(CollatzMath.OddStepCountToOne(decaysTo) == oddDecaySteps);
                    }
                    break;
                default:
                    break;
            }
        }
    }
    [Fact]
    public void TestDecayViaFunctionIn1()
    {
        CollatzDecayFormulaRecursive collatzDecayFormulaRecursive = new(1, 2, 1);
        CollatzDecayFormula collatzDecayFormula = new(1, 2, 2, 1, 1);
        CollatzDecayFormulaBitManipulation collatzDecayFormulaBitManipulation = new(1);
        List<ICollatzDecayFormula> collatzFormulaList = new();
        collatzFormulaList.Add(collatzDecayFormulaRecursive);
        collatzFormulaList.Add(collatzDecayFormula);
        collatzFormulaList.Add(collatzDecayFormulaBitManipulation);
        int trials = GatedOddScanLimit;
        ulong oddDecaySteps = 0;
        // Test decay in 1
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            if (oddDecaySteps == 1)
            {
                Assert.True(CollatzMath.DecayInNFormulaList(odd, collatzDecayFormulaRecursive) == 1);
                Assert.True(collatzDecayFormulaRecursive.IsMember(odd));
                Assert.True(collatzDecayFormula.IsMember(odd));
                Assert.True(collatzDecayFormulaBitManipulation.IsMember(odd));
            }
            else
            {
                Assert.False(CollatzMath.DecayInNFormulaList(odd, collatzDecayFormulaRecursive) == 1);
                Assert.False(collatzDecayFormula.IsMember(odd));
                if (collatzDecayFormulaBitManipulation.IsMember(odd))
                    Assert.False(collatzDecayFormulaBitManipulation.IsMember(odd));
            }
            int isMemberCt = 0;
            foreach (ICollatzDecayFormula collatzFormula in collatzFormulaList)
            {
                if (collatzFormula.IsMember(odd))
                    ++isMemberCt;
            }
            if (oddDecaySteps == 1)
                Assert.True(isMemberCt == collatzFormulaList.Count);
            else
                Assert.True(isMemberCt == 0);
        }
    }
    [Fact]
    public void TestDecayViaFunctionIn2()
    {
        //CollatzDecayFormulaRecursive collatzDecayFormulaRecursive = new(1, 2, 1);
        //CollatzDecayFormula collatzDecayFormula = new(1, 2, 2, 1, 1);
        //CollatzDecayFormulaBitManipulation collatzDecayFormulaBitManipulation = new(1);
        List<ICollatzDecayFormula> collatzFormulaList = new();
        //collatzFormulaList.Add(collatzDecayFormulaRecursive);
        //collatzFormulaList.Add(collatzDecayFormula);
        //collatzFormulaList.Add(collatzDecayFormulaBitManipulation);
        int trials = GatedOddScanLimit;
        ulong oddDecaySteps = 0;
        int isMemberCt;
        // Test decay in 2
        List<CollatzDecayFormulaRecursive> collatzDecayFormulaRecursiveList = new();
        //collatzDecayFormulaRecursiveList.Add(new CollatzDecayFormulaRecursive(1, 2, 1));
        collatzDecayFormulaRecursiveList.Add(new CollatzDecayFormulaRecursive(2, 6, 35));
        collatzDecayFormulaRecursiveList.Add(new CollatzDecayFormulaRecursive(2, 6, 49));
        List<CollatzDecayFormula> collatzDecayFormulaList = new();
        //collatzDecayFormulaList.Add(new CollatzDecayFormula(1, 2, 2, 1, 1));
        collatzDecayFormulaList.Add(new CollatzDecayFormula(2, 6, -1, 5, 2));
        collatzDecayFormulaList.Add(new CollatzDecayFormula(2, 6, 4, 7, 2));
        List<CollatzDecayFormulaBitManipulation> CollatzDecayFormulaBitManipulationList = new();
        CollatzDecayFormulaBitManipulationList.Add(new CollatzDecayFormulaBitManipulation(2));
        collatzFormulaList.Add(collatzDecayFormulaRecursiveList[0]);
        collatzFormulaList.Add(collatzDecayFormulaRecursiveList[1]);
        collatzFormulaList.Add(collatzDecayFormulaList[0]);
        collatzFormulaList.Add(collatzDecayFormulaList[1]);
        collatzFormulaList.Add(CollatzDecayFormulaBitManipulationList[0]);
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMemberCt = 0;
            foreach (CollatzDecayFormulaRecursive cdfr in collatzDecayFormulaRecursiveList)
            {
                if (cdfr.IsMember(odd))
                {
                    Assert.True(cdfr.StepsToOne == oddDecaySteps);
                    ++isMemberCt;
                }
            }
            Assert.True((isMemberCt == 1) == (oddDecaySteps == 2));
            isMemberCt = 0;
            foreach (CollatzDecayFormula cdf in collatzDecayFormulaList)
            {
                if (cdf.IsMember(odd))
                {
                    if (cdf.StepsToOne != oddDecaySteps)
                        Assert.True(false);
                    Assert.True(cdf.StepsToOne == oddDecaySteps);
                    ++isMemberCt;
                }
                //else
                //    Assert.False(cdf.StepsToOne == oddDecaySteps);
            }
            isMemberCt = 0;
            foreach (CollatzDecayFormulaBitManipulation cdbm in CollatzDecayFormulaBitManipulationList)
            {
                if (cdbm.IsMember(odd))
                {
                    Assert.True(cdbm.StepsToOne == oddDecaySteps);
                    ++isMemberCt;
                }
                //else
                //    Assert.False(cdbm.StepsToOne == oddDecaySteps);
            }
            Assert.True((isMemberCt == 1) == (oddDecaySteps == 2));
        }
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMemberCt = 0;
            foreach (ICollatzDecayFormula collatzFormula in collatzFormulaList)
            {
                if (collatzFormula.IsMember(odd))
                    ++isMemberCt;
            }
            if (oddDecaySteps == 2)
            {
                if (isMemberCt != 3)
                    Assert.True(isMemberCt == 3);
            }
            else
                Assert.True(isMemberCt == 0);
        }
    }
    [Fact]
    public void TestDecayViaFunctionIn3()
    {
        CollatzDecayFormulaBitManipulation collatzDecayFormulaBitManipulationIn3 = new(3);
        int trials = GatedOddScanLimit;
        ulong oddDecaySteps = 0;
        bool isMember;
        for (int odd = 1; odd < trials; odd += 2)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMember = collatzDecayFormulaBitManipulationIn3.IsMember(odd);
            Assert.True((oddDecaySteps == 3) == isMember);
        }
    }
    [Fact]
    public void Test4nPlus1()
    {
        int trials = GatedOddScanLimit;
        ulong oddDecaySteps = 0, fourNplusOneSteps = 0;
        for (int odd = 1; odd < trials; odd += 2)
        {
            BigInteger c = odd;
            while ((c - 1) % 4 == 0 && c > 4)
            {
                {
                    --c;
                    c >>= 2;
                    if (c.IsEven)
                        break;
                }
            }
            if (c == odd || c.IsEven)
                continue;
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            fourNplusOneSteps = CollatzMath.OddStepCountToOne(c);
            Assert.True(oddDecaySteps == fourNplusOneSteps);
        }
    }
    [Fact]
    public void TestFunctionConstruction()
    {
        int trials = 1000;
        CollatzDecayFormulaRecursive collatzDecayFormulaRecursive = new(1, 2, 1);
        List<ICollatzDecayFormula> collatzFormulaInOneStepList = new();
        collatzFormulaInOneStepList.Add(collatzDecayFormulaRecursive);
        int isMemberCt = 0;
        ulong oddDecaySteps = 0;
        // Test decay in 1 step
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMemberCt = 0;
            foreach (ICollatzDecayFormula collatzFormula in collatzFormulaInOneStepList)
            {
                if (collatzFormula.IsMember(odd))
                    ++isMemberCt;
            }
            if (oddDecaySteps == 1)
                Assert.True(isMemberCt == 1);
            else
                Assert.True(isMemberCt == 0);
        }
        // Test decay in 2 steps
        List<ICollatzDecayFormula> collatzFormulaInTwoStepsList = new();
        collatzFormulaInTwoStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInOneStepList[0], 2));
        collatzFormulaInTwoStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInOneStepList[0], 1));
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMemberCt = 0;
            foreach (ICollatzDecayFormula collatzFormula in collatzFormulaInTwoStepsList)
            {
                if (collatzFormula.IsMember(odd))
                    ++isMemberCt;
            }
            if (oddDecaySteps == 2)
                Assert.True(isMemberCt == 1);
            else
                Assert.True(isMemberCt == 0);
        }
        // Test decay in 3 steps
        List<ICollatzDecayFormula> collatzFormulaInThreeStepsList = new();
        collatzFormulaInThreeStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInTwoStepsList[0], 2));
        collatzFormulaInThreeStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInTwoStepsList[0], 1));
        collatzFormulaInThreeStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInTwoStepsList[1], 2));
        collatzFormulaInThreeStepsList.Add(new CollatzDecayFormulaRecursive(collatzFormulaInTwoStepsList[1], 1));
        for (int odd = 1; odd < trials; odd++)
        {
            oddDecaySteps = CollatzMath.OddStepCountToOne(odd);
            isMemberCt = 0;
            foreach (ICollatzDecayFormula collatzFormula in collatzFormulaInThreeStepsList)
            {
                if (collatzFormula.IsMember(odd))
                    ++isMemberCt;
            }
            if (oddDecaySteps == 3)
            {
                Assert.True(isMemberCt == 1);
            }
            else
                Assert.True(isMemberCt == 0);
        }
    }
    [Fact]
    public void TestDecayInTwoSuccessorDecaysInOne()
    {
        // Was ExploreDecayInTwo, which asserted this over 100,000,000 values and then built
        // three CSVs it discarded. The claim is the assertion; the tables were the experiment.
        // Bounded gate here; DecayInTwoSweep in Collatz.Experiments runs the full range and
        // emits the tables.
        for (int i = 1; i < GatedOddScanLimit; i++)
        {
            if (CollatzMath.OddStepCountToOne(i) != 2)
                continue;
            if ((i & 1) == 0) // even
                continue;
            BigInteger decayIn1 = CollatzMath.NextOdd(i);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
        }
    }
    [Fact]
    public void TestDecayInThreeSuccessorsDecayInTwoThenOne()
    {
        // Was ExploreDecayInThree; see the note on the decay-in-two case above. The full
        // sweep and its tables are DecayInThreeSweep in Collatz.Experiments.
        for (int i = 1; i < GatedOddScanLimit; i++)
        {
            if (CollatzMath.OddStepCountToOne(i) != 3)
                continue;
            if ((i & 1) == 0) // even
                continue;
            BigInteger decayIn2 = CollatzMath.NextOdd(i);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            BigInteger decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
        }
    }
    [Fact]
    public void ExploreSpecificDecay()
    {
        int sampleCt = 50;
        List<string> sampleList = new();
        StringBuilder sb1 = new("1000110111101001");
        int idx1 = sb1.Length;
        BigInteger odd1 = CollatzMath.toBigIntegerFromBinaryBigEndianString(sb1.ToString());
        StringBuilder sb2 = new("1000111000110111101001");
        int idx2 = sb2.Length;
        BigInteger odd2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(sb2.ToString());

        BigInteger decayIn3, decayIn2, decayIn1;
        while (sampleList.Count < sampleCt)
        {
            if (odd1 < odd2)
            {
                decayIn3 = odd1;
                if (sb1.Length == idx1)
                    sb1.Insert(0, "1");
                else
                    sb1.Insert(sb1.Length - idx1, sb1[sb1.Length - idx1 - 1] == '0' ? '1' : '0');
                odd1 = CollatzMath.toBigIntegerFromBinaryBigEndianString(sb1.ToString());
            }
            else
            {
                decayIn3 = odd2;
                if (sb2.Length == idx2)
                    sb2.Insert(0, "1");
                else
                    sb2.Insert(sb2.Length - idx2, sb2[sb2.Length - idx2 - 1] == '0' ? '1' : '0');
                odd2 = CollatzMath.toBigIntegerFromBinaryBigEndianString(sb2.ToString());
            }
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            BigInteger c = decayIn3;
            while ((c & 3) == 1) // Equivalent to c % 4 == 1
                c = (c - 1) >> 2; // Equivalent to (c - 1) / 4
            if (c.IsEven)
                c = (c << 2) + 1;
            sampleList.Add(new string(decayIn3.ToString(CultureInfo.InvariantCulture) + ',' + decayIn2.ToString(CultureInfo.InvariantCulture) + ',' + decayIn1.ToString(CultureInfo.InvariantCulture)
                + ',' + CollatzMath.toBinaryBigEndianString(decayIn3)));
        }
    }
    [Fact]
    public void ExploreTo5461()
    {
        //        38,833
        //        77,667
        //     2,485,361
        //     4,970,723
        //   159,063,153
        //   318,126,307
        // 5,090,020,913
        BigInteger decayIn3 = 38833, decayIn2, decayIn1;
        bool is32_17 = false;
        int ct = 0;
        while (true)
        {
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            Assert.True(decayIn1 == 5461);
            if (++ct > 10)
                break;
            if (is32_17)
                decayIn3 = decayIn3 * 32 + 17;
            else
                decayIn3 = decayIn3 * 2 + 1;
            is32_17 = !is32_17;
        }
    }
    [Fact]
    public void ExploreTo349525()
    {
        //   621377
        //  1242755
        // 39768177
        // 79536355

        BigInteger decayIn3 = 621377, decayIn2, decayIn1;
        bool is32_17 = false;
        int ct = 0;
        while (true)
        {
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            Assert.True(decayIn1 == 349525);
            if (++ct > 10)
                break;
            if (is32_17)
                decayIn3 = decayIn3 * 32 + 17;
            else
                decayIn3 = decayIn3 * 2 + 1;
            is32_17 = !is32_17;
        }
    }
    [Fact]
    public void ExploreTo1398101()
    {
        // 4971025
        // 9942051

        BigInteger decayIn3 = 4971025, decayIn2, decayIn1;
        bool is32_17 = false;
        int ct = 0;
        while (true)
        {
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            Assert.True(decayIn1 == 1398101);
            if (++ct > 10)
                break;
            if (is32_17)
                decayIn3 = decayIn3 * 32 + 17;
            else
                decayIn3 = decayIn3 * 2 + 1;
            is32_17 = !is32_17;
        }
    }
    [Fact]
    public void ExploreTo22369621()
    {
        // 19884107

        BigInteger decayIn3 = 19884107, decayIn2, decayIn1;
        bool is32_17 = true;
        int ct = 0;
        while (true)
        {
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            Assert.True(decayIn1 == 22369621);
            if (++ct > 10)
                break;
            if (is32_17)
                decayIn3 = decayIn3 * 32 + 17;
            else
                decayIn3 = decayIn3 * 2 + 1;
            is32_17 = !is32_17;
        }
    }
    [Fact]
    public void ExploreTo89478485()
    {
        // 39768215

        BigInteger decayIn3 = 39768215, decayIn2, decayIn1;
        bool is32_17 = true;
        int ct = 0;
        while (true)
        {
            Assert.True(CollatzMath.OddStepCountToOne(decayIn3) == 3);
            decayIn2 = CollatzMath.NextOdd(decayIn3);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn2) == 2);
            decayIn1 = CollatzMath.NextOdd(decayIn2);
            Assert.True(CollatzMath.OddStepCountToOne(decayIn1) == 1);
            Assert.True(decayIn1 == 89478485);
            if (++ct > 10)
                break;
            if (is32_17)
                decayIn3 = decayIn3 * 32 + 17;
            else
                decayIn3 = decayIn3 * 2 + 1;
            is32_17 = !is32_17;
        }
    }
    #endregion Fact Methods
    #region Helper Methods
    private static void AssertDecayIn1(string _AnchorBigEnd)
    {
        BigInteger currOdd = CollatzMath.toBigIntegerFromBinaryBigEndianString(_AnchorBigEnd.ToString());
        Assert.True(CollatzMath.NextOdd(currOdd) == 1);
    }
    private static BigInteger fnDecayToOneInOne(int _N)
    {
        // f(n) = (2^(2n+2) - 1) / 3
        // f(n) = 2^2 * f(n-1) + 1
        Assert.True(_N >= 0);
        BigInteger pow2 = (BigInteger)2 << 2 * _N + 1;
        Assert.True(pow2 == BigInteger.Pow(2, 2 * _N + 2));
        Assert.True(pow2 == BigInteger.Pow(4, _N + 1));
        BigInteger decayInOne = pow2 - 1;
        Assert.True(decayInOne % 3 == 0);
        decayInOne /= 3;
        Assert.True(CollatzMath.NextOdd(decayInOne) == 1);
        Assert.True(decayInOne == (BigInteger.Pow(2, 2 * _N + 2) - 1) / 3);
        return decayInOne;
    }
    private static BigInteger fnDecayToOneInTwo_Mod2(int _N)
    {
        // f(n) = [(2^(6n-1) - 5) / 9
        // f(n) = 2^6 * f(n-1) + 35
        Assert.True(_N > 0);
        BigInteger pow2 = BigInteger.Pow(2, 6 * _N - 1);
        BigInteger decayInTwo = pow2 - 2;
        Assert.True(decayInTwo % 3 == 0);
        decayInTwo /= 3;
        --decayInTwo;
        Assert.True(decayInTwo % 3 == 0);
        decayInTwo /= 3;
        BigInteger decayInOne = CollatzMath.NextOdd(decayInTwo);
        Assert.True(CollatzMath.NextOdd(decayInOne) == 1);
        Assert.True(decayInTwo == (BigInteger.Pow(2, 6 * _N - 1) - 5) / 9);
        return decayInTwo;
    }
    private static BigInteger fnDecayToOneInTwo_Mod1(int _N)
    {
        // f(n) = [2^(6n+4) - 7] / 9
        // f(n) = 2^6 * f(n-1) + 49
        Assert.True(_N > 0);
        BigInteger pow2 = BigInteger.Pow(2, 6 * _N + 3);
        BigInteger decayInTwo = 2 * (pow2 - 2);
        Assert.True(decayInTwo % 3 == 0);
        decayInTwo /= 3;
        --decayInTwo;
        Assert.True(decayInTwo % 3 == 0);
        decayInTwo /= 3;
        BigInteger decayInOne = CollatzMath.NextOdd(decayInTwo);
        Assert.True(CollatzMath.NextOdd(decayInOne) == 1);
        Assert.True(decayInTwo == (BigInteger.Pow(2, 6 * _N + 4) - 7) / 9);
        return decayInTwo;
    }
    #endregion Helper Methods
}
