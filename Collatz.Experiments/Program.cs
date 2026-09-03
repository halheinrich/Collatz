namespace HalHeinrich.Numerics.Collatz.Experiments;

/// <summary>
/// Entry point for the Collatz bench's experiments.
/// </summary>
/// <remarks>
/// Run one by name, or list what is available:
/// <code>
/// dotnet run --project Collatz.Experiments -- list
/// dotnet run --project Collatz.Experiments -- Pow2OddDecayCount &gt; pow2.csv
/// </code>
/// Nothing here has a pass or a fail, so the exit code says only whether the named
/// experiment was found and ran to completion. See <see cref="DecayExperiments"/> for why
/// this is not a test project.
/// </remarks>
internal static class Program
{
    private static readonly (string Name, Action Run)[] Catalogue =
    [
        ("Pow2OddDecayCount",               DecayExperiments.Pow2OddDecayCount),
        ("TwoToTheNPlusOne",                DecayExperiments.TwoToTheNPlusOne),
        ("TwoToTheNPlusOneFormula",         DecayExperiments.TwoToTheNPlusOneFormula),
        ("PowerOfTwoPlusConstantConst",     DecayExperiments.PowerOfTwoPlusConstantConst),
        ("PowerOfTwoPlusConstantSurvivors", DecayExperiments.PowerOfTwoPlusConstantSurvivors),
        ("DeriveDecayInNFormula",           DecayExperiments.DeriveDecayInNFormula),
        ("DecayInTwoSweep",                 DecaySweeps.DecayInTwoSweep),
        ("DecayInThreeSweep",               DecaySweeps.DecayInThreeSweep),
        ("DecayViaFunctionIn1Sweep",        DecaySweeps.DecayViaFunctionIn1Sweep),
        ("DecayViaFunctionIn2Sweep",        DecaySweeps.DecayViaFunctionIn2Sweep),
        ("DecayViaFunctionIn3Sweep",        DecaySweeps.DecayViaFunctionIn3Sweep),
        ("FourNPlusOneSweep",               DecaySweeps.FourNPlusOneSweep),
        ("DecayAsExpectedSweep",            DecaySweeps.DecayAsExpectedSweep),
        ("SeedIndexSweep",                  DecaySweeps.SeedIndexSweep),
    ];

    private static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != 1 || args[0] == "list" || args[0] == "--help")
        {
            Console.Error.WriteLine("Collatz experiments - runs whose answers are not known in advance.");
            Console.Error.WriteLine("Data goes to stdout; labels and progress go to stderr.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("usage: dotnet run --project Collatz.Experiments -- <name>");
            Console.Error.WriteLine();
            foreach ((string name, _) in Catalogue)
                Console.Error.WriteLine("  " + name);
            return args.Length == 1 && (args[0] == "list" || args[0] == "--help") ? 0 : 2;
        }

        foreach ((string name, Action run) in Catalogue)
        {
            if (!string.Equals(name, args[0], StringComparison.OrdinalIgnoreCase))
                continue;
            Console.Error.WriteLine($"running {name} ...");
            run();
            return 0;
        }

        Console.Error.WriteLine($"no experiment named '{args[0]}' - try 'list'.");
        return 2;
    }
}
