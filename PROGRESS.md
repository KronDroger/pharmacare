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

## Phase 27: Membership UI redesign & payment confirmation flow (done)
- [x] `Views/Portal/Membership.cshtml`: Redesigned customer-facing membership browse page adapted to CarePlus design tokens (`var(--cp-forest)`, `var(--cp-mint)`, `var(--cp-accent)`, `var(--cp-surface)`, dark-mode safe):
  - Top pill badge: `"No Lock-in, Cancel Anytime"`.
  - Header: `"Choose Your Membership"` + reassuring subtitle.
  - Card layout: circular icon badge, "Most Popular" ribbon badge on recommended tier (`Health Plus VIP`), tier name, concise description, large bold monthly price with plain honest pricing (no fake strikethroughs).
  - Perks checklist: cleanly extracted from model fields (`DiscountPercent`, `FreeDelivery`, `PriorityDispensing`, `HasDedicatedPharmacist`, `MaxFamilyAccounts`).
  - Preserved disabled "Current Plan -- Free" (opacity .75, solid secondary) and "Current Plan" states without regression.
  - Footer card: honest, friendly welcome notice for pharmacy context (*"New Members Are Always Welcome"*).
  - Membership history table preserved with theme tokens.
- [x] `Views/Portal/SubscribeConfirm.cshtml`: New confirmation page for paid tiers (`GET /Portal/SubscribeConfirm/{id}`), displaying plan summary, included perks, fee breakdown, and payment method button group (`Cash`, `GCash`, `Card`, `PayMongo`) matching `Views/Sales/Create.cshtml`.
- [x] `Controllers/PortalController.cs`:
  - Added `SubscribeConfirm(int id)` action for paid membership checkout.
  - Updated `Subscribe(int id, string? paymentMethod)` POST action:
    - Free tier skips payment and subscribes directly.
    - Paid tier creates `CustomerMembership` with actual chosen `PaymentMethod` (replacing hardcoded "Portal" string).
    - Concurrently creates a matching `Sale` and `Billing` record (`InvoiceNumber = MBR-YYYY-XXXXX`, `DateIssued = today`, `PaymentStatus = Paid`, `PaymentMethod` as selected) within a single database transaction.
- [x] `Views/Billing/Details.cshtml`: Added line-item fallback for membership invoices when `Sale.Details` is empty so official invoices render cleanly.
- [x] Build verified clean (`dotnet build`: 0 warnings, 0 errors). Changes committed in `67dfdf4`.

---

# Near-Expiry "Buy 1 Take 1" (BOGO) Promo — Phases 1-6 of 6 (COMPLETE)

> All six phases implemented, verified and committed — global phases 28-33 below.
> One display-only gap remains open: the POS on-screen subtotal still ignores the BOGO
> reduction (see "Remaining" at the end of this section).

Automatic promo that discounts near-expiring stock out of inventory instead of writing it off.

## Phase 28: BOGO — Phase 1 of 6: `MedicineBatch.IsNearExpiry` flag (done, commit `4f4da50`)
- [x] `Models/MedicineBatch.cs`: `public const int NearExpiryThresholdDays = 120;` and `[NotMapped] public bool IsNearExpiry => Quantity > 0 && ExpiryDate.Date > DateTime.Today && ExpiryDate.Date <= DateTime.Today.AddDays(NearExpiryThresholdDays);`
- [x] Deliberately a computed `[NotMapped]` property (no new column, **no migration**), mirroring the existing computed `SellableStock` / `ExpiredStock` style in `Models/Medicine.cs`.
- [x] Boundaries hand-checked: expiring **today** is NOT near-expiry (still sellable), expired is excluded, `Quantity == 0` is excluded. Upper bound at exactly +120 days is inclusive.
- [x] Caveat recorded: a computed C# property **cannot be translated to SQL**, so every query that needs this filter must restate the predicate (see Phase 29).

