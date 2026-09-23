# CarePlus Pharmacy ERP & CRM - Implementation Progress

## Phase 1: Remove Hero Banners & Clean Toolbars
- [x] Views/Home/Index.cshtml (Dashboard banner removed, buttons relocated)
- [x] Views/Medicines/Expiring.cshtml (Expiry Monitor banner removed, buttons relocated)
- [x] Views/Crm/Index.cshtml (CRM banner removed, buttons relocated)
- [x] Search ALL other views and CSS for hero/banner patterns and remove dead banner CSS
- [x] Ensure no empty gap at the top of pages

## Phase 2: Stock by Category Doughnut & Dashboard Chart Polish
- [x] Fixed ~320px container with maintainAspectRatio: false
- [x] Legend hidden (`legend: { display: false }`)
- [x] HoverOffset ~8, pointer cursor on hover, touch support
- [x] Tooltip format: "Category - 660 units (11.2%)"
- [x] Unique color per category generated from category count (dynamic HSL palette / curated fallback)
- [x] Matching units tooltip on Top-Selling bar chart
- [x] Data passed cleanly with `@Json.Serialize`

## Phase 3: Numeric Right-Alignment
- [x] site.css: `th.num, td.num { text-align:right; font-variant-numeric:tabular-nums; padding-right:20px; }`
- [x] site.css: separate rule for inputs (`input.num, .form-control.num { text-align: right; font-variant-numeric: tabular-nums; }`)
- [x] Currency formatted as ₱1,560.00 via shared conventions with units in headers
- [x] Negative/discount values aligned properly
- [x] Form inputs right-aligned: Sales/Create, Medicines Create/Edit/AddBatch, PurchaseOrders/Create, Subscriptions forms, Sales/Billing Details, CRM modal
- [x] EXCLUSIONS respected: "Items Dispensed" (left-aligned), Expiry Date cell (left-aligned)

## Phase 4: Pagination on ALL Lists
- [x] `Models/PaginatedList.cs`: Single generic `PaginatedList<T>` with `CountAsync`, `Skip/Take` in SQL, clamping `page >= 1`, allowed page sizes `[10, 25, 50, 100]` default 10, empty handled (`TotalPages = 1, Skip(0)`)
- [x] `Views/Shared/_Pagination.cshtml`: Showing X–Y of Z, rows per page dropdown, First/Prev/Numbered with ellipsis/Next/Last, brand-green active page, preserves all query parameters, empty state
- [x] Module 1: Branches (`BranchesController.cs` & `Views/Branches/Index.cshtml`)
- [x] Module 2: Sales (`SalesController.cs` & `Views/Sales/Index.cshtml`) - KPI cards independent of page
- [x] Module 3: Billing (`BillingController.cs` & `Views/Billing/Index.cshtml`) - Summary metrics independent of page
- [x] Module 4: Prescriptions (`PrescriptionsController.cs` & `Views/Prescriptions/Index.cshtml`) - Status counts independent of page
- [x] Module 5: Medicines (`MedicinesController.cs` & `Views/Medicines/Index.cshtml`) - Stock/valuation metrics independent of page
- [x] Module 6: Medicines/Expiring (`MedicinesController.cs` & `Views/Medicines/Expiring.cshtml`) - Tab query parameter, full dataset counts/valuations across tabs, print all filtered
- [x] Module 7: Suppliers (`SuppliersController.cs` & `Views/Suppliers/Index.cshtml`)
- [x] Module 8: Purchase Orders (`PurchaseOrdersController.cs` & `Views/PurchaseOrders/Index.cshtml`)
- [x] Module 9: CRM Patients (`CrmController.cs` & `Views/Crm/Index.cshtml`) - Full ledger summary
- [x] Module 10: Customers (`CustomersController.cs` & `Views/Customers/Index.cshtml`)
- [x] Module 11: Subscriptions Plans & DueRefills (`SubscriptionsController.cs`, `Views/Subscriptions/Plans.cshtml`, `Views/Subscriptions/DueRefills.cshtml`, `Views/Subscriptions/MySubscriptions.cshtml`)
- [x] Module 12: Audit Logs (`AuditLogsController.cs` & `Views/AuditLogs/Index.cshtml`) - Replace Take(100), print all filtered
- [x] Module 13: User Management / Staff Roles (`UserManagementController.cs` & `Views/UserManagement/Index.cshtml`)
- [x] Module 14: Reports & Analytics (`ReportsController.cs` & `Views/Reports/Index.cshtml`) - Top medicines & customers paginated, print all filtered

