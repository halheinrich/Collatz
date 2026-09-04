using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace HalHeinrich.Numerics.Collatz.Experiments;

/// <summary>
/// The Collatz bench's experiments: runs whose answers are not known in advance.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately NOT a test project, and nothing here asserts. The umbrella's
/// contract separates the two: a target with a known answer belongs in a test project and
/// gates CI, while a target with an unknown answer belongs in a runnable project and its
/// output is data. Every method below produced a table and then threw it away behind an
/// <c>Assert.True(true)</c>, which reported coverage it did not have and cost the gating
/// suite minutes per run. They emit that table now instead.
/// </para>
/// <para>
/// If you find yourself wanting to add an assertion here, the method has stopped being an
/// experiment and belongs in Collatz.Tests. Move it rather than making this project
/// half-and-half.
/// </para>
/// <para>
/// Data goes to stdout so a run can be redirected to a file; labels and progress go to
/// stderr, so the redirected file is the table alone.
/// </para>
/// </remarks>
internal static class DecayExperiments
{
    /// <summary>Writes one labelled table: the label to stderr, the data to stdout.</summary>
    private static void Emit(string label, string csv)
    {
        Console.Error.WriteLine($"--- {label} ({csv.Length:N0} chars) ---");
        Console.Out.Write(csv);
    }

    internal static void Pow2OddDecayCount()
    {
        int trials = (int)1.0E+07;
        BigInteger pow2 = 1, diff;
        StringBuilder sb = new();
        sb.AppendLine("Pow2,Offset,Cand,DecayCount,StepsToSmaller");
        for (BigInteger i = 3; i < trials; i += 2)
        {
            while (true)
            {
                diff = i - pow2;
                if (diff < pow2)
                    break;
                pow2 <<= 1;
            }
            UInt64 stepsToSmaller = CollatzMath.OddStepCountToSmaller(i);
            sb.AppendLine(pow2.ToString(CultureInfo.InvariantCulture) + ',' + (i - pow2).ToString(CultureInfo.InvariantCulture) + ',' + i.ToString(CultureInfo.InvariantCulture) + ',' + CollatzMath.OddStepCountToOne(i).ToString(CultureInfo.InvariantCulture) + ',' + stepsToSmaller.ToString(CultureInfo.InvariantCulture));
        }
        Emit("pow2-odd-decay-count", sb.ToString());
        List<List<int>> oddStepCountToSmallerList = new();
        for (BigInteger i = 3; i < trials; i += 2)
        {
            int stepsToSmaller = (int)CollatzMath.OddStepCountToSmaller(i);
            if (stepsToSmaller >= oddStepCountToSmallerList.Count)
            {
                int ct = oddStepCountToSmallerList.Count;
                for (int j = ct; j <= stepsToSmaller; j++)
                    oddStepCountToSmallerList.Add(new List<int>());
            }
            oddStepCountToSmallerList[stepsToSmaller].Add((int)i);
        }
        sb.Clear();
        sb.AppendLine("StepsToSmaller,Cycle Length,Cycle Sum,First Example");
        int valDiff = -1, prevValDiff = -1;
        for (int stepCt = 0; stepCt < oddStepCountToSmallerList.Count; stepCt++)
        {
            int cycleLength = -1;
            for (int j = oddStepCountToSmallerList[stepCt].Count - 1; j > 0; j--)
            {
                int stepDiff = oddStepCountToSmallerList[stepCt][j] - oddStepCountToSmallerList[stepCt][j - 1], prevStepDiff = -1;
                for (int j2 = j - 1; j2 > 0; j2--)
                {
                    prevStepDiff = oddStepCountToSmallerList[stepCt][j2] - oddStepCountToSmallerList[stepCt][j2 - 1];
                    if (prevStepDiff == stepDiff)
                    {
                        int cycleLengthCand = j - j2;
                        for (int v = oddStepCountToSmallerList[stepCt].Count - 1; v > cycleLengthCand; v--)
                        {
                            valDiff = oddStepCountToSmallerList[stepCt][v] - oddStepCountToSmallerList[stepCt][v - 1];
                            prevValDiff = oddStepCountToSmallerList[stepCt][v - cycleLengthCand] - oddStepCountToSmallerList[stepCt][v - cycleLengthCand - 1];
                            if (valDiff != prevValDiff)
                            {
                                cycleLengthCand = -1;
                                break;
                            }
                        }
                        if (cycleLengthCand > 0)
                        {
                            cycleLength = cycleLengthCand;
                            break;
                        }
                    }
                }
                if (prevStepDiff == stepDiff)
                    break;
            }
            if (cycleLength > 0)
            {
                for (int j = oddStepCountToSmallerList[stepCt].Count - 1; j > cycleLength; j--)
                {
                    valDiff = oddStepCountToSmallerList[stepCt][j] - oddStepCountToSmallerList[stepCt][j - 1];
                    prevValDiff = oddStepCountToSmallerList[stepCt][j - cycleLength] - oddStepCountToSmallerList[stepCt][j - cycleLength - 1];
                    if (valDiff != prevValDiff)
                        Debug.Assert(valDiff == prevValDiff);
                }
                sb.Append(stepCt.ToString(CultureInfo.InvariantCulture) + ',' + cycleLength.ToString(CultureInfo.InvariantCulture) + ',' + (oddStepCountToSmallerList[stepCt][cycleLength] - oddStepCountToSmallerList[stepCt][0]).ToString(CultureInfo.InvariantCulture) + ',' + oddStepCountToSmallerList[stepCt][0].ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < cycleLength; i++)
                {
                    sb.Append(',' + (oddStepCountToSmallerList[stepCt][i + 1] - oddStepCountToSmallerList[stepCt][i]).ToString(CultureInfo.InvariantCulture));
                }
                sb.AppendLine();
            }
        }
        Emit("steps-to-smaller-cycles", sb.ToString());
    }

