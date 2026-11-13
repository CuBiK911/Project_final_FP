# Assignment 5: Composing Functions Using Pipelining

## Project Workflow
**Topic:** University Application Scoring System
**Intended Users:** University Admissions Office

### Business Rules
1. **Validation:** Age must be a valid integer, GPA must be a decimal between 0.0 and 4.0, Hours must be a valid integer, and Criminal Record must be a boolean. Missing or malformed fields are rejected.
2. **Eligibility (Stage 1):** Applicants must be between 18 and 25 years old (inclusive) and have no criminal record.
3. **Scoring (Stage 2):** 
   - *Academic Policy:* (GPA * 200) + (Hours * 1)
   - *Holistic Policy:* (GPA * 100) + (Hours * 5)
4. **Result (Stage 3):** Applicants are ranked by score descending, and a summary is generated.



### Dataset & Expected Results
1. Alice (20, 3.0 GPA, 110 hrs, no record) -> **Accepted**. Academic: 710, Holistic: 850.
2. Bob (17, 3.5 GPA, 10 hrs, no record) -> **Rejected** (Age < 18).
3. Charlie (21, 4.0 GPA, 0 hrs, no record) -> **Accepted**. Boundary value (GPA=4.0). Academic: 800, Holistic: 400.
4. Diana (22, -1.0 GPA, 15 hrs, no record) -> **Rejected** (Invalid GPA).
5. Eve (21, 3.2 GPA, 50 hrs, criminal record) -> **Rejected** (Criminal record).


## Requirement Mapping
- **Workflow & Composition:** `Workflow.Run` in `Program.cs` composes `FilterEligible`, `ComputeScores`, and `GenerateReport`.
- **Functional Separation:** `RawApplication` and `ValidApplication` separate raw text from domain data. No mutable state is used.
- **Configurable Behaviour:** `ComputeScores` is a higher-order function accepting a `Func<ValidApplication, decimal>` delegate.
- **Tests:** `PipelineTests.cs` contains 6 tests covering all required cases.

## Test Cases & Outcomes
1. `NormalWorkflow_ProducesDocumentedResult`: **Passed**
2. `EmptyDataset_ProducesEmptyResultWithoutCrashing`: **Passed**
3. `BoundaryValue_FollowsInclusionRule`: **Passed**
4. `InvalidAndMissingInput_AreRejected`: **Passed**
5. `TwoPolicies_ProduceDifferentResults`: **Passed**
6. `PurityAndStages_CallerOwnedInputRemainsUnchanged`: **Passed**

## Assignment 6: Explicit States and Safe Transitions


**Unsafe Before Example**

In Assignment 5, the workflow had a hidden dependency between stages. The `ComputeScores` method was called directly after parsing, without first passing the data through `FilterEligible`.

**The Problem:**

- `ComputeScores` assumes input has already been filtered by `FilterEligible`
- If the filter step is omitted, ineligible applicants (underage, criminal record) receive scores
- No compile-time or runtime enforcement of the required order
- Observable result: Criminal applicant appears in admission report with a score

**Factory Implementation**

`ApplicationFactory.Create` enforces all domain rules before returning a usable value. It performs five checks: name is required, age must be a valid integer in the range 18 to 25, GPA must be a valid decimal between 0.0 and 4.0, hours must be a non-negative integer, and the criminal record flag must be a valid boolean set to false.

**Access Restrictions:**

- `ValidatedApplication` constructor is internal
- External callers cannot bypass the factory
- Only the factory (and tests via InternalsVisibleTo) can construct validated instances
- Partial construction never escapes as a valid state

**Validation Outcome Model**

The `ValidationOutcome` is an abstract record with two sealed derived types: `Valid` and `Invalid`.

**Mutual Exclusivity:**

- An outcome is either Valid OR Invalid, never both
- Each case carries only the data relevant to that case
- Pattern matching must handle both cases explicitly

**Workflow State Model**

The `WorkflowState` is an abstract record with three sealed derived types: `Pending`, `Confirmed`, and `Rejected`.

**Cases:**

- **Pending:** Application awaiting decision
- **Confirmed:** Application approved with score
- **Rejected:** Application denied with reason

**Validation Handling**

A switch statement handles every case explicitly. The Valid case adds the application to the valid list, the Invalid case logs the rejection reasons, and the default case throws an exception for unexpected outcomes.


**Compile-Time Enforcement**

The `Score` method only accepts `ValidatedApplication` as its first parameter. This prevents invalid sequences at compile time because raw applications cannot be passed directly to the scoring method.

**This prevents invalid sequences at compile time:**

- Attempting to score a raw application results in a compilation error
- The caller must first validate the application using the factory
- Only validated applications can be scored

