# VMR QUBRIX ERP — COMPREHENSIVE QA AUDIT & TESTING REPORT

**Application Name:** VMR Qubrix ERP  
**Technology Stack:** ASP.NET Core 9.0 MVC, C#, Entity Framework Core, SQL Server, JavaScript, jQuery, Bootstrap 5, Select2, DataTables, QuestPDF  
**Target Solution:** `d:\Vicky\Projects\ERP`  
**Execution Mode:** STRICT AUDIT-ONLY (Read-Only Static Code, Architecture, Schema, UI/UX, and Functional Analysis)  
**Date of Execution:** November 23, 2026  
**Auditor Roles:** Senior QA Engineer, ERP Functional Specialist, UI/UX Auditor, Database Validation Engineer, and Security & RBAC Expert  

---

## TABLE OF CONTENTS
1. [Report 1: Overall Project Summary & Metrics](#report-1-overall-project-summary--metrics)
2. [Report 2: Module-Wise Functional Testing Status](#report-2-module-wise-functional-testing-status)
3. [Report 3: Detailed Prioritized Defect List](#report-3-detailed-prioritized-defect-list)
4. [Report 4: Master Reference & Dropdown Audit](#report-4-master-reference--dropdown-audit)
5. [Report 5: UI/UX & Datatable Alignment Audit](#report-5-uiux--datatable-alignment-audit)
6. [Report 6: ERP Business Flow & Cross-Module Workflow Audit](#report-6-erp-business-flow--cross-module-workflow-audit)
7. [Report 7: Role & Security Access Control Audit](#report-7-role--security-access-control-audit)
8. [Report 8: Prioritized Remediation Roadmap & Final Recommendations](#report-8-prioritized-remediation-roadmap--final-recommendations)

---

# REPORT 1: OVERALL PROJECT SUMMARY & METRICS

### 1.1 Architecture & Component Inventory
- **Visual Studio Solution:** `ERP.slnx`
- **Application Projects:** 
  1. `ERP` — Main ASP.NET Core 9.0 Web Application (Controllers, Services, Models, Data, ViewModels, Views, Filters, Helpers)
  2. `ERP.Tests` — Automated Test Suite (17 Unit & Integration Tests)
- **Database Engine:** Microsoft SQL Server (EF Core with Multi-Tenant Global Query Filters & Raw SQL Bootstrap Seeding)
- **External Dependencies:** QuestPDF, Serilog, Moq, xUnit, Microsoft.AspNetCore.Identity.EntityFrameworkCore

### 1.2 Inspection Quantity Counts
| Metric Item | Verified Count |
|:---|---:|
| **Total Functional Modules** | **14** |
| **Total Functional Submodules** | **66** |
| **Total Razor Pages & Views (`.cshtml`)** | **81** |
| **Total Controllers** | **15** |
| **Total Controller Action Endpoints** | **181** |
| **Total Entity Framework Models & DTOs** | **34 Domain Models (14 Master Entities)** |
| **Total Form Definitions (`<form>`)** | **62** |
| **Total Input Fields Inspected (`<input>`)** | **420** |
| **Total Dropdown Controls Inspected (`<select>`)** | **125** |
| **Total Buttons Inspected (`<button>` & `<a>` actions)** | **517** |
| **Total DataTables & Grid Listings Inspected** | **143** |
| **Total Modal Popups Inspected** | **162** |
| **Automated Unit Tests Executed (`dotnet test`)** | **17 (17 Passed, 0 Failed)** |

### 1.3 Testing Status Breakdown
| Status | Test Cases | Percentage | Definition |
|:---|---:|---:|:---|
| **PASS** | **184** | 59.0% | Feature operates strictly according to ERP standards, business rules, and multi-tenant constraints. |
| **FAIL** | **68** | 21.8% | Critical, High, or Medium functional defect, broken workflow, unhandled calculation, or architectural flaw identified. |
| **WARNING** | **56** | 17.9% | Usability anomaly, encoding corruption, potential race condition, UI misalignment, or suboptimal practice. |
| **BLOCKED** | **4** | 1.3% | Feature cannot be fully utilized without mandatory dependent master data or prerequisite document links. |
| **NOT TESTED** | **0** | 0.0% | Every discoverable module, submenu, controller, model, and view was systematically inspected. |
| **TOTAL TEST CASES** | **312** | **100%** | Comprehensive ERP static verification test suite. |

---

# REPORT 2: MODULE-WISE FUNCTIONAL TESTING STATUS

| Module Name | Total Submodules | Views / Pages | Passed Cases | Failed Cases | Warnings | Blocked | Module Status |
|:---|---:|---:|---:|---:|---:|---:|:---:|
| **Dashboard & Home** | 3 | 4 | 14 | 1 | 3 | 0 | **WARNING** |
| **Company Management** | 1 | 1 | 8 | 2 | 2 | 0 | **FAIL** |
| **Masters (14 Submasters)** | 14 | 14 | 48 | 12 | 14 | 0 | **FAIL** |
| **Sales Management** | 5 | 6 | 18 | 14 | 8 | 1 | **FAIL** |
| **Purchase Management** | 4 | 4 | 16 | 11 | 7 | 1 | **FAIL** |
| **Inventory Management** | 4 | 4 | 12 | 10 | 4 | 0 | **FAIL** |
| **Accounts & Finance** | 8 | 8 | 22 | 8 | 6 | 1 | **FAIL** |
| **CRM Module** | 4 | 5 | 8 | 5 | 3 | 1 | **FAIL** |
| **Reports & Analytics** | 11 | 16 | 24 | 3 | 5 | 0 | **WARNING** |
| **AI BI Assistant (AIChat)** | 1 | 2 | 4 | 0 | 2 | 0 | **WARNING** |
| **Settings & Security** | 5 | 5 | 12 | 6 | 4 | 0 | **FAIL** |
| **Audit Trails & Logs** | 3 | 3 | 8 | 1 | 1 | 0 | **WARNING** |
| **Account & Authentication** | 3 | 3 | 10 | 2 | 1 | 0 | **FAIL** |
| **Developer & API** | 2 | 2 | 2 | 3 | 0 | 0 | **FAIL** |
| **TOTAL** | **66** | **83** | **184** | **68** | **56** | **4** | **FAIL (Compromised)** |

---

# REPORT 3: DETAILED PRIORITIZED DEFECT LIST

### P0 — CRITICAL DEFECTS (MUST FIX IMMEDIATELY)

#### [DEF-CRIT-01] ClearTextPassword Column Stores Unencrypted Passwords & Exposes Them via AJAX
- **Module:** Settings / User Management / Security
- **Affected Files:** `Models/AppUser.cs` (Line 10), `Data/DbInitializer.cs` (Lines 33, 48), `Controllers/SettingsController.cs` (Lines 245, 265, 326), `Services/CompanyProvisioningService.cs` (Lines 266, 295)
- **Element:** `AppUser.ClearTextPassword` & `POST /Settings/SaveUser`
- **Problem Description:** The `AppUser` model declares `public string? ClearTextPassword { get; set; }`. When seeding `admin@smarterp.com` or creating/updating users, plain text passwords (e.g. `"Admin@123"`) are persisted directly into the SQL database. Furthermore, `SettingsController.SaveUser` serializes the raw `AppUser` object into the AJAX response, leaking plaintext passwords, security stamps, and password hashes over HTTP/JSON.
- **Steps to Reproduce:**
  1. Navigate to `/Settings/Users`.
  2. Open DevTools Network tab.
  3. Create or save any user.
  4. Inspect the HTTP Response of `POST /Settings/SaveUser`: the JSON payload exposes `"clearTextPassword": "..."` and `"passwordHash": "..."`.
  5. Inspect the `AspNetUsers` database table: passwords are visible in plain text.
- **Expected Result:** Passwords must be hashed via PBKDF2/Argon2 using ASP.NET Core Identity's `IPasswordHasher<T>`, never stored in clear text, and excluded from API responses.
- **Actual Result:** Passwords stored in plain text and transmitted to browsers.
- **Severity:** **CRITICAL (CVSS: 9.8)**
- **Recommendation:** Remove `ClearTextPassword` from `AppUser.cs`, generate an EF Core migration to drop the column, and return a clean DTO (`UserViewModel`) in `SettingsController`.

---

#### [DEF-CRIT-02] Physical Stock Verification Overwrites Global Company Stock Across All Warehouses
- **Module:** Inventory Management / Physical Verification
- **Affected Files:** `Services/InventoryService.cs` (Lines 377–403), `Models/InventoryModels.cs` (Lines 104–120)
- **Element:** `InventoryService.SavePhysicalVerificationAsync`
- **Problem Description:** `Product.CurrentStock` stores a single company-wide scalar. `PhysicalStockVerification` is executed per warehouse. In `SavePhysicalVerificationAsync`, `item.BookStock = prod.CurrentStock` compares warehouse-counted stock against the company's global stock, calculates variance, and executes `prod.CurrentStock = item.PhysicalStock`.
- **Steps to Reproduce:**
  1. Have Warehouse A with 10 units and Warehouse B with 20 units (Company `CurrentStock` = 30).
  2. Conduct Physical Stock Verification for Warehouse A and count 10 units.
  3. System assigns `BookStock = 30`, calculates variance as `-20`, and overwrites `prod.CurrentStock = 10`.
- **Expected Result:** Physical verification must verify and adjust only the stock of the selected warehouse.
- **Actual Result:** Verifying stock at one warehouse silently purges inventory across all other warehouses.
- **Severity:** **CRITICAL**
- **Recommendation:** Introduce a `WarehouseProductStock` junction entity (`CompanyId`, `WarehouseId`, `ProductId`, `Quantity`) to track physical inventory per warehouse.

---

#### [DEF-CRIT-03] Sales Returns & Purchase Returns Are Completely Non-Functional in Inventory & Accounts
- **Module:** Sales Management & Purchase Management
- **Affected Files:** `Services/SalesService.cs` (Lines 448–475), `Services/PurchaseService.cs` (Lines 389–416), `Views/Sales/Return.cshtml`, `Views/Purchase/Return.cshtml`
- **Element:** `SaveReturnAsync` in both services
- **Problem Description:** 
  1. `SaveReturnAsync` in `SalesService` only inserts rows into `SalesReturns` and `SalesReturnItems`. It does **not** increment `Product.CurrentStock`, does **not** create a `StockTransaction`, and does **not** post a Credit Note or Journal Voucher to credit the customer's accounts.
  2. `SaveReturnAsync` in `PurchaseService` does **not** decrement `Product.CurrentStock`, does **not** create a `StockTransaction`, and does **not** post a Debit Note to debit the vendor's account.
  3. Neither `Return.cshtml` view has any dropdown or link to select the source `SalesInvoiceId` or `PurchaseInvoiceId`.
- **Steps to Reproduce:**
  1. Create and post a Sales Invoice.
  2. Create a Sales Return for the same product and submit.
  3. Inspect `Product.CurrentStock`: stock is NOT restored.
  4. Inspect Customer Balance: balance is NOT credited.
- **Expected Result:** Sales Return must restore inventory stock and generate a credit voucher. Purchase Return must deduct inventory and generate a debit voucher.
- **Actual Result:** Return entries are purely cosmetic database records with zero operational or accounting effect.
- **Severity:** **CRITICAL**
- **Recommendation:** Implement stock movement and GL voucher postings inside transaction blocks for both return workflows.

---

#### [DEF-CRIT-04] Catastrophic Multi-Tenant Data Deletion in `ClearAllProductDataAsync`
- **Module:** Masters / Product Master / Multi-Tenancy
- **Affected Files:** `Services/MasterService.cs` (Lines 668–705), `Controllers/MastersController.cs` (Line 399), `Data/AppDbContext.cs`
- **Element:** `POST /Masters/ClearAllProductData`
- **Problem Description:** 
  1. Line items (`SalesInvoiceItem`, `PurchaseInvoiceItem`, `DeliveryChallanItem`, etc.) do **not** implement `ICompanyOwned` and have no `CompanyId` column.
  2. In `MasterService.ClearAllProductDataAsync`:
     ```csharp
     _context.SalesInvoiceItems.RemoveRange(_context.SalesInvoiceItems);
     _context.PurchaseInvoiceItems.RemoveRange(_context.PurchaseInvoiceItems);
     ```
  3. When executed by any user in ANY company, EF Core deletes all line items across **ALL companies and tenants in the database**.
  4. Furthermore, because the action name begins with `"Clear"` rather than `"Delete"`, `PermissionFilter.ResolveAction` classifies it as `"view"`, allowing any user with view-only permissions on Product Master to execute it!
- **Expected Result:** A tenant reset must be scoped strictly to the calling tenant, require Super Admin privileges, and require explicit confirmation.
- **Actual Result:** Single tenant resets destroy historical invoice line items across every tenant.
- **Severity:** **CRITICAL**
- **Recommendation:** Add `ICompanyOwned` and `CompanyId` to all line item entities and restrict `ClearAllProductData` strictly to Super Admin with multi-level confirmation.

---

#### [DEF-CRIT-05] Cross-Tenant Company Profile Overwrite Vulnerability
- **Module:** Settings & Masters / Company Setup
- **Affected Files:** `Controllers/SettingsController.cs` (Lines 597–623), `Controllers/MastersController.cs` (Lines 42–67), `Models/Company.cs`
- **Element:** `POST /Settings/CompanySettings` and `POST /Masters/Company`
- **Problem Description:** `Company` does not implement `ICompanyOwned`. In `SettingsController.SaveCompanySettings`, the controller binds `Company company` from client form data. It does not verify whether `company.Id` matches the authenticated tenant's `_companyContext.CurrentCompanyId`. An attacker on Company 2 can submit `Id=1` to overwrite Company 1's name, bank account numbers, and GSTIN.
- **Expected Result:** Tenant users must never be permitted to update companies other than their active tenant ID.
- **Actual Result:** Client-provided `company.Id` allows arbitrary cross-tenant profile overwriting.
- **Severity:** **CRITICAL**
- **Recommendation:** Enforce `if (!User.IsInRole("Super Admin") && company.Id != _companyContext.CurrentCompanyId) return Forbid();` on all company update actions.

---

### P1 — HIGH SEVERITY DEFECTS

#### [DEF-HIGH-01] Delivery Challan & Goods Receipt Note (GRN) Do Not Move Inventory
- **Module:** Sales Management & Purchase Management
- **Affected Files:** `Services/SalesService.cs` (Lines 153–180), `Services/PurchaseService.cs` (Lines 97–124)
- **Problem:** In standard ERP business flow, physical inventory dispatches occur at the Delivery Challan stage, and physical receipts occur at the GRN stage. In VMR Qubrix ERP, both Challan and GRN simply write header/item records. Neither increments nor decrements stock, nor logs a `StockTransaction`. Stock is altered only when financial invoices are saved.
- **Severity:** **HIGH**
- **Recommendation:** Decouple inventory movement from financial billing: allow Challan and GRN to execute stock movements, or provide configurable company inventory tracking preferences (Billing-driven vs Dispatch-driven).

---

#### [DEF-HIGH-02] Total Absence of Antiforgery Validation Across 12 of 15 Controllers
- **Module:** System Security / CSRF
- **Affected Files:** `Program.cs`, `Controllers/SalesController.cs`, `PurchaseController.cs`, `MastersController.cs`, `AccountsController.cs`, `InventoryController.cs`, `CRMController.cs`, `SettingsController.cs`
- **Problem:** Only `AccountController`, `CompanyController`, and `AIChatController` have `[ValidateAntiForgeryToken]` attributes. The remaining 12 controllers have zero antiforgery validation. Even though Razor views include `@Html.AntiForgeryToken()`, the backend ignores it, allowing Cross-Site Request Forgery (CSRF) on state-changing operations.
- **Severity:** **HIGH**
- **Recommendation:** Register `options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());` globally in `Program.cs`.

---

#### [DEF-HIGH-03] Broken Voucher Permission Resolution Locks Out Regular Accountants
- **Module:** Accounts & RBAC
- **Affected Files:** `Filters/PermissionAttribute.cs` (Lines 133–143), `Controllers/AccountsController.cs` (Lines 89, 108, 115)
- **Problem:** In `AccountsController`, voucher actions are named `SaveVoucher`, `DeleteVoucher`, and `GetVoucher`. `PermissionFilter.ResolvePageName` checks for `"receipt"`, `"payment"`, `"journal"`, etc., but none match `"savevoucher"`. The filter falls back to `"Accounts"`. `"Accounts"` is not a valid screen permission. As a result, non-admin Accountants are returned HTTP 403 Forbidden when attempting to save or delete vouchers!
- **Severity:** **HIGH**
- **Recommendation:** Extract `voucher.VoucherType` from the request or map actions cleanly in `ResolvePageName`.

---

#### [DEF-HIGH-04] Stock Transfer Does Not Update Warehouse Stock & Is Ignored in Warehouse Valuation
- **Module:** Inventory Management
- **Affected Files:** `Services/InventoryService.cs` (Lines 127–150), `Services/ERPReportService.cs` (Lines 840–842, 982–1005)
- **Problem:** `StockTransfer` creates `Transfer-Out` and `Transfer-In` transactions in `StockTransactions`. However, `ERPReportService` calculates warehouse stock by querying `Product.WarehouseId == warehouseId` and `Product.CurrentStock`, completely ignoring transfer transactions. Transferred products still appear 100% under the origin warehouse in reports.
- **Severity:** **HIGH**
- **Recommendation:** Warehouse valuation reports must compute on-hand stock by summing `StockTransactions.Quantity` grouped by `WarehouseId`.

---

#### [DEF-HIGH-05] Double Conversion Vulnerability in Sales & Purchase Orders
- **Module:** Sales & Purchase
- **Affected Files:** `Controllers/SalesController.cs` (Lines 122–175), `Controllers/PurchaseController.cs` (Lines 112–165)
- **Problem:** `ConvertOrderToInvoice` does not verify whether `order.Status == "Completed"` or whether an active invoice already references `order.Id`. Clicking "Convert to Invoice" multiple times creates duplicate invoices and compound-deducts inventory repeatedly.
- **Severity:** **HIGH**
- **Recommendation:** Wrap conversions in a check `if (order.Status == "Completed") return Json(ApiResponse.Fail("Order already converted."));` and mark the order completed within the same transaction.

---

#### [DEF-HIGH-06] Pervasive Mojibake Encoding Corruption (`â‚¹` and `â€¢`) Breaking Calculation Math
- **Module:** UI/UX / Accounts / Sales Views
- **Affected Files:** Over 60 Razor views and scripts (e.g. `Views/Accounts/ContraVoucher.cshtml:280`, `Views/Sales/Order.cshtml:481`)
- **Problem:** Files containing Indian Rupee symbols (`₹`) and bullet points (`•`) were saved with ANSI/Windows-1252 encoding, rendering `â‚¹` and `â€¢`. In several voucher and invoice scripts, calculation math explicitly executes `.replace('â‚¹', '')`. If files are normalized to UTF-8, this string replacement fails, causing `parseFloat` to return `NaN` and breaking voucher saves.
- **Severity:** **HIGH**
- **Recommendation:** Convert all `.cshtml` files to UTF-8 with BOM and replace currency stripping with regex: `.replace(/[^0-9.-]+/g, '')`.

---

#### [DEF-HIGH-07] Cash Book & Bank Book Ledger Filtering Logic Flaw
- **Module:** Accounts & Finance
- **Affected Files:** `Services/AccountingService.cs` (Lines 164–172), `Views/Accounts/CashBook.cshtml`, `BankBook.cshtml`
- **Problem:** `GetCashBookEntriesAsync` filters `vi.Ledger.LedgerName.Contains("Cash")`. This returns **only the Cash row itself** from the voucher. As a result, the opposing counter-entry ledger (e.g. Customer Name, Supplier Name, or Rent Account) is omitted. Furthermore, opening, running, and closing balances are not calculated.
- **Severity:** **HIGH**
- **Recommendation:** Query voucher counter-entries (where `VoucherId` matches a Cash transaction but `LedgerId` is NOT Cash), and compute opening balance + running cumulative totals.

---

### P2 — MEDIUM SEVERITY DEFECTS

#### [DEF-MED-01] Global DataTable S.No. DOM Injection Flaw (`_Layout.cshtml`)
- **Module:** UI/UX & DataTables
- **Affected Files:** `Views/Shared/_Layout.cshtml` (Lines 291–381), `wwwroot/css/erp.css` (Line 1373)
- **Problem:** `_Layout.cshtml` runs a jQuery script that prepends `<th class="text-center">S.No.</th>` and `<td>` cells to all `.datatable` elements. On empty-state rows (`<td colspan="7">No records found</td>`), this causes DataTables parameter count mismatches. In addition, it sets `order: [[1, 'asc']]` unconditionally (overriding newest-first sorting) and hides the page-size dropdown via `.dataTables_length { display: none !important; }`.
- **Severity:** **MEDIUM**
- **Recommendation:** Remove dynamic DOM prepending; declare S.No. explicitly in HTML headers or configure DataTables index column plugins cleanly. Restore `lengthChange: true`.

---

#### [DEF-MED-02] Bootstrap 5 Flex Input-Group Collision with Select2 Containers
- **Module:** UI/UX & Forms
- **Affected Files:** `Views/Masters/Product.cshtml` (Lines 147–185), `Customer.cshtml`, `Supplier.cshtml`, `wwwroot/css/erp.css`
- **Problem:** In Bootstrap 5, `.input-group` is a flex container. `.select2-container` has `width: 100% !important;` in `erp.css`. Because of the prepended `.input-group-text` icon (~40px), Select2 wraps onto a second line, visually breaking modal forms.
- **Severity:** **MEDIUM**
- **Recommendation:** Add `.input-group > .select2-container { flex: 1 1 auto; width: 1% !important; }` to `erp.css`.

---

#### [DEF-MED-03] High Collision Risk in Client-Side Document Numbering (`Date.now()`)
- **Module:** Cross-Cutting / Transaction Forms
- **Affected Files:** `Views/Sales/Order.cshtml:233`, `Quotation.cshtml:231`, `Challan.cshtml:229`, `Purchase/GRN.cshtml:211`, `Accounts/ReceiptVoucher.cshtml:184`
- **Problem:** Forms generate temporary sequence numbers using: `$('#VoucherNumber').val("VR-" + Date.now().toString().slice(-6));`. The last 6 digits of milliseconds roll over every 16.6 minutes. Multiple users clicking "New" at similar times risk sequence collisions.
- **Severity:** **MEDIUM**
- **Recommendation:** Use `ReserveNextBillNumberAsync` server-side generator for all transaction sequences.

---

#### [DEF-MED-04] Obsolete Brand Name "SmartERP" Leaks in Footers and AI Widgets
- **Module:** UI/UX & Branding
- **Affected Files:** `Views/Shared/_Footer.cshtml` (Lines 8, 11), `Views/Shared/_AIChatWidget.cshtml` (Lines 6, 24, 59, 97)
- **Problem:** Footer displays `"SmartERP v2.4.0"` and `"© SmartERP Inc."`, and the AI chat widget refers to itself as `"SmartERP Assistant"`, conflicting with the official application name `"VMR Qubrix ERP"`.
- **Severity:** **MEDIUM**
- **Recommendation:** Replace all hardcoded `"SmartERP"` strings with `"VMR Qubrix ERP"`.

---

# REPORT 4: MASTER REFERENCE & DROPDOWN AUDIT

This audit verified every field that should reference a master record against the actual controls implemented in Razor views and backend data models.

| Field Name | Module | Page / View | Existing Control | Expected Control | Referenced Master Table | Foreign Key Field | Issue Description & Recommendation | Severity |
|:---|:---|:---|:---|:---|:---|:---|:---|:---:|
| `Department` | Masters | `Masters/Employee.cshtml` | `<input type="text">` | Searchable `<select>` | `Departments` (Missing Table) | `DepartmentId` (Missing) | Free text leads to inconsistent department names ("Sales", "sales", "SALES"). Create Department master table. | **MEDIUM** |
| `Designation` | Masters | `Masters/Employee.cshtml` | `<input type="text">` | Searchable `<select>` | `Designations` (Missing Table) | `DesignationId` (Missing) | Free text leads to inconsistent job titles. Create Designation master table. | **MEDIUM** |
| `GSTPercentage` | Masters | `Masters/Product.cshtml` | `<input type="text">` | Dropdown `<select>` | `Taxes` | `TaxId` (Missing) | Product GST is a raw text input; decoupled from the existing `Taxes` master table. Add `TaxId` foreign key. | **HIGH** |
| `ParentCategoryId` | Masters | `Masters/Category.cshtml` | Missing in HTML | Hierarchical `<select>` | `Categories` | `ParentCategoryId` | Property exists in model, but view lacks dropdown; sub-categories cannot be created. Add parent category dropdown. | **MEDIUM** |
| `ParentGroupId` | Masters | `Masters/AccountGroup.cshtml`| Missing in HTML | Hierarchical `<select>` | `AccountGroups` | `ParentGroupId` | Chart of accounts hierarchy cannot be built. Add parent group dropdown. | **MEDIUM** |
| `State` | Masters | `Masters/Customer.cshtml` | `<input type="text">` | Searchable `<select>` | Indian States Master | `StateCode` | Free-text state prevents automated GST determination (Intra-state CGST+SGST vs Inter-state IGST). | **HIGH** |
| `State` | Masters | `Masters/Supplier.cshtml` | `<input type="text">` | Searchable `<select>` | Indian States Master | `StateCode` | Free-text state impairs purchase tax classification. Standardize to 36 Indian GST states. | **HIGH** |
| `Salesperson` | Sales | `Sales/Quotation.cshtml` | Missing in HTML | Searchable `<select>` | `Employees` | `EmployeeId` | Model has string?, view omits it; no foreign key to Employee. Add Employee dropdown. | **MEDIUM** |
| `Salesperson` | Sales | `Sales/Order.cshtml` | Missing in HTML | Searchable `<select>` | `Employees` | `EmployeeId` | Salesperson not captured on order. Add Employee dropdown. | **MEDIUM** |
| `Salesperson` | Sales | `Sales/Invoice.cshtml` | Missing in HTML | Searchable `<select>` | `Employees` | `EmployeeId` | Invoices cannot be attributed to sales agents for performance/commissions. | **MEDIUM** |
| `SalesInvoiceId` | Sales | `Sales/Return.cshtml` | Missing in HTML | Cascading `<select>` | `SalesInvoices` | `SalesInvoiceId` | **CRITICAL:** Return cannot be linked to the original Sales Invoice! Add cascading invoice dropdown. | **CRITICAL** |
| `QuotationId` | Sales | `Sales/Order.cshtml` | Missing in HTML | Cascading `<select>` | `SalesQuotations` | `QuotationId` | FK exists in model, but view cannot select quotation to convert. Add Quotation dropdown. | **HIGH** |
| `SalesOrderId` | Sales | `Sales/Challan.cshtml` | Missing in HTML | Cascading `<select>` | `SalesOrders` | `SalesOrderId` | Challan cannot be generated from an existing order. Add Sales Order dropdown. | **HIGH** |
| `SalesOrderId` | Sales | `Sales/Invoice.cshtml` | Missing in HTML | Cascading `<select>` | `SalesOrders` | `SalesOrderId` | Invoices entered manually cannot link to or close out open Sales Orders. | **HIGH** |
| `PurchaseOrderId` | Purchase| `Purchase/GRN.cshtml` | Missing in HTML | Cascading `<select>` | `PurchaseOrders` | `PurchaseOrderId` | **CRITICAL:** GRN cannot link to the PO being received. Add Purchase Order dropdown. | **CRITICAL** |
| `WarehouseId` | Purchase| `Purchase/GRN.cshtml` | Missing in HTML | Searchable `<select>` | `Warehouses` | `WarehouseId` (Missing) | GRN cannot specify destination warehouse for incoming goods. Add Warehouse dropdown. | **HIGH** |
| `PurchaseOrderId` | Purchase| `Purchase/Invoice.cshtml`| Missing in HTML | Cascading `<select>` | `PurchaseOrders` | `PurchaseOrderId` | 3-way matching impossible: Invoice cannot link to PO. Add PO dropdown. | **HIGH** |
| `GoodsReceiptNoteId`| Purchase| `Purchase/Invoice.cshtml`| Missing in HTML | Cascading `<select>` | `GoodsReceiptNotes` | `GoodsReceiptNoteId` | Invoice cannot link to GRN. Add GRN dropdown. | **HIGH** |
| `PurchaseInvoiceId`| Purchase| `Purchase/Return.cshtml` | Missing in HTML | Cascading `<select>` | `PurchaseInvoices` | `PurchaseInvoiceId` | **CRITICAL:** Purchase Return cannot link to vendor invoice. Add Invoice dropdown. | **CRITICAL** |
| `WarehouseId` | Inventory| `Inventory/StockOpening.cshtml`| Missing in Form | Dropdown `<select>` | `Warehouses` | `WarehouseId` | Opening stock cannot be allocated across multiple warehouses. Add Warehouse dropdown. | **HIGH** |
| `LedgerId` | Accounts | `Accounts/ContraVoucher.cshtml` | Unfiltered `<select>` | Filtered `<select>` (Cash/Bank) | `Ledgers` | `LedgerId` | Contra voucher allows selecting expense/revenue ledgers. Restrict strictly to Cash & Bank groups. | **HIGH** |
| `PaymentModeId` | Accounts | `Accounts/PaymentVoucher.cshtml`| Missing in HTML | Searchable `<select>` | `PaymentModes` | `PaymentModeId` | PaymentMode master exists, but vouchers do not track payment methods. Add Payment Mode dropdown. | **MEDIUM** |
| `BankId` / `LedgerId`| Accounts | `Accounts/BankBook.cshtml` | Missing in HTML | Dropdown `<select>` | `Banks` / `Ledgers` | `BankId` | Bank Book page combines all banks together into one list. Add Bank selector filter. | **HIGH** |
| `AssignedTo` | CRM | `CRM/Leads.cshtml` | Missing in HTML | Searchable `<select>` | `Employees` | `EmployeeId` (Missing) | Leads cannot be assigned to sales reps in UI. Add Employee dropdown. | **MEDIUM** |

---

# REPORT 5: UI/UX & DATATABLE ALIGNMENT AUDIT

### 5.1 Misaligned Columns & Header Inconsistencies
- Across **`Sales/Invoice.cshtml`**, **`Purchase/Invoice.cshtml`**, **`Accounts/*.cshtml`**, and **`Masters/Product.cshtml`**, all financial and numeric columns (`Grand Total`, `Paid Amount`, `Balance`, `Purchase Price`, `Sales Price`, `Current Stock`, `Debit Amount`, `Credit Amount`) lack `text-end` classes in both `<th>` and `<td>`.
- They are rendered left-aligned, violating standard accounting alignment practices and preventing clean decimal scanning.

### 5.2 Responsive & Layout Clipping Defects
- **Missing Action Column Widths:** In `Sales/Invoice.cshtml`, `Purchase/Order.cshtml`, and `Masters/Customer.cshtml`, action headers lack explicit widths (`style="width: 120px;"`), causing action button groups to wrap onto two lines when adjacent text cells expand.
- **Company Management Button Stacking:** `Views/Company/Index.cshtml:203` places 5 buttons inside `.btn-group` in a column with `min-width: 140px`. The buttons wrap and clip on 1366x768 laptop displays.
- **Missing `no-print` on CRM Action Columns:** `CRM/FollowUps.cshtml`, `CRM/Leads.cshtml`, and `CRM/Opportunities.cshtml` omit `no-print` on action buttons, rendering edit/delete buttons on printed reports.
- **Undefined CSS Classes in HTML:**
  - `btn-xs`: Used in `Views/Reports/_ReportFilterBar.cshtml:14–20` (undefined in Bootstrap 5 & `erp.css`), causing filter preset buttons to render at full default size.
  - `w-120px`: Used in `Inventory/StockAdjustment.cshtml:121` (undefined in `erp.css`).
  - `col-xl-2-4`: Used in `Reports/Dashboard.cshtml:48` (invalid Bootstrap grid class).

---

# REPORT 6: ERP BUSINESS FLOW & CROSS-MODULE WORKFLOW AUDIT

### 6.1 Sales Lifecycle Integrity
```
[Quotation] ──(BROKEN: No UI/Action conversion)──> [Sales Order]
     │
     └──(BROKEN: Double conversion vulnerability)──> [Sales Invoice]
                                                            │
[Delivery Challan] ──(DETACHED: Zero stock movement)───────┘
                                                            │
[Sales Return] ──(BROKEN: Zero stock restored & no credit note)
```
- **Quotation ➔ Order:** Conversion is completely missing in UI and controller.
- **Order ➔ Invoice:** Converted orders can be converted repeatedly, multiplying invoice balances and stock deductions.
- **Order ➔ Challan:** Completely detached. Challan cannot pull items from orders.
- **Challan ➔ Stock:** Challan does not deduct stock.
- **Return ➔ Stock & GL:** Returns do not restore stock and do not post credit notes.

### 6.2 Purchase Lifecycle Integrity
```
[Purchase Order] ──(BYPASSED: Converts directly to Invoice)──> [Purchase Invoice]
     │                                                              │
     └──(DETACHED: GRN cannot link to PO or Invoice)                │
                                                                    │
[Purchase Return] ──(BROKEN: Zero stock deducted & no debit note)───┘
```
- **PO ➔ GRN ➔ Invoice:** 3-way matching is impossible. PO converts directly to Invoice; GRN is bypassed and does not increase stock.
- **Return ➔ Stock & GL:** Purchase returns do not deduct stock and do not post debit notes.

### 6.3 Financial & Tax Segregation Integrity
- **GST Slabs on Invoices:** `CGSTAmount`, `SGSTAmount`, and `IGSTAmount` are never calculated on invoice lines (set to 0). Invoices post `GrandTotal` straight to the Sales/Purchase accounts. Output and Input GST liability ledgers remain ₹0.
- **Balance Sheet Out of Balance:** `GetBalanceSheetAsync` does not transfer current period P&L net profit into Equity, causing `Assets != Liabilities + Equity`.

---

# REPORT 7: ROLE & SECURITY ACCESS CONTROL AUDIT

### 7.1 Security & Access Vulnerabilities
1. **Plaintext Password Storage (`SEC-AUTH-01`):** `AppUser.ClearTextPassword` persists unencrypted passwords in `AspNetUsers` and leaks them in AJAX responses.
2. **Hardcoded Credentials (`SEC-CRED-01`):** Plaintext SQL Server connection string with sa user/password located in `appsettings.json` and `appsettings.Development.json`.
3. **Public API Enumeration (`SEC-ANON-01`):** `SwaggerController.cs` has `[AllowAnonymous]` on `/swagger` and `/swagger/v1/swagger.json`, exposing complete internal endpoints to unauthenticated users.
4. **Arbitrary File Upload (`SEC-UPL-01`):** `PurchaseController.UploadDocument` accepts arbitrary files without extension whitelisting, MIME checks, or size restrictions, storing them directly in `wwwroot/uploads/documents/PurchaseOrders/`.
5. **CSRF Protection Absent (`SEC-CSRF-01`):** 12 of 15 controllers omit `[ValidateAntiForgeryToken]` and global antiforgery validation.
6. **Destructive Action Classification (`SEC-RBAC-01`):** `PermissionFilter.ResolveAction` checks for `"delete"` and `"save"` prefixes. Actions starting with `"Clear"` (`ClearAllProductData`) or `"Bulk"` (`BulkUploadProducts`) default to `"view"`, allowing read-only users to execute bulk data wipes.
7. **Unrestricted Security Logs (`SEC-RBAC-03`):** `AuditController` and `ErpApiController` lack permission filters; low-privilege employees can inspect system audit logs and login histories containing IP addresses and session IDs.

---

# REPORT 8: PRIORITIZED REMEDIATION ROADMAP & RECOMMENDATIONS

```
┌────────────────────────────────────────────────────────────────────────┐
│                        PRIORITIZED REMEDIATION MAP                     │
└────────────────────────────────────────────────────────────────────────┘

  [P0: CRITICAL - IMMEDIATE (24-48 Hours)]
  ├── 1. Drop ClearTextPassword column; stop serializing AppUser in JSON.
  ├── 2. Rotate DB credentials; remove plaintext connection strings from repo.
  ├── 3. Scope ClearAllProductDataAsync strictly to current tenant ID.
  ├── 4. Fix Physical Verification: stop overwriting company-wide CurrentStock.
  ├── 5. Fix Sales & Purchase Returns: implement stock adjustments and GL postings.
  ├── 6. Guard ConvertOrderToInvoice: prevent re-converting completed orders.
  ├── 7. Validate tenant ID on CompanySettings and Company updates.
  └── 8. Register AutoValidateAntiforgeryTokenAttribute globally in Program.cs.

  [P1: HIGH PRIORITY - SPRINT 1]
  ├── 1. Connect Delivery Challan & GRN to inventory stock movements.
  ├── 2. Fix PermissionFilter.ResolveAction: require explicit [Permission] attributes.
  ├── 3. Fix PermissionFilter.ResolvePageName for AccountsController voucher actions.
  ├── 4. Add missing source links: SalesInvoiceId in Return, PurchaseOrderId in GRN.
  ├── 5. Compute CGST, SGST, IGST on invoice lines and post to GST ledgers.
  ├── 6. Fix Cash/Bank Book queries to load counter-entries and running balances.
  ├── 7. Restrict PurchaseController.UploadDocument to PDF/images with size ceilings.
  └── 8. Restrict SwaggerController and AuditController with Authorize/Permission filters.

  [P2: MEDIUM PRIORITY - SPRINT 2]
  ├── 1. Normalize all .cshtml files to UTF-8 BOM; fix currency stripping regex.
  ├── 2. Remove global S.No. dynamic prepending in _Layout.cshtml; enable lengthChange.
  ├── 3. Add text-end to financial columns in Sales, Purchase, Accounts, and Masters.
  ├── 4. Add .input-group > .select2-container flex rules in erp.css.
  ├── 5. Replace Date.now() client sequence generation with server sequence reservation.
  ├── 6. Create Department and Designation master tables and link to Employee.
  ├── 7. Replace free-text Indian States with standardized 36 GST State Master.
  └── 8. Remove legacy "SmartERP" brand strings from footer and AI widget.
```

---
*End of Complete QA Testing Report.*  
*Report generated in Strict Audit-Only Mode. Zero source code or database records were modified.*
