import { Component, Input } from '@angular/core';
import { InvoiceStatus } from '../../../core/models/invoice.models';
import { VendorStatus } from '../../../core/models/vendor.models';
import { StatusLabelPipe } from '../../pipes/status-label.pipe';

type StatusValue = InvoiceStatus | VendorStatus;

const INVOICE_COLOR: Record<InvoiceStatus, string> = {
  [InvoiceStatus.Uploaded]: 'badge--blue',
  [InvoiceStatus.Extracting]: 'badge--purple',
  [InvoiceStatus.Extracted]: 'badge--orange',
  [InvoiceStatus.PendingApproval]: 'badge--amber',
  [InvoiceStatus.InApproval]: 'badge--deep-orange',
  [InvoiceStatus.Approved]: 'badge--green',
  [InvoiceStatus.Rejected]: 'badge--red',
};

const VENDOR_COLOR: Record<VendorStatus, string> = {
  [VendorStatus.Active]: 'badge--green',
  [VendorStatus.Whitelisted]: 'badge--blue',
  [VendorStatus.Blacklisted]: 'badge--red',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [StatusLabelPipe],
  template: `
    <span [class]="'badge ' + colorClass">
      {{ value | statusLabel: type }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
    .badge--blue        { background: #e3f2fd; color: #1565c0; }
    .badge--purple      { background: #f3e5f5; color: #6a1b9a; }
    .badge--orange      { background: #fff3e0; color: #e65100; }
    .badge--amber       { background: #fffde7; color: #f57f17; }
    .badge--deep-orange { background: #fbe9e7; color: #bf360c; }
    .badge--green       { background: #e8f5e9; color: #2e7d32; }
    .badge--red         { background: #ffebee; color: #c62828; }
  `],
})
export class StatusBadgeComponent {
  @Input({ required: true }) value!: StatusValue;
  @Input() type: 'invoice' | 'vendor' = 'invoice';

  get colorClass(): string {
    if (this.type === 'vendor') return VENDOR_COLOR[this.value as VendorStatus] ?? '';
    return INVOICE_COLOR[this.value as InvoiceStatus] ?? '';
  }
}
