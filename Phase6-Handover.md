# Phase 6 Handover — Faizan Cosmetics POS

**Status at handover: Phase 6 is COMPLETE.** Not a mid-task handover. The next session should
start **Phase 7 (Returns & Inventory Adjustments)**.

## 1. Project Overview

Faizan Cosmetics is a Windows desktop POS/Inventory/Khata (credit-ledger) management system for
a cosmetics retail business. Stack: C# / .NET 8 / WPF (MVVM via CommunityToolkit.Mvvm), EF Core 8
+ SQL Server (code-first migrations), Clean Architecture (Domain / Application / Infrastructure /
UI), DI via `Microsoft.Extensions.DependencyInjection`, Serilog file logging, BCrypt password
hashing, LiveChartsCore for the dashboard, xUnit + FluentAssertions + EF Core InMemory for tests.

## 2. Current Architecture

Unchanged from Phase 5's handover — re-reading that document's §2 is still accurate. Key points
repeated here for convenience:

- **`FaizanCosmetics.Domain`** — entities/enums only. All entities for the entire planned system
  already exist (modeled in Phase 1), even for modules not yet built.
- **`FaizanCosmetics.Application`** — interfaces, DTOs, business exceptions, service
  implementations. No EF Core/DI package reference.
- **`FaizanCosmetics.Infrastructure`** — `ApplicationDbContext` + configurations, repositories,
  `UnitOfWork`, `DbInitializer`, `DependencyInjection.cs` (composition-root registration).
- **`FaizanCosmetics.UI`** — WPF, MVVM. `App.xaml.cs` is the composition root. ViewModels use
  `[ObservableProperty]`/`[RelayCommand]`. Views instantiated via implicit `DataTemplate` for
  navigated screens (parameterless constructor, inherited `DataContext` — **never** set
  `DataContext` explicitly in a `DataTemplate`-instantiated View's constructor) vs.
  DI-constructor-injected for modal `Window`s.
- **`FaizanCosmetics.Tests`** — xUnit against `TestUnitOfWorkFactory`'s real
  repository/service wiring over EF Core InMemory.

**Critical patterns established through Phase 5, still in force:**
- `IUnitOfWork.ExecuteInTransactionAsync(Func<CancellationToken, Task> operation)` is the ONLY
  way to do a multi-step atomic write. Implementation in `UnitOfWork.cs` uses the explicit
  4-parameter `IExecutionStrategy.ExecuteAsync<TState,TResult>` overload — do not "simplify" this
  back to a convenience overload; that exact simplification broke the build once already (see
  Phase 6 bug-fix log below).
- `INavigationService`/`NavigationService` creates a fresh DI scope per navigation via
  `IServiceScopeFactory`, disposing the previous one. Do not revert to a raw root
  `IServiceProvider`.
- **Style resources must be global** (`Styles/Controls.xaml` or `Styles/Colors.xaml`/
  `Typography.xaml`), never defined only inside one View's local `<UserControl.Resources>` if
  any other View might reasonably reuse them — this caused a real crash in Phase 6 (see below).
  A scripted audit (see §11) now checks this before every delivery.
- The `Sum(Debit) - Sum(Credit)` (client ledger) / `Sum(Credit) - Sum(Debit)` (supplier ledger,
  reversed sign) balance-computation pattern — never read the last row's stored running-`Balance`
  column for aggregate queries; `GroupBy().OrderBy().First()` doesn't reliably translate to SQL
  in EF Core.

## 3. Phase Status

- **Phases 1–5: complete.** Auth, shell/nav/Dashboard, Products/Inventory, Clients/Khata, Sales
  Invoice posting/cancellation.
- **Phase 6: complete.** Suppliers, Supplier Ledger, Supplier Payments, direct Purchase Invoice
  entry, Purchase History.
- **Phase 7 (Returns & Inventory Adjustments): not started.** Next task — see §9.

## 4. Completed Phase 6 Work

- `ISupplierLedgerRepository`/`SupplierLedgerRepository`, `ISupplierPaymentRepository`/
  `SupplierPaymentRepository`, `IPurchaseInvoiceRepository`/`PurchaseInvoiceRepository` — new
  repositories, added to `IUnitOfWork`/`UnitOfWork` (now 12 repository properties) and to
  `TestUnitOfWorkFactory`.
- `ISupplierLedgerService`/`SupplierLedgerService` — mirrors `IClientLedgerService` with the
  reversed Credit/Debit sign convention (`Application/Interfaces/ISupplierLedgerService.cs`,
  `Application/Services/SupplierLedgerService.cs`).
- `ISupplierService`/`SupplierService`, `ISupplierPaymentService`/`SupplierPaymentService` —
  mirror the Client equivalents exactly in shape.
- `IPurchaseInvoiceService`/`PurchaseInvoiceService` — `PostInvoiceAsync` (item validation, item
  discount + centralized tax, atomic stock-in + supplier-ledger-credit posting) and read methods
  (`GetByIdAsync`, `SearchAsync`). No `CancelAsync` yet — purchase cancellation/reversal is not
  in Phase 6's scope (not requested, not built; Phase 7's Purchase Returns is the intended path
  for correcting a bad purchase, not a direct cancel).
