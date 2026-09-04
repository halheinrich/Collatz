# Collatz

> Collaboration contract → `../AGENTS.md`.
> Cross-cutting status & dependency graph → `../INSTRUCTIONS.md`.
> Mission, principles & repo conventions → `../VISION.md`.

The deep working reference for this submodule.

## Stack

A C# class library, its xUnit test project and a console experiments project;
language version, target framework and namespace conventions are umbrella-wide
and live in `../VISION.md` and `Directory.Build.props`.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\Math\Collatz\Collatz.sln`

## Repo

`https://github.com/halheinrich/Collatz`, branch `main`.

## Depends on

- **BigRationalLibrary** — `HalHeinrich.Numerics.BigRational`, and **the edge is
  one method wide**. `BigRational` reaches the public surface at
  `CollatzMath.SolveForLoop`'s `out` parameter and nowhere else, using `Zero`,
  `Create(BigInteger, BigInteger)`, `IsInteger`, and comparison against zero.
  Arithmetic everywhere else in this repository is `BigInteger`. That narrowness
  is the settled shape of this member rather than a gap in it: the umbrella's
  invariant is exactness with tracked error, and this member satisfies it with a
  different instrument, because its objects are integers. Rational arithmetic is
  what that invariant needs for *real constants*; it is not the mission
  (halheinrich/Math#13). So the edge is expected to stay one method wide, and it
  is correct rather than decorative — the `n` it solves for genuinely is a
  rational, and the alternatives are floating point, which is banned, or a
  hand-rolled fraction type, which would duplicate the substrate.

  By `ProjectReference`, per the umbrella's ruling on intra-umbrella edges, at
  `..\..\BigRationalLibrary\BigRationalLibrary\BigRationalLibrary.csproj`. That
  path **escapes this repository** — see § Pitfalls.

## Layout

- **`Collatz`** — the library. `CollatzMath` holds the domain helpers, and the
  three `ICollatzDecayFormula` implementations sit beside it.
- **`Collatz.Tests`** — xUnit, and **the project that gates**. Every test in it
  asserts an answer known in advance. It sees `internal` members by
  `InternalsVisibleTo`.
- **`Collatz.Experiments`** — a console app, and **deliberately not a test
  project**. Its output is data. That distinction is load-bearing rather than
  tidy; § Architecture says why.

## Architecture

### One contract, three unrelated descriptions

`ICollatzDecayFormula` describes a family of odd integers that reach one in a
fixed number of *odd steps*, an odd step being `n -> 3n + 1` followed by
dividing out every factor of two. Three implementations describe the same
families in unrelated ways:

| implementation | describes a family as |
| --- | --- |
| `CollatzDecayFormulaRecursive` | a recurrence, `f(i) = 2^E * f(i-1) + A`, from a seed anchor |
| `CollatzDecayFormula` | a closed form, `[2^(Fn + C) - S] / 3^E` |
| `CollatzDecayFormulaBitManipulation` | a prefix / repeat / suffix pattern over base-2 digits |

Being unrelated is the point. Agreement between a recurrence, a closed form and
a bit pattern is evidence about the families; agreement between two variants of
one derivation is evidence about the derivation. A fourth description is
welcome; a refactor that makes two of these share a derivation is not.

They do not cover the same ground, and the gap is measured rather than hidden —
`RecursiveConstructionDepthThreeCoverage` in `Collatz.Experiments` reports it as
data, because what the right answer would be is halheinrich/Math#2's open model
question. § Pitfalls says why that is an experiment and not a test.

### Two projects, split on whether the answer is known in advance

**A target whose answer is known in advance belongs in `Collatz.Tests`; a target
whose answer is not belongs in `Collatz.Experiments`.** That is the rule the
three-project shape exists to enforce, and it is not a matter of taste.
`Collatz.Experiments` holds both halves of the history that produced it.
`DecayExperiments` was a set of `[Fact]` methods that each built a table,
dropped it, and asserted `Assert.True(true)` — reporting coverage they did not
have. `DecaySweeps` is the unbounded half of controls that did assert something
real, at ranges that took the gating suite past an hour. Both are failures of
the same kind: a green report over nothing checked, and a gate too slow to be
run.

**Nothing in `Collatz.Experiments` may assert.** Its output is data: tables to
stdout, labels and progress to stderr, so a redirect captures the table alone.
A method there that starts wanting an assertion has stopped being an experiment
and belongs in `Collatz.Tests` — moved, not copied, so the claim keeps living in
exactly one place. `Program` dispatches by name, and its exit code says only
whether the named experiment was found and ran.

### The gate is bounded by named constants

`Collatz.Tests` bounds its scans with `GatedOddScanLimit` and
`GatedSeedIndexLimit`, named rather than written as literals at each use. The
scans behind them once ran to 10,000,000, 100,000,000 or `int.MaxValue`, which
put the suite past an hour and so stopped it gating anything. What each test
claims is unchanged; only the range it is checked over is, and each constant's
comment records the coverage the bound still buys.

**Raising a bound is moving a claim, not widening a gate.** Each bounded control
has an unbounded counterpart in `Collatz.Experiments`' `DecaySweeps` that runs
the range the test used to run and *reports* instead of asserting. A reader who
wants a claim checked further runs the sweep; a reader who edits the constant
upward has traded the gate's usefulness for coverage that was already available
elsewhere.

### `CollatzMath`

The domain helpers, all `static`, all `BigInteger` but for the one `BigRational`
noted in § Depends on. `NextOdd` is the odd step itself, and
`OddStepCountToOne` and `OddStepCountToSmaller` count applications of it.
`GenerateExponentTuplesWithMax` enumerates the exponent arrays the formulas are
indexed by - those of a given length over `[1, maxExponent]` in which
`maxExponent` actually appears, so tuples with repetition rather than
permutations, and fewer than all tuples over that range - materialised into a
`List<int[]>` with an exactly computed capacity hint. `SolveForLoop` solves the
rational `N` implied by a hypothetical cycle of odd steps and reports whether
that `N` is a positive integer; it is the one member that reports invalid input
through its return value rather than by throwing, because a caller sweeping
exponent arrays wants a verdict per array rather than an exception per malformed
one.

Neither step counter returns for an argument whose orbit never terminates. That
is the Collatz conjecture, not a missing guard, and the distinction is worth
keeping straight: the guards below exist for arguments that are *nonsense*, not
for arguments that are *hard*.

### The odd-index bijection has a domain, and the domain is checked

`IndexOfOdd` and `OddOfIndex` are inverses over the odd integers 3, 5, 7, 9, …
and their zero-based positions. Both check their domain rather than assuming it,
because the two predecessors of this pair were silently wrong outside it:

- The arithmetic alone fails quietly. Measured on SDK 10.0.400, the unguarded
  `(odd - 3) / 2` maps an even argument to the index of the odd value below it —
  4 to 0, 6 to 1, with no error — and an argument below three to a negative
  quotient whose conversion throws `OverflowException`, which tells a caller
  nothing about what it did wrong.
- `OddOfIndex` takes a `BigInteger` deliberately. The test-local version it
  replaced took an `int` and evaluated `2 * index + 3` in `int` before widening,
  so index 1073741823 returned -2147483647 instead of 2147483649, silently.

A helper that is wrong outside its domain and silent about it is worse than one
that is absent, which is why this pair moved into the product with a domain
rather than staying a test-local convenience.

### The collapse family's bounds are not uniform, and two were wider than the family

`CollapseInOne`, its two `ModOut` refinements and the two `CollapseInTwo`
methods each reach `BigInteger.Pow` with an exponent derived from an argument,
and until halheinrich/Math#27 that call was the only thing checking any of
them — so a rejection surfaced as an `ArgumentOutOfRangeException` naming
`exponent`, a parameter of an internal call the caller never passed. Each
method checks its own arguments now and names them.

The bounds are worth stating because they differ. `CollapseInOne` starts at
one; `CollapseInOneModOneOut` at zero, because `3n + 1` already selects
`CollapseInOne(1)` there; `CollapseInOneModTwoOut` and both `CollapseInTwo`
methods at one. Each has an upper bound too — `(int.MaxValue - 1) / 3` for a
`CollapseInOne` index, `(int.MaxValue - 1) / 2` for a `CollapseInTwo`
half-exponent — because the exponent arithmetic is unchecked `int`.

**Two of them were wider than the family they enumerate, and silent about it**,
which is the odd-index pair's failure again. Measured on SDK 10.0.400:

- `CollapseInOne(0)` returned 0, a value outside the sequence its own summary
  describes.
- `CollapseInTwoModOne` accepted `n2 = 0` and returned an even value that
  decays in neither two odd steps nor any small count — 28 for `n1 = 1`, which
  takes five; 1820 for 2, twelve; 116508 for 3, thirty-four. It also accepted
  `n1 = 0`, whose `v` is the fixed point 1, so what came back were one-step
  decayers rather than two-step ones. **`CollapseInTwoModTwo` rejected both
  cases already, but only because its exponent `2n2 - 1` goes negative where
  `2n2` goes to zero.** That accident is the whole difference between the two
  siblings, and it is why only one of them answered wrongly.
- At the upper end nothing checked either method:
  `CollapseInOneModOneOut(1431655766)` wrapped to an exponent of 3 and returned
  21, and `(1431655765)` wrapped to 0 and returned 0.

### `FloorLog2Ratio` is exact integer arithmetic, and internal

`FloorLog2Ratio` returns the exact floor of `log2(a / b)` for positive `a` and
`b` without leaving integer arithmetic. It replaced a `double` / `Math.Log`
derivation, which is an exactness violation under `../AGENTS.md` § Exactness
discipline.

**The bit-length difference alone is not the answer.** For positive `a` and `b`,
`a` lies in `[2^(la-1), 2^la)` and `b` in `[2^(lb-1), 2^lb)`, so `a/b` lies in
`(2^(la-lb-1), 2^(la-lb+1))` and the floor is either `la-lb` or one less. Which
of the two it is cannot be read off the lengths; it is settled by the exact
comparison `a >= b * 2^(la-lb)`, written as a shift so that no division and no
rounding takes place.

It is `internal` because it is a derivation detail of
`CollatzDecayFormulaRecursive` rather than part of what this library offers. The
tests reach it through `InternalsVisibleTo` on `Collatz.Tests`, which is the
intended route: it is checked against an independent reference implementation
and against the specific cases the `double` derivation got wrong.

### `Debug.Assert` vanishes in Release, so an invariant asserted is not checked

A conditional call and *its arguments* are both removed in a Release build. That
is why four checks in `CollatzMath` now throw rather than assert, each carrying a
remark recording what its assertion had been hiding, measured on SDK 10.0.400:

- `OddStepCountToOne` — positivity. Zero drove the strip loop forever, because
  `0 >> 1` is 0 and 0 is even; a negative value reached -1, which `NextOdd` maps
  back to -1. Release simply did not return.
- `OddStepCountToSmaller` — positivity again, hiding something different. Zero
  hangs the same way, but a negative argument usually hangs in the step loop
  instead, and sometimes does not hang at all: -17 returned 1, because -25
  genuinely is less than -17. A plausible number for a nonsensical argument is
  worse than a hang, so the guard is on the argument rather than on the loop.
- `CollapseInTwoModOne` and `CollapseInTwoModTwo` — a congruence whose assertion
  carried the subtraction in its own argument (`--retval % 3 == 0`), so check
  and decrement vanished together, and the returned value differed between
  configurations for `v` a multiple of three.

**The recursive constructors have since had the same treatment**
(halheinrich/Math#28). Their parameter checks are now `ArgumentOutOfRangeException`
and `ArgumentException`, both derivation failures are `InvalidOperationException`,
and one invariant was deleted rather than converted, under a proof that it can
never fire. No `Debug.Assert` **call** remains anywhere in `Collatz/Collatz.cs`:
the six occurrences of the name still in that file are `<see cref>` references in
XML docs, each recording what the check it sits on used to be. `Collatz.Experiments`
still calls `Debug.Assert` in five places, which is not the same defect — it is a
reporting harness whose output a human reads, not the product.

## Public API

Namespace `HalHeinrich.Numerics.Collatz`.

```csharp
public static class CollatzMath
{
    public static IReadOnlyList<int[]> GenerateExponentTuplesWithMax(int length,
                                                                    int maxExponent);
    public static BigInteger NextOdd(BigInteger collatz);

    // inverses over the odd integers 3, 5, 7, ... and their zero-based positions
    public static BigInteger IndexOfOdd(BigInteger odd);    // odd, >= 3
    public static BigInteger OddOfIndex(BigInteger index);  // >= 0

    // lower bounds differ across the family; upper bounds keep the exponent an int
    public static BigInteger CollapseInOne(int n);            // (4^n - 1) / 3,      n >= 1
    public static BigInteger CollapseInOneModOneOut(int n);   // (4^(3n+1) - 1) / 3, n >= 0
    public static BigInteger CollapseInOneModTwoOut(int n);   // (4^(3n-1) - 1) / 3, n >= 1
    public static BigInteger CollapseInTwoModOne(int n1, int n2);   // n1 >= 1, n2 >= 1
    public static BigInteger CollapseInTwoModTwo(int n1, int n2);   // n1 >= 1, n2 >= 1

    // base-2 string conversions; each writer pairs with the reader of its own order
    public static string ToBinaryLittleEndianString(BigInteger bigInt);          // lsb first
    public static string ToBinaryLittleEndianStringGtInt64(BigInteger bigInt);   // lsb first
    public static string ToBinaryBigEndianString(BigInteger bigInt);             // msb first
    public static string ToBinaryBigEndianStringGtInt64(BigInteger bigInt);      // msb first
    public static BigInteger ToBigIntegerFromBinaryLittleEndianString(string s); // reads lsb first
    public static BigInteger ToBigIntegerFromBinaryBigEndianString(string s);    // reads msb first

    // 1, 101, 10101, ... - all palindromes, so this one asserts no digit order
    public static IEnumerable<string> GetDecayInOneBitPatterns();  // endless

    public static UInt64 OddStepCountToOne(BigInteger n);      // n > 0
    public static UInt64 OddStepCountToSmaller(BigInteger n);  // n > 0

    public static ulong DecayInNFormulaList(int c, CollatzDecayFormulaRecursive f);
    public static bool SolveForLoop(int[] twosExponentArray, out BigRational n);

    internal static int FloorLog2Ratio(BigInteger numerator, BigInteger denominator);
}
```

`GenerateExponentTuplesWithMax`, `IndexOfOdd`, `OddOfIndex`, both step counters,
all five `Collapse` methods and `FloorLog2Ratio` throw
`ArgumentOutOfRangeException` outside their domains.
`CollapseInTwoModOne` and `CollapseInTwoModTwo` throw
`InvalidOperationException` when the value they are about to divide is not
congruent to 1 modulo 3. `SolveForLoop` throws nothing: it returns `false` with
`n` set to `BigRational.Zero`.

```csharp
public interface ICollatzDecayFormula
{
    UInt32 StepsToOne { get; }
    bool IsMember(BigInteger c);
}

// Enumeration is separate because not every family here can do it: membership
// by digit pattern runs in one direction only. See § Pitfalls.
public interface IIndexedCollatzDecayFormula : ICollatzDecayFormula
{
    BigInteger NthMember(int n);   // index order is the implementation's own
}

public class CollatzDecayFormulaRecursive : IIndexedCollatzDecayFormula
{
    public BigInteger PowerOfTwo { get; }      // 2^TwosExponent
    public Int32 TwosExponent { get; }
    public Int64 AdditiveConstant { get; }

    // explicit recurrence; seeds the anchor list from a known first member
    public CollatzDecayFormulaRecursive(UInt32 stepsToOne, Int32 twosExponent,
                                        Int64 additiveConstant);

    // derives from a predecessor by walking ITS NthMember - see § Pitfalls
    public CollatzDecayFormulaRecursive(IIndexedCollatzDecayFormula predecessorFormula,
                                        int modThree);

    public override string ToString();   // "f(n) = 2^E * f(n-1) + A"
}

public class CollatzDecayFormula : IIndexedCollatzDecayFormula
{
    public int NFactor { get; }
    public int NConstant { get; }
    public Int32 ThreesExponent { get; }
    public BigInteger PowerOfThree { get; }    // 3^ThreesExponent
    public Int64 SubtractiveConstant { get; }

    public CollatzDecayFormula(UInt32 stepsToOne, int nFactor, int nConst,
                               Int64 subtractiveConstant, Int32 threesExponent);

    public override string ToString();   // "f(n) = [2^(Fn+C) - S] / 3^E"
}

// Membership only, deliberately: it matches digit patterns, and nothing in it
// generates the values those patterns accept.
public class CollatzDecayFormulaBitManipulation : ICollatzDecayFormula
{
    public CollatzDecayFormulaBitManipulation(UInt32 stepsToOne);   // 1..3 only

    public override string ToString();   // "decay in N odd steps, decided by ..."
}
```

`IsMember` returns `false` for any `c` below one on all three.
`CollatzDecayFormulaBitManipulation` throws no `NotImplementedException`, and has
no member that could: halheinrich/Math#24 removed `NthMember` from the type when
it split the interface, and halheinrich/Math#36 made the constructor reject any
`StepsToOne` outside 1..3, which left the `IsMember` default arm unreachable and
it went with them. No `throw` of `NotImplementedException` survives in the
product — the two occurrences of the name in `Collatz.cs` are `<see cref>` doc
references recording exactly that history.

The two formula-printing `ToString` overrides — `CollatzDecayFormulaRecursive`'s
and `CollatzDecayFormula`'s; the third prints a pattern description, not a
formula — take each constant's operator from that constant's own sign, and drop
a term whose constant is zero. So `A` negative prints
`f(n) = 2^6 * f(n-1) - 5` and `A` zero prints `f(n) = 2^6 * f(n-1)`; `C`
negative prints `[2^(6n-1) - 5] / 3^2`, `S` negative prints
`[2^(6n+4) + 5] / 3^2` because subtracting a negative adds it, and both zero
print `[2^(6n)] / 3^2`. The shapes commented above are those renderings at
positive constants. One internal helper carries the rule for all five sites that
print a formula - the two `ToString`s and the three exception messages - so a
correction cannot land on some of them and leave the rest to diverge
(halheinrich/Math#31).

## Pitfalls

- **This repository does not build standalone.** The `ProjectReference` to
  `BigRationalLibrary` escapes the repo and resolves only when this checkout
  sits beside a `BigRationalLibrary` checkout, as it does inside the umbrella. A
  clone of this repository alone fails at *restore*, not at compile. That is the
  accepted price of the `ProjectReference` ruling, not an oversight. The escape
  survived the migration into the umbrella unedited only because
  `Projects\X\Proj` and `Math\X\Proj` are equal depth — a coincidence of layout,
  not a property to rely on.
- **The recursive derivation is sound but incomplete at depth three, and that
  is measured, not asserted.** It never claims a non-member; it misses. Four
  derived families cover 20 of the 90 values below 2,000,000 that decay in three
  odd steps, where the bit-pattern implementation covers all 90.
  halheinrich/Math#2 split the test that used to assert otherwise: its depth-one
  and depth-two blocks were controls and stayed as
  `TestRecursiveFormulaConstruction_DepthOnePartitionsBelowScanLimit` and its
  depth-two sibling, and its depth-three block became
  `RecursiveConstructionDepthThreeCoverage` in `Collatz.Experiments`, which
  emits coverage per bound and has no pass or fail. **Do not turn it back into
  an assertion that passes.** An assertion that no longer claims a partition
  would report coverage the recursion does not have, which is worse than the
  red it replaced. The suite therefore has no deliberate failure;
  halheinrich/Math#2 stays open on the model question, which no test result
  settles.
- **`NthMember` is not on the shared contract, and index zero is not guaranteed
  to be a member.** halheinrich/Math#24 moved enumeration to
  `IIndexedCollatzDecayFormula`, which `CollatzDecayFormulaRecursive` and
  `CollatzDecayFormula` implement and `CollatzDecayFormulaBitManipulation` does
  not — it decides membership by matching base-2 digit patterns, and nothing in
  it generates the values those patterns accept. Both surviving implementations
  work: `CollatzDecayFormula.NthMember` is the closed form rather than the stub
  that used to return zero, and it throws `ArgumentOutOfRangeException` on a
  negative index or exponent and `InvalidOperationException` when its division is
  not exact, rather than letting `BigInteger` truncate and returning something
  indistinguishable from a member. What a caller must still do is **filter**. The
  index order is each implementation's own and index zero can fall outside the
  family, or outside the integers altogether: `[2^(6n-1) - 5] / 3^2` has its
  first member at `n` of one, so index zero throws. The derivation constructor
  filters exactly this way, and because it now takes an
  `IIndexedCollatzDecayFormula`, seeding it with a bit-pattern formula is a
  compile error rather than a runtime surprise. halheinrich/Math#35 tracks the
  indexing contract this leaves unshared.
- **That constructor's walk is unbounded, and that is the live hazard.** It loops
  on `while (true)` with no iteration cap, so a predecessor that never yields
  enough qualifying members does not return. Nothing tracks this; it is the one
  thing in the derivation a reader still has to know. What is **no longer** true:
  the checks inside the two recursive constructors were `Debug.Assert` until
  halheinrich/Math#28 converted them, so a Release build no longer proceeds
  silently from a failed consistency check — both derivation failures throw
  `InvalidOperationException` in every configuration. The XML remarks on those
  constructors cite halheinrich/Math#28 and no longer cite the closed
  halheinrich/Math#6.
- **`RestoreLockedMode` is dormant here.** `Directory.Build.props` gates it on
  `ContinuousIntegrationBuild`, and with no workflow in this repository nothing
  sets that, so a `packages.lock.json` out of step with a `.csproj` fails
  nothing today. It becomes live the moment a workflow lands, which is when
  committing the regenerated lock file starts to matter (halheinrich/Math#29).
- **xUnit v2 discovers only public test classes**, so CA1515 ("types can be made
  internal") is off for test files in `.editorconfig`. Complying with it would
  not fail the build — it would discover nothing and report green, which is why
  the rule is suppressed rather than satisfied.
- **`nuget.config` deliberately omits `packageSourceMapping`** where the other
  members carry it. The reasoning is measured, and written at the point of edit
  in that file; read it before harmonising the four.

## Subproject-internal next steps

The open backlog for this member lives in the umbrella tracker, and is
deliberately not enumerated here. It used to be, and that enumeration was a
drift *generator* rather than a drifted instance: it named halheinrich/Math#24
and halheinrich/Math#28 as open, and became false the moment they closed, with
no mechanism that could have noticed. Correcting the list would only have reset
the clock on the same failure. This is the defect class halheinrich/Math#41
names, and the reason `AGENTS.md` § Subproject INSTRUCTIONS.md standard forbids
drift-prone duplicative content: a member doc restating umbrella state has no
way to stay true.

Query the tracker for what is open against this member. § Pitfalls above is
written to be read without it — it states what the code does today, not which
issues are outstanding.

Cross-cutting items are `../INSTRUCTIONS.md`'s — among them halheinrich/Math#13,
which asks whether this member is the rational-arithmetic consumer its place in
the graph implies, and the workflow this repository does not yet have.
