# 📚 LibraTrack — Multi-Branch Library Management System


**Unit:** HIT-339 | Assessment 2 — Library Management System  
**Framework & Language:** ASP.NET Core 10.0 MVC (C#)  
**Database:** Entity Framework Core with SQLite (`libratrack.db`)  
**Front-End & UI:** Academic Editorial Theme (`library-theme.css`), Bootstrap 5.3, Bootstrap Icons 1.11.3, Chart.js v4  
**PDF Engine:** QuestPDF 2026.9.1 (Community License)  
**CSV Parsing & Serialization:** CsvHelper 33.1.0  
**API Documentation:** Swashbuckle OpenAPI 3.0 / Swagger UI  

---

## 📋 Table of Contents
1. [System Requirements & Installation](#1-system-requirements--installation)
2. [Multi-Branch Configuration](#2-multi-branch-configuration)
3. [Database Auto-Reset & Seeding](#3-database-auto-reset--seeding)
4. [Demo Accounts & Kiosk Credentials](#4-demo-accounts--kiosk-credentials)
5. [Feature Modules & URL Directory](#5-feature-modules--url-directory)
   - [A. Self-Service Kiosk (`/Kiosk`)](#a-self-service-kiosk-kiosk)
   - [B. Secure Public REST API (`/api/*`)](#b-secure-public-rest-api-api)
   - [C. Simulated Notification System (`/Notifications`)](#c-simulated-notification-system-notifications)
   - [D. Multi-Branch Inventory & Transfers (`/Transfers`)](#d-multi-branch-inventory--transfers-transfers)
   - [E. Reservations & Waitlist Queue (`/Reservations`)](#e-reservations--waitlist-queue-reservations)
   - [F. Admin Data Importer & Mock Metadata (`/Import`)](#f-admin-data-importer--mock-metadata-import)
   - [G. Manager Analytics & PDF/CSV Exports (`/Manager`)](#g-manager-analytics--pdfcsv-exports-manager)
6. [Step-by-Step Integration Testing Guide](#6-step-by-step-integration-testing-guide)
7. [Known Limitations & Architecture Decisions](#7-known-limitations--architecture-decisions)

---

## 1. System Requirements & Installation

### Prerequisites
- **.NET 10.0 SDK** (Verify by running `dotnet --version` in terminal).
- Any modern web browser (Google Chrome, Microsoft Edge, Mozilla Firefox, or Apple Safari).
- Terminal or command prompt (zsh, bash, PowerShell, or cmd).

### Clone and Run
The steps to clone and launch the application are identical for every deployment and branch location:

```bash
# 1. Clone the repository
git clone <repository-url>
cd "Minhaz Uddin Ahmed Mayaz_S378194_HIT-339_A2[LibraTrack]"

# 2. Restore NuGet dependencies
dotnet restore

# 3. Build the project
dotnet build

# 4. Run the application
dotnet run --urls "http://localhost:5000"
```

Open your browser to:
- **Application Portal:** [http://localhost:5000](http://localhost:5000)
- **API Documentation & Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

## 2. Multi-Branch Configuration

LibraTrack includes native multi-branch support across three physical branch libraries in the Northern Territory:

| Branch Code | Branch Name | Address | Phone |
| :--- | :--- | :--- | :--- |
| **`CEN`** | Darwin Central Library | Civic Centre, Harry Chan Ave, Darwin NT 0800 | +61 8 8930 0300 |
| **`CAS`** | Casuarina Library | 17 Bradshaw Terrace, Casuarina NT 0810 | +61 8 8930 0200 |
| **`PAL`** | Palmerston City Library | The Boulevard, Palmerston City NT 0830 | +61 8 8935 9999 |

### Setting the Branch Location
The deployment runs the exact same code base for every branch. To specify which branch library an installation represents, update the `"CurrentBranchCode"` setting in `appsettings.json`:

```json
{
  "CurrentBranchCode": "CEN"
}
```

Alternatively, set the environment variable without editing files:
```bash
# Run as Casuarina Library
CurrentBranchCode="CAS" dotnet run --urls "http://localhost:5000"

# Run as Palmerston City Library
CurrentBranchCode="PAL" dotnet run --urls "http://localhost:5000"
```

---

## 3. Database Auto-Reset & Seeding

The application uses Entity Framework Core with an embedded SQLite database (`libratrack.db`).

### Automatic Reset on Start (Development Mode)
To ensure all tables, multi-branch inventories, loans, notifications, reservations, and audit logs are freshly populated without manual SQL scripts, configure `appsettings.json`:

```json
{
  "ResetDatabaseOnStart": true
}
```

When `"ResetDatabaseOnStart": true` is enabled in development mode:
1. The existing SQLite file is cleanly removed.
2. EF Core schema creation runs automatically (`EnsureCreated()`).
3. `DbInitializer` populates the complete database:
   - 3 Staff accounts with hashed passwords (`Admin`, `Reception`, `Manager`).
   - 3 Active branch locations (`CEN`, `CAS`, `PAL`).
   - 8 Registered library patrons with 4-digit PINs.
   - 21 Seeded catalog items (Books, Vinyl Albums, Toys) distributed across branches.
   - 17 Historical and active loan transactions (active, returned, and overdue with fines).
   - Seeded automated notifications and hold reservations.
   - Initial catalog import audit logs.

To persist newly imported items and loans across app restarts, simply set `"ResetDatabaseOnStart": false`.

---

## 4. Demo Accounts & Kiosk Credentials

### A. Staff Logins (`/Account/Login`)
Log in at `/Account/Login` with any of the pre-seeded staff credentials:

| Role | Username | Password | Dashboard URL | Capabilities |
| :--- | :--- | :--- | :--- | :--- |
| **Admin** | `admin` | `Admin@123` | `/Items` | Catalog CRUD, Branch Transfers, Reservations, CSV/Mock Importer, Notifications |
| **Reception** | `reception` | `Reception@123` | `/Borrowers` | Patron CRUD, Issue/Return Loans, Fines Collection, Branch Transfers, Reservations |
| **Manager** | `manager` | `Manager@123` | `/Manager` | Executive Analytics, Borrowing Stats, Fine Audits, Inventory Health, CSV & PDF Exports |

### B. Seeded Kiosk Patron Accounts (`/Kiosk`)
Patrons access the self-service touch kiosk at `/Kiosk` using their library card number and 4-digit PIN:

| Card Number | Patron Name | 4-Digit PIN | Status | Pre-seeded Activity |
| :--- | :--- | :---: | :---: | :--- |
| **`BRW-1001`** | Alice Morgan | `1234` | Active | Active loan: *Atomic Habits* (`BOK-1002`) |
| **`BRW-1002`** | David Chen | `1234` | Active | Overdue loan: *Sapiens* (`BOK-1005`) |
| **`BRW-1003`** | Sophia Taylor | `1234` | Active | Ready Reservation: *Random Access Memories* (`MUS-2003`) |
| **`BRW-1004`** | Liam O'Connor | `1234` | Active | Overdue loan: *Settlers of Catan* (`TOY-3002`) |
| **`BRW-1005`** | Emma Watson | `1234` | Active | Clean account, past loan history |
| **`BRW-1006`** | Noah Patel | `1234` | Active | Waiting Queue: *Catan* (`TOY-3002`) |
| **`BRW-1007`** | Grace Kim | `1234` | Active | Waiting Queue: *Pride and Prejudice* (`BOK-1007`) |
| **`BRW-1008`** | Lucas Miller | `1234` | Active | Clean new patron account |

> **Security Note:** Kiosk sessions automatically lock patron accounts after 3 consecutive failed PIN attempts (`IsLockedOut = true`).

---

## 5. Feature Modules & URL Directory

### A. Self-Service Kiosk (`/Kiosk`)
- **URL:** [http://localhost:5000/Kiosk](http://localhost:5000/Kiosk)
- **Design:** Touch-optimized UI with high-contrast buttons and dedicated kiosk layout (standard navigation hidden).
- **Features:**
  - Card number and 4-digit PIN authentication.
  - **Check Out Flow:** Enter or scan item library code (e.g. `BOK-1003`). Validates item availability, ready holds, and condition before issuing.
  - **Barcode Scanner Support:** Text boxes support physical barcode scanners with automated Enter submission.
  - **My Account Flow:** Displays current borrowed items with due dates, accrued fines, and reservation queue status.
  - **Safety & Privacy:** 60-second inactivity countdown timer automatically signs out unattended sessions; includes instant "Finish / Logout" button.

---

### B. Secure Public REST API (`/api/*`)
- **Documentation UI:** [http://localhost:5000/ApiDocs](http://localhost:5000/ApiDocs) & [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Authentication:** All endpoints require the `X-Api-Key` header stored in `appsettings.json`:
  ```
  X-Api-Key: libra-track-secret-api-key-2026
  ```
- **Policy:** Read-only (`GET` only). Non-GET verbs (e.g. `POST`, `DELETE`) return `405 Method Not Allowed`. Missing or invalid API keys return `401 Unauthorized`.
- **CORS:** Enabled for `GET` with header allowance (`X-Api-Key`, `Content-Type`, `Accept`).
- **Rate Limiting:** Fixed-window rate limiter (60 requests/minute) returning `429 Too Many Requests`.
- **DTOs Only:** Internal database models and private patron/staff credentials are never exposed.

#### API Endpoints & Example cURL Calls:

1. **System Health & Catalog Metrics:**
   ```bash
   curl -s -H "X-Api-Key: libra-track-secret-api-key-2026" http://localhost:5000/api/status
   ```

2. **Paginated Items with Filters (`search`, `category`, `status`, `branchCode`, `page`, `pageSize`):**
   ```bash
   curl -s -H "X-Api-Key: libra-track-secret-api-key-2026" \
     "http://localhost:5000/api/items?category=Book&status=Available&branchCode=CEN&page=1&pageSize=5"
   ```

3. **Get Item by ID (with polymorphic Book/Music/Toy details):**
   ```bash
   curl -s -H "X-Api-Key: libra-track-secret-api-key-2026" http://localhost:5000/api/items/1
   ```

4. **Category Breakdown:**
   ```bash
   curl -s -H "X-Api-Key: libra-track-secret-api-key-2026" http://localhost:5000/api/categories
   ```

5. **Branch Locations & Catalog Holdings:**
   ```bash
   curl -s -H "X-Api-Key: libra-track-secret-api-key-2026" http://localhost:5000/api/branches
   ```

---

### C. Simulated Notification System (`/Notifications`)
- **Staff Dashboard:** [http://localhost:5000/Notifications](http://localhost:5000/Notifications) (Admin & Manager)
- **Service Contract:** `INotificationService` registered with Dependency Injection.
- **Channels:** Simulated `Email` and `SMS` channels. Writes to the `Notifications` table and outputs formatted console audit logs without sending real external emails.
- **Event Triggers:**
  1. Item issued to a patron (`NotificationType.Borrowed`).
  2. Late return incurring overdue fees (`NotificationType.FineIssued`).
  3. Returned item satisfies a waiting patron hold (`NotificationType.ReservationReady`).
- **Automated Background Monitor:**
  - `LoanNotificationBackgroundService` executes every minute (configurable via `NotificationSettings:CheckIntervalSeconds`).
  - Scans active loans due within 2 days (`NotificationType.DueSoon`) and overdue loans (`NotificationType.Overdue`).
  - Prevents duplicate notification spam using composite database index `(LoanId, Type)`.

---

### D. Multi-Branch Inventory & Transfers (`/Transfers`)
- **URL:** [http://localhost:5000/Transfers](http://localhost:5000/Transfers) (Admin & Reception)
- **Models:** `Branch` and `ItemTransfer` tracking `FromBranch`, `ToBranch`, `Status`, `RequestedAt`, `CompletedAt`, and `Notes`.
- **Transfer Lifecycle:**
  $$\text{Requested} \longrightarrow \text{In Transit} \longrightarrow \text{Received (Branch ID Updated)}$$
- **Safety Rule:** Items currently on loan (`Status == ItemStatus.Borrowed`) cannot be transferred until returned.
- **Search Integration:** Public catalog search (`/Catalog`) features a branch filter dropdown with live holding indicators.

---

### E. Reservations & Waitlist Queue (`/Reservations`)
- **URL:** [http://localhost:5000/Reservations](http://localhost:5000/Reservations) (Admin & Reception)
- **Features:**
  - Patrons can reserve items that are currently **Borrowed** or **Damaged**.
  - Duplicate reservations for the same item by the same patron are blocked.
  - Item detail pages display real-time queue lengths (e.g. *"2 patrons in queue"*).
  - When an item is returned in Good condition:
    1. The first waiting reservation transitions to `Ready`.
    2. An expiration date is set for 3 days later (`ExpiresAt = DateTime.UtcNow.AddDays(3)`).
    3. An automated `ReservationReady` notification is dispatched.
  - **Hold Lockout:** Other patrons cannot borrow an item with an active `Ready` reservation.
  - Background monitor automatically expires uncollected holds and promotes the next patron in queue.

---

### F. Admin Data Importer & Mock Metadata (`/Import`)
- **URL:** [http://localhost:5000/Import](http://localhost:5000/Import) (Admin Only)
- **CSV Batch Importer (CsvHelper):**
  - Downloadable template with sample Book, Music, and Toy records (`/Import/DownloadTemplate`).
  - **Validation Preview Table:** Verifies row format (`XXX-0000`), duplicate codes (against DB and within file), and active branch codes before import.
  - **Atomic Transaction:** Valid rows are committed in a single database transaction. Invalid rows are skipped and logged.
  - **Downloadable Error Report:** Generates a CSV audit of all skipped rows and their validation error reasons (`/Import/DownloadErrorReport`).
- **Mock Metadata Provider (`IBookMetadataProvider`):**
  - Reads [Data/MockBooks.json](file:///Users/nahyan/Desktop/Assignment%20works/Minhaz%20Uddin%20Ahmed%20Mayaz_S378194_HIT-339_A2[LibraTrack]/Data/MockBooks.json) with rich book metadata (ISBN, Author, Publisher, Page Count, Descriptions, Covers).
  - Search box on the `/Import` page allows searching by Title, Author, or ISBN.
  - Admins can import any result directly into the selected branch with candidate code generation.

---

### G. Manager Analytics & PDF/CSV Exports (`/Manager`)
- **Manager Dashboard:** [http://localhost:5000/Manager](http://localhost:5000/Manager)
- **Interactive Reports:**
  1. **Borrowing Statistics:** [http://localhost:5000/Manager/BorrowingStatistics](http://localhost:5000/Manager/BorrowingStatistics)
  2. **Fine Revenue Audit:** [http://localhost:5000/Manager/FineRevenueAudit](http://localhost:5000/Manager/FineRevenueAudit)
  3. **Inventory Health:** [http://localhost:5000/Manager/InventoryHealth](http://localhost:5000/Manager/InventoryHealth)
  4. **Executive KPI Statistics:** [http://localhost:5000/Manager/Statistics](http://localhost:5000/Manager/Statistics)
- **Export Formats:**
  - **CSV Exports:** Pure RFC-compliant tabular data including branch locations.
  - **QuestPDF Exports:** Vector PDF documents with headers, date range, generation timestamp, summary KPI cards, styled data tables, and page counters.

---

## 6. Step-by-Step Integration Testing Guide

Follow these steps to test all integrated workflows:

### Test 1: Self-Service Kiosk Checkout
1. Open browser to `http://localhost:5000/Kiosk`.
2. Click **Check Out**.
3. Enter Card Number: `BRW-1001` and PIN: `1234`.
4. Enter Item Code: `BOK-1003` (*Dune: Chronicles of Arrakis*).
5. Click **Confirm Checkout**.
6. Item is checked out, notification logged, and success receipt displayed.
7. Click **My Account** to verify the active loan with due date.
8. Click **Finish / Logout**.

### Test 2: Multi-Branch Transfer Workflow
1. Log in at `/Account/Login` as `admin` (`Admin@123`).
2. Navigate to **Transfers** (`/Transfers`).
3. Click **Request Item Transfer**.
4. Select item `BOK-1001` (currently at Darwin Central Library) and destination `Palmerston City Library`.
5. Click **Create Transfer**. Status shows `Requested`.
6. Click **Mark in Transit**. Status updates to `InTransit`.
7. Click **Mark Received**. Item's branch location changes to Palmerston. Verify on public catalog search (`/Catalog`).

### Test 3: Reservation Hold & Auto-Notification
1. While logged in as `reception` (`Reception@123`), go to `/Reservations`.
2. Notice item `BOK-1002` (*Atomic Habits*) is currently Borrowed.
3. Place a reservation for patron `BRW-1005` (Emma Watson).
4. Go to **Loans & Fines** (`/Loans/Return`).
5. Process return of `BOK-1002` in **Good** condition.
6. The system sets the hold status to `Ready` and issues a `ReservationReady` notification to Emma Watson.
7. Attempt to checkout `BOK-1002` to another patron (e.g. `BRW-1006`) at the Kiosk; checkout is blocked with an active hold warning.

### Test 4: CSV Catalog Import & Error Handling
1. Log in as `admin` (`Admin@123`) and navigate to **Data Importer** (`/Import`).
2. Click **Download CSV Template** to inspect the column layout.
3. Upload `test_bad.csv` (included in project directory).
4. Review preview table: 4 rows marked `Invalid` (bad format, duplicate code, unknown branch, missing author) and 1 row marked `Valid`.
5. Click **Confirm & Import Valid Rows (1)**.
6. Summary page displays 1 imported, 4 skipped with errors.
7. Click **Download Error Report (CSV)** to retrieve the failure log.
8. Upload `test_good.csv`. Preview displays 3 Valid rows. Click Confirm to import all items into catalog.

### Test 5: Mock Metadata Search & Import
1. On the `/Import` page, switch to the **Mock Metadata Provider** tab.
2. Search for `"Project Hail Mary"`.
3. Click **Add to Catalog**.
4. Select destination branch (`Casuarina Library`) and verify code (`BOK-1030`).
5. Click **Confirm & Save**. The book is added to catalog and recorded in `ImportLog`.

### Test 6: Manager PDF & CSV Exports
1. Log in as `manager` (`Manager@123`).
2. Navigate to **Borrowing Stats** (`/Manager/BorrowingStatistics`).
3. Select Branch: `Casuarina Library` and click **Apply Filter**.
4. Click **Export CSV**: downloads filtered circulation data with branch name.
5. Click **Export PDF**: downloads QuestPDF vector audit document.
6. Repeat on **Fine Audit** (`/Manager/FineRevenueAudit`) and **Inventory Health** (`/Manager/InventoryHealth`).

---

## 7. Known Limitations & Architecture Decisions

1. **Simulated Notification Dispatch:**
   - Notifications write directly to the `Notifications` table and output formatted logging to the console. Real SMTP email relay or external SMS APIs (e.g. Twilio) are intentionally not configured to avoid external API charges and network credentials during academic assessment.
2. **SQLite Concurrency & Multi-Process Model:**
   - The application uses SQLite for zero-configuration testing. SQLite uses file-level locking, which is ideal for single-server or development deployments. In high-concurrency enterprise clusters, switching the connection string to PostgreSQL or Microsoft SQL Server is recommended.
3. **API Key Management:**
   - The public REST API uses a unified secure header key (`X-Api-Key`) stored in `appsettings.json`. A multi-tenant database-backed API key registration portal is not implemented to keep the read-only catalog API lightweight.
4. **Rate Limiting Window:**
   - In-memory fixed-window rate limiter is set to 60 requests/minute per client IP address. Distributed Redis rate limiting would be utilized in multi-node cloud deployments.
