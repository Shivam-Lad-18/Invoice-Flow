import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { UserService } from '../../../core/services/user.service';
import { VendorService } from '../../../core/services/vendor.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UserRole, USER_ROLE_LABELS } from '../../../core/models/user.models';
import { VendorDto } from '../../../core/models/vendor.models';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressBarModule, MatDividerModule,
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
})
export class UserFormComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly vendorService = inject(VendorService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly showPassword = signal(false);
  readonly vendors = signal<VendorDto[]>([]);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: [''],
    lastName: [''],
    role: [UserRole.Employee as UserRole, Validators.required],
    vendorId: [''],
  });

  readonly roleOptions = Object.entries(USER_ROLE_LABELS).map(([k, v]) => ({
    value: Number(k) as UserRole,
    label: v,
  }));

  readonly UserRole = UserRole;

  get isVendorRole(): boolean {
    return this.form.get('role')?.value === UserRole.Vendor;
  }

  ngOnInit(): void {
    // Load non-blacklisted vendors for linking
    this.vendorService.getAll({ pageSize: 100 }).subscribe({
      next: res => this.vendors.set(res.items.filter(v => v.status !== 2)),
    });

    // Toggle vendor required validation when role changes
    this.form.get('role')!.valueChanges.subscribe(role => {
      const vendorCtrl = this.form.get('vendorId')!;
      if (role === UserRole.Vendor) {
        vendorCtrl.setValidators(Validators.required);
      } else {
        vendorCtrl.clearValidators();
        vendorCtrl.setValue('');
      }
      vendorCtrl.updateValueAndValidity();
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.saving.set(true);

    this.userService.create({
      email: v.email!,
      password: v.password!,
      firstName: v.firstName || null,
      lastName: v.lastName || null,
      role: v.role!,
      vendorId: v.vendorId || null,
    }).subscribe({
      next: () => {
        this.notify.success('User created successfully.');
        this.router.navigate(['/users']);
      },
      error: (err) => {
        const msg = err.error?.message ?? 'Failed to create user. Please try again.';
        this.notify.error(msg);
        this.saving.set(false);
      },
    });
  }
}
