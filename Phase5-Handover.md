# Phase 5 Handover — Faizan Cosmetics POS

**Status at handover: Phase 5 is COMPLETE.** This is not a mid-task handover — Phase 5 was
finished, tested (by hand-review; see caveat in §7), and is ready to package. The next session
should start **Phase 6 (Purchases & Suppliers)**, not resume Phase 5.

## 1. Project Overview

Faizan Cosmetics is a Windows desktop POS/Inventory/Khata (credit-ledger) management system for
a cosmetics retail business. Stack: C# / .NET 8 / WPF (MVVM via CommunityToolkit.Mvvm), EF Core 8
+ SQL Server (code-first migrations), Clean Architecture (Domain / Application / Infrastructure /
UI), DI via `Microsoft.Extensions.DependencyInjection`, Serilog file logging, BCrypt password
hashing, LiveChartsCore for the dashboard, xUnit + FluentAssertions + EF Core InMemory for tests.

## 2. Current Architecture

- **`FaizanCosmetics.Domain`** — entities/enums only, no dependencies. All entities for the
  *entire* planned system already exist here (Sales, Purchases, Returns, Khata, Suppliers,
  Adjustments, Audit, Settings) even where the corresponding module isn't built yet — this was a
  deliberate Phase 1 decision so the schema wouldn't keep changing across phases.
- **`FaizanCosmetics.Application`** — interfaces (`I*Service`, `I*Repository`, `IUnitOfWork`),
  DTOs, business exceptions (`Common/AppExceptions.cs`), and service implementations
  (`Services/*.cs`). No EF Core or DI package reference — depends only on Domain.
- **`FaizanCosmetics.Infrastructure`** — `ApplicationDbContext` + per-entity fluent
  configurations (`Data/Configurations/`), repository implementations, `UnitOfWork`,
  `DbInitializer` (idempotent seeding), `DependencyInjection.cs` (the composition-root
  registration module), cross-cutting services (`PasswordHasher`, `CurrentUserService`,
  `AuditService`, `InventoryService`).
