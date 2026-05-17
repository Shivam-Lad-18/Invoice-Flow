import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApproveInvoiceResponse,
  CorrectExtractionRequest,
  CorrectExtractionResponse,
  GetInvoicesParams,
  GetInvoicesResponse,
  AuditLog,
  InvoiceDetail,
  InvoiceDownloadUrl,
  InvoiceStats,
  RejectInvoiceResponse,
  SubmitForApprovalResponse,
  UploadInvoiceResponse,
} from '../models/invoice.models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/invoices`;

  getAll(params: GetInvoicesParams = {}): Observable<GetInvoicesResponse> {
    let p = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));
    if (params.status != null) p = p.set('status', String(params.status));
    if (params.vendorId) p = p.set('vendorId', params.vendorId);
    if (params.from) p = p.set('from', params.from.toISOString());
    if (params.to) p = p.set('to', params.to.toISOString());
    return this.http.get<GetInvoicesResponse>(this.base, { params: p });
  }

  getById(id: string): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.base}/${id}`);
  }

  getStats(): Observable<InvoiceStats> {
    return this.http.get<InvoiceStats>(`${this.base}/stats`);
  }

  getDownloadUrl(id: string): Observable<InvoiceDownloadUrl> {
    return this.http.get<InvoiceDownloadUrl>(`${this.base}/${id}/download`);
  }

  getAuditLogs(id: string): Observable<AuditLog[]> {
    return this.http.get<AuditLog[]>(`${this.base}/${id}/audit`);
  }

  upload(vendorId: string, file: File): Observable<UploadInvoiceResponse> {
    const fd = new FormData();
    fd.append('file', file);
    fd.append('vendorId', vendorId);
    return this.http.post<UploadInvoiceResponse>(`${this.base}/upload`, fd);
  }

  submitForApproval(id: string): Observable<SubmitForApprovalResponse> {
    return this.http.post<SubmitForApprovalResponse>(`${this.base}/${id}/submit`, {});
  }

  approve(id: string, comment?: string): Observable<ApproveInvoiceResponse> {
    return this.http.post<ApproveInvoiceResponse>(`${this.base}/${id}/approve`, { comment: comment ?? null });
  }

  reject(id: string, reason: string): Observable<RejectInvoiceResponse> {
    return this.http.post<RejectInvoiceResponse>(`${this.base}/${id}/reject`, { reason });
  }

  correctExtraction(id: string, data: CorrectExtractionRequest): Observable<CorrectExtractionResponse> {
    return this.http.put<CorrectExtractionResponse>(`${this.base}/${id}/extraction`, data);
  }
}
