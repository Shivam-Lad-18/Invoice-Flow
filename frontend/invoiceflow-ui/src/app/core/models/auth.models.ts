export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  userId: string;
  email: string;
  role: string;
  vendorId: string | null;
}

export interface AuthUser {
  userId: string;
  email: string;
  role: string;
  vendorId: string | null;
}

export const ROLES = {
  VENDOR: 'Vendor',
  EMPLOYEE: 'Employee',
  MANAGER: 'Manager',
  FINANCE_HEAD: 'FinanceHead',
  CFO: 'CFO',
  ADMIN: 'Admin',
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];

/** Manager, FinanceHead, CFO, Admin — can approve/reject */
export const APPROVER_ROLES: Role[] = [ROLES.MANAGER, ROLES.FINANCE_HEAD, ROLES.CFO, ROLES.ADMIN];

/** Employee and above — can submit/correct extractions (excludes Vendor) */
export const STAFF_ROLES: Role[] = [ROLES.EMPLOYEE, ROLES.MANAGER, ROLES.FINANCE_HEAD, ROLES.CFO, ROLES.ADMIN];

/** Admin only */
export const ADMIN_ROLES: Role[] = [ROLES.ADMIN];
