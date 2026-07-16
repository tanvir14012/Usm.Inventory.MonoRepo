import { Component, ChangeDetectionStrategy, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLinkActive } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { TranslateModule } from '@ngx-translate/core';
import { PermissionService } from '../../shared/services/permission.service';

interface NavItem {
  labelKey: string;
  icon: string;
  route?: string;
  permission?: string;
  children?: Omit<NavItem, 'children'>[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule, RouterModule, RouterLinkActive,
    MatListModule, MatIconModule, MatExpansionModule, TranslateModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="sidebar flex flex-col h-full overflow-y-auto">
      <!-- Logo area -->
      <div class="sidebar-header p-4 border-b border-gray-200 dark:border-gray-700">
        <span class="font-bold text-lg text-primary">USM Inventory</span>
      </div>

      <!-- Navigation -->
      <mat-nav-list class="flex-1 pt-2">
        @for (item of navItems; track item.labelKey) {
          @if (!item.permission || permissions.can(item.permission)) {
            @if (!item.children?.length) {
              <a mat-list-item [routerLink]="item.route" routerLinkActive="active-link"
                 (click)="linkClicked.emit()">
                <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
                <span matListItemTitle>{{ item.labelKey | translate }}</span>
              </a>
            } @else {
              <mat-expansion-panel class="nav-expansion mat-elevation-z0">
                <mat-expansion-panel-header>
                  <mat-panel-title class="flex items-center gap-2">
                    <mat-icon>{{ item.icon }}</mat-icon>
                    {{ item.labelKey | translate }}
                  </mat-panel-title>
                </mat-expansion-panel-header>
                @for (child of item.children; track child.labelKey) {
                  @if (!child.permission || permissions.can(child.permission)) {
                    <a mat-list-item [routerLink]="child.route" routerLinkActive="active-link"
                       class="pl-8" (click)="linkClicked.emit()">
                      <mat-icon matListItemIcon>{{ child.icon }}</mat-icon>
                      <span matListItemTitle>{{ child.labelKey | translate }}</span>
                    </a>
                  }
                }
              </mat-expansion-panel>
            }
          }
        }
      </mat-nav-list>
    </div>
  `,
  styles: [`
    .active-link { background: rgba(var(--mat-primary-rgb), 0.12) !important; font-weight: 600; }
    .nav-expansion { box-shadow: none !important; background: transparent !important; }
    ::ng-deep .nav-expansion .mat-expansion-panel-body { padding: 0; }
  `],
})
export class SidebarComponent {
  readonly linkClicked = output<void>();
  readonly permissions = inject(PermissionService);

  readonly navItems: NavItem[] = [
    { labelKey: 'navigation.dashboard', icon: 'dashboard', route: '/dashboard' },
    {
      labelKey: 'navigation.administration', icon: 'business',
      children: [
        { labelKey: 'navigation.departments', icon: 'account_tree', route: '/administration/departments', permission: 'departments.read' },
      ],
    },
    {
      labelKey: 'navigation.iam', icon: 'manage_accounts',
      children: [
        { labelKey: 'navigation.roles', icon: 'badge', route: '/iam/roles', permission: 'roles.read' },
        { labelKey: 'navigation.users', icon: 'people', route: '/iam/users', permission: 'users.read' },
      ],
    },
    { labelKey: 'navigation.procurement', icon: 'shopping_cart', route: '/procurement', permission: 'procurement.read' },
    { labelKey: 'navigation.storeHouse', icon: 'warehouse', route: '/store-house', permission: 'storehouse.read' },
    { labelKey: 'navigation.issueReceipt', icon: 'swap_horiz', route: '/issue-receipt', permission: 'issuedreceipts.read' },
    { labelKey: 'navigation.reporting', icon: 'bar_chart', route: '/reporting', permission: 'reporting.read' },
    { labelKey: 'navigation.settings', icon: 'settings', route: '/settings' },
  ];
}
