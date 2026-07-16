import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { NavbarComponent } from '../navbar/navbar.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { LoadingService } from '../../core/services/loading.service';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatSidenavModule, MatToolbarModule, MatProgressBarModule,
    NavbarComponent, SidebarComponent, BreadcrumbComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading.isLoading()) {
      <mat-progress-bar mode="indeterminate" class="global-progress" />
    }
    <mat-sidenav-container class="app-sidenav-container h-screen">
      <mat-sidenav
        #sidenav
        [mode]="isMobile() ? 'over' : 'side'"
        [opened]="!isMobile() && sidenavOpen()"
        class="app-sidenav w-64">
        <app-sidebar (linkClicked)="isMobile() && sidenav.close()" />
      </mat-sidenav>

      <mat-sidenav-content class="flex flex-col">
        <app-navbar (menuToggle)="toggleSidenav(sidenav)" />
        <main class="flex-1 overflow-auto p-4 md:p-6">
          <app-breadcrumb />
          <router-outlet />
        </main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .global-progress { position: fixed; top: 0; left: 0; right: 0; z-index: 9999; }
    .app-sidenav-container { height: 100vh; }
    .app-sidenav { border-inline-end: 1px solid var(--mat-sidenav-container-divider-color); }
  `],
})
export class MainLayoutComponent {
  readonly loading = inject(LoadingService);

  private readonly breakpoints = inject(BreakpointObserver);
  readonly isMobile = toSignal(
    this.breakpoints.observe([Breakpoints.XSmall, Breakpoints.Small]).pipe(
      map(state => state.matches),
    ),
    { initialValue: false },
  );

  readonly sidenavOpen = signal(true);

  toggleSidenav(sidenav: { toggle: () => void }): void {
    if (this.isMobile()) {
      sidenav.toggle();
    } else {
      this.sidenavOpen.update(v => !v);
    }
  }
}