    internal static void TwoToTheNPlusOne()
    {
        StringBuilder sb = new("11");
        StringBuilder csvSb = new();
        BigInteger c;
        int multStepCt;
        while (sb.Length < 128)
        {
            c = CollatzMath.ToBigIntegerFromBinaryLittleEndianString(sb.ToString());
            multStepCt = 0;
            while (true)
            {
                csvSb.AppendLine(c.ToString(CultureInfo.InvariantCulture) + ',' + CollatzMath.ToBinaryLittleEndianString(c));
                c = CollatzMath.NextOdd(c);
                ++multStepCt;
                if (c == 1)
                {
                    csvSb.Append(Environment.NewLine);
                    break;
                }
            }
            sb.Insert(1, '0');
        }
        Emit("two-to-the-n-plus-one", csvSb.ToString());
    }

    internal static void TwoToTheNPlusOneFormula()
    {
        // Represent 2^(n+i0) + 2^(n+i1) + 2^(n+i2) + ... + 1 
        List<Int64> expNoffsetList = new();
        StringBuilder sb = new();
        sb.AppendLine("0");
        StringBuilder sbNorm = new();
        sb.AppendLine("0");
        expNoffsetList.Add(0);
        int csvLnCt = 0;
        while (true)
        {
            for (int i = expNoffsetList.Count - 1; i >= 0; i--)
                expNoffsetList.Add(expNoffsetList[i] + 1);
            expNoffsetList.Sort();
            int j = 0;
            while (true)
            {
                if (expNoffsetList[j] == expNoffsetList[j + 1])
                {
                    ++expNoffsetList[j];
                    expNoffsetList.RemoveAt(j + 1);
                    expNoffsetList.Sort();
                    if (j + 1 < expNoffsetList.Count)
                        continue;
                    else
                        break;
                }
                else
                    if (++j >= expNoffsetList.Count - 1)
                        break;
            }
            for (int i = 0; i < expNoffsetList.Count; i++)
                expNoffsetList[i] -= 2;
            sb.Append(expNoffsetList.Last().ToString(CultureInfo.InvariantCulture));
            for (int i = expNoffsetList.Count - 2; i >= 0; i--)
                sb.Append(',' + expNoffsetList[i].ToString(CultureInfo.InvariantCulture));
            sb.Append(Environment.NewLine);
            Int64 norm = expNoffsetList.Last();
            sbNorm.Append((expNoffsetList.Last() - norm).ToString(CultureInfo.InvariantCulture));
            for (int i = expNoffsetList.Count - 2; i >= 0; i--)
                sbNorm.Append(',' + (expNoffsetList[i] - norm).ToString(CultureInfo.InvariantCulture));
            sbNorm.Append(Environment.NewLine);
            if (++csvLnCt > 256)
                break;
        }
        Emit("two-to-the-n-plus-one-formula", sb.ToString());
        Emit("two-to-the-n-plus-one-formula-normalised", sbNorm.ToString());
    }

