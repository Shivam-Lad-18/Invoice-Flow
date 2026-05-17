export enum VendorStatus {
  Active = 0,
  Whitelisted = 1,
  Blacklisted = 2,
}

export interface VendorDto {
  id: string;
  name: string;
  email: string;
  taxId: string | null;
  status: VendorStatus;
  registeredAt: string;
  invoiceCount: number;
}

export interface VendorDetail {
  id: string;
  name: string;
  email: string;
  taxId: string | null;
  status: VendorStatus;
  registeredAt: string;
  registeredByUserId: string;
  userAccountId: string | null;
  invoiceCount: number;
}

export interface GetVendorsParams {
  page?: number;
  pageSize?: number;
  status?: VendorStatus | null;
  search?: string | null;
}

export interface GetVendorsResponse {
  items: VendorDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateVendorRequest {
  name: string;
  email: string;
  taxId?: string | null;
}

export interface UpdateVendorRequest {
  name: string;
  email: string;
  taxId?: string | null;
}

export interface CreateVendorResponse {
  vendorId: string;
  name: string;
  email: string;
  status: VendorStatus;
}