- **`FaizanCosmetics.UI`** — WPF. `App.xaml.cs` is the composition root (builds the
  `ServiceProvider`, Serilog, global exception handling, calls `DbInitializer` on startup).
  ViewModels use `[ObservableProperty]`/`[RelayCommand]` (CommunityToolkit.Mvvm source
  generators). Views instantiated via WPF's implicit `DataTemplate` mechanism for
  navigated-to screens (parameterless constructors, `DataContext` inherited automatically —
  **never** set `DataContext` in the constructor of a `DataTemplate`-instantiated View, see
  `DashboardView.xaml.cs`'s comment) vs. DI-constructor-injected for modal `Window`s.
- **`FaizanCosmetics.Tests`** — xUnit. `Common/TestUnitOfWorkFactory.cs` wires real
  repository/service classes against an EF Core InMemory `ApplicationDbContext` — tests exercise
  actual production code, not mocks.

**Critical infrastructure pattern (read before touching `IUnitOfWork` or navigation):**
- `IUnitOfWork.ExecuteInTransactionAsync(Func<CancellationToken, Task> operation)` is the ONLY
  way to do a multi-step atomic write. It wraps everything in
  `Database.CreateExecutionStrategy().ExecuteAsync(...)` using the low-level 4-parameter
  overload (state = the operation delegate, TResult = dummy `bool`) — NOT one of
  `IExecutionStrategy`'s convenience overloads, which failed to resolve unambiguously against
  the user's exact EF Core package version (see §10). Falls back gracefully when
  `BeginTransactionAsync` throws `InvalidOperationException` (EF Core InMemory provider, used by
  tests, doesn't support relational transactions).
- `INavigationService`/`NavigationService` creates a **fresh DI scope per navigation** via
  `IServiceScopeFactory`, disposing the previous one. This was a Phase 5 bug fix (see §10) — do
  not revert to holding a raw root `IServiceProvider` in `NavigationService`.

## 3. Phase Status

- **Phases 1–4: complete.** Auth, MainWindow shell/navigation/Dashboard, Products/Categories/
  Inventory, Clients/Khata.
- **Phase 5: complete.** Sales Invoice posting/cancellation, New Invoice screen, Sales History.
- **Phase 6 (Purchases & Suppliers): not started.** This is the next task.

## 4. Completed Phase 5 Work

- `ITaxCalculationService` / `TaxCalculationService` — centralized tax math, both
  tax-exclusive (add on top) and tax-inclusive (extract embedded tax) modes.
  (`Application/Interfaces/ITaxCalculationService.cs`, `Application/Services/TaxCalculationService.cs`)
- `ISalesInvoiceService` / `SalesInvoiceService` — `PostInvoiceAsync` (full spec §9 posting
  sequence: validate → calculate item + proportionally-distributed invoice-level discount →
  tax via the centralized service → validate `PaidAmount ≤ GrandTotal` → block Walk-in due
  balances → credit-limit check with Manager/Admin override → post stock via
  `IInventoryService` → post Khata debit via `IClientLedgerService` — all inside one
  `ExecuteInTransactionAsync`) and `CancelAsync` (Admin/Manager only, reason required, reverses
  stock via a `SaleReturn` inventory transaction and reverses the Khata debit, never deletes the
  invoice). (`Application/Interfaces/ISalesInvoiceService.cs`,
  `Application/Services/SalesInvoiceService.cs`)
- New Invoice screen (`ViewModels/SalesInvoiceViewModel.cs`, `Views/SalesInvoiceView.xaml[.cs]`):
  barcode input with Enter-to-scan and auto-refocus, `Models/CartLine.cs` (live client-side
  total preview using the same `ITaxCalculationService`), Client/Product picker dialogs
  (F4/F3), item + invoice-level discount fields, payment panel, F2/F3/F4/F5/F6/F7/F8/Esc
  keyboard shortcuts (F2/F7 focus-only handled in code-behind per the project's code-behind
  policy; the rest are real `Command` bindings).
- `Views/ClientPickerWindow.xaml[.cs]` + `ViewModels/ClientPickerViewModel.cs`,
  `Views/ProductPickerWindow.xaml[.cs]` + `ViewModels/ProductPickerViewModel.cs` — reusable
  search-and-select dialogs.
- `Views/ReasonPromptWindow.xaml[.cs]` — reusable confirm-with-reason dialog (no ViewModel,
  intentionally plain code-behind — see its own doc comment), used for invoice cancellation and
  reusable for any future destructive-action-with-audit-trail flow.
- Sales History screen (`ViewModels/SalesHistoryViewModel.cs`,
  `Views/SalesHistoryView.xaml[.cs]`): paged/searchable by invoice number + date range, item
  detail panel, Cancel-with-reason button (visible only to Admin/Manager, both
  UI-hidden-and-service-enforced).
- Dashboard/MainWindowViewModel wiring: "New Invoice"/"Sales History" sidebar items and any
  related quick actions now navigate to real screens (previously Phase-2/3 placeholders).
- Tests: `tests/FaizanCosmetics.Tests/Sales/TaxCalculationServiceTests.cs` (4 tests),
  `tests/FaizanCosmetics.Tests/Sales/SalesInvoiceServiceTests.cs` (12 tests) — cash sale
  totals + stock deduction, item discount, paid-exceeds-total guard, Walk-in due-balance guard,
  insufficient stock leaves no partial invoice, credit-sale ledger debit, credit-limit block +
  Manager override, cancel-by-cashier blocked, cancel-by-manager reverses stock+ledger exactly,
  re-cancelling an already-cancelled invoice blocked.

## 5. Files Modified During Phase 5

**Application layer** (new): `Interfaces/ITaxCalculationService.cs`,
`Interfaces/ISalesInvoiceService.cs`, `Services/TaxCalculationService.cs`,
`Services/SalesInvoiceService.cs`, `DTOs/SalesInvoiceDtos.cs`.

**Application layer** (modified as part of the bug fix): `Interfaces/IUnitOfWork.cs` (removed
`BeginTransactionAsync`/`CommitTransactionAsync`/`RollbackTransactionAsync`, added
`ExecuteInTransactionAsync`), `Services/ProductService.cs` (updated `CreateAsync` to the new API).

**Infrastructure layer** (modified): `Repositories/UnitOfWork.cs` (transaction-strategy rewrite,
twice — see §10), `DependencyInjection.cs` (registered `ITaxCalculationService`,
`ISalesInvoiceService`).

**UI layer** (new): `Models/CartLine.cs`, `ViewModels/SalesInvoiceViewModel.cs`,
`ViewModels/SalesHistoryViewModel.cs`, `ViewModels/ClientPickerViewModel.cs`,
`ViewModels/ProductPickerViewModel.cs`, `Views/SalesInvoiceView.xaml[.cs]`,
`Views/SalesHistoryView.xaml[.cs]`, `Views/ClientPickerWindow.xaml[.cs]`,
`Views/ProductPickerWindow.xaml[.cs]`, `Views/ReasonPromptWindow.xaml[.cs]`.

**UI layer** (modified): `App.xaml.cs` (Phase 5 DI registrations), `Views/MainWindow.xaml`
(DataTemplates for the two new screens), `ViewModels/MainWindowViewModel.cs` (Sales section
wired to real screens; also the `IServiceProvider` → `IServiceScopeFactory` bug fix, see §10),
`Services/NavigationService.cs` (full rewrite for the DI-scoping bug fix, see §10),
`Views/FaizanCosmetics.UI.csproj` (removed empty `<ApplicationIcon />`, see §10).

**Tests** (new): `Sales/TaxCalculationServiceTests.cs`, `Sales/SalesInvoiceServiceTests.cs`.

## 6. Current Implementation State

The user can currently: log in, see a live Dashboard, manage Products/Categories/Low-Stock,
manage Clients/Khata (add/edit/deactivate, opening balance, receive payment, statement), **and
now ring up a full sale**: scan/search products into a cart, select a client (or Walk-in),
apply item and invoice discounts, take a partial or full payment, post the invoice (which
correctly deducts stock and — for credit sales — debits the client's Khata, blocked if it would
exceed their credit limit unless the cashier's session is Manager/Admin), and view/cancel past
invoices in Sales History with full stock/ledger reversal on cancel.

## 7. Unresolved Issues

- **No local .NET SDK in the development sandbox at any point in this project** — nothing has
  ever been compiled or run by the assistant. All code is hand-reviewed only. The user has hit
  and reported two real bugs already (an `IExecutionStrategy` overload-resolution failure, and
  the DI-scoping issue below) — expect more first-build friction is possible, though the
  patterns are now well-established and repeated across 5 phases without new categories of error
  in the last two rounds.
- **Two bug fixes in this delivery are unverified by the user yet**: (a) the
  `ExecuteInTransactionAsync` rewrite using the explicit 4-parameter `ExecuteAsync` overload,
  and (b) the `NavigationService` DI-scoping rewrite for the "clients don't appear in the list"
  report. Both are reasoned fixes for real, identified problems, but neither has been confirmed
  working by the user post-fix as of this handover.
- **No known compilation errors identified by review** beyond the above.
- **Design decision still open**: `User.MaxDiscountPercent` exists on the entity but is
  deliberately NOT enforced anywhere yet — the user explicitly chose "no limit for now, add
  later" when asked. Do not add enforcement without being asked; if asked, it belongs in
  `SalesInvoiceService.PostInvoiceAsync`'s per-item validation, checked against
  `ICurrentUserService.MaxDiscountPercent`.

## 8. Current Task

None in progress — Phase 5 is complete and ready to package/deliver. This handover document
itself was the last piece of work.

## 9. Exact Next Task

Start **Phase 6 — Purchases & Suppliers**, following the established pattern from Phases 3–5:

1. `IPurchaseInvoiceService`/`PurchaseInvoiceService` (Application) — receiving a purchase posts
   stock IN via the existing `IInventoryService` (type `InventoryTransactionType.Purchase`) and
   posts a Credit... actually a **Debit-to-supplier** entry via a new `ISupplierLedgerService`
   (mirror `IClientLedgerService`'s design exactly — `Domain/Entities/SupplierLedgerEntry.cs`
   already exists from Phase 1, same `Sum(Debit) - Sum(Credit)` balance-computation pattern to
   avoid the `GroupBy().OrderBy().First()` EF translation limitation already documented in
   `ClientLedgerRepository`). For a purchase, Credit increases what we owe the supplier
   (opposite sign convention from the client ledger — see `SupplierLedgerEntry`'s existing doc
   comment on the entity).
2. `ISupplierService`/`SupplierService` — CRUD mirroring `IClientService` (deactivate-only,
   never physical delete; no Walk-in-equivalent concept needed for suppliers).
2a. `ISupplierPaymentService` mirroring `IClientPaymentService`.
3. `PurchaseOrder`/`PurchaseOrderItem` entities already exist (Phase 1) but nothing uses them
   yet — spec calls for PO creation → receive-against-PO → PurchaseInvoice. Decide (and document
   in this file's successor) whether Phase 6 implements the full PO→receive workflow or a
   simpler direct-purchase-invoice-only flow first; either is defensible, but state the choice.
4. UI: Suppliers list/edit screen (mirror `ClientsView`/`ClientEditWindow`), a Purchase entry
   screen (mirror `SalesInvoiceView`'s cart pattern but for incoming stock — no barcode-scan
   urgency here, a product search/add-line flow is fine), Supplier Payments (mirror
   `ReceivePaymentWindow`), and wire the existing "PURCHASES" sidebar section's Placeholder
   items in `MainWindowViewModel.cs` to the real screens.
5. Tests mirroring `ClientServiceTests`/`ClientLedgerServiceTests`/`ClientPaymentServiceTests`
   and `SalesInvoiceServiceTests`' patterns.
6. Update `README.md`'s phase-status section and `Phase5-Handover.md` → rename/replace with a
   `Phase6-Handover.md` equivalent if instructed to produce one again.

## 10. Important Development Decisions

- **`ExecuteInTransactionAsync` must use the explicit 4-parameter `IExecutionStrategy.ExecuteAsync<TState,TResult>` overload**, not a convenience wrapper — this was tried once, failed to compile
  against the user's EF Core version, and was fixed. Do not "simplify" this back to
  `strategy.ExecuteAsync(async () => {...})`.
- **`NavigationService` must create a fresh `IServiceScopeFactory`-based scope per navigation**
  and dispose the previous one. Do not change it back to holding a raw `IServiceProvider`. Any
  ViewModel that opens modal dialogs via an injected `IServiceProvider` relies on that provider
  being the current navigation scope's provider (automatic DI behavior, not something to
  special-case per ViewModel).
- **Invoice-level discount is distributed proportionally across line subtotals before tax**,
  with the last line absorbing any rounding remainder — not applied as a flat post-tax
  deduction. If Phase 6's purchase-side ever needs an analogous invoice-level discount, follow
  the same pattern (`SalesInvoiceService.PostInvoiceAsync`'s two-pass line-calculation loop).
- **Discount-permission enforcement (`MaxDiscountPercent`) is explicitly deferred** per user
  instruction — do not add it speculatively.
- **Client-side (`CartLine`) tax preview is intentionally approximate** (doesn't account for
  invoice-level discount's effect on the taxable base) — documented in code as preview-only;
  the server (`SalesInvoiceService`) is always authoritative. Same pattern should apply to any
  Phase 6 purchase-entry cart preview.

## 11. Do Not Change

- The Domain entity schema for anything already modeled (Sales*, Client*, Purchase*, Supplier*,
  Inventory*, Audit, AppSetting) — it was deliberately modeled in full during Phase 1 to avoid
  churn; Phase 6 should consume existing `PurchaseOrder`/`PurchaseInvoice`/`SupplierLedgerEntry`
  entities as-is.
- The `IUnitOfWork` repository-per-aggregate pattern and its `ExecuteInTransactionAsync` method.
- The WPF navigation pattern: DI-resolved ViewModel → implicit `DataTemplate` → parameterless
  View with inherited `DataContext` for navigated screens; DI-constructor-injected `Window` for
  modals. Do not mix the two (e.g. don't give a `DataTemplate`-instantiated View a
  ViewModel-injecting constructor — this was tried once in Phase 2 and reverted; see
  `DashboardView.xaml.cs`'s comment).
- The soft-delete-only (`IsActive`/deactivate, never physical delete) convention for Products,
  Categories, Clients — apply the same to Suppliers in Phase 6.
- The `Sum(Debit) - Sum(Credit)` balance-computation pattern in `ClientLedgerRepository` (avoids
  an EF Core `GroupBy` + `OrderBy().First()` translation limitation) — replicate exactly for
  `SupplierLedgerRepository` in Phase 6.