    internal static void PowerOfTwoPlusConstantConst()
    {
        const uint seriesLength = 64;
        BigInteger pow2 = 1;
        List<(BigInteger PowerOfTwo, BigInteger Constant)> LessThanSeedList = new();
        List<(BigInteger PowerOfTwo, BigInteger Constant)> SurvivorList = new();
        BigInteger collatzConstant = 1;
        while (collatzConstant < 16384)
        {
            pow2 = 1;
            while (pow2 < collatzConstant)
                pow2 <<= 1;
            while (!SolvePowerOfTwoPlusConstant(pow2, collatzConstant, seriesLength, ref LessThanSeedList, ref SurvivorList))
                pow2 <<= 1;
            collatzConstant += 2;
        }
        StringBuilder sb = new();
        sb.AppendLine("PowerOfTwo,Constant,Bit Prefix");
        for (int n = 0; n < LessThanSeedList.Count; n++)
        {
            int bitLength = GetBitLength(LessThanSeedList[n].PowerOfTwo);
            string binaryString = CollatzMath.ToBinaryLittleEndianString(LessThanSeedList[n].Constant);
            string bitPrefix;
            if (bitLength > binaryString.Length)
                binaryString = binaryString.PadRight(bitLength, '0');
            bitPrefix = binaryString.Substring(0, bitLength - 1);
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{LessThanSeedList[n].PowerOfTwo.ToString("G", CultureInfo.InvariantCulture)},{LessThanSeedList[n].Constant.ToString("G", CultureInfo.InvariantCulture)},{bitPrefix}"));
        }
        Emit("power-of-two-plus-constant-less-than-seed", sb.ToString());
    }

    internal static void PowerOfTwoPlusConstantSurvivors()
    {
        const uint seriesLength = 64;
        BigInteger pow2 = 1, pow2Max = 16384;
        List<(BigInteger PowerOfTwo, BigInteger Constant)> LessThanSeedList = new();
        List<(BigInteger PowerOfTwo, BigInteger Constant)> SurvivorList = new();
        while (true)
        {
            pow2 <<= 1;
            BigInteger c = 1;
            while (c < pow2)
            {
                SolvePowerOfTwoPlusConstant(pow2, c, seriesLength, ref LessThanSeedList, ref SurvivorList);
                c += 2;
            }
            if (pow2 > pow2Max)
                break;
        }
        StringBuilder sb = new();
        sb.AppendLine("PowerOfTwo,Constant");
        for (int n = 0; n < LessThanSeedList.Count; n++)
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{LessThanSeedList[n].PowerOfTwo.ToString("G", CultureInfo.InvariantCulture)},{LessThanSeedList[n].Constant.ToString("G", CultureInfo.InvariantCulture)}"));
        Emit("power-of-two-plus-constant-less-than-seed", sb.ToString());
        sb.Clear();
        sb.AppendLine("PowerOfTwo,Constant,Bit Prefix");
        for (int n = 0; n < SurvivorList.Count; n++)
        {
            int bitLength = GetBitLength(SurvivorList[n].PowerOfTwo);
            string binaryString = CollatzMath.ToBinaryLittleEndianString(SurvivorList[n].Constant);
            string bitPrefix;
            if (bitLength > binaryString.Length)
                binaryString = binaryString.PadRight(bitLength, '0');
            bitPrefix = binaryString.Substring(0, bitLength - 1);
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{SurvivorList[n].PowerOfTwo.ToString("G", CultureInfo.InvariantCulture)},{SurvivorList[n].Constant.ToString("G", CultureInfo.InvariantCulture)},{bitPrefix}"));
        }
        Emit("power-of-two-plus-constant-survivors", sb.ToString());
    }

    internal static void DeriveDecayInNFormula()
    {
        int decayInTarget = 3;
        List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> decayInN_FirstDecayPairList = GetDecayInN_FirstDecayPair(decayInTarget, 300, 300000000);
        StringBuilder sb = new(), littleEndSb = new();
        sb.AppendLine("N,N % 3,FirstDecay,Power of 2,LittleEndian N,LittleEndian Core");
        foreach ((BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo) tuple in decayInN_FirstDecayPairList)
        {
            littleEndSb.Clear();
            littleEndSb.Append(CollatzMath.ToBinaryLittleEndianString(tuple.DecayInTarget));
            string littleEndTxt = littleEndSb.ToString();
            string littleEndCore = Strip_10_000111_LittleEnd(littleEndSb.ToString());
            sb.AppendLine(tuple.DecayInTarget.ToString(CultureInfo.InvariantCulture) + ',' + (tuple.DecayInTarget % 3).ToString(CultureInfo.InvariantCulture) + ',' +
                tuple.FirstDecay.ToString(CultureInfo.InvariantCulture) + ',' + tuple._PowTwo.ToString(CultureInfo.InvariantCulture) + ',' + littleEndTxt + ',' + littleEndCore);
        }
        Emit("decay-in-n-first-decay-pairs", sb.ToString());
        Emit("decay-in-n-formula-check", CheckFormula(decayInN_FirstDecayPairList));
    }

