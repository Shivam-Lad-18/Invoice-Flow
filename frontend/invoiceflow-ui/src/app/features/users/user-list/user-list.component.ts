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
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { UserService } from '../../../core/services/user.service';
import { UserListItem, UserRole, USER_ROLE_LABELS } from '../../../core/models/user.models';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    RouterLink, DatePipe, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatPaginatorModule,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatSelectModule, MatInputModule, MatProgressBarModule,
    MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserService);

  readonly users = signal<UserListItem[]>([]);
  readonly loading = signal(false);
  readonly totalCount = signal(0);
  readonly pageSize = signal(20);
  readonly pageIndex = signal(0);

  readonly searchControl = new FormControl('');
  readonly roleControl = new FormControl<UserRole | null>(null);

  readonly displayedColumns = ['name', 'email', 'role', 'vendor', 'status', 'created', 'actions'];

  readonly roleOptions: { label: string; value: UserRole | null }[] = [
    { label: 'All Roles', value: null },
    ...Object.entries(USER_ROLE_LABELS).map(([k, v]) => ({ label: v, value: Number(k) as UserRole })),
  ];

  readonly UserRole = UserRole;
  readonly USER_ROLE_LABELS = USER_ROLE_LABELS;

  ngOnInit(): void {
    this.loadUsers();

    this.searchControl.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => { this.pageIndex.set(0); this.loadUsers(); });

    this.roleControl.valueChanges
      .subscribe(() => { this.pageIndex.set(0); this.loadUsers(); });
  }

  loadUsers(): void {
    this.loading.set(true);
    this.userService.getAll({
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      role: this.roleControl.value,
      search: this.searchControl.value ?? undefined,
    }).subscribe({
      next: res => {
        this.users.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPage(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.loadUsers();
  }

  roleBadgeClass(role: UserRole): string {
    const map: Record<UserRole, string> = {
      [UserRole.Vendor]: 'badge--purple',
      [UserRole.Employee]: 'badge--blue',
      [UserRole.Manager]: 'badge--orange',
      [UserRole.FinanceHead]: 'badge--amber',
      [UserRole.CFO]: 'badge--deep-orange',
      [UserRole.Admin]: 'badge--red',
    };
    return map[role] ?? 'badge--blue';
  }

  getRoleLabel(role: UserRole): string {
    return USER_ROLE_LABELS[role] ?? String(role);
  }
}
