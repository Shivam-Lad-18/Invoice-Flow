// Shared TypeScript models matching the backend domain/DTOs

export type InvoiceStatus =
  | 'Uploaded'
  | 'Extracting'
  | 'Extracted'
  | 'PendingApproval'
  | 'InApproval'
  | 'Approved'
  | 'Rejected';

export type ApprovalStepStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Delegated'
  | 'Skipped';

export type UserRole =
  | 'Vendor'
  | 'Employee'
  | 'Manager'
  | 'FinanceHead'
  | 'CFO'
  | 'Admin';

export type VendorStatus = 'Active' | 'Whitelisted' | 'Blacklisted';

export interface InvoiceListItem {
  id: string;
  originalFileName: string;
  status: InvoiceStatus;
  vendorName: string;
  uploadedAt: string;
  totalAmount?: number;
  currency?: string;
}

export interface InvoiceDetail extends InvoiceListItem {
  blobPath: string;
  fileSizeBytes: number;
  uploadedByUserId: string;
  extractionResult?: ExtractionResultDto;
  approvalWorkflow?: ApprovalWorkflowDto;
}

export interface ExtractionResultDto {
  id: string;
  vendorName?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  dueDate?: string;
  totalAmount?: number;
  subTotal?: number;
  taxAmount?: number;
  currency?: string;
  confidenceScores: Record<string, number>;
  hasLowConfidenceFields: boolean;
  isManuallyCorrected: boolean;
  extractedAt: string;
  lineItems: LineItemDto[];
}

export interface LineItemDto {
  description?: string;
  quantity?: number;
  unitPrice?: number;
  amount?: number;
  confidence: number;
}

export interface ApprovalWorkflowDto {
  id: string;
  currentStepNumber: number;
  totalSteps: number;
  completedAt?: string;
  steps: ApprovalStepDto[];
}

export interface ApprovalStepDto {
  id: string;
  stepNumber: number;
  requiredRole: UserRole;
  assignedToUserId?: string;
  assignedToName?: string;
  status: ApprovalStepStatus;
  comment?: string;
  decidedAt?: string;
}

export interface VendorDto {
  id: string;
  name: string;
  email: string;
  taxId?: string;
  status: VendorStatus;
  registeredAt: string;
}

export interface NotificationPayload {
  type: 'ExtractionComplete' | 'ApprovalRequired' | 'StepDecided' | 'Escalation';
  title: string;
  body: string;
  invoiceId?: string;
}
