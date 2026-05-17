// ── Enums (match backend UserRole) ───────────────────────────────────────────

export enum UserRole {
  Vendor = 0,
  Employee = 1,
  Manager = 2,
  FinanceHead = 3,
  CFO = 4,
  Admin = 5,
}

export const USER_ROLE_LABELS: Record<UserRole, string> = {
  [UserRole.Vendor]: 'Vendor',
  [UserRole.Employee]: 'Employee',
  [UserRole.Manager]: 'Manager',
  [UserRole.FinanceHead]: 'Finance Head',
  [UserRole.CFO]: 'CFO',
  [UserRole.Admin]: 'Admin',
};

// ── DTOs ──────────────────────────────────────────────────────────────────────

export interface UserListItem {
  userId: string;
  email: string;
  fullName: string | null;
  role: UserRole;
  vendorId: string | null;
  vendorName: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface GetUsersResponse {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  role?: UserRole | null;
  search?: string | null;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string | null;
  lastName: string | null;
  role: UserRole;
  vendorId: string | null;
}

export interface CreateUserResponse {
  userId: string;
  email: string;
  role: UserRole;
  vendorId: string | null;
}