    private static bool SolvePowerOfTwoPlusConstant(BigInteger _PowerOfTwo, BigInteger _Constant, uint _SeriesLength, ref List<(BigInteger PowerOfTwo, BigInteger Constant)> LessThanSeedList, ref
    List<(BigInteger PowerOfTwo, BigInteger Constant)> SurvivorList
)
    {
        bool isLatestLessThanSeed = false;
        List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> SeedList = new();
        SeedList.Clear();
        for (int n = 0; n < _SeriesLength; n++)
        {
            SeedList.Add(new());
            BigInteger seed = n * _PowerOfTwo + _Constant;
            (BigInteger Seed, BigInteger M3a1, int Mod2Power) valueData = GetSuccessor(seed);
            SeedList[n].Add(valueData);
        }
        if (IsDuplicatePowerOfTwoPlusConstant(LessThanSeedList, _PowerOfTwo, _Constant, _SeriesLength))
        {
            isLatestLessThanSeed = true;
        }
        else
        {
            while (true)
            {
                GetMinMaxModTwoPower(SeedList, out int minModTwoPower, out int maxModTwoPower);
                if (minModTwoPower == 1 && maxModTwoPower != minModTwoPower)
                {
                    SurvivorList.Add((_PowerOfTwo, _Constant));
                    break;
                }
                int powTwoDivisor = 1 << minModTwoPower;
                int lastIndex = SeedList[0].Count - 1;
                for (int n = 0; n < _SeriesLength; n++)
                {
                    BigInteger seedSuccessor = 3 * SeedList[n][lastIndex].Seed + 1;
                    Debug.Assert(seedSuccessor % powTwoDivisor == 0);
                    seedSuccessor >>= minModTwoPower;
                    SeedList[n].Add(GetSuccessor(seedSuccessor));
                }
                if (IsLatestLessThanSeed(SeedList))
                {
                    LessThanSeedList.Add((_PowerOfTwo, _Constant));
                    isLatestLessThanSeed = true;
                    break;
                }
                if (!isAllOdd(SeedList))
                {
                    SurvivorList.Add((_PowerOfTwo, _Constant));
                    break;
                }
            }
        }
        return isLatestLessThanSeed;
    }

    private static bool isAllOdd(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList)
    {
        bool isAllOdd = true;
        int lastIndex = _ValueList[0].Count - 1;
        for (int n = 0; n < _ValueList.Count; n++)
        {
            if (_ValueList[n][lastIndex].Seed % 2 == 0)
                isAllOdd = false;
        }
        return isAllOdd;
    }

    private static bool IsDuplicatePowerOfTwoPlusConstant(List<(BigInteger PowerOfTwo, BigInteger Constant)> _LessThanSeedList, BigInteger _PowerOfTwo, BigInteger _Constant, uint _SeriesLength)
    {
        bool isDuplicate = false;
        if (_LessThanSeedList.Count < 1)
            return isDuplicate;
        for (int i = 0; i < _LessThanSeedList.Count; i++)
        {
            uint matchCtr = 0;
            for (int n = 0; n < _SeriesLength; n++)
            {
                BigInteger seed = n * _PowerOfTwo + _Constant;
                if ((seed - _LessThanSeedList[i].Constant) % _LessThanSeedList[i].PowerOfTwo == 0)
                    if (seed >= _LessThanSeedList[i].Constant)
                        ++matchCtr;
                if (matchCtr == _SeriesLength)
                {
                    Debug.Assert(matchCtr == 0 || matchCtr == _SeriesLength);
                    isDuplicate = true;
                    break;
                }
            }
        }
        return isDuplicate;
    }

    private static bool IsLatestLessThanSeed(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList)
    {
        int lastIndex = _ValueList[0].Count - 1;
        bool isAllLessThan = true;
        for (int n = 0; n < _ValueList.Count; n++)
        {
            Debug.Assert(_ValueList[n].Count == lastIndex + 1);
            if (_ValueList[n][0].Seed < _ValueList[n][lastIndex].Seed)
                isAllLessThan = false;
        }
        return isAllLessThan;
    }

    private static (BigInteger Seed, BigInteger M3a1, int Mod2Power) GetSuccessor(BigInteger _Value)
    {
        BigInteger m3a1 = 3 * _Value + 1;
        int modTwoPower = 0;
        while (m3a1 % 2 == 0)
        {
            m3a1 >>= 1;
            modTwoPower++;
        }
        m3a1 <<= modTwoPower;
        return new(_Value, m3a1, modTwoPower);
    }