## Phase 29: BOGO — Phase 2 of 6: Expiry Monitor "Near Expiry (BOGO Eligible)" tab (done, commit `e4b5ba5`)
- [x] `Controllers/MedicinesController.cs` `Expiring`: new `nearExpiryQuery` + `ViewBag.NearExpiryCount` / `NearExpiryValuation`, and a `"nearexpiry"` case in the tab switch. Predicate restated in LINQ (not `IsNearExpiry`) with a comment that it must stay in sync with the model.
- [x] `Views/Medicines/Expiring.cshtml`: new filter pill after "Safe Stock", a 5th `stat-card` (BOGO Eligible batch count + recoverable ₱ valuation, `icon-amber`), and a **"BOGO Promo"** table column rendering a `badge text-bg-warning` "BOGO Eligible" + "Near expiry ≤ 120 days" (or `—`); empty-state `colspan` 8 → 9.
- [x] Verified live against 6 hand-staged boundary rows: +1d and +119d and +120d **listed**; +121d, expires-today, qty 0 and expired **excluded**. Tab count matched exactly (3). All Batches tab unaffected. DB restored afterwards.
- [x] Known overlap: the 120-day window sits on top of the existing 90-day "Warning"/"Safe" tiers, so some batches appear under both tabs. Intentional.

## Phase 30: BOGO — Phase 3 of 6: POS cart "BOGO Eligible (near expiry)" badge (done, commit `8b6f334`)
- [x] `Controllers/SalesController.cs`: added `FefoOrder(Medicine)` (batch ordering **deliberately identical** to the Checkout deduction order, with a comment saying so) and `NearExpiryPrefix(Medicine)` → `(Units, Batch, Expiry)`. The `Medicines` catalog endpoint now returns `nearExpiryUnits` / `nearExpiryBatch` / `nearExpiryDate`.
- [x] Because the cart is pure client-side with no per-change server round-trip, the **FEFO projection is computed server-side** and the JS only compares `item.qty` against the server-supplied `nearExpiryUnits`. Sound because FEFO sorts by ascending expiry, so near-expiry batches form a contiguous prefix of the order. The client never derives stock or eligibility itself.
- [x] `SalesController.Create`: for the Rx pre-load path, batches are queried for the Rx medicines and passed via `ViewBag.RxNearExpiry` so pre-loaded lines get the same badge as catalog-added lines.
- [x] `Views/Sales/Create.cshtml`: `addToCart` takes 3 new trailing params; `renderCart` renders the badge with batch #, expiry date, and "N of M units" when a line spans both near-expiry and fresh stock. **Visual only — no pricing in this phase.**
- [x] Verified live in real Chrome via Playwright (the cart is JS-rendered): badge correct on Tempra ×4, Calpol ×4, Calpol ×50 ("42 of 50 units" span), Rx 57 pre-load; **no** badge on Alaxan / Dolfenal / Celebrex (fresh-only). All line totals stayed at full price (₱30.00 / ₱580.00 / ₱1740.00), confirming no pricing leaked in. 0 JS errors.
- [x] Known gap: the badge is a *projection*, correct only while stock is unchanged between add-to-cart and checkout. Checkout stays authoritative.