- UI: `SuppliersViewModel`/`SuppliersView` (paged/searchable list), `SupplierEditViewModel`/
  `SupplierEditWindow` (add/edit with opening balance), `SupplierStatementViewModel`/
  `SupplierStatementWindow` (date-filtered ledger statement, mirrors Khata Statement),
  `SupplierPaymentViewModel`/`SupplierPaymentWindow` (mirrors Receive Payment),
  `SupplierPickerViewModel`/`SupplierPickerWindow` (mirrors Client Picker),
  `PurchaseInvoiceViewModel`/`PurchaseInvoiceView` (cart-based entry screen, product-picker-
  driven, no barcode urgency), `PurchaseHistoryViewModel`/`PurchaseHistoryView` (mirrors Sales
  History, no cancel button since `CancelAsync` doesn't exist for purchases).
- `Models/PurchaseCartLine.cs` — mirrors `CartLine` but `UnitCost` is editable (the negotiated
  cost for this receipt) rather than fixed to a selling price.
- Navigation: `MainWindowViewModel`'s PURCHASES section and the Dashboard's "Purchase" quick
  action now point at real screens.
- Tests: `Suppliers/SupplierLedgerServiceTests.cs`, `Suppliers/SupplierServiceTests.cs`,
  `Suppliers/SupplierPaymentServiceTests.cs`, `Purchases/PurchaseInvoiceServiceTests.cs` — 14
  tests total, including a specific test asserting a purchase does NOT silently change
  `Product.PurchasePrice`.

### Bug fixes made at the start of this Phase 6 session (before new feature work)

1. **New Invoice `XamlParseException`** (`Cannot find resource named 'TileLabel'`): confirmed
   via screenshot. Root cause: `TileLabel`/`TileValue`/`TileBorder`/`QuickActionButton` were
   defined only inside `DashboardView.xaml`'s local `<UserControl.Resources>`, and
   `SalesInvoiceView.xaml` referenced `TileLabel` — invisible across Views.
   **Fix**: promoted all four styles into `Styles/Controls.xaml` (global), removed the
   now-duplicate local definitions from `DashboardView.xaml`. A scripted audit (bash, see §11)
   was run confirming zero other instances of this bug pattern anywhere in the codebase.
2. **"Clients added but list stays empty"** (reported again after the Phase 5 DI-scoping fix,
   meaning that fix — while a real, valid improvement — was likely not the actual root cause of
   this specific symptom). Exhaustive code review of `ClientRepository.SearchAsync`,
   `ClientService.SearchAsync`, `ClientsViewModel.LoadAsync`, `ClientService.CreateAsync`, and
   the DI/scope chain found no additional certain bug. **Fix applied**: `ClientsViewModel` now
   injects `ILogger<ClientsViewModel>` and logs every caught exception (previously silently
   discarded — a real gap against spec §39, "log technical details to a log file"), and
   temporarily surfaces `ex.GetType().Name: ex.Message` directly in the on-screen error banner,
   plus a distinct diagnostic message when the query succeeds with zero results. **This is not
   confirmed to fix the underlying issue** — it's confirmed to make the issue diagnosable. See
   §7 and §9 for what the next session should do with whatever the user reports back.

## 5. Files Modified During Phase 6

**Application layer** (new): `Interfaces/ISupplierLedgerRepository.cs`,
`Interfaces/ISupplierPaymentRepository.cs`, `Interfaces/IPurchaseInvoiceRepository.cs`,
`Interfaces/ISupplierService.cs`, `Interfaces/ISupplierLedgerService.cs`,
`Interfaces/ISupplierPaymentService.cs`, `Interfaces/IPurchaseInvoiceService.cs`,
`DTOs/SupplierDtos.cs`, `DTOs/PurchaseInvoiceDtos.cs`, `Services/SupplierLedgerService.cs`,
`Services/SupplierService.cs`, `Services/SupplierPaymentService.cs`,
`Services/PurchaseInvoiceService.cs`.

**Application layer** (modified): `Interfaces/IUnitOfWork.cs` (added `SupplierLedgers`,
`SupplierPayments`, `PurchaseInvoices` properties).

**Infrastructure layer** (new): `Repositories/SupplierLedgerRepository.cs`,
`Repositories/SupplierPaymentRepository.cs`, `Repositories/PurchaseInvoiceRepository.cs`.

**Infrastructure layer** (modified): `Repositories/UnitOfWork.cs` (constructor + properties for
the three new repos), `DependencyInjection.cs` (registered the three new repos + four new
Application services).

**UI layer** (new): `Models/PurchaseCartLine.cs`, `ViewModels/SuppliersViewModel.cs`,
`ViewModels/SupplierEditViewModel.cs`, `ViewModels/SupplierStatementViewModel.cs`,
`ViewModels/SupplierPaymentViewModel.cs`, `ViewModels/SupplierPickerViewModel.cs`,
`ViewModels/PurchaseInvoiceViewModel.cs`, `ViewModels/PurchaseHistoryViewModel.cs`,
`Views/SuppliersView.xaml[.cs]`, `Views/SupplierEditWindow.xaml[.cs]`,
`Views/SupplierStatementWindow.xaml[.cs]`, `Views/SupplierPaymentWindow.xaml[.cs]`,
`Views/SupplierPickerWindow.xaml[.cs]`, `Views/PurchaseInvoiceView.xaml[.cs]`,
`Views/PurchaseHistoryView.xaml[.cs]`.

**UI layer** (modified): `App.xaml.cs` (Phase 6 DI registrations), `Views/MainWindow.xaml`
(DataTemplates for the 4 new navigable screens), `ViewModels/MainWindowViewModel.cs` (PURCHASES
section wired to real screens), `ViewModels/DashboardViewModel.cs` (Purchase quick action
wired), `Styles/Controls.xaml` (promoted `TileLabel`/`TileValue`/`TileBorder`/
`QuickActionButton` — bug fix), `Views/DashboardView.xaml` (removed now-duplicate local resource
definitions — bug fix), `ViewModels/ClientsViewModel.cs` (added `ILogger` + diagnostic error
surfacing — bug-diagnosis fix, see §4).

**Tests** (new): `Common/TestUnitOfWorkFactory.cs` (modified — wired 3 new repos),
`Suppliers/SupplierLedgerServiceTests.cs`, `Suppliers/SupplierServiceTests.cs`,
`Suppliers/SupplierPaymentServiceTests.cs`, `Purchases/PurchaseInvoiceServiceTests.cs`.

## 6. Current Implementation State

The user can now, in addition to everything through Phase 5: manage Suppliers (add/edit/
deactivate/reactivate), view a Supplier Statement with date filtering, pay a supplier down
(general on-account payment), and enter a Purchase Invoice (pick supplier via picker, add
product lines via product picker, set quantity/negotiated unit cost/item discount per line,
take a partial or full payment, post) which correctly increases stock and — for any unpaid
portion — credits the supplier ledger (increasing what's owed). Purchase History shows past
purchases with an item-detail panel; there is no cancel/return capability yet (Phase 7).

## 7. Unresolved Issues

- **No local .NET SDK anywhere in this project's history** — nothing has ever been compiled or
  run by the assistant. All code is hand-reviewed only, cross-checked with scripted greps for
  the specific bug classes already encountered (missing DI registrations, orphaned
  `StaticResource` references, missing `using FaizanCosmetics.UI;` for `App.Services` calls,
  `IUnitOfWork` constructor/property consistency). These scripted checks passed clean as of this
  handover, but they only catch what they're written to catch.
- **"Clients list stays empty" is NOT confirmed fixed** — see §4, item 2. The fix ships
  diagnostics, not a confirmed root-cause fix. **The exact next thing to do when the user reports
  back is in §9.**
- **Four bug fixes across Phases 5–6 are unverified by the user as of this handover**: the
  `ExecuteInTransactionAsync` rewrite, the `NavigationService` DI-scoping rewrite, the
  `TileLabel` global-resource fix, and the `ClientsViewModel` diagnostic logging. The `TileLabel`
  fix is the most likely to be genuinely confirmed-correct on inspection alone (it's a
  straightforward, mechanical XAML resource-scope fix, and the scripted audit found zero other
  instances of the pattern) — but "likely correct on review" is not the same as "user-confirmed."
- **No known NEW compilation errors identified by review** in Phase 6's code specifically.
- Purchase invoices have no `CancelAsync` — if a Phase 6 purchase needs correcting before Phase
  7 exists, there is currently no UI path to do that (by design/scope, not an oversight — but
  worth knowing if the user asks).

## 8. Current Task

None in progress — Phase 6 is complete and ready to package/deliver. This handover document
itself was the last piece of work, created proactively (not because of an actual detected usage
limit — the assistant has no reliable way to observe that directly) per explicit user instruction
to produce one as a deliverable.

## 9. Exact Next Task

**First**, if the user has reported back on the Clients-list diagnostic message (§4 item 2, §7):
read whatever `ClientsViewModel`'s on-screen error now says (either the logged exception type/
message, or the "completed with no error, 0 results" diagnostic) and fix the ACTUAL root cause
it reveals — don't re-guess. Once fixed, **revert the temporary diagnostics**: remove the
`ex.GetType().Name: ex.Message` text from the `ErrorMessage` in the catch block (replace with a
plain friendly message) and remove the "Diagnostic: ... 0 results" message for the zero-results
case, per the project's normal "never show technical details to users" policy — these were
explicitly temporary. Keep the `ILogger` call in the catch block; that part is a permanent,
legitimate fix for spec §39 and should probably be extended to other ViewModels' catch blocks
over time (not urgently, but worth doing when convenient).

**Then**, start **Phase 7 — Returns & Inventory Adjustments**:

1. `ISalesReturnService`/`SalesReturnService` — user selects a Posted `SalesInvoice`, sees its
   items, selects a return quantity per line (validated: `returnQty ≤ soldQty - alreadyReturned`,
   using `SalesInvoiceItem.QuantityReturned`, which already exists on the entity from Phase 1
   but nothing increments it yet — Phase 7 must start updating it). Posts a `SalesReturn` +
   `SalesReturnItem` rows (entities exist, unused so far), increases stock via the existing
   `IInventoryService` (`InventoryTransactionType.SaleReturn`), and credits the client ledger via
   the existing `IClientLedgerService` if the original sale had a due amount tied to it — mirror
   `SalesInvoiceService.CancelAsync`'s reversal logic closely, since a full-invoice cancel is
   really "return everything," so the return math should be a generalization of it, not a
   from-scratch design.
2. `IPurchaseReturnService`/`PurchaseReturnService` — mirror, for `PurchaseInvoiceItem`/
   `PurchaseReturn`/`PurchaseReturnItem` (entities exist, unused), decreasing stock and debiting
   the supplier ledger (opposite sign convention, as established).
3. `IInventoryAdjustmentService`/`InventoryAdjustmentService` — the `InventoryAdjustment` entity
   already exists (Phase 1, unused). Reasons: Damage/Theft/Expiry/StockCorrection/OpeningStock/
   Other (enum `AdjustmentReason` already exists). Posts one `InventoryTransaction` via the
   existing `IInventoryService` (`AdjustmentIncrease`/`AdjustmentDecrease`/`Damage`/`Theft`/
   `Expiry`/`OpeningStock` — all already exist in `InventoryTransactionType`). Fully audited.
4. UI: Sales Return screen (select invoice → line items with return-qty inputs → post), Purchase
   Return screen (mirror), Inventory Adjustment screen (product picker + reason + quantity +
   notes). Wire the existing "Sales Return"/"Purchase Returns"/"Stock Adjustment" Placeholder
   items in `MainWindowViewModel.cs` to the real screens.
5. Tests mirroring the established pattern.
6. Update `README.md`'s status section and produce `Phase7-Handover.md` if instructed to again.

## 10. Important Development Decisions

All of Phase 5-Handover.md's §10 still applies (transaction-strategy overload, navigation
scoping, invoice-level discount distribution, discount-permission deferral, client-side tax
preview approximation). Additionally, from Phase 6:

