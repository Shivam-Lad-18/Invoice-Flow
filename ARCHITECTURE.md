# InvoiceFlow — Architecture & Implementation Plan

> AI-Powered Invoice Processing & Approval System  
> Stack: .NET 10 Web API · Angular 18 · Microsoft Azure  
> Pattern: Clean Architecture · CQRS + MediatR · Event-Driven

---

## 1. Confirmed Decisions

| Decision | Choice |
|---|---|
| Auth | ASP.NET Core Identity + custom JWT |
| Notifications | In-app only via Azure SignalR |
| Frontend hosting | Azure Static Web Apps (free tier) |
| Vendor creation | Admin-only via admin panel |
| Local dev | Real Azure dev resources + Key Vault with local credentials (DefaultAzureCredential) |

---

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                                 │
│   Angular 18 SPA  (Azure Static Web Apps)                           │
│   • Upload UI  • Approver Dashboard  • Admin Panel  • Analytics     │
└──────────────────────────┬──────────────────────────────────────────┘
                           │ HTTPS / JWT
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API LAYER                                   │
│   .NET 10 Web API  (Azure App Service F1)                           │
│   • REST endpoints    • JWT auth middleware                         │
│   • MediatR dispatcher → Commands / Queries                         │
│   • SignalR Hub for real-time push                                  │
└───────┬───────────────────────┬─────────────────────────────────────┘
        │ publish                │ query / command
        ▼                        ▼
┌───────────────┐     ┌──────────────────────────────────────────────┐
│ Azure Service │     │              APPLICATION LAYER               │
│     Bus       │     │  Commands · Queries · Validators · Behaviours│
│  (Queue)      │     │  Domain Events · Workflow Engine             │
└───────┬───────┘     └──────────────┬───────────────────────────────┘
        │ trigger                    │ calls
        ▼                            ▼
┌───────────────────┐   ┌────────────────────────────────────────────┐
│  Azure Functions  │   │           INFRASTRUCTURE LAYER             │
│  • AI Extractor   │   │  EF Core → Azure SQL                       │
│  • Notifier       │   │  Azure Blob Storage (invoice files)        │
│  • Daily Reminder │   │  Azure AI Document Intelligence            │
└───────────────────┘   │  Azure Key Vault (secrets at runtime)      │
                        │  Application Insights (telemetry)          │
                        └────────────────────────────────────────────┘
