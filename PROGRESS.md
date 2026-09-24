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

## Phase 17: Purchase Order Sample Data — Seed for Filters Demo (done)
- [x] Added idempotent `EnsurePurchaseOrderDataAsync(context)` to `Data/DbInitializer.cs` (guard: `if (context.PurchaseOrders.AnyAsync()) return;` — no duplicates, fresh-install safe) and registered the call next to the existing Ensure* calls in `SeedAsync` (~line 216)
- [x] No controller / view / migration changes — the PurchaseOrders filters already support `search`, `status`, `from`, `to`
- [x] Suppliers looked up by name (fallback to first supplier); medicines resolved by name with a fallback to the catalog's first entries (so DBs seeded with only the 35+ rich catalog, which lack "Biogesic 500mg" etc., still get rows). Seeded 8 POs (1–2 line items each), spread across suppliers and OrderDates (last ~35 days → today):

| # | Supplier (by name) | OrderDate | Status | Line items (Medicine × Qty @ UnitCost) |
|---|--------------------|-----------|--------|----------------------------------------|
| 1 | MediSource PH | −35d | Received | Tempra 500mg ×200 @6.00, Alaxan FR ×100 @11.50 |
| 2 | PharmaLink Distributors | −28d | Received | Augmentin 625mg ×150 @30.00 |
| 3 | HealthWell Supply Co. | −20d | Received | Betaloc 50mg ×120 @9.75, Claritin 10mg ×80 @21.50 |
| 4 | HealthWell Supply Co. | −12d | Pending | Januvia 100mg ×160 @36.50 |
| 5 | MediSource PH | −6d | Pending | Buscopan 10mg ×100 @14.20, Tempra 500mg ×300 @6.00 |
| 6 | PharmaLink Distributors | −3d | Pending | Ciprox 500mg ×200 @22.00, Allerkid 5mg/5mL ×50 @21.50 |
| 7 | HealthWell Supply Co. | −1d | Pending | Forxiga 10mg ×140 @38.00 |
| 8 | MediSource PH | today | Cancelled | Fluimucil 600mg ×60 @11.50 |

- [x] Verified live: `dotnet build` 0 warn/err → ran `--launch-profile http` (localhost:5000) → auto-login via decoded `captchaToken` → `/PurchaseOrders?pageSize=100` total = **8**; narrowing `status=Received` → **3**, `status=Pending` → **4**, `status=Cancelled` → **1**, `search=MediSource` → **3**, `from=2026-08-01&to=2026-09-10` → **3**, `from=2026-09-11&to=2026-09-30` → **5**; filter bar (search/status/from/to) renders; restarted the app and the total stayed **8** (idempotent — no duplicate rows); app stopped
- [x] Committed separately

## Phase 18: Membership Tiers (paid plans) — Phase 1 of 5: Data Model (done)
- [x] `Models/MembershipTier.cs` (new): Name, MonthlyPrice (decimal(10,2)), DiscountPercent (decimal(5,2)), FreeDelivery, MaxFamilyAccounts (default 1), HasDedicatedPharmacist, PriorityDispensing, IsActive
- [x] `Models/CustomerMembership.cs` (new): CustomerId FK, MembershipTierId FK, StartDate, NextBillingDate, `MembershipStatus { Active, Paused, Cancelled }`, PaymentMethod
- [x] `Models/Customer.cs`: `[NotMapped] CurrentMembership` helper (most recent Active membership) + `Memberships` collection
- [x] `ApplicationDbContext`: `MembershipTiers` / `CustomerMemberships` DbSets + FK config (Customer → cascade, Tier → restrict)
- [x] `DbInitializer`: idempotent `EnsureMembershipTiersAsync` seeding Basic (₱0/0%), Health Plus VIP (₱12.99/15%, free delivery, priority dispensing) and Family/Chronic (₱22.99/15%, all VIP perks + MaxFamilyAccounts=5 + dedicated pharmacist)
- [x] Migration `20260924152456_AddMembershipTiers` applied cleanly; DB verified: 3 tiers seeded exactly to spec; no controller/UI yet

