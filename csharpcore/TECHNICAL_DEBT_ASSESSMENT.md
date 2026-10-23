# Technical Debt Assessment and Remediation (C#)

This is the companion to `SENIOR_REVIEW_REPORT.md`. Baseline: `e65651c`. Original implementation: `b9e1757`.

## 1. Branches

| Stage | Branch | Nature | Merge target |
|---|---|---|---|
| A: safe architectural improvements | `review/senior-hardening` | Behavior-preserving | May follow `modernization/tell-dont-ask` |
| B: domain hardening | `stage-b/domain-validation` (rebased on A at `7a6a094`, HEAD `2f3e2ae`) | **Intentional behavior changes**, approved item by item | Separate release; never into the assessment branch |
| C: framework modernization | `upgrade/dotnet-lts` (from `e65651c`) | Build-only; no source changes | Independent; rebase onto whichever line is released |

## 2. Prioritized remediation backlog

| # | Item | Class | Stage | Status |
|---|---|---|---|---|
| 1 | Unchecked `Order(int, OrderStatus)` constructor (null currency) | SAFE | A | Done: removed, with test fixtures going through `Restore` |
| 2 | `Restore` crashes on null items or null elements | SAFE (new API) | A | Done: `ArgumentNullException` / `ArgumentException` |
| 3 | `Items` returns a new wrapper on every call | SAFE | A | Done: one cached read-only view |
| 4 | Regression gaps (save failure after shipment, retry, save counts, `Restore` copying and currency, negative rounding, scale) | SAFE (tests only) | A | Done |
| 5 | Unknown order id gives `NullReferenceException` | BREAKING (approved) | B | Done: `OrderNotFoundException` |
| 6 | Null request inputs give `NullReferenceException` | BREAKING (approved) | B | Done: `ArgumentNullException` / `ArgumentException` before any lookup |
| 7 | Product with a null category | BREAKING (approved) | B | Done: rejected in the constructor |
| 8 | Undefined `OrderStatus` values pass every check | BREAKING (approved) | B | Done: `Restore` throws `ArgumentOutOfRangeException` |
| 9 | `Ship(null)` | BREAKING (approved) | B | Done: `ArgumentNullException`, after the status checks |
| 10 | `AddItem` on a Shipped order | BREAKING (approved) | B | Done: `ShippedOrdersCannotBeChangedException` |
| 11 | End-of-life `netcoreapp3.1`, old test tooling (NU1903) | CONDITIONAL | C | Done: `net10.0`, updated packages |
| 12 | Domain depends on the `UseCase` namespace (exceptions) | BREAKING | Deferred | Next major version (see 4.1) |
| 13 | Domain depends on `IShipmentService` | CONDITIONAL | Deferred | Retained, by decision (see 4.2) |
| 14 | Saved line amounts not rehydratable | CONDITIONAL | Deferred | Needs a persistence design |
| 15 | Duplicate shipment on retry; concurrent shipment | CONDITIONAL | Deferred | Idempotency key and optimistic concurrency |
| 16 | `Money`, `Currency` and `Quantity` value objects | CONDITIONAL | Deferred | Low value with a single currency |

## 3. Stage A: safe improvements

### 3.1 Dependency cleanup (A1)

The domain throws exceptions declared in `TellDontAskKata.Main.UseCase`, so `Domain` depends on `UseCase`. Within one assembly there is no compatible fix:

| Option | Effect |
|---|---|
| Move the types | Breaks every `using` and every `catch` that names them |
| New Domain base types, with the old UseCase types deriving from them | Keeps `catch` compatibility, but `Order` still has to throw the UseCase types, so the dependency direction doesn't improve |
| The domain throws new types derived from the old ones | `Domain` still depends on `UseCase` through inheritance |

**Decision:** deferred to a major version. The approach then is to move the types to `Domain`, add a common `DomainException` base, and mark the change as breaking. No change was made in Stage A, because the user's constraint was not to break public exception contracts.

### 3.2 Test quality (A2)

- No assertions were weakened.
- Two strict-subset duplicates were merged into the original tests, losing no assertion.
- New double: `TestOrderRepository.SaveCount`.
- New doubles: `ThrowingOnSaveOrderRepository` and `FlakyShipmentService`.
- Every new characterization scenario was also run against `b9e1757`: 31 ported cases, all passing.

### 3.3 Order invariants (A3)

- One way to rebuild an order (`Restore`), which:
  - checks its arguments for null;
  - copies the supplied items once;
  - recomputes totals from the items.
- `Items` is a single read-only live view.
- No new business validation was added in Stage A.

## 4. Stage B: behavior specifications

Every change below was approved by the user before implementation. Each was written test-first: the red run was recorded, then a minimal fix was made. Old incidental tests were updated in the same commit, and each commit body lists them as `Intentional behavior change: old -> new`.