## Phase 31: BOGO — Phase 4 of 6: Server-side BOGO pricing (done, commit `57c477f`)
- [x] `SalesController.Checkout`: per dispensed batch, `payableUnits = isNearExpiry ? (take + 1) / 2 : take` (integer division = ceil, so 3 units → pay 2) and `bogoSaved = (take - payableUnits) × UnitPrice`. **Server-side only, no JavaScript.** FEFO-span handled naturally by the per-batch loop; pairing never spans two batches.
- [x] **Critical ordering hazard fixed:** `IsNearExpiry` requires `Quantity > 0`, and the FEFO loop does `batch.Quantity -= take` *before* building the line — so a fully-drained batch would flip the flag to `false` and silently lose its discount exactly when the whole near-expiry batch sells out. The flag is now captured into a local **before** the deduction, with a comment explaining why.
- [x] `Models/SaleDetail.cs`: new `BogoDiscountAmount` column + `ChargedTotal => LineTotal - BogoDiscountAmount`. `Quantity`/`UnitPrice`/`LineTotal` deliberately keep the **true dispensed value at full price** so inventory and audit stay truthful; the giveaway is recorded separately.
- [x] `Models/Sale.cs`: `GrossAmount` now sums `ChargedTotal`, plus a derived `BogoDiscountAmount`. This was **required**, not cosmetic: `Sale.GrossAmount` and the Sales KPI both recompute from `Details`, so a local-only change in `Checkout` would have left the ledger claiming ₱200 while charging ₱100 — and `TotalAmount` feeds ~8 views.
- [x] BOGO reduces **gross**, deliberately, not `DiscountAmount`: Phase 6 requires the percentage discount to apply to the already-halved amount, and gross is what feeds `PricingService`. VAT therefore accrues only on what is actually billed (₱100 → VAT ₱10.71, verified).
- [x] `Controllers/SalesController.cs` `Index` KPI and `Controllers/ReportsController.cs` top-medicines revenue updated to net off `BogoDiscountAmount`; both confirmed to translate in EF without errors (`/Sales/Index`, `/Reports/Index`, `/Sales/Details`, `/Billing/Details`, `/Customers/Index`, `/Crm/Index` all HTTP 200).
- [x] Migration `20260926094733_AddBogoDiscountToSaleDetails` (additive, `decimal(10,2) NOT NULL DEFAULT 0`, so the 90 existing sales are untouched). No existing migration edited.
- [x] **Required hand-check passed: 4 units of a ₱50 near-expiry item = pay ₱100.00, not ₱200.00** (line persisted as `ZZNEAR qty=4 price=50.00 bogoSaved=100.00`).
- [x] Full matrix on a ₱50 item: 1u→₱50 · 2u→₱50 · 3u→₱100 · **4u→₱100** · 5u→₱150 · 6u→₱150.
- [x] FEFO-span: 3 near-expiry + buy 5 → `ZZNEAR 3u saved ₱50` + `ZZFRESH 2u saved ₱0` = ₱200; 4 + buy 6 → ₱200; 200 + buy 250 → saved ₱5,000 + 50 fresh ₱0 = ₱7,500. Only the near-expiry portion is discounted.
- [x] Drain regression tests (the hazard above): 4 near-expiry buying exactly 4 → **₱100**; 2 buying 2 → ₱50; 1 buying 1 → ₱50 with ₱0 saved.
- [x] Per-line independence: 2-line cart ₱50×4 + ₱80×3 = **₱260.00**, matching hand calculation.
- [x] Data integrity: for every test sale `Σ(ChargedTotal) == VatableSales == Billings.AmountDue`, and the receipt page shows a consistent Gross / Total Amount Due. Test medicines, batches and the 13 test sales purged (90 sales / 90 billings intact); build 0 warn / 0 err.