## Phase 19: Membership Tiers (paid plans) — Phase 2 of 5: Admin CRUD (done)
- [x] `Controllers/MembershipTiersController.cs` (new, `[Authorize(Roles="Admin")]`): Index (Include Memberships, ordered by MonthlyPrice then Name), Create, Edit, Delete + audit log entries `TIER_CREATED` / `TIER_UPDATED` / `TIER_DELETED`; Delete blocked with `TempData["Error"]` when memberships exist
- [x] `Views/MembershipTiers/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml` (new): table-card / btn-accent / form-switch style, perk badges (Free Delivery / Priority / Dedicated Pharmacist / Family:N), status badge, enrolled count, delete confirm with in-page guard for enrolled tiers
- [x] `Views/Shared/_Layout.cshtml`: "Membership Tiers" nav link added in the Admin CRM & Patients block (next to Subscription Plans)
- [x] Verified live as Admin: 3 seeded tiers listed, create → edit → delete cycle works (18/18 automated checks incl. pharmacist blocked → `Account/AccessDenied`); build 0 warn / 0 err; committed `77c6b0f`

## Phase 20: Membership Tiers (paid plans) — Phase 3 of 5: Customer Portal (done)
- [x] `Models/ViewModels/PortalDashboardViewModel.cs`: added `PortalMembershipViewModel` (Customer, CurrentMembership, Tiers, History)
- [x] `Controllers/PortalController.cs`: `Membership` GET (current membership + active tier cards + history), `Subscribe` POST (single active membership; same tier → info message, different tier → close old + open new, i.e. upgrade/downgrade), `CancelMembership` POST (own active record only); audit logs `MEMBERSHIP_SUBSCRIBED` / `MEMBERSHIP_CANCELLED`
- [x] `Views/Portal/Membership.cshtml` (new): current-membership panel with perks + cancel form, per-tier pricing cards (Current Plan disabled / Switch to This Plan / Subscribe Now), membership history table
- [x] `Views/Shared/_Layout.cshtml`: "My Membership" nav link in My Account (Customer) section with `member` badge when an active membership exists (uses `activeMembershipCount` via direct COUNT — `portalCustomer` is loaded with `FindAsync`, so its `CurrentMembership` nav collection is empty)
- [x] Verified live as Customer `customer@careplus.ph`: browse 3 cards → subscribe VIP → switch to Family/Chronic (upgrade) → cancel; success banners (`Welcome to…`, `Membership switched to…`, `has been cancelled`) confirmed via curl (PowerShell `Invoke-WebRequest` drops the TempData cookie on 302; curl honors it); pharmacist blocked → `Account/AccessDenied`; 27/27 automated checks; demo customer membership history reset to baseline (0 rows); build 0 warn / 0 err; committed `972c3a6`

## Phase 21: Membership Tiers (paid plans) — Phase 4 of 5: POS Discount (done)
- [x] `Models/Sale.cs`: new `AppliedDiscountSource` field (`None` / `SeniorPWD` / `Membership:<TierName>`); migration `20260924155110_AddAppliedDiscountSourceToSales` applied cleanly
- [x] `Services/PricingService.cs`: `PriceBreakdown.DiscountAmount` made settable so checkout can promote the winning discount
- [x] `Controllers/SalesController.cs` `Checkout`: loads the customer's active membership, computes `tier% × gross`, and applies only the LARGER single discount vs the Senior/PWD 20% (never stacked); when membership wins → `DiscountType = None`, `DiscountIdNumber` dropped; when statutory wins → existing VAT-removing path; `AppliedDiscountSource` recorded + logged in the `POS_SALE_COMPLETED` audit entry
- [x] `Views/Sales/Details.cshtml` receipt: shows `Membership Discount (<Tier>)` line (green) whenever `AppliedDiscountSource` starts with `Membership:`; statutory line unchanged and mutually exclusive
- [x] Verified live as Cashier (16/16 automated checks, math exact): walk-in → no discount; VIP member → `-₱11.25` (15% of ₱75.00, source `Membership:Health Plus VIP`, no senior line); VIP + Senior → `-₱13.39` (20% ex-VAT wins, senior line + ID, **no membership line = no stacking**); Family/Chronic at 30% + Senior → `-₱22.50` (membership wins, **no senior line**); membership baseline + tier discount restored to 15% after tests; build 0 warn / 0 err; committed `b2cbea2`

