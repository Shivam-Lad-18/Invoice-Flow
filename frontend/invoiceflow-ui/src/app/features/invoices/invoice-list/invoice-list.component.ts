import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { InvoiceService } from '../../../core/services/invoice.service';
import { VendorService } from '../../../core/services/vendor.service';
import { AuthService } from '../../../core/services/auth.service';
import { InvoiceListItem, InvoiceStatus } from '../../../core/models/invoice.models';
import { VendorDto } from '../../../core/models/vendor.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    RouterLink, DatePipe, DecimalPipe, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatPaginatorModule,
    MatFormFieldModule, MatSelectModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressBarModule, MatTooltipModule,
    StatusBadgeComponent, FileSizePipe,
  ],
  templateUrl: './invoice-list.component.html',
  styleUrl: './invoice-list.component.scss',
})
export class InvoiceListComponent implements OnInit {
  private readonly invoiceService = inject(InvoiceService);
  private readonly vendorService = inject(VendorService);
  readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly invoices = signal<InvoiceListItem[]>([]);
  readonly totalCount = signal(0);
  readonly vendors = signal<VendorDto[]>([]);

  page = 1;
  pageSize = 20;
  selectedStatus: InvoiceStatus | null = null;
  selectedVendorId: string | null = null;

  readonly displayedColumns = ['file', 'vendor', 'status', 'amount', 'size', 'date', 'actions'];

  readonly statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Uploaded', value: InvoiceStatus.Uploaded },
    { label: 'Extracting', value: InvoiceStatus.Extracting },
    { label: 'Extracted', value: InvoiceStatus.Extracted },
    { label: 'Pending Approval', value: InvoiceStatus.PendingApproval },
    { label: 'In Approval', value: InvoiceStatus.InApproval },
    { label: 'Approved', value: InvoiceStatus.Approved },
    { label: 'Rejected', value: InvoiceStatus.Rejected },
  ];

  ngOnInit(): void {
    this.loadInvoices();
    if (!this.auth.isVendor()) {
      this.vendorService.getAll({ pageSize: 100 }).subscribe({
        next: res => this.vendors.set(res.items),
      });
    }
  }

  loadInvoices(): void {
    this.loading.set(true);
    this.invoiceService
      .getAll({
        page: this.page,
        pageSize: this.pageSize,
        status: this.selectedStatus,
        vendorId: this.selectedVendorId,
      })
      .subscribe({
        next: res => {
          this.invoices.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onStatusChange(status: InvoiceStatus | null): void {
    this.selectedStatus = status;
    this.page = 1;
    this.loadInvoices();
  }

  onVendorChange(vendorId: string | null): void {
    this.selectedVendorId = vendorId;
    this.page = 1;
    this.loadInvoices();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadInvoices();
  }
}
