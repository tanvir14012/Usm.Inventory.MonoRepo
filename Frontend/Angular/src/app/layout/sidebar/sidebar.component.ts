import { Component, ChangeDetectionStrategy, computed, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLinkActive } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { PermissionService } from '../../shared/services/permission.service';
import { ModuleNavigationService } from '../../features/administration/module-navigation/module-navigation.service';

interface NavItem {
  labelKey?: string;
  label?: string;
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
        @for (item of navItems(); track item.route ?? item.label ?? item.labelKey) {
          @if (!item.permission || permissions.can(item.permission)) {
            @if (!item.children?.length) {
              <a mat-list-item [routerLink]="item.route" routerLinkActive="active-link"
                 (click)="linkClicked.emit()">
                <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
                <span matListItemTitle>{{ item.labelKey ? (item.labelKey | translate) : item.label }}</span>
              </a>
            } @else {
              <mat-expansion-panel class="nav-expansion mat-elevation-z0">
                <mat-expansion-panel-header>
                  <mat-panel-title class="flex items-center gap-2">
                    <mat-icon>{{ item.icon }}</mat-icon>
                    {{ item.labelKey ? (item.labelKey | translate) : item.label }}
                  </mat-panel-title>
                </mat-expansion-panel-header>
                @for (child of item.children; track child.route ?? child.label ?? child.labelKey) {
                  @if (!child.permission || permissions.can(child.permission)) {
                    <a mat-list-item [routerLink]="child.route" routerLinkActive="active-link"
                       class="pl-8" (click)="linkClicked.emit()">
                      <mat-icon matListItemIcon>{{ child.icon }}</mat-icon>
                      <span matListItemTitle>{{ child.labelKey ? (child.labelKey | translate) : child.label }}</span>
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
  private readonly navigationService = inject(ModuleNavigationService);
  private readonly militaryModules = toSignal(this.navigationService.loadMilitaryModules(1), { initialValue: [] });

  readonly navItems = computed<NavItem[]>(() => [
    { labelKey: 'navigation.dashboard', icon: 'dashboard', route: '/dashboard' },
    {
      labelKey: 'navigation.administration', icon: 'business',
      children: [
        { labelKey: 'navigation.moduleNavigation', icon: 'menu_open', route: '/administration/module-navigation' },
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
    ...this.militaryModules()
      .filter(module => module.isActive && !['dashboard', 'administration'].includes(module.menuId))
      .map(module => ({
        label: module.localizedName,
        icon: module.materialIconName,
        children: module.sidebarItems.length
          ? module.sidebarItems
              .filter(item => item.isActive)
              .map(item => ({
                label: item.localizedName,
                icon: item.materialIconName,
                route: `/operations/${module.menuId}/${item.menuId}`,
              }))
          : [
              {
                label: `${module.localizedName} Overview`,
                icon: module.materialIconName,
                route: `/operations/${module.menuId}`,
              },
            ],
      })),
  ]);
}
