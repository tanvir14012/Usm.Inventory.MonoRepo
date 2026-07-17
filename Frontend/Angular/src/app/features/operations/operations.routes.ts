import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const operationsRoutes: Routes = [
  {
    path: ':module/:view',
    loadComponent: () => import('./operations-workspace.component').then(m => m.OperationsWorkspaceComponent),
    canActivate: [authGuard],
    data: { breadcrumb: 'navigation.operations' },
  },
  {
    path: ':module',
    loadComponent: () => import('./operations-workspace.component').then(m => m.OperationsWorkspaceComponent),
    canActivate: [authGuard],
    data: { breadcrumb: 'navigation.operations' },
  },
];
