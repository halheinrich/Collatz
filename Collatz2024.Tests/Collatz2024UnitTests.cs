using HalHeinrich.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Collatz2024Namespace
{
    public class Collatz2024UnitTests
    {
        #region Fact Methods
        [Fact]
        public void TextSolveForLoop()
        {
            bool isCollatzLoop;
            BigRational n;

            // Loop length 1
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(1, -1));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2 }, out n);
            Assert.True(isCollatzLoop && n == new BigRational(1, 1));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 3 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(1, 5));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 4 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(1, 13));

            // Loop length 2
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(5, -5));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 2 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(5, -1));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(7, -1));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 2 }, out n);
            Assert.True(isCollatzLoop && n == new BigRational(7, 7));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 3 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(5, 7));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 3, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(11, 7));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 3 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(7, 23));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 3, 2 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(11, 23));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 3, 3 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(11, 55));

            // Loop length 3
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 1, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(19, -19));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 1, 2 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(19, -11));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 2, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(23, -11));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 1, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(29, -11));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 1, 2, 2 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(23, 5));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 1, 2 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(29, 5));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 2, 1 }, out n);
            Assert.True(!isCollatzLoop && n == new BigRational(37, 5));
            isCollatzLoop = Collatz2024.SolveForLoop(new[] { 2, 2, 2 }, out n);
            Assert.True(isCollatzLoop && n == new BigRational(37, 37));

            // All 2s
            for (int i = 1; i < 10; i++)
            {
                int[] twosExponentArray = new int[i];
                for (int j = 0; j < twosExponentArray.Length; j++)
                    twosExponentArray[j] = 2;
                isCollatzLoop = Collatz2024.SolveForLoop(twosExponentArray, out n);
                Assert.True(isCollatzLoop && n == 1);
            }

            // Length 2, order 2: expect {1,2},{2,1},{2,2}
            List<int[]> permsOrder2Len2List = Collatz2024.GenerateExponentPermutations(2, 2);
            List<string> permsOrder2Len2 = permsOrder2Len2List
                .Select(a => string.Join(",", a))
                .OrderBy(s => s)
                .ToList();
            List<string> expected2 = new List<string> { "1,2", "2,1", "2,2" };
            Assert.True(permsOrder2Len2.Count == expected2.Count);
            foreach (string e in expected2) Assert.Contains(e, permsOrder2Len2);

            // Length 2, order 3: expect {1,3},{2,3},{3,1},{3,2},{3,3}
            List<int[]> permsOrder3Len2List = Collatz2024.GenerateExponentPermutations(2, 3);
            List<string> permsOrder3Len2 = permsOrder3Len2List
                .Select(a => string.Join(",", a))
                .OrderBy(s => s)
                .ToList();
            List<string> expected3 = new List<string> { "1,3", "2,3", "3,1", "3,2", "3,3" };
            Assert.True(permsOrder3Len2.Count == expected3.Count);
            foreach (string e in expected3) Assert.Contains(e, permsOrder3Len2);

            // Length 1, order k: only [k]
            List<int[]> single = Collatz2024.GenerateExponentPermutations(1, 5);
            Assert.Single(single);
            Assert.True(single[0][0] == 5);

            // Sanity: count formula order^len - (order-1)^len
            int len = 4, ord = 3;
            List<int[]> permsOrder3Len4List = Collatz2024.GenerateExponentPermutations(len, ord);
            int all = permsOrder3Len4List.Count();
            int expectedCount = (int)(Math.Pow(ord, len) - Math.Pow(ord - 1, len));
            Assert.True(all == expectedCount);

            // Build one summary string per length (1..5)
            List<string> loopSummaryByLength = new();
            for (int length = 1; length <= 5; length++)
            {
                StringBuilder sbLen = new();
                sbLen.AppendLine("Order,Permutation,N,IsLoop,Numerator,Denominator,Double");
                for (int order = 1; order <= 5; order++)
                {
                    foreach (int[] perm in Collatz2024.GenerateExponentPermutations(length, order))
                    {
                        bool isLoop = Collatz2024.SolveForLoop(perm, out n);
                        sbLen.Append(order)
                             .Append(",[")
                             .Append(string.Join(' ', perm))
                             .Append("],")
                             .Append(n.ToString())
                             .Append(',')
                             .Append(isLoop ? '1' : '0')
                             .Append(',')
                             .Append(n.Numerator.ToString())
                             .Append(',')
                             .Append(n.Denominator.ToString())
                             .Append(',')
                             .Append(((double)n).ToString())
                             .AppendLine();
                    }
                }
                loopSummaryByLength.Add(sbLen.ToString());
            }
        }
        [Fact]
        public void TestPowerOfTwoPlusConstant_Const()
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
                string binaryString = Collatz2024.toBinaryBigEndianString(LessThanSeedList[n].Constant);
                string bitPrefix;
                if (bitLength > binaryString.Length)
                    binaryString = binaryString.PadRight(bitLength, '0');
                bitPrefix = binaryString.Substring(0, bitLength - 1);
                sb.AppendLine($"{LessThanSeedList[n].PowerOfTwo.ToString("G")},{LessThanSeedList[n].Constant.ToString("G")},{bitPrefix}");
            }
            string lessThanFormula = sb.ToString();
        }
        [Fact]
        public void TestPowerOfTwoPlusConstant()
        {
            const uint seriesLength = 64;
            BigInteger pow2 = 1, pow2Max = 16384;
            //List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> SeedList = new();
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
                sb.AppendLine($"{LessThanSeedList[n].PowerOfTwo.ToString("G")},{LessThanSeedList[n].Constant.ToString("G")}");
            string lessThanFormula = sb.ToString();
            sb.Clear();
            sb.AppendLine("PowerOfTwo,Constant,Bit Prefix");
            for (int n = 0; n < SurvivorList.Count; n++)
            {
                int bitLength = GetBitLength(SurvivorList[n].PowerOfTwo);
                string binaryString = Collatz2024.toBinaryBigEndianString(SurvivorList[n].Constant);
                string bitPrefix;
                if (bitLength > binaryString.Length)
                    binaryString = binaryString.PadRight(bitLength, '0');
                bitPrefix = binaryString.Substring(0, bitLength - 1);
                sb.AppendLine($"{SurvivorList[n].PowerOfTwo.ToString("G")},{SurvivorList[n].Constant.ToString("G")},{bitPrefix}");
            }
            string survivorFormula = sb.ToString();
        }
        bool SolvePowerOfTwoPlusConstant(BigInteger _PowerOfTwo, BigInteger _Constant, uint _SeriesLength, ref List<(BigInteger PowerOfTwo, BigInteger Constant)> LessThanSeedList, ref
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
        bool isAllOdd(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList)
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
        bool IsDuplicatePowerOfTwoPlusConstant(List<(BigInteger PowerOfTwo, BigInteger Constant)> _LessThanSeedList, BigInteger _PowerOfTwo, BigInteger _Constant, uint _SeriesLength)
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
        bool IsLatestLessThanSeed(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList)
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
        (BigInteger Seed, BigInteger M3a1, int Mod2Power) GetSuccessor(BigInteger _Value)
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
        void GetMinMaxModTwoPower(List<List<(BigInteger Seed, BigInteger M3a1, int Mod2Power)>> _ValueList, out int _MinModTwoPower, out int _MaxModTwoPower)
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
        [Fact]
        public void TestNextOdd()
        {
            BigInteger clltz = Collatz2024.NextOdd(3);
            Assert.True(clltz == 5);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101"));

            clltz = Collatz2024.NextOdd(5);
            Assert.True(clltz == 1);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1"));

            clltz = Collatz2024.NextOdd(7);
            Assert.True(clltz == 11);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1011"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1101"));

            clltz = Collatz2024.NextOdd(9);
            Assert.True(clltz == 7);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("111"));

            clltz = Collatz2024.NextOdd(11);
            Assert.True(clltz == 17);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("10001"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10001"));

            Assert.True(Collatz2024.NextOdd(3) == 5);
            Assert.True(Collatz2024.NextOdd(5) == 1);
            Assert.True(Collatz2024.NextOdd(7) == 11);
            Assert.True(Collatz2024.NextOdd(9) == 7);
            Assert.True(Collatz2024.NextOdd(11) == 17);

        }
        [Fact]
        public void TestCollapseInOne()
        {
            BigInteger clltz = Collatz2024.CollapseInOne(1);
            Assert.True(clltz == 1);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1"));

            clltz = Collatz2024.CollapseInOne(2);
            Assert.True(clltz == (BigInteger)5);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101"));

            clltz = Collatz2024.CollapseInOne(3);
            Assert.True(clltz == (BigInteger)21);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("10101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101"));

            clltz = Collatz2024.CollapseInOne(4);
            Assert.True(clltz == (BigInteger)85);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101"));

            clltz = Collatz2024.CollapseInOne(5);
            Assert.True(clltz == (BigInteger)341);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101"));

            clltz = Collatz2024.CollapseInOne(6);
            Assert.True(clltz == (BigInteger)1365);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("10101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101010101"));

            clltz = Collatz2024.CollapseInOne(7);
            Assert.True(clltz == (BigInteger)5461);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101"));

            clltz = Collatz2024.CollapseInOne(8);
            Assert.True(clltz == (BigInteger)21845);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101010101"));

            clltz = Collatz2024.CollapseInOne(9);
            Assert.True(clltz == (BigInteger)87381);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("10101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101010101010101"));

            clltz = Collatz2024.CollapseInOne(10);
            Assert.True(clltz == (BigInteger)349525);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101010101"));

            Assert.True(Collatz2024.CollapseInOne(1) == (BigInteger)1);
            Assert.True(Collatz2024.CollapseInOne(2) == (BigInteger)5);
            Assert.True(Collatz2024.CollapseInOne(3) == (BigInteger)21);
            Assert.True(Collatz2024.CollapseInOne(4) == (BigInteger)85);
            Assert.True(Collatz2024.CollapseInOne(5) == (BigInteger)341);
            Assert.True(Collatz2024.CollapseInOne(6) == (BigInteger)1365);
            Assert.True(Collatz2024.CollapseInOne(7) == (BigInteger)5461);
            Assert.True(Collatz2024.CollapseInOne(8) == (BigInteger)21845);
            Assert.True(Collatz2024.CollapseInOne(9) == (BigInteger)87381);
            Assert.True(Collatz2024.CollapseInOne(10) == (BigInteger)349525);
        }
        [Fact]
        public void TestCollapseInOne_ModOneOut()
        {
            BigInteger clltz = Collatz2024.CollapseInOne_ModOneOut(1);
            Assert.True(clltz == (BigInteger)85);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101"));

            clltz = Collatz2024.CollapseInOne_ModOneOut(2);
            Assert.True(clltz == (BigInteger)5461);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101"));

            clltz = Collatz2024.CollapseInOne_ModOneOut(3);
            Assert.True(clltz == (BigInteger)349525);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101010101"));

            clltz = Collatz2024.CollapseInOne_ModOneOut(4);
            Assert.True(clltz == (BigInteger)22369621);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101010101010101"));

            clltz = Collatz2024.CollapseInOne_ModOneOut(5);
            Assert.True(clltz == (BigInteger)1431655765);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1010101010101010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101010101010101010101010101"));

            Assert.True(Collatz2024.CollapseInOne_ModOneOut(1) == (BigInteger)85);
            Assert.True(Collatz2024.CollapseInOne_ModOneOut(2) == (BigInteger)5461);
            Assert.True(Collatz2024.CollapseInOne_ModOneOut(3) == (BigInteger)349525);
            Assert.True(Collatz2024.CollapseInOne_ModOneOut(4) == (BigInteger)22369621);
            Assert.True(Collatz2024.CollapseInOne_ModOneOut(5) == (BigInteger)1431655765);
        }
        [Fact]
        public void TestCollapseInOne_ModTwoOut()
        {
            BigInteger clltz = Collatz2024.CollapseInOne_ModTwoOut(1);
            Assert.True(clltz == (BigInteger)5);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101"));

            clltz = Collatz2024.CollapseInOne_ModTwoOut(2);
            Assert.True(clltz == (BigInteger)341);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101"));

            clltz = Collatz2024.CollapseInOne_ModTwoOut(3);
            Assert.True(clltz == (BigInteger)21845);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101010101"));

            clltz = Collatz2024.CollapseInOne_ModTwoOut(4);
            Assert.True(clltz == (BigInteger)1398101);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101010101010101"));

            clltz = Collatz2024.CollapseInOne_ModTwoOut(5);
            Assert.True(clltz == (BigInteger)89478485);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("101010101010101010101010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101010101010101010101010101"));

            Assert.True(Collatz2024.CollapseInOne_ModTwoOut(1) == (BigInteger)5);
            Assert.True(Collatz2024.CollapseInOne_ModTwoOut(2) == (BigInteger)341);
            Assert.True(Collatz2024.CollapseInOne_ModTwoOut(3) == (BigInteger)21845);
            Assert.True(Collatz2024.CollapseInOne_ModTwoOut(4) == (BigInteger)1398101);
            Assert.True(Collatz2024.CollapseInOne_ModTwoOut(5) == (BigInteger)89478485);
        }
        [Fact]
        public void TestCollapseInTwo_ModOne()
        {
            BigInteger clltz = Collatz2024.CollapseInTwo_ModOne(1, 1);
            Assert.True(clltz == (BigInteger)113);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModOne(1, 1) == (BigInteger)113);

            clltz = Collatz2024.CollapseInTwo_ModOne(1, 2);
            Assert.True(clltz == (BigInteger)453);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(2, 1);
            Assert.True(clltz == (BigInteger)7281);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(2, 2);
            Assert.True(clltz == (BigInteger)29125);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000111000101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101000111000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModOne(1, 2) == (BigInteger)453);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(2, 1) == (BigInteger)7281);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(2, 2) == (BigInteger)29125);

            clltz = Collatz2024.CollapseInTwo_ModOne(1, 3);
            Assert.True(clltz == (BigInteger)1813);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(2, 3);
            Assert.True(clltz == (BigInteger)116501);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(3, 1);
            Assert.True(clltz == (BigInteger)466033);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001110001"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(3, 2);
            Assert.True(clltz == (BigInteger)1864133);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000111000111000101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(3, 3);
            Assert.True(clltz == (BigInteger)7456533);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011100010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101000111000111000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModOne(1, 3) == (BigInteger)1813);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(2, 3) == (BigInteger)116501);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(3, 1) == (BigInteger)466033);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(3, 2) == (BigInteger)1864133);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(3, 3) == (BigInteger)7456533);

            clltz = Collatz2024.CollapseInTwo_ModOne(1, 4);
            Assert.True(clltz == (BigInteger)7253);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(2, 4);
            Assert.True(clltz == (BigInteger)466005);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(3, 4);
            Assert.True(clltz == (BigInteger)29826133);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001110001010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(4, 1);
            Assert.True(clltz == (BigInteger)29826161);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001110001110001"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1000111000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(4, 2);
            Assert.True(clltz == (BigInteger)119304645);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000111000111000111000101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101000111000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(4, 3);
            Assert.True(clltz == (BigInteger)477218581);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011100011100010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101000111000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModOne(4, 4);
            Assert.True(clltz == (BigInteger)1908874325);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001110001110001010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1010101000111000111000111000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModOne(1, 4) == (BigInteger)7253);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(2, 4) == (BigInteger)466005);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(3, 4) == (BigInteger)29826133);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(4, 1) == (BigInteger)29826161);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(4, 2) == (BigInteger)119304645);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(4, 3) == (BigInteger)477218581);
            Assert.True(Collatz2024.CollapseInTwo_ModOne(4, 4) == (BigInteger)1908874325);
        }
        [Fact]
        public void TestCollapseInTwo_ModTwo()
        {
            BigInteger clltz = Collatz2024.CollapseInTwo_ModTwo(1, 1);
            Assert.True(clltz == (BigInteger)3);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("11"));

            Assert.True(Collatz2024.CollapseInTwo_ModTwo(1, 1) == (BigInteger)3);

            clltz = Collatz2024.CollapseInTwo_ModTwo(1, 2);
            Assert.True(clltz == (BigInteger)13);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1011"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(2, 1);
            Assert.True(clltz == (BigInteger)227);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("11000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(2, 2);
            Assert.True(clltz == (BigInteger)909);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1011000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModTwo(1, 2) == (BigInteger)13);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(2, 1) == (BigInteger)227);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(2, 2) == (BigInteger)909);

            clltz = Collatz2024.CollapseInTwo_ModTwo(1, 3);
            Assert.True(clltz == (BigInteger)53);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("110101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101011"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(2, 3);
            Assert.True(clltz == (BigInteger)3637);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000110101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101011000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(3, 1);
            Assert.True(clltz == (BigInteger)14563);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("11000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(3, 2);
            Assert.True(clltz == (BigInteger)58253);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1011000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(3, 3);
            Assert.True(clltz == (BigInteger)233013);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000111000110101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101011000111000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModTwo(1, 3) == (BigInteger)53);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(2, 3) == (BigInteger)3637);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(3, 1) == (BigInteger)14563);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(3, 2) == (BigInteger)58253);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(3, 3) == (BigInteger)233013);

            clltz = Collatz2024.CollapseInTwo_ModTwo(1, 4);
            Assert.True(clltz == (BigInteger)213);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101011"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(2, 4);
            Assert.True(clltz == (BigInteger)14549);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101011000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(3, 4);
            Assert.True(clltz == (BigInteger)932053);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101011000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(4, 1);
            Assert.True(clltz == (BigInteger)932067);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011100011"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("11000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(4, 2);
            Assert.True(clltz == (BigInteger)3728269);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("1110001110001110001101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("1011000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(4, 3);
            Assert.True(clltz == (BigInteger)14913077);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("111000111000111000110101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("101011000111000111000111"));

            clltz = Collatz2024.CollapseInTwo_ModTwo(4, 4);
            Assert.True(clltz == (BigInteger)59652309);
            Assert.True(Collatz2024.toBinaryLittleEndianString(clltz).Equals("11100011100011100011010101"));
            Assert.True(Collatz2024.toBinaryBigEndianString(clltz).Equals("10101011000111000111000111"));

            Assert.True(Collatz2024.CollapseInTwo_ModTwo(1, 4) == (BigInteger)213);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(2, 4) == (BigInteger)14549);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(3, 4) == (BigInteger)932053);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(4, 1) == (BigInteger)932067);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(4, 2) == (BigInteger)3728269);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(4, 3) == (BigInteger)14913077);
            Assert.True(Collatz2024.CollapseInTwo_ModTwo(4, 4) == (BigInteger)59652309);
        }
        [Fact]
        public void TestBinaryStringToBigInt()
        {
            BigInteger gtUint64max = BigInteger.Parse("818446744073709551615"); // Larger than UInt64.MaxValue
            string czTxt = Collatz2024.toBinaryLittleEndianString(gtUint64max);
            BigInteger clltz = Collatz2024.toBigIntegerFromBinaryLittleEndianString(czTxt);
            Assert.True(clltz == gtUint64max);

            clltz = Collatz2024.toBigIntegerFromBinaryLittleEndianString("1");
            Assert.True(clltz == 1);
            clltz = Collatz2024.toBigIntegerFromBinaryLittleEndianString("11100011100011100011010101");
            Assert.True(clltz == 59652309);
            clltz = Collatz2024.toBigIntegerFromBinaryBigEndianString("10101011000111000111000111");
            Assert.True(clltz == 59652309);
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
                    seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
                    Assert.True(seed == pow4sum);
                    Assert.True(Collatz2024.NextOdd(seed) == 1);
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
                            Assert.True(seedBinaryBE.Equals("1"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 1:
                            Assert.True(seed == 5);
                            Assert.True(seedBinaryBE.Equals("101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 2:
                            Assert.True(seed == 21);
                            Assert.True(seedBinaryBE.Equals("10101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 3:
                            Assert.True(seed == 85);
                            Assert.True(seedBinaryBE.Equals("1010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 4:
                            Assert.True(seed == 341);
                            Assert.True(seedBinaryBE.Equals("101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 5:
                            Assert.True(seed == 1365);
                            Assert.True(seedBinaryBE.Equals("10101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 6:
                            Assert.True(seed == 5461);
                            Assert.True(seedBinaryBE.Equals("1010101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 7:
                            Assert.True(seed == 21845);
                            Assert.True(seedBinaryBE.Equals("101010101010101"));
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
                foreach (string seedBinaryBE in Collatz2024.GetBinaryBigEndianDecaysInOne().Take(100))
                {
                    seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
                    Assert.True(seed == pow4sum);
                    Assert.True(Collatz2024.NextOdd(seed) == 1);
                    Assert.True(seed % 3 == mod3);
                    if (++mod3 == 3)
                        mod3 = 0;
                    pow4 <<= 2;
                    pow4sum += pow4;
                    switch (i)
                    {
                        case 0:
                            Assert.True(seed == 1);
                            Assert.True(seedBinaryBE.Equals("1"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 1:
                            Assert.True(seed == 5);
                            Assert.True(seedBinaryBE.Equals("101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 2:
                            Assert.True(seed == 21);
                            Assert.True(seedBinaryBE.Equals("10101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 3:
                            Assert.True(seed == 85);
                            Assert.True(seedBinaryBE.Equals("1010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 4:
                            Assert.True(seed == 341);
                            Assert.True(seedBinaryBE.Equals("101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 5:
                            Assert.True(seed == 1365);
                            Assert.True(seedBinaryBE.Equals("10101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 6:
                            Assert.True(seed == 5461);
                            Assert.True(seedBinaryBE.Equals("1010101010101"));
                            Assert.False(isCase[i]);
                            isCase[i] = true;
                            break;
                        case 7:
                            Assert.True(seed == 21845);
                            Assert.True(seedBinaryBE.Equals("101010101010101"));
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
            const int maxLoopIdx = 75000;
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

                nxtOdd = OddOfIndex(loopIdx);
                thisOddList.Add(nxtOdd);
                currIdx = loopIdx;
                while (true)
                {
                    nxtOdd = Collatz2024.NextOdd(nxtOdd);
                    if (nxtOdd == 1)
                    {
                        for (int i = 0; i < thisOddList.Count; i++)
                        {
                            seedList[(int)IndexofOdd(thisOddList[i])] = (ulong)(thisOddList.Count - i);
                        }
                        break;
                    }
                    thisOddList.Add(nxtOdd);
                    nxtIdx = IndexofOdd(nxtOdd);
                    while (nxtIdx >= seedList.Count)
                    {
                        seedList.Add(0);
                    }
                    currIdx = IndexofOdd(nxtOdd);
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
                    BigInteger bi = OddOfIndex(i);
                    Assert.True(seedList[i] == Collatz2024.OddStepCountToOne(bi));
                }
            }
            StringBuilder sb = new();
            string result = string.Empty;
            for (int i = 0; i < seedList.Count; i++)
            {
                ulong targetDecay = 1;
                if (true && seedList[i] == targetDecay)
                {
                    List<BigInteger> decayInN_List = [];
                    decayInN_List.Add(OddOfIndex(i));
                    for (ulong j = 1; j < targetDecay; j++)
                        decayInN_List.Add(Collatz2024.NextOdd(decayInN_List[decayInN_List.Count - 1]));
                    for (int j = 0; j < decayInN_List.Count; j++)
                        sb.Append(decayInN_List[j].ToString() + ',');
                    sb.AppendLine();
                }
            }
            result = sb.ToString();
        }
        [Fact]
        public void DeriveDecayInN_Formula()
        {
            int decayInTarget = 3;
            List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> decayInN_FirstDecayPairList = GetDecayInN_FirstDecayPair(decayInTarget, 300, 300000000);
            StringBuilder sb = new(), bigEndSb = new();
            sb.AppendLine("N,N % 3,FirstDecay,Power of 2,BigEndian N,BigEndian Core");
            foreach ((BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo) tuple in decayInN_FirstDecayPairList)
            {
                bigEndSb.Clear();
                bigEndSb.Append(Collatz2024.toBinaryBigEndianString(tuple.DecayInTarget));
                string bigEndTxt = bigEndSb.ToString();
                string bigEndCore = Strip_10_000111_BigEnd(bigEndSb.ToString());
                sb.AppendLine(tuple.DecayInTarget.ToString() + ',' + (tuple.DecayInTarget % 3).ToString() + ',' +
                    tuple.FirstDecay.ToString() + ',' + tuple._PowTwo.ToString() + ',' + bigEndTxt + ',' + bigEndCore);
            }
            string csvResult = sb.ToString();
            string hh = CheckFormula(decayInN_FirstDecayPairList);
        }
        static string CheckFormula(List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> _DecayInN_TupleList)
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
                sb.AppendLine(tuple.DecayInTarget.ToString() + ',' + tuple.FirstDecay.ToString() + ',' + x.ToString() + ',' + k.ToString() + ',' + c.ToString());
            }
            return sb.ToString();
        }
        // Remove optional leading "10" blocks and optional trailing "000111" block.
        static string Strip_10_000111_BigEnd(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            while (s.EndsWith("000111", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - 6);

            while (s.StartsWith("10", StringComparison.Ordinal))
                s = s.Substring(2);

            return s;
        }
        public uint GetPowTwoForCollatzPair(BigInteger _N, BigInteger _OddDecay)
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
        List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> GetDecayInN_FirstDecayPair(int _DayTarget, int _MaxListCount, BigInteger _MaxSeed)
        {
            int decayCt;
            List<(BigInteger DecayInTarget, BigInteger FirstDecay, uint _PowTwo)> decayInN_List = [];
            BigInteger seed = -1, firstDecay, nextDecay;
            while (true)
            {
                seed += 2;
                firstDecay = Collatz2024.NextOdd(seed);
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
                    nextDecay = Collatz2024.NextOdd(nextDecay);
                    ++decayCt;
                }
                if (decayInN_List.Count >= _MaxListCount || seed > _MaxSeed)
                    break;
            }
            return decayInN_List;
        }
        ulong IndexofOdd(BigInteger _Odd)
        {
            return (ulong)((_Odd - 3) / 2);
        }
        BigInteger OddOfIndex(int _Index)
        {
            return _Index * 2 + 3; ;
        }

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
        //    //    decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndDecayIn2.ToString());
        //    //    decayIn1 = Collatz2024.NextOdd(decayIn2);
        //    //    Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //    //    BigEndEchoIn2.Clear().Append(BigEndDecayIn2);
        //    //    for (int j = 0; j < trials; j++)
        //    //    {
        //    //        BigEndEchoIn2.Insert(0, echoPrefix);
        //    //        decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
        //    //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //    //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //    //    }
        //    //    if (i < 32)
        //    //        BigEndDecayIn2.Insert(0, "10");
        //    //    else
        //    //        BigEndDecayIn2.Insert(0, "1");
        //    //}
        //    BigEndDecayIn2.Clear().Append("1");
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndDecayIn2.ToString());
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        BigEndEchoIn2.Clear().Append(BigEndDecayIn2);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEchoIn2.Insert(0, echoPrefix);
        //            decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        }
        //        if (i < 8)
        //            BigEndDecayIn2.Insert(1, "1");
        //        else
        //            BigEndDecayIn2.Insert(1, '0');
        //    }
        //    foreach (string seedBinaryBE in Collatz2024.GetBinaryBigEndianDecaysInTwo())
        //    {
        //        decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(seedBinaryBE);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        BigEndEchoIn2.Clear().Append(seedBinaryBE);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEchoIn2.Insert(0, echoPrefix);
        //            decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEchoIn2.ToString());
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        }
        //    }
        //    StringBuilder sb = new();
        //    sb.AppendLine("Anchor,DecayIn1,DecayIn2,Steps");
        //    foreach (string ln in Collatz2024.GetBinaryBigEndianDecaysInTwo().Take(256))
        //    {
        //        decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(ln);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        sb.AppendLine(ln + ',' + decayIn1.ToString() + ',' + decayIn2.ToString() + ',' + Collatz2024.OddStepCountToSmaller(decayIn2).ToString());
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
        //    //        seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(seedTxt);
        //    //        decayOnce = Collatz2024.NextOdd(seed);
        //    //        Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
        //    //        sbEcho = new StringBuilder(sbEcho.ToString());
        //    //        for (int j = 0; j < 100; j++)
        //    //        {
        //    //            sbSeed.Insert(0, echoPrefix);
        //    //            seedTxt = sbSeed.ToString();
        //    //            seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(seedTxt);
        //    //            decayOnce = Collatz2024.NextOdd(seed);
        //    //            Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
        //    //        }
        //    //        sbSeed.Append(seedSuffix);
        //    //    }
        //    //}
        //    sbSeed = new StringBuilder(mod1SeedTxt);
        //    for (int i = 0; i < 100; i++)
        //    {
        //        seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(sbSeed.ToString());
        //        decayOnce = Collatz2024.NextOdd(seed);
        //        Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
        //        sbEcho = new StringBuilder(sbEcho.ToString());
        //        for (int j = 0; j < 100; j++)
        //        {
        //            sbEcho.Insert(0, echoPrefix);
        //            decayOnce = Collatz2024.NextOdd(seed);
        //            Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
        //        }
        //        sbSeed.Append(seedSuffix);
        //    }
        //    sbSeed = new StringBuilder(mod2SeedTxt);
        //    for (int i = 0; i < 100; i++)
        //    {
        //        seed = Collatz2024.toBigIntegerFromBinaryBigEndianString(sbSeed.ToString());
        //        decayOnce = Collatz2024.NextOdd(seed);
        //        Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
        //        sbEcho = new StringBuilder(sbEcho.ToString());
        //        for (int j = 0; j < 100; j++)
        //        {
        //            sbEcho.Insert(0, echoPrefix);
        //            decayOnce = Collatz2024.NextOdd(seed);
        //            Assert.True(Collatz2024.NextOdd(decayOnce) == 1);
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
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 17);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001") == 35);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110001") == 1137);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110001") == 2275);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110001110001") == 72817);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110001110001") == 145635);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110001110001110001") == 4660337);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110001110001110001") == 9320675);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110001110001110001110001") == 298261617);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110001110001110001110001") == 596523235);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110001110001110001110001110001") == 19088743537);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110001110001110001110001110001") == 38177487075);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }
        //    // 5 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010001") == 69);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010001") == 277);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010001") == 1109);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101010001") == 4437);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10110001") == 141);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010110001") == 565);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010110001") == 2261);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010110001") == 9045);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010001110001") == 4549);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010001110001") == 18197);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010001110001") == 72789);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101010001110001") == 291157);

        //    // 85 anchor
        //    targetDecayIn1 = 85;
        //    targetDecayInThreeBigEnd = "1101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 75);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("100011101001") == 2417);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1100011101001") == 4835);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }

        //    // 85 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101101001") == 301);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101101001") == 1205);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101101001") == 4821);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010110001") == 19285);
        //    Assert.True(Collatz2024.toBinaryLittleEndianString(191).Equals("10111111"));
        //    Assert.True(Collatz2024.toBinaryBigEndianString(191).Equals("11111010"));

        //    // 341 anchor
        //    targetDecayIn1 = 341;
        //    targetDecayInThreeBigEnd = "11101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 151);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111101001") == 4849);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111101001") == 9699);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111101001") == 310385);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111101001") == 620771);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }

        //    // 341 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1011101001") == 605);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101011101001") == 2421);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101011101001") == 9685);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101011101001") == 38741);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101000111101001") == 19397);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101000111101001") == 77589);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101000111101001") == 310357);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010101000111101001") == 1241429);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1011000111101001") == 38797);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101100011101001") == 155189);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101011000111101001") == 620757);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101011000111101001") == 2483029);

        //    // 5461 anchor
        //    targetDecayIn1 = 5461;
        //    targetDecayInThreeBigEnd = "1000110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 38833);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000110111101001") == 77667);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000110111101001") == 2485361);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111101001") == 497073);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }
        //    // 5461 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101000110111101001") == 155333);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101000110111101001") == 621365);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101000110111101001") == 2485333);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010101000110111101001") == 9941333);
        //    Assert.True(Collatz2024.toBinaryLittleEndianString(191).Equals("10111111"));
        //    Assert.True(Collatz2024.toBinaryBigEndianString(191).Equals("11111010"));

        //    // 21845 anchor
        //    targetDecayIn1 = 21845;
        //    targetDecayInThreeBigEnd = "100110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 19417);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1100110111101001") == 38835);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("100011100110111101001") == 1242737);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1100011100110111101001") == 2485475);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }
        //    // 21845 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10100110111101001") == 77669);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010100110111101001") == 310677);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010100110111101001") == 1242709);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010100110111101001") == 4970837);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101100110111101001") == 155341);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101100110111101001") == 621365);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101100110111101001") == 2485461);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010101100110111101001") == 9941845);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10100011100110111101001") == 4970949);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010100011100110111101001") == 19883797);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010100011100110111101001") == 79535189);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010100011100110111101001") == 318140757);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101100011100110111101001") == 9941901);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101100011100110111101001") == 39767605);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101100011100110111101001") == 159070421);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010101100011100110111101001") == 636281685);

        //    // 349525 anchor
        //    targetDecayIn1 = 349525;
        //    targetDecayInThreeBigEnd = "10000010110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 621377);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110000010110111101001") == 1242755);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10001110000010110111101001") == 39768177);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("110001110000010110111101001") == 79536355);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111101001") == 19864689);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111101001") == 39729379);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000111000111000111101001") == 1271340145);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111000111000111000111101001") == 2542680291);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }
        //    // 349525 echo
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010000010110111101001") == 2485509);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010000010110111101001") == 9942037);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010000010110111101001") == 39768149);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101010000010110111101001") == 159072597);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10110000010110111101001") == 4971021);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010110000010110111101001") == 19884085);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010110000010110111101001") == 79536341);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010110000010110111101001") == 318145365);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010001110000010110111101001") == 159072709);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010001110000010110111101001") == 636290837);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010001110000010110111101001") == 2545163349);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010101010001110000010110111101001") == 10180653397);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10110001110000010110111101001") == 318145421);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1010110001110000010110111101001") == 1272581685);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("101010110001110000010110111101001") == 5090326741);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("10101010110001110000010110111101001") == 20361306965);

        //    // 1398101 anchor
        //    targetDecayIn1 = 1398101;
        //    targetDecayInThreeBigEnd = "10001000010110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 4971025);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }

        //    // 22369621 anchor
        //    targetDecayIn1 = 22369621;
        //    targetDecayInThreeBigEnd = "1101001000010110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 19884107);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }

        //    // 89478485 anchor
        //    targetDecayIn1 = 89478485;
        //    targetDecayInThreeBigEnd = "11101001000010110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 39768215);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111101001000010110111101001") == 1272582897);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000111101001000010110111101001") == 2545165795);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("100011000111101001000010110111101001") == 40722652721);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        string BigEndTxt = BigEndAnchor.ToString();
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        string LittleEndTxt = Collatz2024.toBinaryLittleEndianString(decayIn3);
        //        Assert.True(Collatz2024.toBigIntegerFromBinaryLittleEndianString(LittleEndTxt) == decayIn3);
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }

        //    // 1431655765 anchor
        //    targetDecayIn1 = 1431655765;
        //    targetDecayInThreeBigEnd = "1000110111101001000010110111101001";
        //    trialParity = targetDecayInThreeBigEnd[1] == '0' ? 0 : 1;
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString(targetDecayInThreeBigEnd) == 10180663217);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("11000110111101001000010110111101001") == 20361326435);
        //    Assert.True(Collatz2024.toBigIntegerFromBinaryBigEndianString("1000111000110111101001000010110111101001") == 651562445937);
        //    BigEndAnchor = new(targetDecayInThreeBigEnd);
        //    for (int i = 0; i < trials; i++)
        //    {
        //        string BigEndTxt = BigEndAnchor.ToString();
        //        decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
        //        string LittleEndTxt = Collatz2024.toBinaryLittleEndianString(decayIn3);
        //        Assert.True(Collatz2024.toBigIntegerFromBinaryLittleEndianString(LittleEndTxt) == decayIn3);
        //        decayIn2 = Collatz2024.NextOdd(decayIn3);
        //        decayIn1 = Collatz2024.NextOdd(decayIn2);
        //        Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //        Assert.True(decayIn1 == targetDecayIn1);
        //        BigEndEcho.Clear().Append(BigEndAnchor);
        //        for (int j = 0; j < trials; j++)
        //        {
        //            BigEndEcho.Insert(0, echoPrefix);
        //            decayIn3 = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
        //            decayIn2 = Collatz2024.NextOdd(decayIn3);
        //            decayIn1 = Collatz2024.NextOdd(decayIn2);
        //            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
        //            Assert.True(decayIn1 == targetDecayIn1);
        //        }
        //        string insertTxt = (i & 1) == trialParity ? "1" : "00011";
        //        BigEndAnchor.Insert(1, insertTxt);
        //    }
        //    StringBuilder sb = new();
        //    sb.AppendLine("DecayIn3,DecayIn2,DecayIn1,Binary");
        //    foreach ((BigInteger decayIn3, BigInteger decayIn2, BigInteger decayIn1) in Collatz2024.GetBinaryBigEndianDecaysInThree().Take(1024))
        //    {
        //        sb.AppendLine(decayIn3 + "," + decayIn2 + "," + decayIn1 + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
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
            for (int n = 1; n < 1000; n++)
            {
                ToOneInOneSet.Add(fnDecayToOneInOne(n));
                ToOneInTwoSet.Add(fnDecayToOneInTwo_Mod2(n));
                ToOneInTwoSet.Add(fnDecayToOneInTwo_Mod1(n));
            }
            ulong oddDecaySteps = 0;
            ulong trials = 10000000;
            bool isContains;
            for (ulong odd = 1; odd < trials; odd += 2)
            {
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                switch (oddDecaySteps)
                {
                    case 1:
                        isContains = ToOneInOneSet.Contains(odd);
                        if (!isContains)
                            isContains = false;
                        break;
                    case 2:
                        ulong decaysTo = odd;
                        //if (decaysTo == 853)
                        //    Assert.True(true);
                        while (!ToOneInTwoSet.Contains(decaysTo))
                        {
                            if ((decaysTo - 1) % 4 != 0)
                                Assert.True(false);
                            decaysTo = (decaysTo - 1) / 4;
                            Assert.True(Collatz2024.OddStepCountToOne(decaysTo) == oddDecaySteps);
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
            int trials = 10000000;
            ulong oddDecaySteps = 0;
            // Test decay in 1
            for (int odd = 1; odd < trials; odd++)
            {
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                if (oddDecaySteps == 1)
                {
                    if (Collatz2024.DecayInN_FormulaList(odd, collatzDecayFormulaRecursive) != 1)
                        Assert.True(true);
                    Assert.True(Collatz2024.DecayInN_FormulaList(odd, collatzDecayFormulaRecursive) == 1);
                    if (!collatzDecayFormulaRecursive.IsMember(odd))
                        Assert.True(true);
                    Assert.True(collatzDecayFormulaRecursive.IsMember(odd));
                    if (!collatzDecayFormula.IsMember(odd))
                        Assert.True(true);
                    Assert.True(collatzDecayFormula.IsMember(odd));
                    if (!collatzDecayFormulaBitManipulation.IsMember(odd))
                        Assert.True(true);
                    Assert.True(collatzDecayFormulaBitManipulation.IsMember(odd));
                }
                else
                {
                    Assert.False(Collatz2024.DecayInN_FormulaList(odd, collatzDecayFormulaRecursive) == 1);
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
            int trials = 10000000;
            //trials = 1000;
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
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                isMemberCt = 0;
                foreach (CollatzDecayFormulaRecursive cdfr in collatzDecayFormulaRecursiveList)
                {
                    //if (odd == 453 && cdfr.AdditiveConstant == 49)
                    //    cdfr.IsMember(odd);
                    if (cdfr.IsMember(odd))
                    {
                        if (cdfr.StepsToOne != oddDecaySteps)
                            cdfr.IsMember(odd);
                        Assert.True(cdfr.StepsToOne == oddDecaySteps);
                        ++isMemberCt;
                    }
                }
                if (!((isMemberCt == 1) == (oddDecaySteps == 2)))
                    Assert.True(true);
                Assert.True((isMemberCt == 1) == (oddDecaySteps == 2));
                isMemberCt = 0;
                foreach (CollatzDecayFormula cdf in collatzDecayFormulaList)
                {
                    //if (odd == 113 && cdf.SubtractiveConstant == 7)
                    //    cdf.IsMember(odd);
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
                        if (cdbm.StepsToOne != oddDecaySteps)
                            cdbm.IsMember(odd);
                        Assert.True(cdbm.StepsToOne == oddDecaySteps);
                        ++isMemberCt;
                    }
                    //else
                    //    Assert.False(cdbm.StepsToOne == oddDecaySteps);
                }
                if (!(isMemberCt == 1) == (oddDecaySteps == 2))
                    CollatzDecayFormulaBitManipulationList[0].IsMember(odd);
                Assert.True((isMemberCt == 1) == (oddDecaySteps == 2));
            }
            for (int odd = 1; odd < trials; odd++)
            {
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
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
            int trials = int.MaxValue;
            //trials = 1000;
            ulong oddDecaySteps = 0;
            bool isMember;
            for (int odd = 1; odd < trials; odd += 2)
            {
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                isMember = collatzDecayFormulaBitManipulationIn3.IsMember(odd);
                if ((oddDecaySteps == 3) != isMember)
                    collatzDecayFormulaBitManipulationIn3.IsMember(odd);
                Assert.True((oddDecaySteps == 3) == isMember);
            }
        }
        [Fact]
        public void Test4nPlus1()
        {
            int trials = 10000000;
            ulong oddDecaySteps = 0, fourNplusOneSteps = 0;
            for (int odd = 1; odd < trials; odd += 2)
            {
                //if (odd == 9)
                //    Assert.True(true);
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
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                fourNplusOneSteps = Collatz2024.OddStepCountToOne(c);
                if (oddDecaySteps != fourNplusOneSteps)
                    Assert.True(true);
                Assert.True(Collatz2024.OddStepCountToOne(odd) == Collatz2024.OddStepCountToOne(c));
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
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
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
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
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
                oddDecaySteps = Collatz2024.OddStepCountToOne(odd);
                isMemberCt = 0;
                foreach (ICollatzDecayFormula collatzFormula in collatzFormulaInThreeStepsList)
                {
                    if (collatzFormula.IsMember(odd))
                        ++isMemberCt;
                }
                if (oddDecaySteps == 3)
                {
                    if (isMemberCt != 1)
                        Assert.True(true);
                    Assert.True(isMemberCt == 1);
                }
                else
                    Assert.True(isMemberCt == 0);
            }
            Assert.True(true);
        }
        [Fact]
        public void ExploreDecayInTwo()
        {
            int trials = int.MaxValue; // 2,147,483,647 
            trials = 100000000; //
            BigInteger decayIn1;
            HashSet<BigInteger> DecayIn2HashSet = new HashSet<BigInteger>();
            HashSet<BigInteger> DecayIn1HashSet = new HashSet<BigInteger>();
            HashSet<(BigInteger DecayIn2, BigInteger DecayIn1)> DecayIn21HashSet = new();
            for (int i = 1; i < trials; i++)
            {
                if (Collatz2024.OddStepCountToOne(i) != 2)
                    continue;
                if ((i & 1) == 0) // even
                    continue;
                int decayIn2candidate = i;
                DecayIn2HashSet.Add(i);
                decayIn1 = Collatz2024.NextOdd(i);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                DecayIn1HashSet.Add(decayIn1);
                DecayIn21HashSet.Add((i, decayIn1));
            }
            StringBuilder sb2 = new();
            foreach (BigInteger c2 in DecayIn2HashSet)
                sb2.AppendLine(c2.ToString());
            string s2 = sb2.ToString();
            StringBuilder sb1 = new();
            foreach (BigInteger c1 in DecayIn1HashSet)
                sb1.AppendLine(c1.ToString());
            string s1 = sb1.ToString();
            StringBuilder sb21 = new();
            foreach ((BigInteger DecayIn2, BigInteger DecayIn1) c21 in DecayIn21HashSet)
                sb21.AppendLine(c21.DecayIn2.ToString() + ',' + c21.DecayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(c21.DecayIn2));
            string s21 = sb21.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreDecayInThree()
        {
            int trials = int.MaxValue; // 2,147,483,647 
            trials = 100000000; //
            BigInteger decayIn1, decayIn2;
            HashSet<BigInteger> DecayIn3HashSet = new HashSet<BigInteger>();
            HashSet<BigInteger> DecayIn2HashSet = new HashSet<BigInteger>();
            HashSet<BigInteger> DecayIn1HashSet = new HashSet<BigInteger>();
            HashSet<(BigInteger DecayIn3, BigInteger DecayIn2, BigInteger DecayIn1)> DecayIn321HashSet = new();
            for (int i = 1; i < trials; i++)
            {
                if (Collatz2024.OddStepCountToOne(i) != 3)
                    continue;
                if ((i & 1) == 0) // even
                    continue;
                int decayIn3candidate = i;
                DecayIn3HashSet.Add(decayIn3candidate);
                decayIn2 = Collatz2024.NextOdd(decayIn3candidate);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                DecayIn2HashSet.Add(decayIn2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                DecayIn1HashSet.Add(decayIn1);
                DecayIn321HashSet.Add((decayIn3candidate, decayIn2, decayIn1));
            }
            StringBuilder sb3 = new();
            foreach (BigInteger c3 in DecayIn3HashSet)
                sb3.AppendLine(c3.ToString());
            string s3 = sb3.ToString();
            StringBuilder sb2 = new();
            foreach (BigInteger c2 in DecayIn2HashSet)
                sb2.AppendLine(c2.ToString());
            string s2 = sb2.ToString();
            StringBuilder sb1 = new();
            foreach (BigInteger c1 in DecayIn1HashSet)
                sb1.AppendLine(c1.ToString());
            string s1 = sb1.ToString();
            StringBuilder sb321 = new();
            foreach ((BigInteger DecayIn3, BigInteger DecayIn2, BigInteger DecayIn1) c321 in DecayIn321HashSet)
                sb321.AppendLine(c321.DecayIn3.ToString() + ',' + c321.DecayIn2.ToString() + ',' + c321.DecayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(c321.DecayIn3)
                    + ',' + Collatz2024.toBinaryBigEndianString(c321.DecayIn2));
            string s321 = sb321.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreSpecificDecay()
        {
            int sampleCt = 50;
            List<string> sampleList = new();
            StringBuilder sb1 = new("1000110111101001");
            int idx1 = sb1.Length;
            BigInteger odd1 = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb1.ToString());
            StringBuilder sb2 = new("1000111000110111101001");
            int idx2 = sb2.Length;
            BigInteger odd2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb2.ToString());

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
                    odd1 = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb1.ToString());
                }
                else
                {
                    decayIn3 = odd2;
                    if (sb2.Length == idx2)
                        sb2.Insert(0, "1");
                    else
                        sb2.Insert(sb2.Length - idx2, sb2[sb2.Length - idx2 - 1] == '0' ? '1' : '0');
                    odd2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb2.ToString());
                }
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                BigInteger c = decayIn3;
                while ((c & 3) == 1) // Equivalent to c % 4 == 1
                    c = (c - 1) >> 2; // Equivalent to (c - 1) / 4
                if (c.IsEven)
                    c = (c << 2) + 1;
                sampleList.Add(new string(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3)));
            }
            StringBuilder sb = new();
            foreach (string ln in sampleList)
                sb.AppendLine(ln);
            string csv = sb.ToString();
            Assert.True(true);
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
            StringBuilder sb = new();
            while (true)
            {
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                Assert.True(decayIn1 == 5461);
                sb.AppendLine(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
                if (++ct > 10)
                    break;
                if (is32_17)
                    decayIn3 = decayIn3 * 32 + 17;
                else
                    decayIn3 = decayIn3 * 2 + 1;
                is32_17 = !is32_17;
            }
            string csv = sb.ToString();
            Assert.True(true);
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
            StringBuilder sb = new();
            while (true)
            {
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                Assert.True(decayIn1 == 349525);
                sb.AppendLine(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
                if (++ct > 10)
                    break;
                if (is32_17)
                    decayIn3 = decayIn3 * 32 + 17;
                else
                    decayIn3 = decayIn3 * 2 + 1;
                is32_17 = !is32_17;
            }
            string csv = sb.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreTo1398101()
        {
            // 4971025
            // 9942051

            BigInteger decayIn3 = 4971025, decayIn2, decayIn1;
            bool is32_17 = false;
            int ct = 0;
            StringBuilder sb = new();
            while (true)
            {
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                Assert.True(decayIn1 == 1398101);
                sb.AppendLine(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
                if (++ct > 10)
                    break;
                if (is32_17)
                    decayIn3 = decayIn3 * 32 + 17;
                else
                    decayIn3 = decayIn3 * 2 + 1;
                is32_17 = !is32_17;
            }
            string csv = sb.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreTo22369621()
        {
            // 19884107

            BigInteger decayIn3 = 19884107, decayIn2, decayIn1;
            bool is32_17 = true;
            int ct = 0;
            StringBuilder sb = new();
            while (true)
            {
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                Assert.True(decayIn1 == 22369621);
                sb.AppendLine(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
                if (++ct > 10)
                    break;
                if (is32_17)
                    decayIn3 = decayIn3 * 32 + 17;
                else
                    decayIn3 = decayIn3 * 2 + 1;
                is32_17 = !is32_17;
            }
            string csv = sb.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreTo89478485()
        {
            // 39768215

            BigInteger decayIn3 = 39768215, decayIn2, decayIn1;
            bool is32_17 = true;
            int ct = 0;
            StringBuilder sb = new();
            while (true)
            {
                Assert.True(Collatz2024.OddStepCountToOne(decayIn3) == 3);
                decayIn2 = Collatz2024.NextOdd(decayIn3);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn2) == 2);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(Collatz2024.OddStepCountToOne(decayIn1) == 1);
                Assert.True(decayIn1 == 89478485);
                sb.AppendLine(decayIn3.ToString() + ',' + decayIn2.ToString() + ',' + decayIn1.ToString()
                    + ',' + Collatz2024.toBinaryBigEndianString(decayIn3));
                if (++ct > 10)
                    break;
                if (is32_17)
                    decayIn3 = decayIn3 * 32 + 17;
                else
                    decayIn3 = decayIn3 * 2 + 1;
                is32_17 = !is32_17;
            }
            string csv = sb.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreTwoToTheNplusOne()
        {
            StringBuilder sb = new("11");
            StringBuilder csvSb = new();
            BigInteger c;
            int multStepCt;
            while (sb.Length < 128)
            {
                c = Collatz2024.toBigIntegerFromBinaryBigEndianString(sb.ToString());
                multStepCt = 0;
                while (true)
                {
                    csvSb.AppendLine(c.ToString() + ',' + Collatz2024.toBinaryBigEndianString(c));
                    c = Collatz2024.NextOdd(c);
                    ++multStepCt;
                    if (c == 341)
                        Assert.True(true);
                    if (c == 1)
                    {
                        csvSb.Append(Environment.NewLine);
                        break;
                    }
                }
                sb.Insert(1, '0');
            }
            string csv = csvSb.ToString();
            Assert.True(true);
        }
        [Fact]
        public void ExploreTwoToTheNplusOneFormula()
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
                sb.Append(expNoffsetList.Last().ToString());
                for (int i = expNoffsetList.Count - 2; i >= 0; i--)
                    sb.Append(',' + expNoffsetList[i].ToString());
                sb.Append(Environment.NewLine);
                Int64 norm = expNoffsetList.Last();
                sbNorm.Append((expNoffsetList.Last() - norm).ToString());
                for (int i = expNoffsetList.Count - 2; i >= 0; i--)
                    sbNorm.Append(',' + (expNoffsetList[i] - norm).ToString());
                sbNorm.Append(Environment.NewLine);
                if (++csvLnCt > 256)
                    break;
            }
            string csvTxt = sb.ToString();
            string csvNormTxt = sbNorm.ToString();
            Assert.True(true);
        }
        [Fact]
        public void Pow2_OddDecayCount()
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
                UInt64 stepsToSmaller = Collatz2024.OddStepCountToSmaller(i);
                sb.AppendLine(pow2.ToString() + ',' + (i - pow2).ToString() + ',' + i.ToString() + ',' + Collatz2024.OddStepCountToOne(i).ToString() + ',' + stepsToSmaller.ToString());
            }
            string csv = sb.ToString();
            Assert.True(true);
            List<List<int>> oddStepCountToSmallerList = new();
            for (BigInteger i = 3; i < trials; i += 2)
            {
                int stepsToSmaller = (int)Collatz2024.OddStepCountToSmaller(i);
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
                    sb.Append(stepCt.ToString() + ',' + cycleLength.ToString() + ',' + (oddStepCountToSmallerList[stepCt][cycleLength] - oddStepCountToSmallerList[stepCt][0]).ToString() + ',' + oddStepCountToSmallerList[stepCt][0].ToString());
                    for (int i = 0; i < cycleLength; i++)
                    {
                        sb.Append(',' + (oddStepCountToSmallerList[stepCt][i + 1] - oddStepCountToSmallerList[stepCt][i]).ToString());
                    }
                    sb.AppendLine();
                }
            }
            csv = sb.ToString();
            Assert.True(true);
        }
        #endregion Fact Methods
        #region Helper Methods
        private void AssertDecayIn1(string _AnchorBigEnd)
        {
            BigInteger currOdd = Collatz2024.toBigIntegerFromBinaryBigEndianString(_AnchorBigEnd.ToString());
            Assert.True(Collatz2024.NextOdd(currOdd) == 1);
        }
        private void AssertDecayIn2(int _EchoTrials, string _DecayInTwoBigEnd)
        {
            BigInteger decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(_DecayInTwoBigEnd);
            Assert.True(decayIn2 != 1);
            BigInteger decayIn1 = Collatz2024.NextOdd(decayIn2);
            Assert.True(decayIn1 != 1);
            Assert.True(Collatz2024.NextOdd(decayIn1) == 1);

            BigInteger decayIn1Anchor = decayIn1;
            const string echoPrefix = "10";
            StringBuilder echoBinBE = new(_DecayInTwoBigEnd);
            for (int echoTrialId = 0; echoTrialId < _EchoTrials; echoTrialId++)
            {
                echoBinBE.Insert(0, echoPrefix);
                decayIn2 = Collatz2024.toBigIntegerFromBinaryBigEndianString(_DecayInTwoBigEnd);
                Assert.True(decayIn2 != 1);
                decayIn1 = Collatz2024.NextOdd(decayIn2);
                Assert.True(decayIn1 == decayIn1Anchor);
                Assert.True(Collatz2024.NextOdd(decayIn1) == 1);
            }
        }
        private void AssertDecayInN(int _DecayInN, int _Trials, BigInteger _TargetDecayInOneBigInt, string _TargetDecayInN_BigEnd)
        {
            string echoPrefix = "10", mult2Add1Txt = "1", mult32Add24Txt = "00011";
            StringBuilder BigEndAnchor = new(_TargetDecayInN_BigEnd), BigEndEcho = new();
            int n, trialParity = _TargetDecayInN_BigEnd[1] == '0' ? 0 : 1;
            for (int i = 0; i < _Trials; i++)
            {
                string BigEndTxt = BigEndAnchor.ToString();
                BigInteger nextOdd, currOdd = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndAnchor.ToString());
                n = _DecayInN;
                while (true)
                {
                    nextOdd = Collatz2024.NextOdd(currOdd);
                    if (--n == 0)
                    {
                        Assert.True(currOdd == _TargetDecayInOneBigInt);
                        Assert.True(nextOdd == 1);
                        break;
                    }
                    currOdd = nextOdd;
                }
                BigEndEcho.Clear().Append(BigEndAnchor);
                for (int j = 0; j < _Trials; j++)
                {
                    BigEndEcho.Insert(0, echoPrefix);
                    currOdd = Collatz2024.toBigIntegerFromBinaryBigEndianString(BigEndEcho.ToString());
                    n = _DecayInN;
                    while (true)
                    {
                        nextOdd = Collatz2024.NextOdd(currOdd);
                        if (--n == 0)
                        {
                            Assert.True(currOdd == _TargetDecayInOneBigInt);
                            Assert.True(nextOdd == 1);
                            break;
                        }
                        currOdd = nextOdd;
                    }
                }
                string insertTxt = (i & 1) == trialParity ? mult2Add1Txt : mult32Add24Txt;
                BigEndAnchor.Insert(1, insertTxt);
            }
        }
        private bool getDecayInThree_Parameters(int _N, out string _ParamCsv)
        {
            _ParamCsv = "";
            BigInteger fourToNplus1 = BigInteger.Pow(4, _N + 1);
            Assert.True((fourToNplus1 - 1) % 3 == 0);
            BigInteger collapseInOne = (fourToNplus1 - 1) / 3;
            int collapseInOneMod3 = (int)(collapseInOne % 3);
            if (collapseInOneMod3 == 0)
                return false;
            BigInteger collapseInOneEcho = collapseInOne * (3 - collapseInOneMod3) * 2;
            Assert.True((collapseInOneEcho - 1) % 3 == 0);
            BigInteger collapseInTwo = (collapseInOneEcho - 1) / 3;
            int collapseInTwoMod3 = (int)(collapseInTwo % 3);
            if (collapseInTwoMod3 == 0)
            {
                Assert.True((4 * collapseInOneEcho - 1) % 3 == 0);
                collapseInTwo = (4 * collapseInOneEcho - 1) / 3;
            }
            collapseInTwoMod3 = (int)(collapseInTwo % 3);
            Assert.True(collapseInTwoMod3 != 0);
            BigInteger collapseInTwoEcho = collapseInTwo * (3 - collapseInTwoMod3) * 2;
            Assert.True((collapseInTwoEcho - 1) % 3 == 0);
            BigInteger collapseInThree = (collapseInTwoEcho - 1) / 3;
            _ParamCsv = $"decayInThreeList.Add(({collapseInOne}, {collapseInThree}, \"{Collatz2024.toBinaryBigEndianString(collapseInThree)}\"));";

            return true;
        }
        private BigInteger fnDecayToOneInOne(int _N)
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
            Assert.True(Collatz2024.NextOdd(decayInOne) == 1);
            Assert.True(decayInOne == (BigInteger.Pow(2, 2 * _N + 2) - 1) / 3);
            return decayInOne;
        }
        private BigInteger fnDecayToOneInTwo_Mod2(int _N)
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
            BigInteger decayInOne = Collatz2024.NextOdd(decayInTwo);
            Assert.True(Collatz2024.NextOdd(decayInOne) == 1);
            Assert.True(decayInTwo == (BigInteger.Pow(2, 6 * _N - 1) - 5) / 9);
            return decayInTwo;
        }
        private BigInteger fnDecayToOneInTwo_Mod1(int _N)
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
            BigInteger decayInOne = Collatz2024.NextOdd(decayInTwo);
            Assert.True(Collatz2024.NextOdd(decayInOne) == 1);
            Assert.True(decayInTwo == (BigInteger.Pow(2, 6 * _N + 4) - 7) / 9);
            return decayInTwo;
        }
        private int GetBitLength(BigInteger value)
        {
            int bitLength = 0;
            while (value > 0)
            {
                value >>= 1;
                bitLength++;
            }
            return bitLength;
        }
        #endregion Helper Methods
    }
}