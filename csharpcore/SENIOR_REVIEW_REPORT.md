# Senior Review: Tell Don't Ask Kata (C# modernization)

| | |
|---|---|
| Scope | `csharpcore/TellDontAskKata` and `csharpcore/MODERNIZATION_REPORT.md` |
| Reviewed baseline | `e65651c`, the published `modernization/tell-dont-ask` branch on [Strawzer/tell-dont-ask-kata](https://github.com/Strawzer/tell-dont-ask-kata) |
| Original implementation | `b9e1757` |
| Review branch | `review/senior-hardening`. Behavior-preserving; contains Stage A of the remediation |
| Other branches | `stage-b/domain-validation` (approved behavior changes), `upgrade/dotnet-lts` (framework upgrade) |

Line references point to `review/senior-hardening` unless a commit is named.

## 1. Executive assessment

**Overall score: 6.5 / 10. Verdict: meets senior expectations, with reservations.**

The modernization does what the kata asks:
- Business rules moved out of the use cases into `Order`, `OrderItem` and `Product`.
- The public setters are gone.
- The use cases are now "load, tell, save".

Behavior was preserved. Independent probes of about 75 scenarios gave identical results on the original and the refactored code (.NET 8, 9 and 10). The differences were stack-trace origins, `Items` being empty instead of null, and the `Items` wrapper identity.

These are the reservations:

- **No test-driven development.** The history proves the work was protected by tests, not driven by them. No commit adds a failing test before the code that makes it pass. The characterization tests in `3ae352e` describe the existing behavior.
- **Invariants held by convention.** The status rules were enforced, but the rest of the invariants were not. Two public paths rebuilt an `Order` without any checks, and `AddItem` worked on shipped orders.
- **Two review-driven corrections.** A risky enum reorder (`74ca6a7`) passed all 39 tests and was only caught by code review. The read-only `Items` leak also came in as a fix after review.
- **Factual errors in the report.** `MODERNIZATION_REPORT.md` had four, now corrected (section 12).

## 2. Scorecard

| Dimension | Score /10 | Evidence | Main weakness |
|---|---|---|---|
| Domain model quality | 7 | Pricing is in `Product.UnitaryTax` and `OrderItem`. Transitions are in `Order.Approve`, `Reject` and `Ship` (`Order.cs:73-118`). `Product` and `Category` are immutable. | At `e65651c`, `Order(int, OrderStatus)` and `Restore` accepted any state, and `AddItem` ignored the status. Saved amounts can't be restored, because `OrderItem` always recomputes from the product. |
| Tell Don't Ask | 8 | The use cases now only load the order, tell it what to do, and save (`OrderCreationUseCase.cs:19-36`). | `Ship(IShipmentService)` runs an external side effect inside the aggregate. That is acceptable, but not pure. |
| SOLID and separation of concerns | 6 | Each class has one responsibility. The existing ports are kept. | The domain depends on `UseCase` (exceptions, `Order.cs:5`) and on `Service` (`Order.cs:4`), and `Service` depends back on the domain. The original report wrongly claimed Open/Closed. |
| Unit and integration test quality | 7 | No assertions were weakened. Domain tests cover the full state matrix. In hand mutation testing, 14 of 20 mutants were killed. | At `e65651c`, nothing caught `Restore` aliasing, `Restore` currency, negative rounding, or a failed save after shipment. The workflow tests are component tests, not integration tests. |
| Behavior preservation | 8 | Probes and characterization tests agree. Enum values are pinned after the revert. | Only code review protected the enum numbering. The tests didn't. |
| Avoiding overengineering | 7 | No new layers, libraries or interfaces. `Money` is a small internal helper. | There were two ways to rebuild an order. There is some test duplication across layers. |

The area outside the six dimensions was weakest: **TDD evidence scored 2/10**. Characterization is not TDD, and the history shows no red-then-green step until the Stage B work in this review.

## 3. Critical findings (on `e65651c`, before this review)

| # | Severity | Finding | Location | Status |
|---|---|---|---|---|
| F1 | MAJOR | A public `Order(int, OrderStatus)` constructor bypassed every check and gave a null currency. It wasn't in the original. | `e65651c:Order.cs:11-14` | Fixed in Stage A: removed, and test rebuilding goes through `Restore`. |
| F2 | MAJOR | `Restore` threw `NullReferenceException` for null items or null elements, and accepted any status. | `e65651c:Order.cs:40-49` | Null checks fixed in Stage A. Undefined status rejected in Stage B. |
| F3 | MAJOR | `AddItem` changed the totals of shipped orders. | `Order.cs:61-64` | Stage B (approved): throws `ShippedOrdersCannotBeChangedException`. |
| F4 | MAJOR (test gap) | No test covered a failed save after a successful shipment, which ships the order twice on retry. This risk is pre-existing. | `OrderShipmentUseCase.cs` | Now characterized. The underlying risk is still open (section 8). |
| F5 | MINOR | `Items` returned a new wrapper on every call, so `order.Items == order.Items` was false. | `e65651c:Order.cs:25` | Fixed in Stage A: one cached `ReadOnlyCollection` (`Order.cs:12,24`). |
| F6 | MINOR | Unknown order id, null request inputs and a null category crashed with `NullReferenceException`. These are pre-existing. | Use cases, `Product.cs:16` | Stage B (approved): explicit exceptions. |
| F7 | MINOR | Undefined status values such as `(OrderStatus)99` passed every check. Pre-existing. | `Order.cs:73-118` | Stage B (approved): `Restore` rejects them. |
| F8 | MINOR | The domain depends on the use-case and service namespaces. | `Order.cs:4-5` | Deferred: fixing it is breaking. |
| F9 | MINOR | Saved amounts can't be rehydrated; `OrderItem` recomputes from the current product price. | `OrderItem.cs` | Deferred: needs a persistence design. |

## 4. Confirmed regressions

**None confirmed.** What was verified:
- **Probes:** side-by-side runs of `b9e1757` and `e65651c`.
- **Ported tests:** 31 newly ported characterization cases also pass on the original code.
- **Mutation testing:** hand mutants of rounding, the order of checks, the side-effect sequence and save counts.

These are not regressions, but they are observable differences from the original:
- **Intentional API changes:**
  - The parameterless constructors and public setters are gone.
  - `Items` is an `IReadOnlyList` and is never null.
  - `Items` now returns the same object on every call.
- **Diagnostics-only changes:** domain exceptions and the null-category `NullReferenceException` are now thrown from domain methods instead of from `Run`.
- **Pre-existing bug, kept by decision:** rounding uses `MidpointRounding.ToPositiveInfinity` (`Money.cs:9`), so every positive fraction of a cent rounds up (1.001 becomes 1.01). The Java reference uses `HALF_UP` (1.001 becomes 1.00). The C# port diverged from the start.

This is not a claim of full behavioral equivalence. The original .NET Core 3.1 runtime isn't installed, so the "original" behavior was observed on .NET 8, 9 and 10 using roll-forward.

## 5. Domain and SOLID evaluation

**Strengths:**
- `Order` owns its transitions. Their order of checks is the same as the original, and no mutant that reordered the checks survived in a way the tests could see. With a single status, the guards never overlap, so the order of checks can't actually be observed.
- `Product`, `Category` and `OrderItem` are immutable (get-only, no virtual members).
- `Total` and `Tax` can only change through `Append` (`Order.cs:66-71`).

**Weaknesses:**
- **Dependency direction:** the domain imports `UseCase` (exceptions) and `Service` (`IShipmentService`), and `IShipmentService` references `Order`.
- **Invariants:** before this review, the only enforced invariants were the status transitions and "totals equal the sum of the lines". Currency, valid status values and status-versus-item consistency were conventions.
- **No value objects:** money and currency are a raw `decimal` and a `string`. That is acceptable with a single currency (EUR).

## 6. TDD and test quality

- **No weakening:** none of the original 11 tests had assertions weakened; only setup changed. Two strict-subset duplicates were merged into the original tests with no assertion lost. A later fix (`7a6a094`) restored a non-zero quantity in `UnknownProduct`.
- **Mutation testing, 20 hand mutants:**
  - 14 were killed.
  - 3 can't be detected by any test: the redundant line rounding, and the two guard swaps that never overlap.
  - 3 were real gaps: `Restore` aliasing, `Restore` currency and negative rounding. Stage A now kills all three.
- **Save counts:** at `e65651c`, only the workflow tests counted saves. Stage A adds exact save-count checks to the use-case tests.
- **Test levels:** the workflow tests are component tests (real use cases over in-memory fakes). There is no infrastructure boundary to integration-test, and none was invented.
- **Test doubles hide persistence failures:** the in-memory doubles return the same object instances, so a missing `Save` is invisible except through save counts.
- **Incidental behavior:** tests tagged `IncidentalBehavior` pin questionable but current behavior. On `review/senior-hardening` they still document the old crashes. On `stage-b/domain-validation` the approved ones were renamed to the new behavior, and their "nothing saved / nothing shipped" checks were kept.

## 7. Technical debt decisions

| Item | Classification | Justification |
|---|---|---|
| Domain exceptions in the `UseCase` namespace | FUTURE IMPROVEMENT | Moving public types is breaking. No same-assembly approach both fixes the dependency direction and keeps compatibility. Do it in a major version. |
| `Order` depends on `IShipmentService` | ACCEPTABLE FOR KATA | It is a recognized way to pass a service into an aggregate method, it keeps the exact original order of steps, and it is easy to test. |
| `default(OrderStatus)` is `Approved` | REQUIRES BUSINESS DECISION | The numeric values are pinned for compatibility. Stage B rejects undefined values; persisting the status by name is recommended. |
| Missing input validation | SHOULD HAVE BEEN FIXED (accidental crashes) / REQUIRES BUSINESS DECISION (quantity, price and tax rules) | The `NullReferenceException`s were defects, and Stage B fixes them with approval. Zero and negative values were kept by decision. |
| `AddItem` without a status guard | SHOULD HAVE BEEN FIXED for Shipped | Shipped is guarded in Stage B. Approved and Rejected stay allowed by decision. |
| Created order `Id` is 0 | ACCEPTABLE FOR KATA | Assigning ids is the repository's job, and no real repository exists. |
| Currency as `string`; no `Money` value object | FUTURE IMPROVEMENT | Little value with a single currency, and it would change public property types. |
| Outdated .NET runtime | SHOULD HAVE BEEN FIXED (separately) | `netcoreapp3.1` is end-of-life, and the tests needed roll-forward. Upgraded on `upgrade/dotnet-lts`, kept separate from the domain changes. |
| Incidental-behavior tests | ACCEPTABLE FOR KATA | They make legacy quirks visible and turn deliberate changes into visible test changes. |

## 8. Shipping design comparison

The current order of steps (check status, call the service while the order is Approved, set Shipped, save) is **identical to the original**. Every reliability risk below is pre-existing:
- A failed save after shipment.
- A second shipment on retry.
- Concurrent shipment of the same order.
- An order that is Shipped in memory while storage still says Approved.

| Option | Complexity | Coupling | Keeps tested behavior? | Kata fit | Production fit |
|---|---|---|---|---|---|
| A. `Order.Ship(IShipmentService)` (current) | Lowest | Domain depends on the port | Yes, exactly | Good | Weak |
| B. `EnsureCanBeShipped()`, then the service call, then `MarkShipped()` | Low | Pure domain | Yes, if `MarkShipped` re-checks the status | Good | Fair |
| C. `Ship()` changes state and raises a domain event | Medium | Loose | No: state changes before the side effect | Overkill | Good, with an outbox |
| D. Application service around pure transitions | Low to medium | Pure domain | Yes | Good | Fair |
| E. Transactional outbox, asynchronous shipment | High | Loose | No: ships after save, asynchronously | Out of scope | Best |

**Decision:** keep A. Stage B adds only a null check on the service, placed after the status checks so the order of exceptions is unchanged. B is optional cleanup. An idempotency key on `IShipmentService`, optimistic concurrency, and an outbox (E) are future production hardening. Each changes the tested order of steps and needs explicit approval.

## 9. Five prioritized follow-up changes

| Priority | Finding | Impact | Regression risk | Effort | Recommendation |
|---|---|---|---|---|---|
| 1 | Unchecked order rebuilding (F1, F2) | High: invalid aggregates | Low | S | **SAFE**, done in Stage A |
| 2 | Missing regression protection: save failure after shipment, save counts, `Restore` aliasing and currency, negative rounding | High: mutants survived | None (tests only) | S | **SAFE**, done in Stage A |
| 3 | Accidental `NullReferenceException`s and undefined statuses (F6, F7) | Medium: unclear failures | Medium: exception types change | S | **BREAKING**, approved and done in Stage B |
| 4 | `AddItem` on a shipped order (F3) | Medium: shipped totals can change | Medium | XS | **BREAKING**, approved and done in Stage B (Shipped only) |
| 5 | End-of-life framework | Medium: unsupported runtime, roll-forward needed | Low | S | **CONDITIONAL**, done separately on `upgrade/dotnet-lts` (net10.0) |

The next items in line are a persistence-oriented `OrderItem` rehydration, the shipping idempotency key, and relocating the exceptions in a major version.

## 10. Changes actually implemented

**Stage A** on `review/senior-hardening` (behavior-preserving; commits `101373a`..`7a6a094`):
- **Order rebuilding:**
  - Removed `Order(int, OrderStatus)`; test fixtures rebuild orders through `Order.Restore`.
  - `Restore` rejects null `items` (`ArgumentNullException`) and null elements (`ArgumentException`), before building any state.
- **Items view:** `Items` exposes one cached, live, read-only view.
- **New tests:**
  - `Restore` copies its input, keeps the given currency and reads its input only once.
  - Exact save counts in every use case.
  - A failed save after shipment, and a retry after a failed shipment.
  - Negative-amount rounding and decimal scale.
  - Changing the request after the run.
  - Null category, the default status and undefined statuses (characterized).
- Two redundant tests merged losslessly, with a follow-up quantity fix.

**Stage B** on `stage-b/domain-validation` (user-approved breaking changes; one `feat!` commit each, each test-first with recorded red runs):
1. `OrderNotFoundException` (new, carries `OrderId`) for unknown order ids in approval and shipment.
2. Null request, null `Requests` or a null item throws `ArgumentNullException` or `ArgumentException` before any lookup or save.
3. `Product` rejects a null category.
4. `Order.Restore` rejects undefined statuses with `ArgumentOutOfRangeException`. The enum values are unchanged.
5. `Order.Ship(null)` throws `ArgumentNullException`, after the status checks.
6. `AddItem` on a Shipped order throws `ShippedOrdersCannotBeChangedException`.

Kept by decision: zero and negative quantities, negative prices and tax rates, the rounding mode, the exception namespaces, and the shipping design.

**Stage C** on `upgrade/dotnet-lts` (only the project files change):
- `netcoreapp3.1` to `net10.0`.
- Microsoft.NET.Test.Sdk 16.5.0 to 18.11.0.
- xunit 2.4.0 to 2.9.3.
- xunit.runner.visualstudio 2.4.0 to 3.1.5.
- coverlet.collector 1.2.0 to 6.0.4.

The retarget alone surfaced NU1903, a known vulnerability in Newtonsoft.Json 9.0.1 pulled in by the old test SDK; the package update removes it.

## 11. Build and test results

All three runs used `dotnet restore`, then `dotnet build TellDontAskKata.sln --no-restore --no-incremental`, then `dotnet test --no-build --logger "console;verbosity=normal"`, from `csharpcore/TellDontAskKata`.

| Branch | HEAD | TFM | Runtime actually used | Build | Tests | `Category=IncidentalBehavior` | `FullyQualifiedName~Round` |
|---|---|---|---|---|---|---|---|
| `modernization/tell-dont-ask` (baseline) | `e65651c` | netcoreapp3.1 | .NET 8.0.31 (`DOTNET_ROLL_FORWARD=Major`) | 0 warnings / 0 errors | 74 / 74 | 5 | 5 |
| `review/senior-hardening` (Stage A) | `7a6a094` | netcoreapp3.1 | .NET 8.0.31 (roll-forward) | 0 / 0 | 107 / 107 | 14 | 8 |
| `stage-b/domain-validation` | `2f3e2ae` | netcoreapp3.1 | .NET 8.0.31 (roll-forward) | 0 / 0 | 135 / 135 | 6 | 8 |
| `upgrade/dotnet-lts` (Stage C) | `4d1a80f` | net10.0 | .NET 10.0.11 (no roll-forward) | 0 / 0 | 74 / 74 | 5 | 5 |

- **Stage B rebase:** Stage B was rebased onto `7a6a094` with `git rebase --exec`. Every one of the six rebased commits built and passed: 111, 116, 117, 126, 130 and 135 tests.
- **Incidental-behavior count:** it drops from 14 to 6 on Stage B because eight characterization cases now describe the deliberately changed behavior and lost the tag.
- **Scope:**
  - **Files:** `git diff --name-only` against `b9e1757` and against `e65651c` shows no file outside `csharpcore/` on any branch.
  - **Review and Stage B:** the `*.csproj` diff against `b9e1757` is empty, so the framework and packages are unchanged.
  - **Upgrade:** only the two project files change.
- **Not run:** the original .NET Core 3.1 runtime isn't installed, so no branch was executed on 3.1 itself.

Minor notes from the Stage B validation, not fixed:
- In `Restore`, an undefined status combined with null items now throws `ArgumentOutOfRangeException` instead of `ArgumentNullException`. No test pins this.
- The approval and shipment "undefined status" tests now fail while setting up the order, before the use case runs, so they duplicate `OrderRestoreValidationTest`.
- The `revert:` commit type is valid in the conventional-commit spec but isn't in this workspace's list of types.

## 12. Commit history evaluation

- **Good:**
  - Small, cohesive commits.
  - Conventional subjects.
  - Each refactoring step built and passed the tests (several were spot-checked by building detached worktrees).
  - Characterization tests came before any production change.
- **Weak:**
  - No commit adds a failing test before the code that makes it pass, so there is no TDD evidence before Stage B.
  - Commits that break the public API are labeled `refactor:` without `!` or a `BREAKING CHANGE` footer.
  - `ff2a185` is `fix:` for a defect introduced on the same branch; it would have been better as a fixup.
  - The enum reorder `74ca6a7` and its `revert:` (`ce966ec`) stay in the history. That is honest, but it shows the risk was only caught by code review.
- **Report corrections** (now fixed in `MODERNIZATION_REPORT.md`):
  - It said "not pushed"; the branch is now published.
  - It cited `1486c97` as the commit the tests ran on, while HEAD is `e65651c`, which adds only the report.
  - It claimed Open/Closed; it is cohesion.
  - It said `AddItem` is "only exposed at creation time"; it is public.

## 13. Remaining risks

- **Shipping reliability:** duplicate shipment on retry after a failed save, and concurrent shipment. These are pre-existing and need idempotency and concurrency control.
- **Rounding:** rounding up diverges from the Java reference. This is a finance decision.
- **No persisted amounts:** stored line amounts are recomputed on rehydration.
- **`default(OrderStatus) == Approved`:** stored zeros mean Approved.
- **Breaking changes still unmerged:** Stage B changes exception types for callers. It isn't merged into the assessment branch and needs release notes.
- **Unverified on 3.1:** behavior on the original .NET Core 3.1 runtime wasn't observed, because the runtime isn't installed.

## 14. Final verdict

**Meets senior expectations, with reservations.**

The design moves were the right ones and were kept small, and behavior preservation was taken seriously and checked. To reach "strong senior", three things were missing:
- Test-first evidence in the history.
- Invariants enforced at every way into the aggregate, not just the happy path.
- An accurate, self-critical report.

The follow-up work in this review closes the invariant and test gaps without changing behavior, and keeps the deliberate behavior changes and the framework upgrade on separate, reviewable branches.