## Phase 22: Membership Tiers (paid plans) — Phase 5 of 5: Perk Badges (done)
- [x] `Controllers/SalesController.cs` `Details`: derives the sale customer's current active membership and sets `ViewBag.MembershipFreeDelivery` (display-only — no schema change)
- [x] `Views/Sales/Details.cshtml` receipt: green `Free Delivery Included` banner (truck icon, "Membership perk — no delivery fee") above line items when the badge applies; absent for walk-in / non-member sales
- [x] `Controllers/PortalController.cs`: new `GetActiveMembershipTierAsync` helper used by `Purchases` (`ViewBag.MembershipFreeDelivery`) and `Prescriptions` (`ViewBag.MembershipPriorityDispensing`)
- [x] `Views/Portal/Purchases.cshtml`: `Free Delivery Included` badge in the Purchase History card header; `Views/Portal/Prescriptions.cshtml`: `Priority Dispensing` badge (lightning icon) next to each prescription's status badge
- [x] Verified live (10/10 automated checks): member sale receipt + My Purchases show Free Delivery badge; walk-in receipt does NOT; Prescriptions shows Priority badge; demo data reset to baseline (membership + tutorial Rx removed); build 0 warn / 0 err

## Phase 23: Refill Plans ↔ Membership decoupling + admin→customer correlation fix (done)
- [x] Audited both modules: `MembershipTier`→`CustomerMembership` and `SubscriptionPlan`→`CustomerSubscription` are **already separate tables**, both FK `Restrict`, customer Browse/My Subscriptions/Portal Membership read live DB rows (no hardcoded plans). Root defects were a checkbox binder bug and unguarded delete.
- [x] **Fixed `IsActive` checkbox binding** in `Views/Subscriptions/PlanCreate.cshtml`, `PlanEdit.cshtml` and `Views/MembershipTiers/Create.cshtml`, `Edit.cshtml` via hidden `<input name="IsActive" value="false" />` before the checkbox — admins can now actually activate/deactivate plans and tiers (previously an unchecked box silently left the model at its `true` initializer, so deactivation never persisted).
- [x] `Controllers/SubscriptionsController.cs` `PlanDelete`: blocked with a TempData error when the plan has any subscriptions (must deactivate instead; historical rows stay intact) — mirrors the existing tier guard and prevents the FK `Restrict` 500; added `PLAN_CREATED` / `PLAN_UPDATED` / `PLAN_DELETED` audits (parity with `TIER_*`).
- [x] Terminology: admin UI renamed to **Refill Plans** (`_Layout` sidebar, `Plans.cshtml` header, create/edit titles, TempData), keeping controller/table names; `Views/Subscriptions/Browse.cshtml` gained a "refill plans ≠ membership tiers" note with a link to My Membership.
- [x] Verified live end-to-end (33/33 checks + re-activate check): admin creates Active plan → listed in admin with Active pill → appears in customer **Browse Refill Plans** with medicine/qty/interval/price from the DB row → customer subscribes → **My Subscriptions** shows plan (medicine/price/next refill) → admin subscriber count 1 → admin deactivates → pill flips to Inactive, **hidden from Browse**, new subscribes blocked (404), existing subscription intact → delete of subscribed plan blocked → orphaned temp tier deletable; also verified a **tier** deactivated via edit disappears from My Membership's tier list; re-activating a plan (checked box) flips it back to Active. Test artifacts purged; canonical tiers (Basic/VIP/Family-Chronic) ensured active; 0 refill plans baseline; build 0 warn / 0 err

