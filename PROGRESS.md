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