## Phase 32: BOGO — Phase 5 of 6: Receipt/invoice BOGO line + audit trail (done, commit `a5068a2`)
- [x] `Views/Sales/Details.cshtml` (receipt) and `Views/Billing/Details.cshtml` (tax invoice) each render a per-batch deduction line reading **"BOGO applied — Batch `<number>` (near expiry)"** with the amount saved, prefixed with a `bi-tag-fill` icon in `--cp-warning`.
- [x] **Closes the Phase 4 gap where receipt lines did not add up to the total.** When a BOGO line exists, a `Subtotal (items at full price)` row is now printed first, so the arithmetic is visible: `₱200.00 − ₱100.00 = ₱100.00` gross. Previously the line items showed full price while Gross below was already halved.
- [x] BOGO line items show the full price struck through above the charged amount, plus an inline `BOGO Buy 1 Take 1 — saved ₱X` badge, so the saving is legible at line level and not only in the totals block.
- [x] `Controllers/BillingController.cs` `Details` now `.ThenInclude(d => d.Batch)` — it previously loaded `Details → Medicine` only, so the invoice had no batch number to print. (The `Index` query was left alone; it does not render batch numbers.)
- [x] `SalesController.Checkout` writes a **second, separate `AuditLog` row with `Action = "BOGO_APPLIED"`** rather than appending to `POS_SALE_COMPLETED`, so promo usage is searchable on its own in Audit Trails. Details string carries sale + invoice number, units discounted, batch count, total saved, and the per-batch breakdown; truncated at 500 chars to respect the column limit.
- [x] Checkout JSON response gained `bogoDiscountAmount` so the POS can read the server's figure instead of recomputing it (feeds the POS subtotal gap below).
- [x] **Required hand-check passed** on the same canonical case as Phase 4 — 4 units of a ₱50 near-expiry item: receipt showed `Subtotal ₱200.00` / `BOGO applied — Batch BOGO-P5-NEAR (near expiry) −₱100.00` / `Gross ₱100.00` / `TOTAL AMOUNT DUE ₱100.00`, and the invoice showed the identical trio. Persisted `SaleDetails` row: `qty=4 price=50.00 bogoDiscount=100.00`.
- [x] VAT confirmed still accruing only on the reduced base: `12% VATable Sales ₱100.00` → `VAT ₱10.71`.
- [x] **Multi-batch case verified** (exercises the `foreach`, not just a single line): 2u of a ₱50 near-expiry batch + 2u of a ₱80 near-expiry batch printed **two** deduction lines (`BOGO-P5B-A −₱50.00`, `BOGO-P5B-B −₱80.00`), `Subtotal ₱260.00 → Gross ₱130.00`. Audit row was 205 chars and listed both batches.
- [x] **Audit rows verified in the UI**, not just the table: `BOGO_APPLIED` renders in `/AuditLogs` under the existing Action / Activity Details columns.
- [x] Regression — non-BOGO receipts are untouched: `/Sales/Details/90` contains no "BOGO", no `Subtotal (items at full price)` row and 0 struck-through prices, and `/Billing/Details/90` (membership invoice, no sale details) renders normally with neither. All BOGO markup is conditional on `BogoDiscountAmount > 0`.
- [x] Test rows purged (2 test sales + their details/billings, 2 `BOGO_APPLIED` + 2 `POS_SALE_COMPLETED` audit rows, 2 test medicines, 3 test batches). DB back to **90 sales / 90 billings / 37 medicines / max AuditLog 142** — identical to the Phase 4 baseline. App stopped.
- [ ] *Observation, not fixed (pre-existing, out of Phase 5 scope):* a membership-fee sale shows `Gross Total ₱0.00` on its receipt, because `GrossAmount` sums `SaleDetails` and membership sales carry no detail rows. Pre-dates Phases 4-5; flagged rather than silently widening scope.