## Phase 24: Refill Plan / Tier IsActive binding, Perks labels, portal "zero tiers" (done)
- [x] **IsActive checkbox was silently binding to the FIRST posted value.** All four forms posted `<input type="hidden" name="IsActive" value="false">` before the checkbox, so a checked submit sent `false,true` and MVC bound `false` — every admin-created plan/tier landed **Inactive** and vanished from customer Browse (`Where(p => p.IsActive)`). Reproduced live: checked `PlanCreate` POST redirected success but saved `Inactive`.
- [x] **Fixed** by removing the hidden inputs and deciding `IsActive` server-side: `plan/tier.IsActive = Request.Form.ContainsKey("IsActive")` in `SubscriptionsController` + `MembershipTiersController` (create + edit), with `[Bind]` dropping `IsActive`. Create GET now passes `new SubscriptionPlan { IsActive = true }` / `new MembershipTier()` so the "Active" checkbox renders **checked by default** → created plans/tiers default to active and display.
- [x] `Views/MembershipTiers/Index.cshtml` Perks column: badges already rendered all four perks; aligned labels to the spec (**"Priority Dispensing"**, **"Family x{N}"**) and fixed a Razor literal-`@t.MaxFamilyAccounts` rendering bug via `@(t.MaxFamilyAccounts)`. Live-verified: Health Plus VIP → "Free Delivery, Priority Dispensing"; Family/Chronic → + "Dedicated Pharmacist, Family x5"; Basic → "—".
- [x] **#3 "Portal showed ZERO tiers" was a data-state bug, not code.** Temporary DIAG logging in `PortalController.Membership()` (customer `customer@careplus.ph` → id=54; `ALL MembershipTiers total=3 active=0`) proved the `Where(t => t.IsActive)` filter was correct — all three rows were `IsActive=false` with Health Plus VIP's perks stripped and canonical "Basic" mutated into "BASIC" (₱100, 5%) during earlier live testing. No PortalController change needed; restored the canonical tier rows (ids 2/3/11, seed values, active) via the dev harness and removed the logging. Portal now lists Basic + Health Plus VIP + Family/Chronic.
- [x] Verified live (12/12 checks): tier create form default-checked; checked create → Active; unchecked → Inactive; perks badges render; plan create default-checked → Active pill; portal shows 3 available tiers. Test data purged; canonical tiers active; build 0 warn / 0 err; committed `44afc5e` (Perks labels), `b653a18` (tier IsActive), `f9db77d` (plan IsActive)

## Phase 25: Portal greys out the free plan + guard redundant free-tier enroll (done)
- [x] `Views/Portal/Membership.cshtml`: when a customer has **no active membership**, the free plan (the first tier with `MonthlyPrice == 0` -- price-based, NOT name-based since "Basic" was once renamed in this DB) is now their implicit current plan. Detected via `freeTier = Model.Tiers.FirstOrDefault(t => t.MonthlyPrice == 0)` and `currentTierId = cur?.MembershipTierId ?? freeTier?.Id`. The free card renders muted (`opacity: .6`), gets a small "Free" badge, and shows a disabled **"Current Plan -- Free"** button instead of "Subscribe Now" -- the ChatGPT-style treatment the user asked for. Paid current tier keeps its existing disabled "Current Plan" button; other tiers keep enabled "Subscribe Now" / "Switch to This Plan".
- [x] `Controllers/PortalController.cs` `Subscribe`: added a guard after the tier-is-active check -- if `tier.MonthlyPrice == 0 && current == null` (customer is already on the implicit free plan and has no paid membership) redirect to Membership with `TempData["Error"] = "You're already on the free plan -- pick a paid tier to enjoy member benefits."` and create NO `CustomerMembership` row (a crafted POST previously created a redundant Active $0 row in History). Downgrade from a paid membership to free is unchanged and still allowed.
- [x] Discovered and purged a stray active tier **id16 "TOYI PLAN" P6,969.00 / 69% / MFA=69** left in the DB from earlier experimentation -- it was rendering as a fourth portal card (price-based order: Basic 0, VIP 12.99, Family 22.99, TOYI 6969). Removed via the dev harness; canonical tiers (ids 2/3/11, seed values, active) restored.
- [x] Verified live 14/14 (customer `customer@careplus.ph`, no active membership): Basic card muted + "Current Plan -- Free" + Free badge; VIP/Family enabled Subscribe Now; POST `Subscribe/11` (free) -> redirect + "already on the free plan" banner + NO row created; subscribe VIP -> Active banner + VIP card marked Current Plan + Basic un-muted with "Switch to This Plan"; downgrade to free -> "Basic Active". Membership + prescription baseline reset (0 active rows), canonical tiers active, build 0 warn / 0 err.

## Phase 26: Readable "Current Plan -- Free" button in light mode (done)
- [x] The muted free card's button used `btn-outline-secondary` (grey text/border) on top of a whole-card `opacity:.6`, making "Current Plan -- Free" nearly invisible in light mode.
- [x] `Views/Portal/Membership.cshtml`: swapped the button to solid **`btn-secondary`** (dark grey bg, white text) and raised the muted card opacity `.6` -> **`.75`** so the label stays clearly legible while the card still reads as the greyed-out/current free plan. Paid "Current Plan" button and Subscribe forms unchanged.
- [x] Verified live (14/14 harness PASS + rendered-HTML spot checks: `btn w-100 btn-secondary`, `opacity:.75`, `Current Plan &#x2014; Free` all present); membership baseline reset (0 active), canonical tiers active, build 0 warn / 0 err.
