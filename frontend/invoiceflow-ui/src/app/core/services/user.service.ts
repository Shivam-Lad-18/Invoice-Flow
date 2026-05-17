import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateUserRequest, CreateUserResponse,
  GetUsersParams, GetUsersResponse,
} from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/users`;

  getAll(params?: GetUsersParams): Observable<GetUsersResponse> {
    let p = new HttpParams();
    if (params?.page) p = p.set('page', params.page);
    if (params?.pageSize) p = p.set('pageSize', params.pageSize);
    if (params?.role != null) p = p.set('role', params.role);
    if (params?.search) p = p.set('search', params.search);
    return this.http.get<GetUsersResponse>(this.base, { params: p });
  }

  create(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(this.base, request);
  }
}