**Invalid Cases:**

1. **Missing Prerequisite:** Cannot score without validating (compile-time error)
2. **Invalid Sequence:** Cannot confirm a Rejected state (returns Rejected unchanged)


### Requirement Mapping (Assignment 6)

| Requirement | File | Method |
|-------------|------|--------|
| Hidden dependency | `README.md` | "Unsafe Before Example" |
| Factory with checks | `ApplicationFactory.cs` | `Create` (5 rules) |
| Prevent unchecked construction | `Models.cs` | `ValidatedApplication` internal constructor |
| Discriminated union | `Models.cs` | `ValidationOutcome`, `WorkflowState` |
| Pattern matching | `SafeWorkflow.cs`, `ConfirmationWorkflow.cs` | `switch` statements/expressions |
| Safe transitions | `ConfirmationWorkflow.cs` | `Confirm`, `Reject` |

### Test Cases (Assignment 6)

| Test | Expected Result | Outcome |
|------|-----------------|---------|
| `Factory_ValidInput_CreatesUsableValidatedValue` | Valid outcome with correct data | ✓ Passed |
| `Factory_InvalidAge_RejectsAndReportsReason` | Invalid with age reason | ✓ Passed |
| `Factory_CriminalRecord_RejectsAndReportsReason` | Invalid with criminal record reason | ✓ Passed |
| `DistinctCases_BothReachableAndPatternMatched` | Both Valid and Invalid reachable | ✓ Passed |
| `NormalOperation_ValidatedValueProducesCorrectResult` | Correct ranking and scores | ✓ Passed |
| `MissingPrerequisite_InvalidSequenceRejected` | Compile-time prevention | ✓ Passed |
| `RepeatedOperation_FollowsDocumentedSafeRule` | Returns existing outcome | ✓ Passed |
| `BoundaryValue_ExactThresholdFollowsRule` | Age 18 included, 17 excluded | ✓ Passed |
| `StatePreservation_FailedTransitionLeavesOriginalUnchanged` | Original state unchanged | ✓ Passed |


## Assignment 7: Exception Handling in FP

### Explicit Domain Outcomes
Three distinct domain failures modeled using `Result<T, DomainError>`:
1. **Invalid Value:** `CheckAcademicStanding` fails if GPA < 2.0.
2. **Missing Item:** `RequireApplicant` fails if lookup returns `None`.
3. **Operation Rejected:** `ConfirmState` fails if state is `Rejected`.

###  Map and Bind Chain
`ProcessApplicantChain` demonstrates composition:
1. **Lookup:** `FindByName` returns `Maybe`, converted to `Result`.
2. **Bind:** `CheckAcademicStanding` returns `Result`. If lookup failed, this is skipped.
3. **Map:** `ScoredApplication` constructor is a pure function. If previous steps failed, this is skipped.

### Boundary Exception Handling
`FileBoundary.LoadApplications` handles operational errors:
- **Missing File:** Returns `Failure` with `FileNotFound`.
- **Malformed Content:** Returns `Failure` with `MalformedFile`.
- File I/O is kept outside pure domain functions. Exceptions are caught and converted to `Result`.


### Requirement Mapping (Assignment 7)

| Requirement | File | Method |
|-------------|------|--------|
| Result type with Success/Failure | `DomainTypes.cs` | `Result<T, E>`, `Success`, `Failure` |
| Maybe type for lookup | `DomainTypes.cs`, `DomainOperations.cs` | `Maybe<T>`, `FindByName` |
| Map and Bind implementation | `ResultExtensions.cs` | `Map`, `Bind` |
| Three domain failures | `DomainOperations.cs` | `CheckAcademicStanding`, `RequireApplicant`, `ConfirmState` |
| Boundary exception handling | `FileBoundary.cs` | `LoadApplications` |

### Test Cases (Assignment 7)

| Test | Expected Result | Outcome |
|------|-----------------|---------|
| `CompleteSuccess_ChainProducesCorrectValue` | Success with score 710 | ✓ Passed |
| `DomainFailure_InvalidGpa_ReturnsError` | Failure with InvalidGpa | ✓ Passed |
| `DomainFailure_MissingApplicant_ReturnsError` | Failure with MissingApplicant | ✓ Passed |
| `DomainFailure_StateTransitionRejected_ReturnsError` | Failure with StateTransitionNotAllowed | ✓ Passed |
| `LookupPresent_ReturnsSome` | Some<ValidatedApplication> | ✓ Passed |
| `LookupAbsent_ReturnsNone` | None<ValidatedApplication> | ✓ Passed |
| `MapSuccess_TransformationApplied` | Success with transformed value | ✓ Passed |
| `BindSuccess_ContinuationApplied` | Success with chained value | ✓ Passed |
| `FailurePropagation_MapSkipsFunction` | Flag remains false | ✓ Passed |
| `FailurePropagation_BindSkipsFunction` | Flag remains false | ✓ Passed |
| `MissingFile_ReturnsFileNotFound` | Failure with FileNotFound | ✓ Passed |
| `MalformedFile_ReturnsMalformedFile` | Failure with MalformedFile | ✓ Passed |
### Expected Output

