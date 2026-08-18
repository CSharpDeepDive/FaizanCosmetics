# Faizan Cosmetics — POS, Inventory & Khata Management

## Status: Phase 6 of 10 complete

**Phase 1–4**: solution structure, full Domain schema, EF Core data layer, DI, logging, auth
(Phase 1) · MainWindow shell, navigation, Dashboard (Phase 2) · Products/Categories/Inventory,
`IInventoryService` (Phase 3) · Clients/Khata, `IClientLedgerService` (Phase 4).

**Phase 5**: Sales Invoice posting/cancellation (`ISalesInvoiceService`), centralized
`ITaxCalculationService`, the New Invoice screen (barcode-driven cart, F2–F8/Esc shortcuts),
Sales History. Plus two bug fixes from user reports: the `IExecutionStrategy.ExecuteAsync`
overload-resolution build error (fixed by calling the low-level 4-parameter overload the
compiler's own error message named), and a DI-scoping fix so `NavigationService` creates a
fresh `IServiceScopeFactory`-based scope per screen visit instead of every screen sharing one
long-lived `ApplicationDbContext` for the whole app session.

**Two more fixes at the start of this delivery** (both from your reports): a confirmed XAML
crash on New Invoice — `TileLabel`/`TileValue`/`TileBorder`/`QuickActionButton` styles were
defined *locally* inside `DashboardView.xaml`'s own `<UserControl.Resources>`, invisible to any
other screen; `SalesInvoiceView.xaml` referenced `TileLabel` and crashed with
`XamlParseException`. Fixed by promoting all four styles into the global
`Styles/Controls.xaml`, and a scripted audit (every `StaticResource` reference checked against
global + local definitions, not a manual spot-check) confirmed no other view has this problem —
that audit now runs before every delivery going forward. Separately, the "clients added but
list stays empty" report couldn't be pinned down to a second certain root cause through code
review alone, so `ClientsViewModel` now logs every caught exception via Serilog (previously
only truly *unhandled* exceptions were ever logged — a real gap against spec §39) and
temporarily surfaces the exact exception text on screen so the real cause can be identified with
certainty next time, instead of guessed at further.

**Branding** (from Phase 4, still current): a vector "Aetheria Labs" mark on the Login footer
and MainWindow top bar, kept secondary to the store's own "Faizan Cosmetics" identity.

**Phase 6** (this delivery): **Purchases and Suppliers**, using direct Purchase Invoice entry —
no `PurchaseOrder` → receive workflow yet (that entity exists in the Domain from Phase 1 but
nothing uses it; deferred and documented, not forgotten — see `Phase5-Handover.md`'s decision
point, which this phase follows).

- `ISupplierLedgerService` — mirrors `IClientLedgerService`'s design exactly, including the
  `Sum(Debit) - Sum(Credit)` balance-computation pattern that avoids a real EF Core
  `GroupBy().OrderBy().First()` translation limitation. Sign convention is reversed from the
  client ledger: Credit increases what *we* owe the supplier (a purchase), Debit decreases it
  (a payment we make) — documented on `SupplierLedgerEntry` itself.
- `ISupplierService` / `ISupplierPaymentService` — mirror `IClientService`/`IClientPaymentService`
  (deactivate-only, never physical delete; general on-account payments, not yet allocated
  against specific invoices — same honestly-scoped limitation as the client side).
- `IPurchaseInvoiceService.PostInvoiceAsync` — mirrors `ISalesInvoiceService`'s atomic posting
  pattern: validates items, calculates item-level discount and tax via the same centralized
  `ITaxCalculationService`, posts stock in via Phase 3's `IInventoryService`
  (`InventoryTransactionType.Purchase`), and posts a supplier ledger Credit for any due amount
  — all inside one `ExecuteInTransactionAsync`. Deliberately does **not** update
  `Product.PurchasePrice` — receiving a purchase at a negotiated cost shouldn't silently change
  the product's standing price (and its audit trail); that stays `IProductService.UpdateAsync`'s
  job, with proper `ProductPriceHistory` logging. No invoice-level discount on purchases (item-
  level only) — a deliberately simpler scope than Sales, documented on the interface.
- **UI**: Suppliers list/edit/statement/pay-supplier screens (mirroring the Client screens
  exactly), a Purchase entry screen (mirrors New Invoice's cart pattern, product-picker-driven
  rather than barcode-urgent since receiving stock isn't a checkout-speed scenario), Purchase
  History. The sidebar's Suppliers/Purchases/Purchase History/Supplier Payments entries and the
  Dashboard's "Purchase" quick action are now wired to these real screens.
- **Tests**: 14 new tests (`SupplierLedgerServiceTests`, `SupplierServiceTests`,
  `SupplierPaymentServiceTests`, `PurchaseInvoiceServiceTests`) covering the reversed ledger
  sign convention, opening balance in both directions, purchase totals with item discount,
  paid-exceeds-total guard, partial-payment ledger posting, fully-paid leaving no ledger entry,
  and the "receiving a purchase must not change the product's standing price" guarantee. 63
  tests total across the whole project now.
- **Scripted consistency audits** (not manual spot-checks) now run every phase: every
  `StaticResource` reference resolves against either the global stylesheet or a local
  definition in the same file; every DI-registered ViewModel/Window has a matching file and vice
  versa; every navigable ViewModel has a `DataTemplate`.

**Honestly scoped**: discount percentage is still not validated against a cashier's
`MaxDiscountPercent` (per your Phase 5 instruction) — unchanged this phase. Receipt printing
(F8) still shows an honest "arrives in Phase 9" message.

## Important — I could not compile-check this code

Still no local .NET SDK in this sandbox. Everything here — including all four bug fixes — was
diagnosed from your error messages/screenshots and reviewed by hand, not from a local repro.
Please rebuild (Clean Solution → Rebuild Solution) and re-test: the original transaction-strategy
crash, the New Invoice `TileLabel` crash, the empty Clients list (now with a diagnostic message
that should tell us definitively what's happening if it recurs), and the new Phase 6 screens.

## Prerequisites

- Visual Studio 2022 (17.8+) or `dotnet` CLI, .NET 8 SDK, with the **.NET desktop development**
  workload (for WPF)
- SQL Server or SQL Server Express (default connection string targets `.\SQLEXPRESS`)
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## First-time setup

```bash
git clone <this-folder-as-a-repo>  # or just open FaizanCosmetics.sln
cd FaizanCosmetics

# Restore all packages
dotnet restore

# Create the initial migration (Infrastructure holds the DbContext + migrations)
dotnet ef migrations add InitialCreate ^
  --project src/FaizanCosmetics.Infrastructure ^
  --startup-project src/FaizanCosmetics.UI

# Apply it (creates FaizanCosmeticsDb on .\SQLEXPRESS) — or just run the app, which calls
# Database.MigrateAsync() automatically on startup
dotnet ef database update ^
  --project src/FaizanCosmetics.Infrastructure ^
  --startup-project src/FaizanCosmetics.UI

# Run
dotnet run --project src/FaizanCosmetics.UI
```

(Use `\` instead of `^` for line continuation on macOS/Linux/PowerShell — these commands are
still only meaningful on Windows since WPF is Windows-only, but `dotnet ef migrations add` can
be run from any OS if you just want to inspect the generated migration.)

Connection string lives in `src/FaizanCosmetics.UI/appsettings.json` — edit
`ConnectionStrings:DefaultConnection` if your SQL Server instance name differs from
`.\SQLEXPRESS`.

## First login

- Username: `admin`
- Password: `Admin@123`

You will be **forced to change this password immediately** — the app will not let you into
the main window until you do, by design (Phase 1, requirement #45).

## Solution layout

```
FaizanCosmetics.sln
src/
  FaizanCosmetics.Domain/          entities, enums — no dependencies on anything else
  FaizanCosmetics.Application/     interfaces, DTOs, business exceptions, AuthService
  FaizanCosmetics.Infrastructure/  EF Core DbContext + configurations, repositories,
                                    Unit of Work, password hashing, audit logging, DI wiring,
                                    database seeding (admin user, Walk-in Customer, categories,
                                    default settings)
  FaizanCosmetics.UI/               WPF app: App.xaml.cs composition root, Login + Change
                                    Password + Main shell windows, MVVM ViewModels, styles
tests/
  FaizanCosmetics.Tests/           test project scaffolded (xUnit + FluentAssertions +
                                    EF Core InMemory); test cases land in Phase 10 per the
                                    build plan, alongside the modules they test
```

## What's next (Phase 7)

Returns & Inventory Adjustments: Sales Returns (select a posted invoice, choose returned
quantities validated against what's already been returned, increase stock, adjust the client's
Khata), Purchase Returns (mirror, decrease stock, adjust the supplier ledger), and a manual
Inventory Adjustment screen (Damage/Theft/Expiry/Stock Correction/Opening Stock/Other reasons,
fully audited) — all posting through the existing `IInventoryService`/`IClientLedgerService`/
`ISupplierLedgerService` rather than new stock-math logic. See `Phase6-Handover.md` for the
precise starting point. Say "continue" and I'll pick up exactly there.
