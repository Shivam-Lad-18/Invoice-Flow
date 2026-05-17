import { Pipe, PipeTransform } from '@angular/core';
import { InvoiceStatus } from '../../core/models/invoice.models';
import { VendorStatus } from '../../core/models/vendor.models';

const INVOICE_LABELS: Record<InvoiceStatus, string> = {
  [InvoiceStatus.Uploaded]: 'Uploaded',
  [InvoiceStatus.Extracting]: 'Extracting',
  [InvoiceStatus.Extracted]: 'Extracted',
  [InvoiceStatus.PendingApproval]: 'Pending Approval',
  [InvoiceStatus.InApproval]: 'In Approval',
  [InvoiceStatus.Approved]: 'Approved',
  [InvoiceStatus.Rejected]: 'Rejected',
};

const VENDOR_LABELS: Record<VendorStatus, string> = {
  [VendorStatus.Active]: 'Active',
  [VendorStatus.Whitelisted]: 'Whitelisted',
  [VendorStatus.Blacklisted]: 'Blacklisted',
};

@Pipe({ name: 'statusLabel', standalone: true })
export class StatusLabelPipe implements PipeTransform {
  transform(value: InvoiceStatus | VendorStatus, type: 'invoice' | 'vendor' = 'invoice'): string {
    if (type === 'vendor') return VENDOR_LABELS[value as VendorStatus] ?? String(value);
    return INVOICE_LABELS[value as InvoiceStatus] ?? String(value);
  }
}