--- File Boundary ---
Loading: sample_input.csv
  Success: Loaded 5 records.

Loading: missing_file.csv
  Failure: DomainError { Code = FileNotFound, Message = File not found: missing_file.csv }

Loading: malformed_file.csv
  Failure: DomainError { Code = MalformedFile, Message = Malformed CSV at line 2. }

--- Domain Chain (Lookup, Bind, Map) ---
Processing: Alice
  Success: ScoredApplication { Applicant = ValidatedApplication { Name = Alice, Age = 20, Gpa = 3.0, Hours = 110 }, Score = 710.0 }

Processing: Bob
  Failure: DomainError { Code = InvalidGpa, Message = GPA 1.5 is below 2.0. }

Processing: Unknown
  Failure: DomainError { Code = MissingApplicant, Message = Applicant 'Unknown' not found. }

--- State Transition ---
  Failure: DomainError { Code = StateTransitionNotAllowed, Message = Cannot confirm a rejected application. }

  
# Assignment 8: Extension Methods and LINQ

### Task 1: Custom Extensions
1. `ScoreAll`: Transforms `IEnumerable<ValidatedApplication>` to `IEnumerable<ScoredApplication>`. Belongs here as it is a domain-specific query transformation.
2. `CalculateAverageGpa`: Aggregates GPA. Belongs here to keep reporting logic fluent.

### Task 2: LINQ Reports
- **Age Group Report:** Uses `GroupBy`, `Count`, `Average`, `OrderByDescending`, and `ThenBy`.
- **High Achievers Report:** Uses `Where`, `OrderByDescending`, `ThenBy`, and `Select`. 
Both reports explicitly call `.ToList()` to materialize the snapshot, preventing repeated enumeration.

### Task 3: Currying and Partial Application
`ScoringFactory.CreateScorer()` is a curried function: `gpaWeight => hoursWeight => app => score`. 
In `Program.cs`, it is partially applied to create two distinct, configured functions: `academicPolicy` and `holisticPolicy`, which produce different results on the same input.

### Task 4 & 5: Deferred Execution and Resource Lifetime
`DataLoader.LoadAndStreamRawApplications` uses `yield return` to demonstrate deferred execution. A static `TransformationCount` probe proves the body does not run until `.ToList()` is called. 
**Resource Ownership:** The `StreamReader` is wrapped in a `using` statement. The file is read into a memory snapshot and the stream is disposed before the `yield return` loop begins. This guarantees the file handle is released immediately while still allowing deferred pure transformation of the snapshot.

### Task 6: Retention and Allocation Note
- **Materialization:** Reports explicitly call `.ToList()` to create stable snapshots. 
- **Retention:** The `DataLoader` does not retain the `StreamReader` ; only the yielded `RawApplication` structs are retained by the caller's `.ToList()`. 
- **Trade-off:** We chose to read the file into a string array snapshot rather than yielding directly from the open `StreamReader`. While yielding directly saves memory for massive files, it keeps the file handle open, risking `IOException` if the caller enumerates slowly or fails. The snapshot approach trades a small, bounded memory allocation for guaranteed, immediate resource release and safer pure processing.

### Task 7, 8, 9: Native C Integration
- **Function:** `calculate_experience_multiplier(int hours)`
- **Contract:** Returns 1 (<50 hrs), 2 (<100 hrs), 3 (>=100 hrs), or -1 (invalid).
- **Platform:** Windows (DLL) / Linux (SO). 
- **Build Command (Windows):** `cl /LD native_scoring.c /Fe:native_scoring.dll`
- **Binding:** `DllImport` with `CallingConvention.Cdecl`.
- **Integration Difficulty:** Initially, the DLL was not found. 
- **Resolution:** Added an `<ItemGroup>` in `.csproj` to copy the compiled `.dll` to the `bin/Debug/net8.0/` output directory automatically.
- **Adapter:** `NativeScoringAdapter` validates inputs  before the P/Invoke call, and catches `DllNotFoundException` to return a safe `Result` failure.