## Phase 5: Sellable Stock & Safe Checkout (POS)
- [x] `Models/Medicine.cs`: computed `SellableStock`, `ExpiredStock`, `NearestSellableExpiry` (only `ExpiryDate >= today` & `Quantity > 0`)
- [x] POS `Medicines` endpoint (`SalesController.cs`) returns `sellableStock` / `expiredStock`
- [x] `Views/Sales/Create.cshtml`: grid uses sellable stock for Out-of-Stock / Low / Add-button state and per-item max; "N exp." badge when expired units exist
- [x] `Checkout` (POST) is fully server-authoritative: Serializable transaction, medicines locked in `MedicineId` order (deadlock-free), prices recomputed from DB, sellable-stock validation, FIFO batch deduction **skipping expired batches**, server-side discount + loyalty points (capped), covers cash tender, marks linked prescription fulfilled
- [x] All counters/modals on the POS flow updated to receive the new totals

## Phase 6: Purchase Order Receiving (Real Batches)
- [x] `Models/ViewModels/PurchaseOrderReceiveViewModel.cs`: form model (line: DetailId, BatchNumber, ExpiryDate)
- [x] `PurchaseOrdersController.Receive` GET builds per-line defaults (`PO-{id}-{medicineId}`, today + 24 months)
- [x] `PurchaseOrdersController.Receive` POST validates non-empty batch + expiry >= today, adds a real `MedicineBatch` per line (stock-in), marks PO `Received`, writes `PO_RECEIVED` audit log
- [x] Removed one-click `MarkReceived`; `Views/PurchaseOrders/Receive.cshtml` form + Index action updated

## Phase 7: Customer Data-Loss Fix (Edit)
- [x] `CustomersController.Edit` POST loads the existing entity and copies only editable fields (FullName/Phone/Email/Address/City/DateOfBirth/Gender) — `LoyaltyPoints` & `DateRegistered` preserved
- [x] `CustomersController.Create` Bind now includes City, DateOfBirth, Gender
- [x] `Views/Customers/Create.cshtml` & `Edit.cshtml`: City, Gender select, DateOfBirth inputs; Edit shows a preserve-note; hidden DateRegistered removed