### Tooling gotchas found while verifying (final)
- **Correction — my earlier "the build does not validate Razor" claim was wrong.** Phase 4's proof was invalid: I injected `@x ?? <span>`, which is *legal* Razor (it renders the literal text `@x `), so of course it compiled. Phase 5 proved the opposite — putting a bare `₱@x` on its own indented line inside a code block produced `CS1056 Unexpected character '₱'` and the build **failed**. **`dotnet build` does validate Razor.** The real trap is *incremental* builds: when no `.cshtml` changed, Razor compilation is skipped entirely and a stale result is reused, so an unchanged project reports "0 warnings, 0 errors" without having compiled anything. Two rules follow: (1) after editing any view, touch the `.cshtml` (or clean) and confirm Razor actually recompiled rather than trusting the summary; (2) a bare `₱`/`@` at the start of an indented line is parsed as C# — wrap it in `<span>…</span>`.
- **A running server locks the build output.** With the app up, `dotnet build` fails with `MSB3027 … file is locked by CarePlusPharmacy (pid)` / `MSB3021`. Kill `CarePlusPharmacy` *and* `dotnet` before building, otherwise you get a confusing copy failure that looks like a code problem. Related: `dotnet run` leaves a child `CarePlusPharmacy.exe` alive, so killing the parent `dotnet` does **not** stop the old server — a stale server keeps serving old markup and looks like a phantom bug. Recovery: kill both, then `rm -rf obj bin`, rebuild, restart.
- **The seed data contains no near-expiry stock** — all 71 seeded batches are 6+ months out, so the BOGO tab shows 0 and the promo has nothing to test on a fresh DB. (The near-expiry demo batches at `DbInitializer.cs:75-83` only run on a truly empty database; the bulk loop at 506/517 uses `AddMonths(6..24)`.) Rows were hand-staged per phase for testing. Adding near-expiry batches to the seed is still an open question.
- **Every VAT-exempt medicine in the seed is `RxRequired = 1`** (checked all three), so the POS correctly refuses to sell them without a linked prescription. To test the VAT-exempt pricing path a temporary non-Rx VAT-exempt medicine had to be staged. Worth knowing before attempting that test.
- **There is no `Active` membership in the seed** — the only enrollment (`CustomerMemberships.Id = 23`, customer 54, Health Plus VIP 15%) has `Status = 2` (`Cancelled`). Membership-stacking tests must activate it temporarily and restore it.
- MariaDB/MySQL here rejects the SQL Server-style ternary (`? :`) — use `IF(cond, a, b)`. Also `Sale.TotalAmount`, `GrossAmount`, `ChargedTotal` and `BogoDiscountAmount` are `[NotMapped]`, so they are **not** columns and cannot be selected; recompute them in SQL when asserting ledger invariants.

## Phase 33: BOGO — Phase 6 of 6: Discount-stacking verification + statutory VAT fix (done, commit `PENDING`)
- [x] **Verified the intended stacking order** (BOGO halves first, then the percentage applies to the already-reduced amount) across an 8-case matrix driven through the real `POST /Sales/Checkout` endpoint and asserted against hand calculations. Every case matched:

| # | Cart | Expected | Charged | |
|---|---|---|---|---|
| A | BOGO only | gross 100.00, total 100.00 | 100.00 | OK |
| B | BOGO + Senior 20% | gross 100.00, discount 17.86, VAT waived 10.71, total 71.43 | 71.43 | OK |
| C | BOGO + PWD 20% | as B | 71.43 | OK |
| D | BOGO + Membership VIP 15% | discount 15.00, total 85.00 | 85.00 | OK |
| E | BOGO + Senior + VIP 15% | statutory wins, total 71.43 | 71.43 | OK |
| F | *fresh* (no BOGO) + Senior | gross 200.00, discount 35.71, VAT 21.43, total 142.86 | 142.86 | OK |
| G | BOGO + Senior + all points | points capped at 71, total 0.43 | 0.43 | OK |
| H | BOGO + Senior on **VAT-exempt** item | gross 39.00, discount 7.80, VAT 0.00, total 31.20 | 31.20 | OK |

