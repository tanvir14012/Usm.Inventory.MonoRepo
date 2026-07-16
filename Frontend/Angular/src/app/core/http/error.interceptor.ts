import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notify = inject(NotificationService);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      switch (error.status) {
        case 401:
          auth.login();
          break;
        case 403:
          router.navigate(['/forbidden']);
          break;
        case 404:
          // Let caller handle 404 locally; optionally navigate
          break;
        case 422:
        case 400:
          // Validation errors – returned to caller for form binding
          break;
        case 500:
        case 503:
          notify.error('common.error');
          break;
        default:
          if (!navigator.onLine) {
            notify.error('Network offline. Please check your connection.');
          }
      }
      return throwError(() => error);
    }),
  );
};