```

---

## 3. Clean Architecture Layer Map

```
InvoiceFlow/
├── src/
│   ├── InvoiceFlow.API/                  # Presentation layer
│   │   ├── Controllers/
│   │   │   ├── InvoicesController.cs
│   │   │   ├── ApprovalsController.cs
│   │   │   ├── VendorsController.cs
│   │   │   ├── AnalyticsController.cs
│   │   │   └── AdminController.cs
│   │   ├── Hubs/
│   │   │   └── NotificationHub.cs        # SignalR hub
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── CorrelationIdMiddleware.cs
│   │   └── Program.cs
│   │
│   ├── InvoiceFlow.Application/          # CQRS + business logic
│   │   ├── Invoices/
│   │   │   ├── Commands/
│   │   │   │   ├── UploadInvoice/
│   │   │   │   │   ├── UploadInvoiceCommand.cs
│   │   │   │   │   └── UploadInvoiceHandler.cs
│   │   │   │   └── CorrectExtractedData/
│   │   │   │       ├── CorrectExtractedDataCommand.cs
│   │   │   │       └── CorrectExtractedDataHandler.cs
│   │   │   └── Queries/
│   │   │       ├── GetInvoiceStatus/
│   │   │       ├── GetInvoiceDetail/
│   │   │       ├── GetInvoiceList/
│   │   │       └── GetBlobSasUrl/
│   │   ├── Approvals/
│   │   │   ├── Commands/
│   │   │   │   ├── ApproveStep/
│   │   │   │   ├── RejectStep/
│   │   │   │   └── DelegateStep/
│   │   │   └── Queries/
│   │   │       ├── GetPendingApprovals/
│   │   │       └── GetWorkflowChain/
│   │   ├── Vendors/
│   │   ├── Analytics/
│   │   ├── Admin/
│   │   ├── Common/
│   │   │   ├── Behaviours/
│   │   │   │   ├── ValidationBehaviour.cs       # FluentValidation pipeline
│   │   │   │   ├── DuplicateDetectionBehaviour.cs
│   │   │   │   └── LoggingBehaviour.cs
│   │   │   └── Interfaces/
│   │   │       ├── IBlobStorageService.cs
│   │   │       ├── IDocumentIntelligenceService.cs
│   │   │       ├── IServiceBusPublisher.cs
│   │   │       └── INotificationService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── InvoiceFlow.Domain/               # Entities, value objects, enums
│   │   ├── Entities/
│   │   │   ├── Invoice.cs
│   │   │   ├── InvoiceLineItem.cs
│   │   │   ├── ExtractionResult.cs
│   │   │   ├── ApprovalWorkflow.cs
│   │   │   ├── ApprovalStep.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── Vendor.cs
│   │   │   └── ApplicationUser.cs        # extends IdentityUser
│   │   ├── Enums/
│   │   │   ├── InvoiceStatus.cs          # Uploaded|Extracting|Extracted|PendingApproval|InApproval|Approved|Rejected
│   │   │   ├── ApprovalStepStatus.cs     # Pending|Approved|Rejected|Delegated|Skipped
│   │   │   └── UserRole.cs               # Vendor|Employee|Manager|FinanceHead|CFO|Admin
│   │   └── Events/
│   │       ├── InvoiceUploadedEvent.cs
│   │       ├── ExtractionCompletedEvent.cs
│   │       └── ApprovalDecisionEvent.cs
│   │
│   └── InvoiceFlow.Infrastructure/       # External service implementations
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/           # EF entity type configs
│       │   └── Migrations/
│       ├── Storage/
│       │   └── BlobStorageService.cs
│       ├── AI/
│       │   └── DocumentIntelligenceService.cs
│       ├── Messaging/
│       │   └── ServiceBusPublisher.cs
│       ├── Notifications/
│       │   └── SignalRNotificationService.cs
│       ├── Identity/
│       │   └── JwtTokenService.cs
│       └── DependencyInjection.cs
│
├── functions/
│   └── InvoiceFlow.Functions/            # Azure Functions
│       ├── ExtractionProcessor.cs        # Service Bus trigger → AI extraction
│       ├── ApprovalReminder.cs           # Timer trigger → daily 48h/72h check
│       └── NotificationDispatcher.cs     # Service Bus trigger → SignalR push
│
├── frontend/
│   └── invoiceflow-ui/                   # Angular 18 SPA
│       ├── src/app/
│       │   ├── features/
│       │   │   ├── auth/
│       │   │   ├── invoices/
│       │   │   ├── approvals/
│       │   │   ├── vendors/
│       │   │   ├── analytics/
│       │   │   └── admin/
│       │   ├── core/
│       │   │   ├── interceptors/         # JWT interceptor, error interceptor
│       │   │   ├── guards/               # Role-based route guards
│       │   │   └── services/
│       │   │       └── signalr.service.ts
│       │   └── shared/
│       └── staticwebapp.config.json      # Azure Static Web Apps routing
│
└── tests/
    ├── InvoiceFlow.UnitTests/
    └── InvoiceFlow.IntegrationTests/
```

---

## 4. Database Schema (Azure SQL)

### Tables

```sql
-- Users (ASP.NET Core Identity base + extensions)
ApplicationUsers
  Id (GUID PK), Email, PasswordHash, Role (enum), DepartmentId, IsActive,
  CreatedAt, LastLoginAt

-- Vendors
Vendors
  Id (GUID PK), Name, Email, TaxId, Status (Active|Blacklisted|Whitelisted),
  RegisteredAt, RegisteredByUserId (FK → Users)

-- Invoices
Invoices
  Id (GUID PK), VendorId (FK), BlobPath, OriginalFileName, FileSizeBytes,
  Status (InvoiceStatus enum), UploadedByUserId (FK), UploadedAt,
  DuplicateCheckHash (SHA256 of VendorId+InvoiceNumber+Amount)

-- ExtractionResults
ExtractionResults
  Id (GUID PK), InvoiceId (FK, unique), VendorName, InvoiceNumber, InvoiceDate,
  DueDate, TotalAmount, Currency, SubTotal, TaxAmount,
  ConfidenceScores (JSON), IsManuallyCorreected, ExtractedAt, CorrectedAt,
  CorrectedByUserId (FK)

-- InvoiceLineItems
InvoiceLineItems
  Id (GUID PK), ExtractionResultId (FK), Description, Quantity, UnitPrice,
  Amount, Confidence

-- ApprovalWorkflows
ApprovalWorkflows
  Id (GUID PK), InvoiceId (FK, unique), CreatedAt, CompletedAt,
  CurrentStepNumber, TotalSteps

