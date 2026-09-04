using System.Globalization;
using System.Numerics;
using System.Text;

namespace HalHeinrich.Numerics.Collatz.Experiments;

/// <summary>
/// The unbounded halves of the suite's expensive controls.
/// </summary>
/// <remarks>
/// <para>
/// Each of these has a counterpart in Collatz.Tests that asserts the same property over
/// GatedOddScanLimit, so the gate stays in seconds. The sweep here runs the range the test
/// used to run - up to int.MaxValue, which is over an hour - and <b>reports</b> rather than
/// asserts. That is the point of the split: the claim is asserted in exactly one place, and
/// widening the range produces data about where the claim survives, not a second copy of it.
/// </para>
/// <para>
/// A sweep that finds nothing prints how far it got. A sweep that finds something prints the
/// first counter-example, which is the input the control's bound should be raised to cover.
/// </para>
/// </remarks>
internal static class DecaySweeps
{
    /// <summary>Range the moved sweeps cover, matching what the tests ran before they were bounded.</summary>
    private const int FullOddScanLimit = 10_000_000;

    /// <summary>The two decay-set sweeps ran an order of magnitude further still.</summary>
    private const int DeepOddScanLimit = 100_000_000;

    /// <summary>Idea2026's own range, over odd indices rather than odd values.</summary>
    private const int FullSeedIndexLimit = 75_000;

    /// <summary>
    /// Scans <paramref name="step"/>-spaced values below <paramref name="limit"/> and reports the
    /// first value for which <paramref name="violation"/> returns a reason, or that none did.
    /// </summary>
    private static void ReportScan(string label, int limit, int step, Func<int, string?> violation)
    {
        ArgumentNullException.ThrowIfNull(violation);
        Console.Error.WriteLine($"--- {label}: scanning to {limit:N0} step {step} ---");

        long examined = 0, counterExamples = 0;
        for (int n = 1; n > 0 && n < limit; n += step)
        {
            examined++;
            string? why = violation(n);
            if (why is null)
                continue;
            if (counterExamples == 0)
                Console.Out.WriteLine($"{label}: FIRST COUNTER-EXAMPLE at {n}: {why}");
            counterExamples++;
        }

        Console.Out.WriteLine(counterExamples == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{label}: no counter-example in {examined:N0} values below {limit:N0}")
            : string.Create(CultureInfo.InvariantCulture, $"{label}: {counterExamples:N0} counter-examples in {examined:N0} values below {limit:N0}"));
    }

