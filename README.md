# TimeClock — Secure Attendance System

> A full-stack, production-grade time-tracking system built with **ASP.NET Core 9** and **React + Vite**.
> Designed around a **No-Trust Local Time** policy — every clock event is timestamped by a verified external time source, making payroll data tamper-resistant by architecture.

---

## Screenshots

> _Screenshots will be added here._

| Login | Employee Dashboard | Admin Dashboard |
|---|---|---|
| _(coming soon)_ | _(coming soon)_ | _(coming soon)_ |

---

## Key Features

### Security & Integrity
- **No-Trust Local Time** — the server clock is never used for payroll. Every Clock In/Out is timestamped by an external time authority (WorldTimeAPI → timeapi.io fallback).
- **JWT Authentication** with zero clock-skew tolerance — tokens expire exactly on time.
- **Role-Based Access Control** — `Employee` and `Admin` roles enforced at the API level.
- **BCrypt password hashing** — passwords are never stored in plain text.

### Resilience
- **Polly retry policy** — the primary time API is retried up to 3 times with exponential back-off (2 s → 4 s → 8 s) before falling back.
- **Automatic fallback** — if WorldTimeAPI fails, the system seamlessly switches to timeapi.io.
- **Hard block on failure** — if both time APIs are unavailable, Clock In/Out is blocked and a `Critical` system alert is raised. No guesses, no silent failures.
- **Manual Admin Close & Audit Trail** — open shifts are never closed automatically. If a shift remains open for more than 12 hours, the system generates a `Warning` alert and highlights the shift in red on the Admin dashboard. Only an administrator can close it by providing an explicit end time and a documented reason.

### Employee Dashboard
- One-tap Clock In / Clock Out with live elapsed timer (`HH:mm:ss`).
- Real-time status chip and last-event details.
- Recent attendance history with colour-coded event chips (including **Admin Closed** for admin-forced closures, showing the recorded reason).
- Graceful 503 error message when the time service is temporarily unavailable.

### Admin Dashboard
- **Live status table** — all clocked-in employees with real-time duration counters.
- **Orphan shift highlighting** — shifts open longer than 12 hours are shown with a **red background** and a warning icon.
- **Force-close shift modal** — admin provides an exact end time (DateTime picker) and a mandatory reason before confirming the close. Both are saved to the audit trail.
- **System Alerts Center** — colour-coded list (Critical / Warning / Info) with a notification badge for recent critical events.
- Auto-refreshes every 30 seconds without a page reload.

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | React 19, Vite 7, TypeScript 5 |
| **UI Library** | Material UI (MUI) v7 |
| **Data Fetching** | TanStack React Query v5 |
| **HTTP Client** | Axios |
| **Routing** | React Router v7 |
| **Backend** | ASP.NET Core 9 Web API |
| **ORM** | Entity Framework Core 9 (Code-First) |
| **Database** | SQL Server / LocalDB |
| **Resilience** | Polly (`Microsoft.Extensions.Http.Polly`) |
| **Auth** | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| **Password Hashing** | BCrypt.Net-Next |
| **API Docs** | Swashbuckle / Swagger UI (with JWT support) |

---

## Prerequisites

Make sure the following are installed before proceeding:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 9.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18 LTS+ | https://nodejs.org |
| SQL Server LocalDB | Included with VS 2022 | https://visualstudio.microsoft.com |
| `dotnet-ef` CLI tool | latest | `dotnet tool install --global dotnet-ef` |

---

## Installation & Setup

### 1. Clone the repository

```bash
git clone <your-repository-url>
cd "Home Task"
```

### 2. Configure the Backend

Open `backend/TimeClock.API/appsettings.json` and update the JWT secret key:

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARS"
  }
}
```

> **Note:** The database connection string defaults to `(localdb)\mssqllocaldb` and works out of the box with Visual Studio / SQL Server Express LocalDB.

### 3. Restore & Run the Backend

```bash
cd backend
dotnet restore
dotnet run --project TimeClock.API
```

On first start, the application will:
1. **Apply EF Core migrations** automatically (`db.Database.Migrate()`).
2. **Seed three default users** if the database is empty.

The API will be available at `https://localhost:7xxx` (exact port shown in the terminal).
Swagger UI: `https://localhost:7xxx/swagger`

#### Default Seed Users

| Role | Email | Password |
|---|---|---|
| Admin | `admin@test.com` | `Admin123!` |
| Employee | `user@test.com` | `User123!` |
| Employee | `dina@test.com` | `dina123!` |