- [x] **No double-counting, proven three ways.** (1) BOGO lands in `SaleDetail.BogoDiscountAmount` only — never inside `Sale.DiscountAmount`, which held exactly the percentage (17.86 / 15.00 / 35.71) or percentage + points. (2) The percentage is computed on the **reduced** base: BOGO sales gave 17.86 (20% of 100/1.12), not the 35.71 that 20% of the undiscounted 200 would produce. (3) For every test sale `Σ(Quantity × UnitPrice) − Σ(BogoDiscountAmount) == VatableSales` (200 − 100 = 100), and the fresh control correctly had BOGO 0.00.
- [x] **Ledger invariant holds for all 15 test sales:** amount actually charged (`Billings.AmountDue`) `==` `Sale.TotalAmount` recomputed independently in SQL. This is the check that was failing before this phase.
- [x] **Fixed a pre-existing Senior/PWD bug: the VAT was never removed from the amount charged.** `Checkout` computed `finalAmount = grossTotal − discount − points`, leaving the VAT in, while `Sale.TotalAmount` (correctly, per RA 9994 / 9257 / 10754) subtracts it. Every Senior/PWD sale therefore **overcharged the customer by the VAT amount** and disagreed with its own ledger: on case B the customer was billed 82.14 while the ledger recorded 71.43. `Checkout` now derives the payable figure from a single shared `payableBeforePoints`, so the amount charged, the points cap and the points-earned calculation cannot drift apart, and the VAT rule matches `Sale.TotalAmount` exactly.
- [x] The statutory test keys off **`appliedDiscountType`, not `model.DiscountType`** — deliberate and load-bearing. When a membership tier discount out-grows the 20% statutory one, the membership wins and `appliedDiscountType` is reset to `None`, so the sale is no longer a statutory sale and the VAT correctly stays in the total. Keying off the requested type would wrongly waive VAT on case E-style sales. Verified by case E (statutory wins) and case D (membership wins, VAT retained).
- [x] **Fixed a second bug this phase uncovered: points could be burned for zero discount.** The points cap used `grossTotal − discount`, which ignores both the BOGO reduction and the VAT waiver, so it **overstated** what was payable. On case G the POS asked to redeem 10 points but the server redeemed **82** and the total clamped to **₱0.00** — the customer lost 82 loyalty points for 71.43 of value, 10.57 points destroyed for nothing. With the shared `payableBeforePoints` the cap is 71 and the total is 0.43: nothing wasted beyond unavoidable rounding. This defect was only reachable *because* BOGO lowers the gross, so it is genuinely a stacking bug rather than unrelated debt.
- [x] `Views/Sales/Create.cshtml` POS summary mirrored both rules (statutory VAT removal, and the points cap) so the screen cannot demand more cash or offer more points than the server will honour. Left explicitly alone: the client's subtotal still ignores the BOGO reduction (the known gap below).
- [x] `Views/Sales/Details.cshtml` prints a `12% VAT waived (Senior Citizen|PWD)` line for statutory sales so the receipt column visibly reconciles — without it the tax breakdown showed a VAT figure the total excluded. Suppressed when `VatAmount == 0`, confirmed by case H (VAT-exempt item: no spurious line).
- [x] Test rows purged: 15 test sales + details + billings, all audit rows above 142, 3 test medicines, 3 test batches. `CustomerMemberships.Id 23` restored to `Status = 2` and customer 54's `LoyaltyPoints` restored to 120. DB back to **90 sales / 90 billings / 37 medicines / max AuditLog 142** — the same baseline as Phases 4 and 5. App stopped; build 0 warn / 0 err.

### Remaining (open after Phase 6)
- [x] ~~**Phase 6 of 6**~~ — done, see Phase 33 above.
- [ ] **POS subtotal gap (still open, now the only BOGO gap left).** The on-screen POS summary is built from full line price, so it ignores the BOGO reduction: a cashier sees ₱200 and collects ₱100, and on a Senior/PWD BOGO sale sees 142.86 and collects 71.43. Every *server*-side figure and both receipts are correct — this is display-only. It was left alone because Phase 4 was specified server-side-only and the fix means mirroring FEFO/batch logic client-side, which is exactly what was ruled out. The right fix is to have the server return per-line payable figures (the projection already sends `nearExpiryUnits` / `nearExpiryBatch` / `nearExpiryDate`) and render those instead of recomputing. **Fix before demo.**
- [ ] Membership-fee sales show `Gross Total ₱0.00` on their receipt, because `GrossAmount` sums `SaleDetails` and membership sales carry no detail rows. Pre-existing, unrelated to BOGO.
- [ ] Seed data still ships with no near-expiry stock, so the promo shows nothing on a fresh database.
- [ ] Audit rows 130–142 are orphans referencing Phase 4's deleted test sales. Left deliberately: audit data was not deleted, only the test transactions.
