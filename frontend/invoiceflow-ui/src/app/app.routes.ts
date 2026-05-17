import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'invoices',
        loadComponent: () =>
          import('./features/invoices/invoice-list/invoice-list.component').then(m => m.InvoiceListComponent),
      },
      {
        path: 'invoices/upload',
        loadComponent: () =>
          import('./features/invoices/invoice-upload/invoice-upload.component').then(m => m.InvoiceUploadComponent),
      },
      {
        path: 'invoices/:id',
        loadComponent: () =>
          import('./features/invoices/invoice-detail/invoice-detail.component').then(m => m.InvoiceDetailComponent),
      },
      {
        path: 'vendors',
        loadComponent: () =>
          import('./features/vendors/vendor-list/vendor-list.component').then(m => m.VendorListComponent),
      },
      {
        path: 'vendors/new',
        loadComponent: () =>
          import('./features/vendors/vendor-form/vendor-form.component').then(m => m.VendorFormComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
      {
        path: 'vendors/:id',
        loadComponent: () =>
          import('./features/vendors/vendor-detail/vendor-detail.component').then(m => m.VendorDetailComponent),
      },
      // ── User Management (Admin only) ───────────────────────────────────
      {
        path: 'users',
        loadComponent: () =>
          import('./features/users/user-list/user-list.component').then(m => m.UserListComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
      {
        path: 'users/new',
        loadComponent: () =>
          import('./features/users/user-form/user-form.component').then(m => m.UserFormComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