| Change | Previous behavior | New behavior | Compatibility |
|---|---|---|---|
| Unknown order id (approval, shipment) | `NullReferenceException` | `OrderNotFoundException` (`ApplicationException`, `OrderId`); nothing saved or shipped | New public type; callers see a different exception type |
| Null request, null `Requests`, null item | `NullReferenceException`, sometimes after earlier lookups | `ArgumentNullException` / `ArgumentException("request")` before any lookup or save | Different exception type |
| Product with a null category | Accepted; `NullReferenceException` when sold | `ArgumentNullException("category")` in the constructor | Fails earlier |
| `Restore` with an undefined status | Accepted; could then be approved, rejected or shipped | `ArgumentOutOfRangeException("status")`; enum values unchanged | Invalid stored data now fails on load |
| `Ship(null)` | `NullReferenceException` for Approved orders | `ArgumentNullException`, after the status checks; the order of the domain exceptions is unchanged | Only the Approved + null case changes |
| `AddItem` on a Shipped order | Accepted; totals changed | `ShippedOrdersCannotBeChangedException`; order unchanged | A call that used to succeed now throws |

### Kept by decision

| Rule | Current behavior | Options considered | Decision |
|---|---|---|---|
| Zero or negative quantity | Accepted, giving zero or negative lines | Reject; ignore; accept | Keep. Negative lines may be returns. Needs business confirmation. |
| Negative price or tax rate | Accepted | Reject negatives; cap tax at 100% | Keep. Needs business confirmation. |
| `AddItem` on Approved or Rejected orders | Accepted | Restrict to Created | Keep. Only Shipped is guarded. |
| Rounding | `ToPositiveInfinity`: every positive fraction of a cent rounds up | `AwayFromZero`, which matches Java `HALF_UP` | Keep. Finance owns this; the divergence is documented. |
| `default(OrderStatus) == Approved` | Pinned numeric values | Renumber (rejected); add a `None = 0` member (rejected); persist status by name | Keep the values. Recommend persisting by name. Undefined values are now rejected in `Restore`. |
| Null product name | `UnknownProductException` | Fail fast with `ArgumentException` | Keep. |

### 4.1 Money, Currency and Quantity (B3)

These were evaluated and **deferred**. They would change the types of the public properties `Total`, `Tax`, `Currency` and `Quantity`, with a lot of test churn and little gain while only EUR exists. If they are introduced later, rounding must stay in a single function so the exact per-unit sequence is preserved. Keep separate:
- the calculation refactoring, which must be behavior-neutral;
- the validation rules, which are business decisions.

### 4.2 Shipping (B4)

`Order.Ship(IShipmentService)` is **retained**; see the comparison in `SENIOR_REVIEW_REPORT.md` section 8. Only the null check on the service was added.

Fixing duplicate shipment and concurrency requires:
- an idempotency key on `IShipmentService`;
- a version or optimistic-concurrency token on `IOrderRepository`;
- optionally, an outbox.

These change the tested order of steps, so they need explicit approval and their own tests.

## 5. Stage C: framework modernization

| | Before | After |
|---|---|---|
| TargetFramework | `netcoreapp3.1` (end-of-life; runtime not installed; tests needed `DOTNET_ROLL_FORWARD=Major`) | `net10.0` (current LTS, supported to Nov 2028) |
| Microsoft.NET.Test.Sdk | 16.5.0 | 18.11.0 |
| xunit | 2.4.0 | 2.9.3 (stays on v2) |
| xunit.runner.visualstudio | 2.4.0 | 3.1.5 |
| coverlet.collector | 1.2.0 | 6.0.4 |

`net8.0` was rejected because its support ends on 2026-11-10. No source changes were needed. The retarget alone surfaces NU1903 (Newtonsoft.Json 9.0.1, a vulnerable version pulled in by the old test SDK); the package update removes it. The rounding tests pass unchanged on .NET 10.

## 6. Architecture before and after

| Concern | Original `b9e1757` | Modernized `e65651c` | After Stage A | After Stage B |
|---|---|---|---|---|
| Pricing and rounding | In `OrderCreationUseCase` | `Product`, `OrderItem`, `Money` | Same | Same |
| Status rules | `if` chains in the use cases | `Order.Approve`, `Reject`, `Ship` | Same | Plus `Ship(null)` check |
| Order state | Public setters, mutable `IList` | Private setters, read-only `Items` | One cached view | Same |
| Rebuilding an order | Object initializer | `Order(id, status)` and `Restore`, both unchecked | `Restore` only, with null checks | Also rejects undefined status |
| `AddItem` | `Items.Add` from anywhere | Public, no status check | Same | Shipped orders blocked |
| Invalid inputs | `NullReferenceException` | `NullReferenceException` (thrown from the domain) | Same (characterized) | Explicit exceptions |
| Domain dependencies | n/a (no logic in the domain) | `UseCase`, `Service` | Same | Same |
| Tests | 11 | 74 | 107 | 135 |

## 7. Remaining technical debt

1. Exceptions live in the `UseCase` namespace, so the domain depends on the use-case layer. Breaking to fix; defer to a major version.
2. `Order` depends on `IShipmentService`, and the namespaces depend on each other. Retained by decision.
3. Duplicate shipment on retry after a failed save, and concurrent shipment. Pre-existing.
4. Saved line amounts are recomputed when an order is rebuilt.
5. `default(OrderStatus) == Approved`.
6. Rounding diverges from the Java reference. Finance decision.
7. Zero and negative quantities, prices and tax rates are accepted. Business decision.
8. Money is a raw `decimal` and currency a raw string.
9. Created orders have `Id` 0; ids are the repository's job.
10. Some test duplication across the domain and use-case layers. There is one implementation-detail test (`ItemsReturnsSameReadOnlyView` uses `Assert.Same`) and one brittle decimal-scale test, tagged as incidental behavior.
