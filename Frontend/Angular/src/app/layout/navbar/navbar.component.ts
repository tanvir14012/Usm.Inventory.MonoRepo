import { Component, ChangeDetectionStrategy, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatToolbarModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatTooltipModule, MatBadgeModule,
    TranslateModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-toolbar color="primary" class="app-navbar shadow-sm z-10">
      <!-- Menu toggle -->
      <button mat-icon-button (click)="menuToggle.emit()" [matTooltip]="'navigation.menu' | translate">
        <mat-icon>menu</mat-icon>
      </button>

      <!-- Brand -->
      <a routerLink="/" class="flex items-center gap-2 ml-2 no-underline text-white">
        <span class="font-semibold text-lg tracking-wide">USM Inventory</span>
      </a>

      <span class="flex-1"></span>

      <!-- Language switcher -->
      <button mat-icon-button [matMenuTriggerFor]="langMenu"
              [matTooltip]="'common.language' | translate">
        <mat-icon>translate</mat-icon>
      </button>
      <mat-menu #langMenu>
        <button mat-menu-item (click)="langSvc.use('en')">
          <span>🇺🇸 English</span>
        </button>
        <button mat-menu-item (click)="langSvc.use('ar')">
          <span>🇸🇦 العربية</span>
        </button>
      </mat-menu>

      <!-- Theme toggle -->
      <button mat-icon-button (click)="themeSvc.toggleTheme()"
              [matTooltip]="(themeSvc.isDark() ? 'common.lightMode' : 'common.darkMode') | translate">
        <mat-icon>{{ themeSvc.isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
      </button>

      <!-- Color palette -->
      <button mat-icon-button [matMenuTriggerFor]="paletteMenu"
              [matTooltip]="'common.colorPalette' | translate">
        <mat-icon>palette</mat-icon>
      </button>
      <mat-menu #paletteMenu>
        @for (palette of palettes; track palette.value) {
          <button mat-menu-item (click)="themeSvc.setPalette(palette.value)">
            <span class="flex items-center gap-2">
              <span class="w-4 h-4 rounded-full" [style.background]="palette.color"></span>
              {{ palette.label }}
            </span>
          </button>
        }
      </mat-menu>

      <!-- User menu -->
      <button mat-icon-button [matMenuTriggerFor]="userMenu"
              [matTooltip]="'common.profile' | translate">
        <mat-icon>account_circle</mat-icon>
      </button>
      <mat-menu #userMenu>
        <div class="px-4 py-2 border-b border-gray-200">
          <p class="font-medium text-sm">{{ authSvc.currentUser()?.name }}</p>
          <p class="text-xs text-gray-500">{{ authSvc.currentUser()?.email }}</p>
        </div>
        <button mat-menu-item routerLink="/profile">
          <mat-icon>person</mat-icon> {{ 'common.profile' | translate }}
        </button>
        <button mat-menu-item (click)="authSvc.logout()">
          <mat-icon>logout</mat-icon> {{ 'auth.signOut' | translate }}
        </button>
      </mat-menu>
    </mat-toolbar>
  `,
  styles: [`
    .app-navbar { position: sticky; top: 0; z-index: 100; }
  `],
})
export class NavbarComponent {
  readonly menuToggle = output<void>();
  readonly authSvc = inject(AuthService);
  readonly themeSvc = inject(ThemeService);
  readonly langSvc = inject(LanguageService);

  readonly palettes = [
    { value: 'indigo' as const, label: 'Indigo', color: '#3f51b5' },
    { value: 'blue' as const, label: 'Blue', color: '#1976d2' },
    { value: 'teal' as const, label: 'Teal', color: '#00897b' },
    { value: 'purple' as const, label: 'Purple', color: '#7b1fa2' },
    { value: 'rose' as const, label: 'Rose', color: '#c2185b' },
  ];
}
