import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
        data: { breadcrumb: 'navigation.dashboard' },
      },
      {
        path: 'administration',
        loadChildren: () => import('./features/administration/administration.routes').then(m => m.administrationRoutes),
        data: { breadcrumb: 'navigation.administration' },
      },
      {
        path: 'iam',
        loadChildren: () => import('./features/iam/iam.routes').then(m => m.iamRoutes),
        data: { breadcrumb: 'navigation.iam' },
      },
    ],
  },
  // Auth callback (handled by angular-oauth2-oidc)
  { path: 'callback', redirectTo: '/' },
  { path: 'logout', redirectTo: '/' },
  {
    path: 'forbidden',
    loadComponent: () => import('./shared/components/empty-state/empty-state.component').then(m => m.EmptyStateComponent),
  },
  { path: '**', redirectTo: '/' },
];