- **Purchases use direct entry, no PO workflow** — this was an open decision flagged in
  Phase5-Handover.md and has now been made and executed. Do not add PO→receive workflow
  speculatively; if the user asks for it, it's a new, separate task built on top of the existing
  `PurchaseOrder`/`PurchaseOrderItem` entities (unused so far).
- **Receiving a purchase never updates `Product.PurchasePrice`** — this is deliberate (see §4)
  and tested (`PostInvoiceAsync_DoesNotChangeProductStandingPurchasePrice`). Do not "fix" this by
  making purchases auto-update the standing price; if the business wants that behavior, it's a
  product decision to raise with the user, not a default to assume.
- **All style resources must live in the global `Styles/*.xaml` files**, never defined only
  locally in one View if there's any chance another View reuses the key name. Run the scripted
  audit (§11) before every future delivery, not just when a crash is reported.
- **Temporary diagnostic code is temporary** — the `ClientsViewModel` exception-message-on-screen
  and the zero-results diagnostic message are explicitly NOT meant to ship long-term. See §9.

## 11. Do Not Change

Everything in Phase5-Handover.md's §11 still applies (Domain schema, `IUnitOfWork` pattern,
DataTemplate-vs-DI-constructor View pattern, soft-delete-only convention, ledger balance
computation pattern — now proven out identically on both Client and Supplier sides). Additionally:

