// ── Enums (numeric — API serialises C# enums as integers by default) ──────────

export enum InvoiceStatus {
  Uploaded = 0,
  Extracting = 1,
  Extracted = 2,
  PendingApproval = 3,
  InApproval = 4,
  Approved = 5,
  Rejected = 6,
}

export enum ApprovalStepStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Delegated = 3,
  Skipped = 4,
}

export enum UserRole {
  Vendor = 0,
  Employee = 1,
  Manager = 2,
  FinanceHead = 3,
  CFO = 4,
  Admin = 5,
}

// ── List / Paginated ─────────────────────────────────────────────────────────

export interface GetInvoicesParams {
  page?: number;
  pageSize?: number;
  status?: InvoiceStatus | null;
  vendorId?: string | null;
  from?: Date | null;
  to?: Date | null;
}

export interface InvoiceListItem {
  id: string;
  originalFileName: string;
  fileSizeBytes: number;
  status: InvoiceStatus;
  vendorId: string;
  vendorName: string | null;
  uploadedAt: string;
  totalAmount: number | null;
  currency: string | null;
}

export interface GetInvoicesResponse {
  items: InvoiceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Detail ───────────────────────────────────────────────────────────────────

export interface LineItem {
  id: string;
  description: string | null;
  quantity: number | null;
  unitPrice: number | null;
  amount: number | null;
  confidence: number;
}

export interface ExtractionResult {
  id: string;
  vendorName: string | null;
  invoiceNumber: string | null;
  invoiceDate: string | null;
  dueDate: string | null;
  totalAmount: number | null;
  subTotal: number | null;
  taxAmount: number | null;
  currency: string | null;
  hasLowConfidenceFields: boolean;
  isManuallyCorrected: boolean;
  extractedAt: string;
  confidenceScores: Record<string, number>;
  lineItems: LineItem[];
}

export interface ApprovalStep {
  id: string;
  stepNumber: number;
  requiredRole: UserRole;
  assignedToUserId: string | null;
  status: ApprovalStepStatus;
  comment: string | null;
  decidedAt: string | null;
}

export interface ApprovalWorkflow {
  id: string;
  currentStepNumber: number;
  totalSteps: number;
  isCompleted: boolean;
  completedAt: string | null;
  steps: ApprovalStep[];
}

export interface InvoiceDetail {
  id: string;
  originalFileName: string;
  blobPath: string;
  fileSizeBytes: number;
  status: InvoiceStatus;
  vendorId: string;
  vendorName: string | null;
  uploadedAt: string;
  duplicateCheckHash: string | null;
  extractionResult: ExtractionResult | null;
  approvalWorkflow: ApprovalWorkflow | null;
  /** ID of an earlier invoice with the same content hash — present after extraction. */
  duplicateOfInvoiceId: string | null;
  /** File name of the duplicate invoice, for display. */
  duplicateOfFileName: string | null;
}

// ── Stats ────────────────────────────────────────────────────────────────────

export interface InvoiceStats {
  total: number;
  uploaded: number;
  extracting: number;
  extracted: number;
  pendingApproval: number;
  inApproval: number;
  approved: number;
  rejected: number;
  approvedTotalValue: number;
}

// ── Download ─────────────────────────────────────────────────────────────────

export interface InvoiceDownloadUrl {
  invoiceId: string;
  downloadUrl: string;
  expiresAt: string;
}

// ── Audit ────────────────────────────────────────────────────────────────────

export interface AuditLog {
  id: string;
  action: string;
  userId: string | null;
  oldValue: string | null;
  newValue: string | null;
  timestamp: string;
}

// ── Command Responses ────────────────────────────────────────────────────────

export interface UploadInvoiceResponse {
  invoiceId: string;
  blobPath: string;
  status: InvoiceStatus;
}

export interface SubmitForApprovalResponse {
  invoiceId: string;
  workflowId: string;
  totalSteps: number;
  status: InvoiceStatus;
}

export interface ApproveInvoiceResponse {
  invoiceId: string;
  status: InvoiceStatus;
  workflowComplete: boolean;
  currentStep: number;
  totalSteps: number;
}

export interface RejectInvoiceResponse {
  invoiceId: string;
  status: InvoiceStatus;
}

export interface CorrectExtractionResponse {
  invoiceId: string;
  status: InvoiceStatus;
}

// ── Command Requests ─────────────────────────────────────────────────────────

export interface CorrectExtractionRequest {
  vendorName?: string | null;
  invoiceNumber?: string | null;
  invoiceDate?: string | null;
  dueDate?: string | null;
  totalAmount?: number | null;
  subTotal?: number | null;
  taxAmount?: number | null;
  currency?: string | null;
}
