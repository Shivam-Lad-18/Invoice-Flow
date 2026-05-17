import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { VendorService } from '../../../core/services/vendor.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { VendorDetail, VendorStatus } from '../../../core/models/vendor.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-vendor-detail',
  standalone: true,
  imports: [
    RouterLink, DatePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatDividerModule,
    MatProgressBarModule, MatSelectModule, StatusBadgeComponent,
  ],
  templateUrl: './vendor-detail.component.html',
  styleUrl: './vendor-detail.component.scss',
})
export class VendorDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vendorService = inject(VendorService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  readonly auth = inject(AuthService);

  readonly vendor = signal<VendorDetail | null>(null);
  readonly loading = signal(true);
  readonly statusUpdating = signal(false);

  readonly statusOptions = [
    { label: 'Active', value: VendorStatus.Active },
    { label: 'Whitelisted', value: VendorStatus.Whitelisted },
    { label: 'Blacklisted', value: VendorStatus.Blacklisted },
  ];

  get vendorId(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit(): void {
    this.vendorService.getById(this.vendorId).subscribe({
      next: v => { this.vendor.set(v); this.loading.set(false); },
      error: () => {
        this.notify.error('Vendor not found.');
        this.router.navigate(['/vendors']);
      },
    });
  }

  onStatusChange(status: VendorStatus): void {
    const action = status === VendorStatus.Blacklisted ? 'Blacklist' : 'Set status for';
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: `${action} vendor?`,
        message: `Change status of "${this.vendor()?.name}" to ${VendorStatus[status]}?`,
        confirmLabel: 'Change Status',
        confirmColor: status === VendorStatus.Blacklisted ? 'warn' : 'primary',
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.statusUpdating.set(true);
      this.vendorService.setStatus(this.vendorId, status).subscribe({
        next: () => {
          this.notify.success('Vendor status updated.');
          this.vendor.update(v => v ? { ...v, status } : v);
          this.statusUpdating.set(false);
        },
        error: () => {
          this.notify.error('Status update failed.');
          this.statusUpdating.set(false);
        },
      });
    });
  }
}