## Instructions
**Prerequisites:** .NET SDK 10.0.
**Run:** `cd Assignment5_FP\src`
-        `dotnet run`
**Test:** `dotnet test`
# Git Link: https://github.com/CuBiK911/Project_final_FP.git  : Branch Main  : "4f23d4cd7454248ee0a1f8c3b9ff5df08fd22973" commit identifier
# Final
## Run from the project root

```powershell
dotnet build FinalProject_FP.slnx
dotnet run --project Assignment5_FP/src -- demo
dotnet run --project Assignment5_FP/src -- rank Assignment5_FP/src/data/sample_input.csv academic admissions-report.txt
dotnet run --project Assignment5_FP/src -- applicant Assignment5_FP/src/data/sample_input.csv academic Alice
dotnet run --project tests/Pipeline.Tests.csproj
```

Use the .NET 10 SDK. The app uses the .NET base libraries. The xUnit test project restores its packages from NuGet. The command above uses its deterministic runner and prints a pass/fail summary. On hosts with a working VSTest testhost, you can also run `dotnet test tests/Pipeline.Tests.csproj`. The sample is copied to the build output, so `demo` can also run from another working directory. For `rank`, use an existing output directory and a filename different from the input. The export replaces an existing report. Exit codes are `0` for success, `1` for a domain or file error, and `2` for invalid command usage or policy.

## Business rules

The CSV columns are `Name,Age,GPA,Hours,HasCriminalRecord`. A row needs a name, an integer age of 18–25 inclusive, a GPA of 0–4 inclusive, nonnegative integer hours, and a false criminal-record flag. Use a decimal point for GPA. Quoted CSV fields can contain commas. A wrong header or malformed CSV stops the import. A structurally valid row with invalid values is listed with its rejection reasons while other rows are processed. A header-only CSV yields an empty report.

| Policy | Formula | Sample ranking |
|---|---|---|
| Academic | GPA × 200 + hours | Charlie 800, Alice 710 |
| Holistic | GPA × 100 + hours × 5 | Alice 850, Charlie 400 |

Ranking ties are resolved by applicant name in ordinal order. Individual confirmation has a minimum GPA of 2.0. It uses an exact, case-sensitive name match and selects the first eligible matching row if the CSV repeats a name. The ranking includes all eligible rows. An age-group report counts eligible applicants and averages GPA by band. The high-achievers report returns up to five applicants with GPA ≥ 3.5, ordered by score and then name.

## Code map

| Requirement | Where to show it |
|---|---|
| Functional core | `Domain/ApplicationFactory.cs` and `Domain/Models.cs` validate and construct immutable applicant values. `Workflows/Workflow.cs` and `Workflows/ReportGenerator.cs` work entirely in memory. |
| Composition | `Workflow.RunDetailed` calls `ValidateApplications` → `ComputeScores` → `GenerateReport`. The scoring stage accepts a `Func<ValidatedApplication, decimal>`. |
| Safe ordering | Only validation constructs a `ValidatedApplication`. Passing a `RawApplication` to `Score` fails to compile. `DomainOperations.ConfirmState` pattern matches pending, confirmed, and rejected states. Rejected applications return a failure and repeat confirmation returns the existing score. |
| Explicit errors | `DomainOperations.ProcessApplicantChain` uses `Maybe`, `Map`, and `Bind` for lookup and confirmation. Missing applicants, low GPA, and illegal state transitions return explicit error codes. |
| LINQ and laziness | `ScoreAll` uses `yield return` and the scoring stage materializes the result once. `AgeGroups` uses `GroupBy`, `Count`, and the `CalculateAverageGpa` extension. `HighAchievers` filters, projects, and orders with a tie-breaker. |
| Persistence | `Infrastructure/FileBoundary.cs` handles file reads and report writes. `Infrastructure/CsvApplications.cs` validates the CSV structure. `data/` contains valid and malformed examples. |
| Automated checks | `tests/` covers pure validation, both complete policy workflows, deferred execution, safe transitions, failure propagation, missing and malformed files, and report export. |

Paths in the code map are relative to `Assignment5_FP/src`. The existing optional C interop example is under `native/`. The admissions commands run without it.

## demo

1. Run `demo`. Compare the two winners and point out that Bob, Diana, and Eve are rejected for three different reasons.
2. Open `Workflow.RunDetailed` and follow the data through validation, scoring, and ranking. Show where the delegate changes the scoring formula.
3. Point out the demo's policy-call count of zero before enumeration and two after materialization. Show the explicit failure when a rejected application is confirmed.
4. Run the `rank` command and open the exported report. To show a failed import, replace `sample_input.csv` with `malformed_input.csv` in the command.
5. Run the tests and show the pass/fail summary.