### 4. Configure the Frontend

Create or edit `frontend/timeclock-client/.env`:

```env
VITE_API_BASE_URL=https://localhost:<YOUR_BACKEND_PORT>/api
```

Replace `<YOUR_BACKEND_PORT>` with the port shown when the backend starts.

### 5. Install & Run the Frontend

```bash
cd frontend/timeclock-client
npm install
npm run dev
```

The app will be available at `http://localhost:5173`.

---

## Project Structure

```
Home Task/
├── backend/
│   ├── TimeClock.sln
│   ├── TimeClock.Core/            # Entities, Interfaces, DTOs, Exceptions (no dependencies)
│   ├── TimeClock.Infrastructure/  # EF Core, Repositories, External HTTP Clients
│   ├── TimeClock.Services/        # Business logic (AttendanceService, TimeProviderService)
│   └── TimeClock.API/             # Controllers, Program.cs, JWT config
└── frontend/
    └── timeclock-client/
        └── src/
            ├── api/               # Axios functions (auth, attendance, admin)
            ├── components/        # Reusable components (ProtectedRoute, etc.)
            ├── pages/             # LoginPage, EmployeeDashboardPage, AdminDashboardPage
            ├── theme/             # MUI theme (Deep Blue + Orange palette)
            └── types/             # TypeScript interfaces mirroring backend DTOs
```

---

## Architecture Overview

### N-Tier Backend

```
Controller  →  Service  →  Repository  →  Database
                  ↓
          ITimeProviderService
                  ↓
     WorldTimeApiClient  (primary, Polly retry)
     TimeApiIoClient     (fallback, fail-fast)
```

Each layer depends only on **interfaces defined in `TimeClock.Core`**, which has zero external dependencies. This makes each layer independently testable and replaceable.

### No-Trust Local Time Policy

The system is designed for environments where **local system clocks cannot be trusted** (e.g., VMs, shared workstations). The decision process on every Clock In/Out:

```
1. Call WorldTimeAPI (Europe/Zurich) — up to 3 retries with exponential back-off
2. If all retries fail → call timeapi.io (single attempt, fail-fast)
3. If both fail → throw TimeProviderUnavailableException
   → Block the operation
   → Create a Critical SystemAlert with user context
   → Return HTTP 503 to the client
```

Timestamps are stored as `DateTimeOffset` (Zurich time with UTC offset) in SQL Server `datetimeoffset` columns, making payroll queries timezone-aware and unambiguous.

### Manual Admin Close & Audit Trail

Open shifts are **never closed automatically**. The workflow for handling a forgotten clock-out is:

```
1. Shift remains open indefinitely — no silent auto-closure.
2. At each admin dashboard refresh, CheckOrphanShiftAlertsAsync() runs:
   → Finds all ClockIn entries open for > 12 hours.
   → Creates a Warning SystemAlert: "User {Name} has an open shift for over 12 hours."
3. Admin sees the shift highlighted in red in the Live Status table.
4. Admin clicks "Force Close" → a modal opens requiring:
   → ManualEndTime  (DateTime picker — the exact end time to record)
   → Reason         (free-text — mandatory, saved to the audit trail)
5. On confirm, the system creates a ManualClose log entry with:
   → EventType       = ManualClose
   → OfficialTimestamp = admin-supplied ManualEndTime
   → TimeSource      = "Admin-Override"
   → IsManuallyClosed = true
   → ManualCloseReason = provided reason
6. An Info SystemAlert is created: "Shift for {Name} was manually closed. Reason: …"
7. The shift disappears from the live status table.
```

The `ManualCloseReason` is visible to the employee in their attendance history, providing full transparency.

---

## API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Returns a signed JWT |

### Attendance
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/attendance/clock-in` | Employee | Record Clock In |
| POST | `/api/attendance/clock-out` | Employee | Record Clock Out |
| GET | `/api/attendance/status` | Employee | Current status + last event |
| GET | `/api/attendance/history` | Employee | Full attendance history |

### Admin
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/admin/live-status` | Admin | All currently clocked-in employees (also triggers orphan-shift alert scan) |
| GET | `/api/admin/alerts` | Admin | System alerts (filterable by severity/time) |
| POST | `/api/admin/close-shift` | Admin | Force-close a user's active shift (requires `ManualEndTime` + `Reason`) |

---

## License

This project is for demonstration purposes.
