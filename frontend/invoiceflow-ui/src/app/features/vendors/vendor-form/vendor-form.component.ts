import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { VendorService } from '../../../core/services/vendor.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-vendor-form',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './vendor-form.component.html',
  styleUrl: './vendor-form.component.scss',
})
export class VendorFormComponent {
  private readonly vendorService = inject(VendorService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);

  readonly form = this.fb.group({
    name:  ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    taxId: [''],
  });

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const val = this.form.getRawValue();
    this.vendorService.create({ name: val.name!, email: val.email!, taxId: val.taxId || null }).subscribe({
      next: res => {
        this.notify.success(`Vendor "${res.name}" created.`);
        this.router.navigate(['/vendors', res.vendorId]);
      },
      error: err => {
        this.notify.error(err.error?.message ?? 'Failed to create vendor.');
        this.saving.set(false);
      },
    });
  }
}