- The now-global `TileLabel`/`TileValue`/`TileBorder`/`QuickActionButton` styles in
  `Styles/Controls.xaml` — do not move them back to being locally scoped in any single View.
- Run this consistency audit (or equivalent) before every future delivery — it has caught real
  bugs twice now and costs almost nothing to run:
  ```bash
  # ViewModels/Windows registered in DI vs files on disk
  grep -oE '[A-Za-z]+ViewModel>' src/FaizanCosmetics.UI/App.xaml.cs | sed 's/>$//' | sort -u \
    > /tmp/reg_vms.txt
  ls src/FaizanCosmetics.UI/ViewModels/*.cs | xargs -n1 basename | sed 's/.cs$//' | sort -u \
    > /tmp/actual_vms.txt
  diff /tmp/reg_vms.txt /tmp/actual_vms.txt   # expect only ViewModelBase as a diff

  # Every StaticResource reference resolves globally or locally within its own file
  grep -ohP 'x:Key="\K[^"]+' src/FaizanCosmetics.UI/Styles/*.xaml src/FaizanCosmetics.UI/App.xaml \
    | sort -u > /tmp/global_keys.txt
  for f in src/FaizanCosmetics.UI/Views/*.xaml; do
    used=$(grep -ohP 'StaticResource \K[^}]+' "$f" | sort -u)
    local=$(grep -ohP 'x:Key="\K[^"]+' "$f" | sort -u)
    for key in $used; do
      grep -qx "$key" /tmp/global_keys.txt || echo "$local" | grep -qx "$key" || \
        echo "MISSING: '$key' used in $f"
    done
  done
  ```
- Purchase invoices' lack of `CancelAsync` — don't add it speculatively; Phase 7's Purchase
  Return is the intended correction path.