    private static void GetMinMaxModTwoPower(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList, out int _MinModTwoPower, out int _MaxModTwoPower)
    {
        int lastIndex = _ValueList[0].Count - 1;
        _MinModTwoPower = _ValueList[0][lastIndex].Mod2Power;
        _MaxModTwoPower = _ValueList[0][lastIndex].Mod2Power;
        for (int n = 1; n < _ValueList.Count; n++)
        {
            if (_ValueList[n][lastIndex].Mod2Power > _MaxModTwoPower)
                _MaxModTwoPower = _ValueList[n][lastIndex].Mod2Power;
            if (_ValueList[n][lastIndex].Mod2Power < _MinModTwoPower)
                _MinModTwoPower = _ValueList[n][lastIndex].Mod2Power;
        }
    }

    private static int GetBitLength(BigInteger value)
    {
        int bitLength = 0;
        while (value > 0)
        {
            value >>= 1;
            bitLength++;
        }
        return bitLength;
    }

    private static string CheckFormula(List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> _DecayInN_TupleList)
    {
        BigInteger x, r, c, k;
        uint pow2;
        StringBuilder sb = new();
        sb.AppendLine("Decay in N,First Decay,Root,k,c");
        for (int i = 0; i < _DecayInN_TupleList.Count; i++)
        {
            (BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo) tuple = _DecayInN_TupleList[i];
            x = 3 * tuple.DecayInTarget + 1;
            pow2 = (uint)BigInteger.TrailingZeroCount(x);
            x >>= (int)pow2;
            Debug.Assert(x == tuple.FirstDecay);
            r = pow2 % 6;
            c = r == 0 ? 0u : 6u - r;
            k = (pow2 + c) / 6;
            sb.AppendLine(tuple.DecayInTarget.ToString(CultureInfo.InvariantCulture) + ',' + tuple.FirstDecay.ToString(CultureInfo.InvariantCulture) + ',' + x.ToString(CultureInfo.InvariantCulture) + ',' + k.ToString(CultureInfo.InvariantCulture) + ',' + c.ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static string Strip_10_000111_LittleEnd(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        while (s.EndsWith("000111", StringComparison.Ordinal))
            s = s.Substring(0, s.Length - 6);

        while (s.StartsWith("10", StringComparison.Ordinal))
            s = s.Substring(2);

        return s;
    }

    private static uint GetPowTwoForCollatzPair(BigInteger _N, BigInteger _OddDecay)
    {
        if (_N < 1) throw new ArgumentOutOfRangeException(nameof(_N), "Must be >= 1.");
        if (_OddDecay < 1) throw new ArgumentOutOfRangeException(nameof(_OddDecay), "Must be >= 1.");

        BigInteger num = (_N * 3) + 1;

        BigInteger rem = num % _OddDecay;
        if (!rem.IsZero)
            throw new InvalidOperationException($"Expected (3*{_N} + 1) to be divisible by {_OddDecay}. Remainder={rem}.");

        num /= _OddDecay;

        BigInteger tz = BigInteger.TrailingZeroCount(num);

        // pow2 can’t exceed bit-length of num; guard cast anyway
        if (tz > int.MaxValue)
            throw new OverflowException($"TrailingZeroCount too large for int: {tz}.");

        int pow2 = (int)tz;
        if (pow2 != 0)
            num >>= pow2;

        return (uint)pow2;
    }

    private static List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> GetDecayInN_FirstDecayPair(int _DayTarget, int _MaxListCount, BigInteger _MaxSeed)
    {
        int decayCt;
        List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> decayInN_List = [];
        BigInteger seed = -1, firstDecay, nextDecay;
        while (true)
        {
            seed += 2;
            firstDecay = CollatzMath.NextOdd(seed);
            nextDecay = firstDecay;
            decayCt = 1;
            while (true)
            {
                if (nextDecay == 1)
                {
                    if (decayCt == _DayTarget)
                    {
                        uint powTwo = (uint)GetPowTwoForCollatzPair(seed, firstDecay);
                        decayInN_List.Add((seed, firstDecay, powTwo));
                    }
                    break;
                }
                if (decayCt == _DayTarget)
                    break;
                nextDecay = CollatzMath.NextOdd(nextDecay);
                ++decayCt;
            }
            if (decayInN_List.Count >= _MaxListCount || seed > _MaxSeed)
                break;
        }
        return decayInN_List;
    }
}