-- ApprovalSteps
ApprovalSteps
  Id (GUID PK), WorkflowId (FK), StepNumber, RequiredRole (UserRole enum),
  AssignedToUserId (FK, nullable), Status (ApprovalStepStatus enum),
  Comment, DecidedAt, DelegatedFromUserId (FK, nullable),
  ReminderSentAt, EscalatedAt

-- AuditLogs
AuditLogs
  Id (GUID PK), InvoiceId (FK, nullable), UserId (FK, nullable), Action,
  OldValue (JSON), NewValue (JSON), IpAddress, CorrelationId, Timestamp
  -- append-only, no UPDATE/DELETE ever allowed on this table

-- ApprovalRules (admin-configurable)
ApprovalRules
  Id INT PK, MaxAmount DECIMAL, RequiredRoles (JSON array), LastUpdatedAt,
  LastUpdatedByUserId (FK)
```

### Key Indexes
- `Invoices.DuplicateCheckHash` — for O(1) duplicate detection
- `AuditLogs.InvoiceId + Timestamp` — for audit trail queries
- `ApprovalSteps.AssignedToUserId + Status` — for "my pending approvals" query
- `Invoices.Status + UploadedAt` — for dashboard list queries

---

## 5. Event-Driven Flow (Service Bus)

### Queues / Topics
| Queue | Producer | Consumer |
|---|---|---|
| `invoice-extraction` | API (UploadInvoiceHandler) | Azure Function: ExtractionProcessor |
| `approval-notifications` | API / Functions | Azure Function: NotificationDispatcher |

### Message Payloads

```json
// invoice-extraction queue
{
  "invoiceId": "guid",
  "blobPath": "invoices/2026/05/guid.pdf",
  "correlationId": "guid"
}

