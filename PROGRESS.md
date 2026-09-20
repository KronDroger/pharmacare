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
