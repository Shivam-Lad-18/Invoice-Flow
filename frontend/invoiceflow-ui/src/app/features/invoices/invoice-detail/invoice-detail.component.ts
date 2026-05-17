import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { InvoiceService } from '../../../core/services/invoice.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  AuditLog, ApprovalStepStatus, InvoiceDetail,
  InvoiceStatus, LineItem, UserRole,
} from '../../../core/models/invoice.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [
    RouterLink, DatePipe, DecimalPipe, PercentPipe, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatTabsModule, MatTableModule,
    MatProgressBarModule, MatProgressSpinnerModule, MatFormFieldModule, MatInputModule,
    MatDialogModule, MatTooltipModule, MatDividerModule, MatExpansionModule,
    StatusBadgeComponent, FileSizePipe, DecimalPipe, PercentPipe,
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss',
})
export class InvoiceDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly invoiceService = inject(InvoiceService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  readonly invoice = signal<InvoiceDetail | null>(null);
  readonly auditLogs = signal<AuditLog[]>([]);
  readonly loading = signal(true);
  readonly actionLoading = signal(false);
  readonly downloadLoading = signal(false);
  readonly showCorrectionForm = signal(false);

  readonly correctionForm = this.fb.group({
    vendorName: [''],
    invoiceNumber: [''],
    invoiceDate: [''],
    dueDate: [''],
    totalAmount: [null as number | null],
    subTotal: [null as number | null],
    taxAmount: [null as number | null],
    currency: [''],
  });

  readonly rejectionReason = signal('');
  readonly approvalComment = signal('');

  // Role-based computed permissions
  readonly canSubmit = computed(() => {
    const inv = this.invoice();
    return inv?.status === InvoiceStatus.Extracted && this.auth.canSubmitCorrect();
  });

  readonly canCorrect = computed(() => {
    const inv = this.invoice();
    return (
      inv !== null &&
      (inv.status === InvoiceStatus.Extracted || inv.status === InvoiceStatus.PendingApproval) &&
      this.auth.canSubmitCorrect()
    );
  });

  readonly canApproveReject = computed(() => {
    const inv = this.invoice();
    return (
      inv !== null &&
      (inv.status === InvoiceStatus.PendingApproval || inv.status === InvoiceStatus.InApproval) &&
      this.auth.canApproveReject()
    );
  });

  readonly canViewAudit = computed(() => !this.auth.isVendor());
  readonly lineItemCols = ['description', 'qty', 'unitPrice', 'amount', 'confidence'];
  readonly auditCols = ['timestamp', 'action', 'user', 'details'];

  /**
   * The currency to display for all monetary amounts on this page.
   * Reflects the correction-form value in real time so that line items
   * and overview fields update immediately when the user types a new currency code.
   */
  private readonly _formCurrency = toSignal(
    this.correctionForm.get('currency')!.valueChanges,
    { initialValue: null }
  );

  readonly displayCurrency = computed<string>(() => {
    // Prefer live form value (even when form is collapsed — user may have changed it)
    const formVal = this._formCurrency();
    if (formVal) return formVal;
    return this.invoice()?.extractionResult?.currency ?? '';
  });

  // Expose enums to template
  readonly InvoiceStatus = InvoiceStatus;
  readonly ApprovalStepStatus = ApprovalStepStatus;
  readonly UserRole = UserRole;

  get invoiceId(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit(): void {
    this.loadInvoice();
    if (!this.auth.isVendor()) {
      this.loadAuditLogs();
    }
  }

  loadInvoice(): void {
    this.loading.set(true);
    this.invoiceService.getById(this.invoiceId).subscribe({
      next: inv => {
        this.invoice.set(inv);
        this.loading.set(false);
        // Pre-fill correction form with current extraction values
        const er = inv.extractionResult;
        if (er) {
          this.correctionForm.patchValue({
            vendorName: er.vendorName ?? '',
            invoiceNumber: er.invoiceNumber ?? '',
            invoiceDate: er.invoiceDate ? er.invoiceDate.substring(0, 10) : '',
            dueDate: er.dueDate ? er.dueDate.substring(0, 10) : '',
            totalAmount: er.totalAmount,
            subTotal: er.subTotal,
            taxAmount: er.taxAmount,
            currency: er.currency ?? '',
          });
        }
      },
      error: () => {
        this.notify.error('Invoice not found or access denied.');
        this.router.navigate(['/invoices']);
      },
    });
  }

  loadAuditLogs(): void {
    this.invoiceService.getAuditLogs(this.invoiceId).subscribe({
      next: logs => this.auditLogs.set(logs),
    });
  }

  download(): void {
    this.downloadLoading.set(true);
    this.invoiceService.getDownloadUrl(this.invoiceId).subscribe({
      next: res => {
        window.open(res.downloadUrl, '_blank', 'noopener,noreferrer');
        this.downloadLoading.set(false);
      },
      error: () => {
        this.notify.error('Could not generate download link.');
        this.downloadLoading.set(false);
      },
    });
  }

  submitForApproval(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Submit for Approval',
        message: 'Submit this invoice for the approval workflow?',
        confirmLabel: 'Submit',
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.actionLoading.set(true);
      this.invoiceService.submitForApproval(this.invoiceId).subscribe({
        next: () => {
          this.notify.success('Invoice submitted for approval.');
          this.loadInvoice();
          this.loadAuditLogs();
          this.actionLoading.set(false);
        },
        error: err => {
          this.notify.error(err.error?.message ?? 'Submit failed.');
          this.actionLoading.set(false);
        },
      });
    });
  }

  approve(): void {
    this.actionLoading.set(true);
    this.invoiceService.approve(this.invoiceId, this.approvalComment() || undefined).subscribe({
      next: () => {
        this.notify.success('Invoice approved.');
        this.approvalComment.set('');
        this.loadInvoice();
        this.loadAuditLogs();
        this.actionLoading.set(false);
      },
      error: err => {
        this.notify.error(err.error?.message ?? 'Approval failed.');
        this.actionLoading.set(false);
      },
    });
  }

  reject(): void {
    if (!this.rejectionReason().trim()) {
      this.notify.error('A rejection reason is required.');
      return;
    }
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Reject Invoice',
        message: `Reject this invoice with reason: "${this.rejectionReason()}"?`,
        confirmLabel: 'Reject',
        confirmColor: 'warn',
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.actionLoading.set(true);
      this.invoiceService.reject(this.invoiceId, this.rejectionReason()).subscribe({
        next: () => {
          this.notify.success('Invoice rejected.');
          this.rejectionReason.set('');
          this.loadInvoice();
          this.loadAuditLogs();
          this.actionLoading.set(false);
        },
        error: err => {
          this.notify.error(err.error?.message ?? 'Rejection failed.');
          this.actionLoading.set(false);
        },
      });
    });
  }

  saveCorrection(): void {
    const val = this.correctionForm.getRawValue();
    this.actionLoading.set(true);
    this.invoiceService
      .correctExtraction(this.invoiceId, {
        vendorName: val.vendorName || null,
        invoiceNumber: val.invoiceNumber || null,
        invoiceDate: val.invoiceDate || null,
        dueDate: val.dueDate || null,
        totalAmount: val.totalAmount,
        subTotal: val.subTotal,
        taxAmount: val.taxAmount,
        currency: val.currency || null,
      })
      .subscribe({
        next: () => {
          this.notify.success('Extraction corrected and saved.');
          this.showCorrectionForm.set(false);
          this.loadInvoice();
          this.actionLoading.set(false);
        },
        error: err => {
          this.notify.error(err.error?.message ?? 'Correction failed.');
          this.actionLoading.set(false);
        },
      });
  }

  toggleCorrectionForm(): void {
    this.showCorrectionForm.update(v => !v);
  }

  confidenceColor(score: number): string {
    if (score >= 0.85) return 'high';
    if (score >= 0.6) return 'medium';
    return 'low';
  }

  stepStatusLabel(status: ApprovalStepStatus): string {
    return ['Pending', 'Approved', 'Rejected', 'Delegated', 'Skipped'][status] ?? String(status);
  }

  roleLabel(role: UserRole): string {
    return ['Vendor', 'Employee', 'Manager', 'Finance Head', 'CFO', 'Admin'][role] ?? String(role);
  }
}
