import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const administrationRoutes: Routes = [
  {
    path: 'departments',
    loadComponent: () => import('./departments/departments.component').then(m => m.DepartmentsComponent),
    canActivate: [authGuard],
    data: { breadcrumb: 'navigation.departments' },
  },
];
