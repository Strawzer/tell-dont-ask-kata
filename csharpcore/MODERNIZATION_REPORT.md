# Tell Don't Ask Kata (C#): Modernization Report

Scope: `csharpcore/TellDontAskKata` only. Other language implementations, the repository attribution and the license are untouched.
Branch: `modernization/tell-dont-ask`, based on `master` at `b9e1757`. It was not pushed when this report was written. It was later published to [Strawzer/tell-dont-ask-kata](https://github.com/Strawzer/tell-dont-ask-kata) and is not merged into `master`.

## 1. Baseline (before any change)

| Item | Value |
|---|---|
| Target framework | `netcoreapp3.1` (C# 8), kept as is |
| Test framework | xUnit 2.4.0, Microsoft.NET.Test.Sdk 16.5.0 |
| SDK used | .NET SDK 10.0.303 |
| Build | 0 warnings, 0 errors |
| Tests | 11 / 11 passed |

**Environment note:** the .NET Core 3.1 runtime isn't installed on the build machine, so tests have to run with `DOTNET_ROLL_FORWARD=Major`. That makes the 3.1 test host run on the .NET 8 runtime. We deliberately didn't change the framework, because an upgrade was out of scope.

```powershell
cd csharpcore\TellDontAskKata
dotnet build TellDontAskKata.sln
$env:DOTNET_ROLL_FORWARD='Major'; dotnet test TellDontAskKata.sln --no-build
```

## 2. Initial architectural problems

- **Anemic domain.** `Order`, `OrderItem`, `Product` and `Category` were property bags with public setters. `Order.Items` was a mutable `IList` that anyone could replace.
- **Tell Don't Ask and Law of Demeter violations.** `OrderCreationUseCase` read `product.Category.TaxPercentage` and computed tax, rounding and line totals itself. It then mutated `order.Items`, `order.Total` and `order.Tax` directly.
- **Business rules lived in the use cases.** The approval, rejection and shipment status rules were `if` chains inside the use cases, followed by `order.Status = ...`.
- **Single Responsibility violation.** Each use case mixed orchestration, pricing rules, state-transition rules and persistence.
- **Invariants weren't protected.** Nothing stopped `Total` and `Tax` drifting from the item amounts, or an illegal status being assigned.
- **Hidden hazard.** `default(OrderStatus)` is `Approved`, so a bare `new Order()` looked approved.

## 3. Refactoring decisions

The refactoring went in small steps, with a green suite after every commit:

1. **`Money.Round`** (internal) is now the only rounding rule: `decimal.Round(x, 2, MidpointRounding.ToPositiveInfinity)`, which always rounds up. We preserved it exactly, including that it rounds per unit before multiplying by quantity.
2. **`Product`** now computes its own `UnitaryTax` and `UnitaryTaxedAmount`.
3. **`OrderItem(product, quantity)`** computes its `TaxedAmount` and `Tax`.
4. **`Order.Create()` and `Order.AddItem(product, quantity)`** keep `Total` and `Tax` equal to the sums of the item amounts. `Items` is exposed as a real read-only wrapper.
5. **`Order.Approve()`, `Order.Reject()` and `Order.Ship(IShipmentService)`** hold the transition rules, with exactly the original order of checks. The shipment service is still called while the order is Approved; the order is marked Shipped afterwards and then saved.
6. **Setters are closed.** `Product` and `Category` are immutable with constructors. `Order` has private setters plus `Order(id, status)` and `Order.Restore(id, status, currency, items)` for rebuilding a saved order. `Restore` recomputes the totals from the items so the invariant holds.
7. **The use cases are thin.** Each one loads the order, tells the domain what to do, and saves. Their constructors and `Run` signatures are unchanged.

**Things we deliberately didn't add:** new interfaces, layers, factories, repositories, libraries, design patterns or framework upgrades.

**Rejected change:** we reverted reordering `OrderStatus` into lifecycle order (commit `74ca6a7`, reverted in `ce966ec`), because it changed the enum's public numeric values. A test now pins those values.

## 4. SOLID and OOP improvements

- **Single Responsibility:**
  - Pricing belongs to `Product` and `OrderItem`.
  - The status rules belong to `Order`.
  - The use cases only orchestrate.
- **Encapsulation:** no public setters remain on domain objects, and the item collection can't be changed from outside.
- **Tell Don't Ask:** the use cases call `order.AddItem`, `Approve`, `Reject` and `Ship` instead of reading state and writing it back.
- **Cohesion:** each transition rule now lives in one place, next to the state it guards. This is not an Open/Closed improvement: adding a status still means editing `Order`.
- **Dependency inversion:** we kept the existing ports (`IOrderRepository`, `IProductCatalog`, `IShipmentService`) unchanged.

## 5. Tests added or improved

| Stage | Tests | Content |
|---|---|---|
| Original | 11 | Use-case tests |
| + Characterization (run against the **original** code) | +28 = 39 | Rounding cases (1.01 at 10% gives 1.12, which proves it rounds up; per-unit rounding), unknown product saves nothing, empty request, item order and duplicates, zero and negative quantities, re-approving and re-rejecting, approving a shipped order, the shipment service seeing an Approved order, a failing shipment service, unknown ids |
| + Review fixes | +7 = 46 | Pinned `OrderStatus` numeric values, `Order.Restore`, read-only `Items` |
| + Test review | +28 = 74 | Full `Order` transition matrix (4 statuses × 3 actions), the totals invariant, product-name lookup behavior through a spy catalog, workflow tests from creation through approval to shipment over an in-memory repository |

**How existing tests changed:** only their setup. `TestOrders.WithStatus` and the product and category constructors replace the old object initializers. No assertion was removed or weakened; a reviewer verified that the assertion counts are identical.

**Incidental behavior:** five tests are tagged `[Trait("Category", "IncidentalBehavior")]`. They pin current but questionable behavior: `NullReferenceException` for an unknown order id or a null item list, and acceptance of negative quantities.

**Integration testing:** the application has no HTTP, database or other infrastructure boundary. The meaningful boundary is the use cases wired to in-memory doubles, which `Workflow/OrderWorkflowTest.cs` covers. We didn't invent any infrastructure.

## 6. Final build and test results

Run on `modernization/tell-dont-ask` at `1486c97`, the last code commit. This report was committed afterwards as `e65651c`, which adds only this file; the senior review re-ran the suite at `e65651c` with the same result.

```
dotnet build --no-incremental  ->  Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test (DOTNET_ROLL_FORWARD=Major)
  ->  Passed! - Failed: 0, Passed: 74, Skipped: 0, Total: 74 - TellDontAskKata.Tests.dll (netcoreapp3.1)
```

`git diff b9e1757..HEAD` touches only files under `csharpcore/`.

## 7. Behavior and API compatibility

- **Unchanged:**
  - Use case constructors and `Run` signatures, the request DTOs, all exception types and their namespace, and the three interfaces.
  - Rounding and computed amounts, the order of checks and the exception that wins, "no save on failure", and the shipment call sequence.
- **Intentional changes to the public domain API (the point of the kata):**
  - Parameterless constructors and public setters on `Order`, `OrderItem`, `Product` and `Category` are gone.
  - `Order.Items` is now an `IReadOnlyList<OrderItem>`.
- **Minor observable difference:** an `Order` built through `Order(id, status)` has an empty `Items` list instead of `null`. No caller relied on `null`.

## 8. Known limitations and remaining technical debt

- **The domain depends on the use-case and service namespaces.** The domain throws exceptions defined in `TellDontAskKata.Main.UseCase`, and `Order.Ship` depends on `Service.IShipmentService`. We avoided moving public types to keep compatibility.
- **`default(OrderStatus)` is still `Approved`.** We kept this for numeric compatibility.
- **No input validation.** Negative or zero quantities, null requests and unknown order ids behave as before, including the `NullReferenceException`s.
- **`AddItem` has no status guard.** Items can be added to approved or shipped orders, as before. `AddItem` is public, so nothing restricts it to creation time; only the creation use case calls it.
- **No order id assignment.** Created orders have `Id == 0`; assigning ids is the repository's job, and no real repository exists.
- **Currency is a hard-coded `"EUR"` string.** Amounts are raw `decimal`s, not a money value object.
- **The framework is out of support.** `netcoreapp3.1` is end-of-life, and tests need roll-forward on current machines.

## 9. Further improvements with more time

1. Upgrade to a supported target (`net8.0`) and current test packages, as a separate, behavior-neutral merge request.
2. Move the domain exceptions into a `Domain` namespace with a common `DomainException` base. This is a planned breaking change.
3. Introduce `Money`/`Currency` value objects and an explicit `Quantity` with validation, once the business confirms those rules.
4. Replace the `NullReferenceException` paths with explicit `OrderNotFoundException` and argument validation (a deliberate behavior change).
5. Decouple shipping, for example with a domain event or a use-case-level `EnsureCanBeShipped` / `MarkShipped` pair, if the domain should stop depending on `IShipmentService`.
6. Consolidate overlapping tests and add builders to cut setup duplication.
