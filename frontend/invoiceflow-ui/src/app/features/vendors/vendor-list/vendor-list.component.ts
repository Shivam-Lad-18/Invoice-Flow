import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { VendorService } from '../../../core/services/vendor.service';
import { AuthService } from '../../../core/services/auth.service';
import { VendorDto, VendorStatus } from '../../../core/models/vendor.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-vendor-list',
  standalone: true,
  imports: [
    RouterLink, DatePipe, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatPaginatorModule, MatButtonModule,
    MatIconModule, MatFormFieldModule, MatSelectModule, MatInputModule,
    MatProgressBarModule, MatTooltipModule, StatusBadgeComponent,
  ],
  templateUrl: './vendor-list.component.html',
  styleUrl: './vendor-list.component.scss',
})
export class VendorListComponent implements OnInit {
  private readonly vendorService = inject(VendorService);
  readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly vendors = signal<VendorDto[]>([]);
  readonly totalCount = signal(0);

  page = 1;
  pageSize = 20;
  searchTerm = '';
  selectedStatus: VendorStatus | null = null;

  readonly searchControl = new FormControl('');
  readonly displayedColumns = ['name', 'email', 'taxId', 'status', 'invoices', 'registered', 'actions'];

  readonly statusOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Active', value: VendorStatus.Active },
    { label: 'Whitelisted', value: VendorStatus.Whitelisted },
    { label: 'Blacklisted', value: VendorStatus.Blacklisted },
  ];

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(val => {
        this.searchTerm = val ?? '';
        this.page = 1;
        this.load();
      });
  }

  load(): void {
    this.loading.set(true);
    this.vendorService
      .getAll({ page: this.page, pageSize: this.pageSize, search: this.searchTerm || null, status: this.selectedStatus })
      .subscribe({
        next: res => {
          this.vendors.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onStatusChange(status: VendorStatus | null): void {
    this.selectedStatus = status;
    this.page = 1;
    this.load();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }
}
