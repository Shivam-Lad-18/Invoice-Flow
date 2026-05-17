import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { InvoiceService } from '../../../core/services/invoice.service';
import { VendorService } from '../../../core/services/vendor.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { VendorDto } from '../../../core/models/vendor.models';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';

const ALLOWED_TYPES = ['application/pdf', 'image/jpeg', 'image/png', 'image/tiff'];
const MAX_SIZE = 20 * 1024 * 1024;

@Component({
  selector: 'app-invoice-upload',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressBarModule, FileSizePipe,
  ],
  templateUrl: './invoice-upload.component.html',
  styleUrl: './invoice-upload.component.scss',
})
export class InvoiceUploadComponent implements OnInit {
  private readonly invoiceService = inject(InvoiceService);
  private readonly vendorService = inject(VendorService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  readonly vendors = signal<VendorDto[]>([]);
  readonly selectedFile = signal<File | null>(null);
  readonly fileError = signal('');
  readonly uploading = signal(false);
  readonly dragOver = signal(false);

  readonly form = this.fb.group({
    vendorId: ['', Validators.required],
  });

  ngOnInit(): void {
    if (this.auth.isVendor()) {
      // Vendor users: auto-fill their own VendorId — no dropdown shown
      const vendorId = this.auth.currentVendorId();
      if (vendorId) {
        this.form.patchValue({ vendorId });
      }
    } else {
      // Staff/Admin: load vendor list for selection (exclude blacklisted)
      this.vendorService.getAll({ pageSize: 100 }).subscribe({
        next: res => this.vendors.set(res.items.filter(v => v.status !== 2)),
      });
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void {
    this.dragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.setFile(file);
  }

  private setFile(file: File): void {
    this.fileError.set('');
    if (!ALLOWED_TYPES.includes(file.type)) {
      this.fileError.set('Unsupported file type. Allowed: PDF, JPEG, PNG, TIFF.');
      return;
    }
    if (file.size > MAX_SIZE) {
      this.fileError.set('File exceeds the 20 MB maximum.');
      return;
    }
    this.selectedFile.set(file);
  }

  clearFile(): void {
    this.selectedFile.set(null);
    this.fileError.set('');
  }

  onSubmit(): void {
    if (this.form.invalid || !this.selectedFile()) {
      this.form.markAllAsTouched();
      if (!this.selectedFile()) this.fileError.set('Please select a file.');
      return;
    }
    this.uploading.set(true);
    const { vendorId } = this.form.getRawValue();

    this.invoiceService.upload(vendorId!, this.selectedFile()!).subscribe({
      next: res => {
        this.notify.success('Invoice uploaded and queued for AI extraction.');
        this.router.navigate(['/invoices', res.invoiceId]);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Upload failed. Please try again.';
        this.notify.error(msg);
        this.uploading.set(false);
      },
    });
  }
}
