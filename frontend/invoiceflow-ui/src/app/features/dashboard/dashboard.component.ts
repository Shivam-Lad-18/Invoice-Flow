import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { InvoiceService } from '../../core/services/invoice.service';
import { AuthService } from '../../core/services/auth.service';
import { InvoiceListItem, InvoiceStats, InvoiceStatus } from '../../core/models/invoice.models';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { FileSizePipe } from '../../shared/pipes/file-size.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink, DatePipe, DecimalPipe,
    MatCardModule, MatIconModule, MatButtonModule,
    MatProgressBarModule, MatTableModule, MatChipsModule,
    StatusBadgeComponent, FileSizePipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly invoiceService = inject(InvoiceService);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly stats = signal<InvoiceStats | null>(null);
  readonly recentInvoices = signal<InvoiceListItem[]>([]);

  readonly recentCols = ['file', 'vendor', 'status', 'amount', 'date'];

  ngOnInit(): void {
    this.invoiceService.getStats().subscribe({
      next: stats => this.stats.set(stats),
    });
    this.invoiceService.getAll({ page: 1, pageSize: 8 }).subscribe({
      next: res => {
        this.recentInvoices.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  pendingCount(): number {
    const s = this.stats();
    if (!s) return 0;
    return s.extracted + s.pendingApproval + s.inApproval;
  }

  get InvoiceStatus() { return InvoiceStatus; }
}
