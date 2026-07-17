import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="login-page min-h-screen flex items-center justify-center p-4">
      <mat-card class="login-card w-full max-w-5xl">
        <div class="grid grid-cols-1 lg:grid-cols-2">
          <div class="brand-side p-8 text-white">
            <h1 class="text-4xl font-semibold mb-3">ORDISS</h1>
            <p class="opacity-90 mb-8">{{ 'auth.loginWith' | translate }} US Military Supply Chain</p>
            <div class="mock-nav">
              <button mat-stroked-button class="mock-pill">Dashboard</button>
              <button mat-stroked-button class="mock-pill">Procurement</button>
              <button mat-stroked-button class="mock-pill">Issue & Receipt</button>
              <button mat-stroked-button class="mock-pill">Traffic & Security</button>
            </div>
          </div>

          <div class="p-8">
            <h2 class="text-2xl font-semibold mb-6">{{ 'auth.signIn' | translate }}</h2>
            <form [formGroup]="form" class="space-y-4" (ngSubmit)="onSignIn()">
              <mat-form-field class="w-full">
                <mat-label>{{ 'auth.username' | translate }}</mat-label>
                <input matInput formControlName="username" />
              </mat-form-field>

              <mat-form-field class="w-full">
                <mat-label>{{ 'auth.password' | translate }}</mat-label>
                <input matInput type="password" formControlName="password" />
              </mat-form-field>

              <button mat-flat-button color="primary" class="w-full mt-2" type="submit">
                <mat-icon>login</mat-icon>
                {{ 'auth.signIn' | translate }}
              </button>
            </form>
          </div>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-page { background: linear-gradient(135deg, #0b5b4b 0%, #0f766e 45%, #e2e8f0 100%); }
    .login-card { overflow: hidden; border-radius: 18px; }
    .brand-side { background: linear-gradient(160deg, #064e3b, #0f766e); }
    .mock-nav { display: grid; gap: 10px; }
    .mock-pill {
      justify-content: flex-start;
      color: #ecfeff !important;
      border-color: rgba(236, 254, 255, 0.45) !important;
    }
  `],
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  constructor() {
    if (this.auth.isAuthenticated()) {
      this.router.navigateByUrl('/');
    }
  }

  onSignIn(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.auth.login();
  }
}
