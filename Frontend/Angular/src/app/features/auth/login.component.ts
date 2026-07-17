import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

interface PublicKeyCredentialRequestOptionsJson {
  challenge: string;
  timeout?: number;
  rpId?: string;
  allowCredentials?: Array<{
    type: PublicKeyCredentialType;
    id: string;
    transports?: AuthenticatorTransport[];
  }>;
  userVerification?: UserVerificationRequirement;
  extensions?: AuthenticationExtensionsClientInputs;
}

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
    MatTabsModule,
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
            <mat-tab-group>
              <mat-tab [label]="'auth.passwordLogin' | translate">
                <form [formGroup]="form" class="space-y-4 mt-4" (ngSubmit)="onPasswordSignIn()">
                  <mat-form-field class="w-full">
                    <mat-label>{{ 'auth.username' | translate }}</mat-label>
                    <input matInput formControlName="username" />
                  </mat-form-field>

                  <mat-form-field class="w-full">
                    <mat-label>{{ 'auth.password' | translate }}</mat-label>
                    <input matInput type="password" formControlName="password" />
                  </mat-form-field>

                  <button mat-flat-button color="primary" class="w-full mt-2" type="submit" [disabled]="isBusy()">
                    <mat-icon>login</mat-icon>
                    {{ 'auth.signIn' | translate }}
                  </button>
                </form>
              </mat-tab>

              <mat-tab [label]="'auth.cac' | translate">
                <div class="mt-4">
                  <button mat-flat-button color="primary" class="w-full mt-2" type="button" (click)="onCacSignIn()" [disabled]="isBusy()">
                    <mat-icon>credit_card</mat-icon>
                    {{ 'auth.signIn' | translate }}
                  </button>
                </div>
              </mat-tab>

              <mat-tab [label]="'auth.fido2' | translate">
                <div class="mt-4">
                  <button mat-flat-button color="primary" class="w-full mt-2" type="button" (click)="onFido2SignIn()" [disabled]="isBusy()">
                    <mat-icon>fingerprint</mat-icon>
                    {{ 'auth.signIn' | translate }}
                  </button>
                </div>
              </mat-tab>
            </mat-tab-group>

            <p class="mt-4 text-red-700" *ngIf="error()">{{ error() }}</p>
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
  readonly isBusy = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    if (this.auth.isAuthenticated()) {
      this.router.navigateByUrl('/');
    }
  }

  async onPasswordSignIn(): Promise<void> {
    if (this.form.invalid || this.isBusy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.isBusy.set(true);

    try {
      const { username, password } = this.form.getRawValue();
      await this.auth.loginWithPassword(username, password);
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('Password login failed', error);
      this.error.set('Password login failed.');
    } finally {
      this.isBusy.set(false);
    }
  }

  async onCacSignIn(): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    this.error.set(null);
    this.isBusy.set(true);

    try {
      await this.auth.loginWithCac();
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('CAC login failed', error);
      this.error.set('CAC login failed. Verify your smart card and certificate trust.');
    } finally {
      this.isBusy.set(false);
    }
  }

  async onFido2SignIn(): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    this.error.set(null);
    this.isBusy.set(true);

    try {
      if (!window.PublicKeyCredential || !navigator.credentials) {
        throw new Error('WebAuthn is not available in this browser.');
      }

      const assertionOptionsJson = await this.auth.beginFido2Login();
      const assertionOptions = JSON.parse(assertionOptionsJson) as PublicKeyCredentialRequestOptionsJson;
      const credential = await navigator.credentials.get({
        publicKey: this.toAssertionOptions(assertionOptions),
      });

      if (!(credential instanceof PublicKeyCredential)) {
        throw new Error('Security key assertion was not returned.');
      }

      const assertionResponse = this.serializeAssertion(credential);
      await this.auth.loginWithFido2(assertionResponse, assertionOptionsJson);
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('FIDO2 login failed', error);
      this.error.set('FIDO2 login failed. Verify your security key and try again.');
    } finally {
      this.isBusy.set(false);
    }
  }

  private toAssertionOptions(json: PublicKeyCredentialRequestOptionsJson): PublicKeyCredentialRequestOptions {
    return {
      challenge: this.base64UrlToBuffer(json.challenge),
      timeout: json.timeout,
      rpId: json.rpId,
      allowCredentials: json.allowCredentials?.map(item => ({
        type: item.type,
        id: this.base64UrlToBuffer(item.id),
        transports: item.transports,
      })),
      userVerification: json.userVerification,
      extensions: json.extensions,
    };
  }

  private serializeAssertion(credential: PublicKeyCredential): unknown {
    const response = credential.response as AuthenticatorAssertionResponse;
    return {
      id: credential.id,
      type: credential.type,
      rawId: this.bufferToBase64Url(credential.rawId),
      response: {
        authenticatorData: this.bufferToBase64Url(response.authenticatorData),
        clientDataJson: this.bufferToBase64Url(response.clientDataJSON),
        signature: this.bufferToBase64Url(response.signature),
        userHandle: response.userHandle ? this.bufferToBase64Url(response.userHandle) : null,
      },
      clientExtensionResults: credential.getClientExtensionResults(),
    };
  }

  private base64UrlToBuffer(value: string): ArrayBuffer {
    const padding = '='.repeat((4 - (value.length % 4)) % 4);
    const base64 = (value + padding).replace(/-/g, '+').replace(/_/g, '/');
    const binary = window.atob(base64);
    const bytes = new Uint8Array(binary.length);

    for (let index = 0; index < binary.length; index += 1) {
      bytes[index] = binary.charCodeAt(index);
    }

    return bytes.buffer;
  }

  private bufferToBase64Url(input: ArrayBuffer): string {
    const bytes = new Uint8Array(input);
    let binary = '';

    for (const byte of bytes) {
      binary += String.fromCharCode(byte);
    }

    return window.btoa(binary)
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/g, '');
  }
}