    /// <summary>Every value decaying to one in two odd steps has a successor decaying in one.</summary>
    internal static void DecayInTwoSweep()
    {
        HashSet<BigInteger> inTwo = [], inOne = [];
        ReportScan("decay-in-two successor decays in one", DeepOddScanLimit, 1, n =>
        {
            if (CollatzMath.OddStepCountToOne(n) != 2 || (n & 1) == 0)
                return null;
            BigInteger next = CollatzMath.NextOdd(n);
            inTwo.Add(n);
            inOne.Add(next);
            return CollatzMath.OddStepCountToOne(next) == 1
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"successor {next} takes {CollatzMath.OddStepCountToOne(next)} steps, not 1");
        });
        EmitSet("decay-in-two members", inTwo);
        EmitSet("decay-in-one members reached from them", inOne);
    }

    /// <summary>Every value decaying in three odd steps passes through a two and then a one.</summary>
    internal static void DecayInThreeSweep()
    {
        HashSet<BigInteger> inThree = [], inTwo = [], inOne = [];
        ReportScan("decay-in-three successors decay in two then one", DeepOddScanLimit, 1, n =>
        {
            if (CollatzMath.OddStepCountToOne(n) != 3 || (n & 1) == 0)
                return null;
            BigInteger next2 = CollatzMath.NextOdd(n);
            BigInteger next1 = CollatzMath.NextOdd(next2);
            inThree.Add(n);
            inTwo.Add(next2);
            inOne.Add(next1);
            if (CollatzMath.OddStepCountToOne(next2) != 2)
                return string.Create(CultureInfo.InvariantCulture, $"first successor {next2} does not decay in two");
            return CollatzMath.OddStepCountToOne(next1) == 1
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"second successor {next1} does not decay in one");
        });
        EmitSet("decay-in-three members", inThree);
        EmitSet("decay-in-two members reached from them", inTwo);
        EmitSet("decay-in-one members reached from them", inOne);
    }

    /// <summary>The depth-one formulas agree with brute force.</summary>
    internal static void DecayViaFunctionIn1Sweep()
    {
        List<ICollatzDecayFormula> formulas =
        [
            new CollatzDecayFormulaRecursive(1, 2, 1),
            new CollatzDecayFormula(1, 2, 2, 1, 1),
            new CollatzDecayFormulaBitManipulation(1),
        ];
        ReportScan("depth-1 formulas vs brute force", FullOddScanLimit, 1, n => Disagreement(formulas, n, 1));
    }

    /// <summary>The depth-two formulas agree with brute force.</summary>
    internal static void DecayViaFunctionIn2Sweep()
    {
        List<ICollatzDecayFormula> formulas =
        [
            new CollatzDecayFormulaRecursive(2, 6, 35),
            new CollatzDecayFormulaRecursive(2, 6, 49),
            new CollatzDecayFormula(2, 6, -1, 5, 2),
            new CollatzDecayFormula(2, 6, 4, 7, 2),
            new CollatzDecayFormulaBitManipulation(2),
        ];
        ReportScan("depth-2 formulas vs brute force", FullOddScanLimit, 1, n =>
        {
            ulong steps = CollatzMath.OddStepCountToOne(n);
            int members = formulas.Count(f => f.IsMember(n));
            // Three of the five describe the same family from different directions, so a
            // decay-in-two value is expected in exactly three of them and no others.
            int expected = steps == 2 ? 3 : 0;
            return members == expected
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"{members} formulas claim it, expected {expected} (brute force says {steps} steps)");
        });
    }

    /// <summary>The depth-three bit-pattern formula agrees with brute force, over the full int range.</summary>
    internal static void DecayViaFunctionIn3Sweep()
    {
        CollatzDecayFormulaBitManipulation formula = new(3);
        ReportScan("depth-3 bit patterns vs brute force", int.MaxValue, 2, n =>
        {
            bool isMember = formula.IsMember(n);
            ulong steps = CollatzMath.OddStepCountToOne(n);
            return (steps == 3) == isMember
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"IsMember={isMember} but brute force says {steps} steps");
        });
    }

    /// <summary>
    /// How much of depth three the recursive construction covers, reported per bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the third block of what was Collatz.Tests' TestFunctionConstruction, moved here by
    /// halheinrich/Math#2. That method had never passed at any commit at which this project
    /// built: its first two blocks are controls with known answers and stayed behind as tests,
    /// and this one is unfinished exploration, so it reports and does not assert. Making it green
    /// would mean weakening it into something that no longer claims a partition, which would
    /// silence the finding rather than settle it.
    /// </para>
    /// <para>
    /// It is <b>not</b> a duplicate of <see cref="DecayViaFunctionIn3Sweep"/>. That one measures
    /// CollatzDecayFormulaBitManipulation(3), which covers depth three with nine hand-listed
    /// pattern families; this one measures the recursive construction at the same depth, which
    /// does not cover it. Same depth, the other implementation - and the disagreement between
    /// them is the evidence against the implicit 2^(d-1) model that halheinrich/Math#2 holds.
    /// </para>
    /// <para>
    /// The scan counts up in ones, so it visits evens; OddStepCountToOne strips factors of two,
    /// so each even aliases its odd core. That is the basis halheinrich/Math#2's table uses, and
    /// it is preserved so the two can be read against each other line for line.
    /// </para>
    /// </remarks>
    internal static void RecursiveConstructionDepthThreeCoverage()
    {
        CollatzDecayFormulaRecursive depthOne = new(1, 2, 1);
        // Declared as the indexed interface rather than ICollatzDecayFormula because these two seed
        // the depth-three derivations below, which walk NthMember.
        List<IIndexedCollatzDecayFormula> depthTwo =
        [
            new CollatzDecayFormulaRecursive(depthOne, 2),
            new CollatzDecayFormulaRecursive(depthOne, 1),
        ];
        List<ICollatzDecayFormula> depthThree =
        [
            new CollatzDecayFormulaRecursive(depthTwo[0], 2),
            new CollatzDecayFormulaRecursive(depthTwo[0], 1),
            new CollatzDecayFormulaRecursive(depthTwo[1], 2),
            new CollatzDecayFormulaRecursive(depthTwo[1], 1),
        ];

        int[] bounds = [1_000, 10_000, 100_000, 1_000_000];
        Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"--- depth-3 recursive construction: {depthThree.Count} formulas, scanning to {bounds[^1]:N0} ---"));

        StringBuilder csv = new();
        csv.AppendLine("Bound,DepthThreeValues,Covered,Missed,DoubleClaimed,FalsePositives,FirstMissed");

        int depthThreeValues = 0, covered = 0, missed = 0, doubleClaimed = 0, falsePositives = 0, firstMissed = 0;

        void Row(int bound) => csv.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{bound},{depthThreeValues},{covered},{missed},{doubleClaimed},{falsePositives},{firstMissed}"));

        int nextBound = 0;
        for (int odd = 1; odd < bounds[^1]; odd++)
        {
            if (nextBound < bounds.Length && odd == bounds[nextBound])
            {
                Row(bounds[nextBound]);
                nextBound++;
            }

            ulong steps = CollatzMath.OddStepCountToOne(odd);
            int claims = depthThree.Count(f => f.IsMember(odd));
            if (steps == 3)
            {
                depthThreeValues++;
                if (claims == 1)
                {
                    covered++;
                }
                else
                {
                    missed++;
                    if (claims > 1)
                        doubleClaimed++;
                    if (firstMissed == 0)
                        firstMissed = odd;
                }
            }
            else if (claims != 0)
            {
                falsePositives++;
            }
        }
        Row(bounds[^1]);

        Emit("depth-3 recursive-construction coverage", csv.ToString());
    }

    /// <summary>Odd-step count is invariant under reversing the 4n+1 map.</summary>
    internal static void FourNPlusOneSweep()
    {
        ReportScan("4n+1 reduction preserves the odd-step count", FullOddScanLimit, 2, n =>
        {
            BigInteger c = n;
            while ((c - 1) % 4 == 0 && c > 4)
            {
                --c;
                c >>= 2;
                if (c.IsEven)
                    break;
            }
            if (c == n || c.IsEven)
                return null;
            ulong here = CollatzMath.OddStepCountToOne(n), there = CollatzMath.OddStepCountToOne(c);
            return here == there
                ? null
                : string.Create(CultureInfo.InvariantCulture, $"reduces to {c}, which takes {there} steps rather than {here}");
        });
    }

    /// <summary>Every decay-in-one value is a member of the closed-form decay-in-one family.</summary>
    internal static void DecayAsExpectedSweep()
    {
        HashSet<BigInteger> inOne = [];
        for (int n = 0; ; n++)
        {
            BigInteger value = (BigInteger.Pow(2, 2 * n + 2) - 1) / 3;
            inOne.Add(value);
            if (value > FullOddScanLimit)
                break;
        }
        ReportScan("decay-in-one values lie in the closed form (2^(2n+2)-1)/3", FullOddScanLimit, 2, n =>
            CollatzMath.OddStepCountToOne(n) != 1 || inOne.Contains(n)
                ? null
                : "decays in one odd step but is not in the closed-form set");
    }

    /// <summary>Idea2026's full index range: the memoised seed table against brute force.</summary>
    internal static void SeedIndexSweep()
    {
        List<ulong> seedList = [0];
        List<BigInteger> thisOddList = [];
        int loopIdx = 0;
        while (true)
        {
            thisOddList.Clear();
            BigInteger nxtOdd = CollatzMath.OddOfIndex(loopIdx);
            thisOddList.Add(nxtOdd);
            while (true)
            {
                nxtOdd = CollatzMath.NextOdd(nxtOdd);
                if (nxtOdd == 1)
                {
                    for (int i = 0; i < thisOddList.Count; i++)
                        seedList[(int)CollatzMath.IndexOfOdd(thisOddList[i])] = (ulong)(thisOddList.Count - i);
                    break;
                }
                thisOddList.Add(nxtOdd);
                BigInteger nxtIdx = CollatzMath.IndexOfOdd(nxtOdd);
                while (nxtIdx >= seedList.Count)
                    seedList.Add(0);
            }
            if (loopIdx > FullSeedIndexLimit)
                break;
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

        long mismatches = 0, filled = 0;
        StringBuilder table = new();
        table.AppendLine("Index,Odd,MemoisedSteps");
        for (int i = 0; i < seedList.Count; i++)
        {
            if (seedList[i] == 0)
                continue;
            filled++;
            BigInteger odd = CollatzMath.OddOfIndex(i);
            ulong bruteForce = CollatzMath.OddStepCountToOne(odd);
            if (seedList[i] != bruteForce)
            {
                if (mismatches == 0)
                    Console.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"seed table: FIRST MISMATCH at index {i} (odd {odd}): memoised {seedList[i]}, brute force {bruteForce}"));
                mismatches++;
            }
            table.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{i},{odd},{seedList[i]}"));
        }
        Console.Out.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"seed table: {mismatches:N0} mismatches across {filled:N0} filled entries below index {FullSeedIndexLimit:N0}"));
        Emit("seed-index-table", table.ToString());
    }

    private static string? Disagreement(List<ICollatzDecayFormula> formulas, int n, ulong depth)
    {
        ulong steps = CollatzMath.OddStepCountToOne(n);
        int members = formulas.Count(f => f.IsMember(n));
        int expected = steps == depth ? formulas.Count : 0;
        return members == expected
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"{members} of {formulas.Count} formulas claim it, expected {expected} (brute force says {steps} steps)");
    }

    private static void EmitSet(string label, HashSet<BigInteger> values)
    {
        StringBuilder sb = new();
        foreach (BigInteger v in values)
            sb.AppendLine(v.ToString(CultureInfo.InvariantCulture));
        Emit(label, sb.ToString());
    }

    private static void Emit(string label, string csv)
    {
        Console.Error.WriteLine($"--- {label} ({csv.Length:N0} chars) ---");
        Console.Out.Write(csv);
    }
}
