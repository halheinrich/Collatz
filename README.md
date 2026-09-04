# Collatz

A .NET 10 exploration of **Collatz decay formulas**: descriptions of the odd
integers that reach 1 in a fixed number of odd steps, where an *odd step* is
`n -> 3n + 1` followed by dividing out every factor of two.

The odd numbers decaying in one step are 1, 5, 21, 85, 341, …; in two steps,
3, 13, 53, 113, 213, 227, …. This repository asks how those families can be
*described* — by a recurrence, by a closed form, by a bit pattern — and checks
each description against brute force.

## What this is not

**Not a rational-arithmetic library.** Arithmetic here is `BigInteger`
throughout. `BigRational` appears at exactly one point in the public surface:
`SolveForLoop`, which solves for the rational N implied by a hypothetical cycle
of odd steps and reports whether that N is a positive integer. That is the
whole of the dependency. Whether this member should be a rational-arithmetic
consumer at all is an open question in the umbrella
([halheinrich/Math#13](https://github.com/halheinrich/Math/issues/13)).

**Not a proof of anything.** Every claim here is checked over a bounded range.
A description that survives to 100,000 has survived to 100,000.

## The contract and its three implementations

`ICollatzDecayFormula` is one small contract — how many odd steps its members
take to reach one, whether a given value is a member, and the nth member:

```csharp
public UInt32 StepsToOne { get; }
public bool IsMember(BigInteger c);
public BigInteger NthMember(int n);
```

Three implementations describe the same families in unrelated ways, which is
what makes them worth cross-checking:

| implementation | describes a family as |
| --- | --- |
| `CollatzDecayFormulaRecursive` | a recurrence, `f(i) = 2^E * f(i-1) + A`, from a seed anchor |
| `CollatzDecayFormula` | a closed form, `[2^(Fn + C) - S] / 3^E` |
| `CollatzDecayFormulaBitManipulation` | a prefix / repeat / suffix pattern over base-2 digits |

They do not all cover the same ground. The recursive derivation is *sound* —
it never claims a non-member — but at depth three it is *incomplete*: four
derived families cover 20 of the 90 values below 2,000,000 that decay in three
odd steps, while the bit-pattern implementation covers all 90. `dotnet test`
therefore ends with **one deliberate failure**, `TestFunctionConstruction`,
which is reporting that gap rather than suffering from a bug. It is tracked as
[halheinrich/Math#2](https://github.com/halheinrich/Math/issues/2) and is left
red on purpose: a red test telling the truth beats a green one that does not.

## Projects

- **`Collatz`** — the library. `CollatzMath` holds the domain helpers
  (`NextOdd`, `OddStepCountToOne`, the odd-index bijection, base-2 string
  conversions, `SolveForLoop`); the three formula types sit beside it.
- **`Collatz.Tests`** — xUnit. **This is the project that gates.** Every test
  in it asserts a known answer and the whole suite runs in about five seconds,
  because a suite that takes an hour gates nothing.
- **`Collatz.Experiments`** — a console app, and **deliberately not a test
  project**. It holds the runs whose answers are not known in advance: the
  unbounded sweeps behind the bounded controls, and the table-producing
  explorations. Nothing in it asserts; its output is data.

  ```powershell
  dotnet run --project Collatz.Experiments -- list
  dotnet run --project Collatz.Experiments -- DecayViaFunctionIn3Sweep > sweep.txt
  ```

  Data goes to stdout and labels to stderr, so a redirect captures the table
  alone. If something there starts wanting an assertion, it has stopped being
  an experiment and belongs in `Collatz.Tests`.

## Building

**This repository does not build standalone.** It references
`BigRationalLibrary` by `ProjectReference`, and that reference escapes the
repo:

```
..\..\BigRationalLibrary\BigRationalLibrary\BigRationalLibrary.csproj
```

That resolves only when this checkout sits beside a `BigRationalLibrary`
checkout, as it does inside the umbrella:

```
Math/
  BigRationalLibrary/
  Collatz/                  <- here
```

A clone of this repository alone cannot restore, and will fail at restore
rather than at compile. This is the accepted price of the umbrella's
`ProjectReference` ruling, not an oversight.

```powershell
dotnet build
```

## Test

```powershell
dotnet test
```

One test fails deliberately, for the reason described above; everything else is
green. The suite builds and runs identically in Debug and Release.

## Licence

MIT — see [LICENSE](LICENSE).