// approval-notifications queue
{
  "type": "ExtractionComplete | ApprovalRequired | StepDecided | Escalation",
  "invoiceId": "guid",
  "recipientUserIds": ["guid"],
  "payload": {}
}
```

---

## 6. Azure Function Details

### ExtractionProcessor (Service Bus Trigger)
1. Read message from `invoice-extraction` queue
2. Update `Invoice.Status = Extracting`
3. Download blob, call Azure AI Document Intelligence prebuilt invoice model
4. Map response → `ExtractionResult` + `InvoiceLineItems`
5. Flag fields with confidence < 0.70
6. Run duplicate detection (SHA256 hash check)
7. Update `Invoice.Status = Extracted` (or `409` duplicate flag)
8. Determine approval chain from `ApprovalRules` based on `TotalAmount`
9. Create `ApprovalWorkflow` + `ApprovalSteps`, set `Status = PendingApproval`
10. Publish to `approval-notifications` queue (type: `ApprovalRequired`)

### NotificationDispatcher (Service Bus Trigger)
1. Read notification message
2. Resolve SignalR connection IDs for recipient user IDs
3. Push to Angular client via SignalR hub

### ApprovalReminder (Timer Trigger — daily at 08:00 UTC)
1. Query all `ApprovalSteps` with `Status = Pending`
2. For steps > 48h old without `ReminderSentAt` → push reminder notification, set `ReminderSentAt`
3. For steps > 72h old without `EscalatedAt` → escalate to step's manager, set `EscalatedAt`, push escalation notification

---

## 7. Approval Workflow Engine

### Routing Rules (seeded + admin-editable)
| MaxAmount | Roles (sequential) |
|---|---|
| ₹50,000 | Manager |
| ₹5,00,000 | Manager → FinanceHead |
| ∞ | Manager → FinanceHead → CFO |

### Step Assignment Logic
- Manager: resolved as the direct manager of the invoice uploader
- FinanceHead / CFO: resolved by role lookup (any active user with that role in org)
- On `Delegate`: old step marked `Delegated`, new step created for target user, same step number

### Status Transitions
```
Invoice:  Uploaded → Extracting → Extracted → PendingApproval → InApproval → Approved / Rejected
Step:     Pending → Approved / Rejected / Delegated
```
- Invoice moves to `InApproval` when step 1 receives first action
- Invoice moves to `Approved` only when ALL steps are `Approved`
- Any single `Rejected` step → entire invoice `Rejected` immediately

---

## 8. JWT Auth & Authorization

- ASP.NET Core Identity stores users + password hashes in Azure SQL
- JWT issued on login: includes `userId`, `email`, `role` claims, 8h expiry
- Refresh token stored in DB, 7-day rolling window
- Policy-based auth in API:

```csharp
// Example policies
"CanApprove"   → roles: Manager, FinanceHead, CFO
"CanUpload"    → roles: Vendor, Employee
"FinanceOnly"  → roles: FinanceHead, CFO
"AdminOnly"    → roles: Admin
```

- Vendors: row-level filtering enforced in EF queries — Vendors can only see their own invoices

---

## 9. Security Practices

- All secrets in Azure Key Vault, loaded at startup via `DefaultAzureCredential` (managed identity in prod, CLI login in dev)
- SAS URLs for blob preview: 60-minute expiry, read-only
- GUID-based blob paths (no predictable URLs)
- Audit log table: no UPDATE/DELETE permissions granted to app DB user
- Input validation via FluentValidation on all commands
- File upload: MIME type + magic bytes validation, 10 MB limit enforced at API level
- CORS: strict origin whitelist (only Static Web Apps domain)

---

## 10. Angular 18 Frontend Structure

### Key Pages / Routes
| Route | Component | Roles |
|---|---|---|
| `/login` | AuthComponent | All |
| `/invoices` | InvoiceListComponent | All (filtered by role) |
| `/invoices/upload` | UploadComponent | Vendor, Employee |
| `/invoices/:id` | InvoiceDetailComponent | All (scoped) |
| `/approvals` | ApprovalDashboardComponent | Manager, FinanceHead, CFO |
| `/analytics` | AnalyticsDashboardComponent | Manager, FinanceHead, CFO, Admin |
| `/admin/vendors` | VendorManagementComponent | Admin |
| `/admin/users` | UserManagementComponent | Admin |
| `/admin/audit-logs` | AuditLogComponent | Admin |
| `/admin/rules` | ApprovalRulesComponent | Admin |

### Real-time (SignalR)
- `SignalRService` connects on login, disconnects on logout
- Events received: `ExtractionComplete`, `ApprovalRequired`, `StepDecided`, `Escalation`
- Toast notification shown + relevant list auto-refreshed

---

## 11. API Endpoint Reference (Complete)

### Invoices
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/invoices/upload | Vendor, Employee | Multipart upload → 202 + invoiceId |
| GET | /api/invoices | All | Paginated list (role-filtered) |
| GET | /api/invoices/{id} | All (scoped) | Full detail + extraction result |
| GET | /api/invoices/{id}/status | All (scoped) | Extraction status polling |
| PUT | /api/invoices/{id}/extracted-data | Employee, Admin | Correct low-confidence fields |
| GET | /api/invoices/{id}/blob-url | All (scoped) | 60-min SAS URL for PDF preview |
| GET | /api/invoices/{id}/audit | All (scoped) | Full audit trail for invoice |

### Approvals
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/approvals/pending | Manager, FinanceHead, CFO | My pending steps |
| POST | /api/approvals/{stepId}/approve | Assigned approver | Approve (optional comment) |
| POST | /api/approvals/{stepId}/reject | Assigned approver | Reject (mandatory comment) |
| POST | /api/approvals/{stepId}/delegate | Assigned approver | Delegate to eligible user |
| GET | /api/approvals/{invoiceId}/workflow | All (scoped) | Full workflow chain |

### Vendors
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/vendors | Admin | List all vendors |
| POST | /api/vendors | Admin | Register vendor + create user account |
| GET | /api/vendors/{id} | Admin | Vendor detail + invoice history |
| PATCH | /api/vendors/{id}/status | Admin | Toggle Whitelisted / Blacklisted |

### Analytics
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/analytics/summary | Manager+ | KPI counts, avg times |
| GET | /api/analytics/spend | FinanceHead, CFO, Admin | Spend by month/vendor |
| GET | /api/analytics/sla | Manager+ | SLA breach tracker |

### Admin
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | /api/admin/users | Admin | All users with roles |
| PATCH | /api/admin/users/{id}/role | Admin | Update user role |
| GET | /api/admin/approval-rules | Admin | Get amount threshold rules |
| PUT | /api/admin/approval-rules | Admin | Update amount threshold rules |
| GET | /api/admin/audit-logs | Admin | Searchable system-wide audit log + CSV export |

### Auth
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | /api/auth/login | Public | Returns JWT + refresh token |
| POST | /api/auth/refresh | Public | Refresh JWT |
| POST | /api/auth/logout | Authenticated | Revoke refresh token |

---

## 12. Application Insights Telemetry

Custom metrics to track:
- `invoice.extraction.duration_ms` — per invoice
- `invoice.extraction.confidence_avg` — per invoice
- `approval.step.duration_hours` — per step
- `invoice.duplicate.detected` — count
- `approval.escalation.triggered` — count

---

## 13. Build Plan (6 Weeks)

### Week 1 — Foundation
- [ ] Create solution structure (API, Application, Domain, Infrastructure, Functions, Frontend)
- [ ] Azure SQL schema + EF Core migrations (all tables)
- [ ] ASP.NET Core Identity setup + `ApplicationUser` entity
- [ ] JWT login/refresh/logout endpoints
- [ ] Azure Blob Storage upload endpoint (POST /api/invoices/upload → 202)
- [ ] Key Vault wiring with `DefaultAzureCredential`
- [ ] Local dev: appsettings.Development.json pointing to real Azure dev resources
- [ ] Seed admin user + approval rules defaults

### Week 2 — AI Core
- [ ] Azure Service Bus queue setup (`invoice-extraction`, `approval-notifications`)
- [ ] `ExtractionProcessor` Azure Function (Service Bus trigger)
- [ ] Azure AI Document Intelligence integration
- [ ] Confidence score flagging logic (< 0.70)
- [ ] Duplicate detection (SHA256 hash + DB check)
- [ ] `ExtractionResult` + `InvoiceLineItems` persistence
- [ ] Status polling endpoint (GET /api/invoices/{id}/status)
- [ ] Manual correction endpoint (PUT /api/invoices/{id}/extracted-data)

### Week 3 — Approval Engine
- [ ] `ApprovalWorkflow` + `ApprovalStep` creation after extraction
- [ ] Approval routing rules engine (amount → role chain)
- [ ] Approve / Reject / Delegate command handlers
- [ ] Invoice status transitions (InApproval → Approved / Rejected)
- [ ] `ApprovalReminder` Azure Function (daily timer)
- [ ] 48h reminder + 72h escalation logic
- [ ] `NotificationDispatcher` Azure Function + SignalR push
- [ ] Audit log writes on every state change

### Week 4 — Angular Frontend
- [ ] Angular 18 project setup with routing, HTTP client, JWT interceptor
- [ ] Auth pages (login) + role-based route guards
- [ ] Invoice upload page (drag-and-drop, progress indicator)
- [ ] Invoice list + detail pages
- [ ] PDF preview (SAS URL in iframe/PDF viewer)
- [ ] Approval dashboard (pending list, approve/reject/delegate actions)
- [ ] SignalR service + toast notifications

### Week 5 — Analytics & Admin
- [ ] Analytics endpoints (summary, spend, SLA)
- [ ] Angular analytics dashboard (KPI cards, Chart.js spend charts, SLA table)
- [ ] Vendor management (admin panel)
- [ ] User management + role assignment (admin panel)
- [ ] Audit log viewer with filtering + CSV export
- [ ] Approval rules admin panel

### Week 6 — Polish & Deploy
- [ ] Application Insights integration (custom metrics, distributed tracing)
- [ ] Health check endpoints (`/health`, `/health/ready`)
- [ ] Global exception handling + ProblemDetails responses
- [ ] CORS, rate limiting, file validation hardening
- [ ] Deploy API → Azure App Service (F1)
- [ ] Deploy Functions → Azure Functions (Consumption)
- [ ] Deploy Angular → Azure Static Web Apps
- [ ] End-to-end smoke test of full flow

---

## 14. Azure Resource Checklist

| Resource | SKU/Tier | Purpose |
|---|---|---|
| App Service Plan + Web App | F1 (free) | Host .NET 10 API |
| Azure SQL Database | Serverless free | All persistent data |
| Azure Blob Storage | LRS, standard | Invoice file storage |
| Azure Service Bus | Basic (~$0.10/mo) | Async queues |
| Azure Functions | Consumption (free 1M) | Background processing |
| Azure AI Document Intelligence | Free (500 pages/mo) | Invoice extraction |
| Azure SignalR Service | Free (20 concurrent) | Real-time notifications |
| Azure Key Vault | Standard | Secret management |
| Application Insights | Free (5GB/mo) | Telemetry |
| Azure Static Web Apps | Free tier | Angular SPA hosting |

**Estimated monthly cost: ~$0.10–$0.50** (only Service Bus has a nominal charge)

---

*Document generated: 2026-05-09. Update as decisions evolve.*