## Phase 8: Sales Ledger Filters
- [x] `SalesController.Index` accepts `search`, `paymentMethod`, `from`, `to`; KPIs still computed over the **filtered** set but independent of page number
- [x] `Views/Sales/Index.cshtml` filter bar (search by customer/sale/invoice #, payment method select, From/To dates, Apply + Clear)
- [x] Filters survive pagination automatically via `_Pagination.cshtml` (preserves all query params)

## Phase 9: Google Maps Polish (Branches)
- [x] `Views/Branches/Index.cshtml`: per-branch "Directions" (`google.com/maps/dir`) and "Open in Maps" (`google.com/maps/search`) links

## Phase 10: Light/Dark Mode Toggle
- [x] `_Layout.cshtml`: anti-flicker head script reads `careplus_theme` localStorage; topbar toggle button persists choice
- [x] Dark CSS appended to `wwwroot/css/site.css` scoped to `[data-theme="dark"] .app-shell` — sidebar/brand colors kept, `--cp-*` variables re-mapped, hardcoded light components overridden (`.stat-card`, `.table-card-header`, thead, row hover, `.pos-cart-*`, pagination, `.btn-outline-careplus`), Bootstrap utilities remapped (`.text-dark/.text-muted/.bg-white/.bg-light`), inline `var(--cp-forest-deep)` / `var(--cp-primary)` colors handled
- [x] Printed receipts/invoices stay paper-white; auth pages unaffected (no `.app-shell`)
- [x] `data-bs-theme="dark"` set on `#appShell` at runtime for Bootstrap 5.3 theming
- [x] Charts adapt: global `window.appChartTheme(dark)` in `_Layout.cshtml`; `Home/Index.cshtml` & `Reports/Index.cshtml` register their Chart.js instances and re-render colors on load/toggle

---

# Reinforcement Phases (Phases 11-16)

## Phase 11: Link Customer Logins Safely (commit `8b87fef`)
- [x] Pre-migration cleanup: duplicate `Customers.Email` deduped via SQL (kept the oldest `Id` per email, blanked 26 duplicates so a unique index could be created)
- [x] `AspNetUsers.CustomerId` int? + FK to `Customers` (restrict) + unique `IX_AspNetUsers_CustomerId`; unique `IX_Customers_Email`
- [x] Migration `20260922201110_LinkUsersToCustomers` applied
- [x] `AccountController.Register`: new email -> creates account + Customer profile linked atomically (single transaction); existing patient email -> links only when the phone matches their record, otherwise blocked with a "contact the pharmacy" message; duplicate/blanked emails still handleable
- [x] Verified e2e: fresh register links; matching-phone link works; non-matching phone blocked

## Phase 12: Customer Portal (commit `f98690e`)
- [x] `PortalController` + views: Index (dashboard), Profile, Purchases (filtered/paginated own sales), PurchaseDetails, Prescriptions, Rewards, Subscriptions
- [x] `CustomerSubscription.PickupBranchId` + migration `20260922202438_AddPortalPickupBranch` (pickup branch selection on subscription)
- [x] "My Account" sidebar entry on the portal; Portal home card; own-data scoping enforced per customer via `CurrentCustomerService`
- [x] Subscribe / Pause / Resume / Set Pickup Branch / Cancel flows (Portal) with `PORTAL_SUBSCRIBE` etc. audit logs
- [x] Verified cashier cannot access portal data and portal pages reject unlinked users

## Phase 13: PWD/Senior Discount + VAT (commit `7218379`)
- [x] `SaleDiscountType { None, Senior, Pwd }` enum; `Medicine.IsVatExempt`; `Sale` + `VatableSales`, `VatExemptSales`, `VatAmount` (decimal(12,2)), `DiscountType`, `DiscountIdNumber`
- [x] `Services/PricingService.cs`: pure engine (12% VAT, 20% Senior/PWD statutory discount); registered singleton; `JsonStringEnumConverter` so the client posts enum strings
- [x] POS `Views/Sales/Create.cshtml`: statutory discount dropdown + required government ID input for Senior/PWD, client-side preview mirroring the server math, "No VAT" badge
- [x] `SalesController.Checkout` is server-authoritative: reprices via `PricingService`, validates the ID number when a discount is chosen, caps points at the payable-after-statutory-discount, persists VAT/discount fields, audit log includes VAT + discount
- [x] Receipt (`Sales/Details`) and invoice (`Billing/Details`) print the stored VAT/statutory/points breakdown
- [x] Medicines Create/Edit: `IsVatExempt` checkbox; DbInitializer seeds VAT-exempt Cozaar + Glucophage and runs an idempotent backfill for existing DBs (outside the `Suppliers.Any()` sample-data guard)
- [x] Migration `20260922205303_AddVatAndDiscountFields` applied; e2e math verified (vatable + exempt totals, VAT 2.46, Senior 8.01, points 34, net 0.49; missing-ID rejection)

## Phase 14: Rx-Required Medicines + Pharmacist-Only Dispensing (commit `85ce0e0`)
- [x] `Medicine.RxRequired`; `Prescription.SaleId` + `Sale? Sale` navigation
- [x] DbInitializer marks Amoxil Rx-required (seed + idempotent backfill for the existing catalog)
- [x] `SalesController.Checkout` Rx gate: Rx-required cart items (or a linked rx) require a Pharmacist/Admin operator, a linked customer, and a **Pending prescription owned by that customer** whose lines cover each Rx item with qty <= prescribed; fulfillment sets `Status = Fulfilled` + `SaleId` inside the same transaction
- [x] POS grid shows an `Rx` badge, enforces `addToCart` rxRequired, and blocks checkout of Rx items without a linked prescription id
- [x] `PrescriptionsController` rewritten: Fulfill, Reopen (Admin-only + audit), Cancel-with-required-reason (≤500 chars, replaces delete; Fulfilled rxs cannot be cancelled -> "void or refund the sale instead")
- [x] `Views/Prescriptions/Index.cshtml`: Dispense (POS) / Fulfill / Cancel-with-reason for Pending rows; receipt link + Admin-only Reopen for Fulfilled rows; Rx badge on medicine names
- [x] Migration `20260922210422_AddRxRequiredAndSaleLink` applied; e2e verified: cashier blocked, pharmacist without rx blocked, dispense OK, re-dispense blocked, admin reopen OK, pharmacist reopen denied, cancel with/without reason

## Phase 15: Void / Refund Completed Sales (commit `2340ad6`)
- [x] `Sale.IsVoided`, `VoidReason` (max 500), `VoidedById`, `VoidedAt`
- [x] `SalesController.Void` (POST, Admin/Pharmacist only): returns stock to the exact sold batches, reverses loyalty points (undo earned, refund redeemed), marks the invoice `Refunded`, flags the sale, audit-logs `SALE_VOIDED` with reason; double-void blocked
- [x] A sale that fulfilled a prescription may only be voided by an **Admin** (it reopens the Rx to Pending with an audit entry) — preserves the Phase 14 guard
- [x] Voided sales excluded from Home dashboard, Reports KPIs/trends/top-medicines/top-customers, customer portal purchases/spent, and Billing ledger totals
- [x] `Sales/Index` gains an Active/Voided filter, VOIDED badge + strikethrough totals, and a Void action (with reason prompt); receipts show a `VOIDED / REFUNDED` banner (+ voided-by/at/reason); invoices show a `Refunded` badge
- [x] Migration `20260922211753_AddVoidFields` applied; e2e verified: cashier denied, pharmacist void restores stock/reverts points (196->156->196), double-void rejected, rx-sale pharmacist denied + admin void reopens rx

## Phase 16: Login Security (commit `767a7cd`)
- [x] Identity lockout: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15` minutes, `lockoutOnFailure: true` on `PasswordSignInAsync`; locked-account messaging on the login page
- [x] Min password length raised to 8 (`Password.RequiredLength`) — all existing demo passwords already comply
- [x] Demo account seeds (`CreateUserIfNotExists` x5) gated to `IHostEnvironment.IsDevelopment()`; "Quick Demo Logins" autofill chips only render in Development
- [x] Removed the fake tile-CAPTCHA ("Select all images") markup/JS and the `/Account/ConfirmCaptcha` endpoint + `RecaptchaService`/`IRecaptchaService` (server verification no longer expected)
- [x] Verified e2e: login page clean of captcha, ConfirmCaptcha 404s, normal login works, 5 failed attempts lock the account (correct password then rejected with the lock message), other accounts unaffected

## Phase 17: Real Google reCAPTCHA v2 (commits `48af267`, `458ca7c`)
- [x] Added Google reCAPTCHA v2 widget to Login and Register — **Production only** (skipped entirely in Development so local demos/tests are not blocked)
- [x] `RecaptchaService` posts the token to `https://www.google.com/recaptcha/api/siteverify` with the configured secret and returns only when `success` is `true`; bot tokens, HTTP errors, and missing secret all fail closed
- [x] Server-side verification wired into `AccountController.Login` and `AccountController.Register` before any credential/account work
- [x] Root cause of "always failing": the secret was never configured anywhere, so verification always returned false. Fixed by reading `Recaptcha:SecretKey` and falling back to the `Recaptcha__SecretKey` environment variable
- [x] Verified e2e in Production locally: without the env var login is blocked with "Please complete the reCAPTCHA"; after `$env:Recaptcha__SecretKey` is set and the box is ticked, login succeeds

> **DEPLOYMENT REQUIREMENT (Production):** reCAPTCHA verification fail-closes unless the secret is present. It
> **must** be set as the `Recaptcha__SecretKey` environment variable on the Production host (or the platform's
> secret store bound to that key). `dotnet user-secrets` is Development-only and does NOT apply in Production.
> The client-side `Recaptcha:SiteKey` lives in `appsettings.json` (public by design — fine to commit); the
> secret must never be committed.
