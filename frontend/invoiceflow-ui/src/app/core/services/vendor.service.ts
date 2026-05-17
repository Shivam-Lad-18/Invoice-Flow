import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateVendorRequest,
  CreateVendorResponse,
  GetVendorsParams,
  GetVendorsResponse,
  UpdateVendorRequest,
  VendorDetail,
  VendorStatus,
} from '../models/vendor.models';

@Injectable({ providedIn: 'root' })
export class VendorService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/vendors`;

  getAll(params: GetVendorsParams = {}): Observable<GetVendorsResponse> {
    let p = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 50));
    if (params.status != null) p = p.set('status', String(params.status));
    if (params.search) p = p.set('search', params.search);
    return this.http.get<GetVendorsResponse>(this.base, { params: p });
  }

  getById(id: string): Observable<VendorDetail> {
    return this.http.get<VendorDetail>(`${this.base}/${id}`);
  }

  create(data: CreateVendorRequest): Observable<CreateVendorResponse> {
    return this.http.post<CreateVendorResponse>(this.base, data);
  }

  update(id: string, data: UpdateVendorRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, data);
  }

  setStatus(id: string, status: VendorStatus): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, { status });
  }
}
