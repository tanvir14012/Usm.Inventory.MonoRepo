import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService, OAuthEvent } from 'angular-oauth2-oidc';
import { filter, from, map, switchMap, tap } from 'rxjs';
import { authConfig } from './auth.config';
import { UserProfile, AuthState } from './models/user-profile.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauthService = inject(OAuthService);
  private readonly router = inject(Router);

  // --- Signals ---
  private readonly _state = signal<AuthState>({
    isAuthenticated: false,
    user: null,
    accessToken: null,
    expiresAt: null,
  });

  readonly isAuthenticated = computed(() => this._state().isAuthenticated);
  readonly currentUser = computed(() => this._state().user);
  readonly accessToken = computed(() => this._state().accessToken);

  /** Initializes OIDC. Call once in app.config.ts via APP_INITIALIZER. */
  initialize(): Promise<boolean> {
    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    this.oauthService.events
      .pipe(filter((e: OAuthEvent) => ['token_received', 'token_refreshed', 'logout'].includes(e.type)))
      .subscribe(() => this._syncState());

    return this.oauthService.loadDiscoveryDocumentAndTryLogin().then(() => {
      this._syncState();
      return this.oauthService.hasValidAccessToken();
    });
  }

  login(): void {
    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
  }

  /** Returns access token string or null. Used by interceptor. */
  getAccessToken(): string | null {
    return this.oauthService.hasValidAccessToken()
      ? this.oauthService.getAccessToken()
      : null;
  }

  /** Checks whether the current user has a given permission code. */
  hasPermission(permission: string): boolean {
    const perms = this._getPermissions();
    return perms.includes(permission);
  }

  hasAnyPermission(permissions: string[]): boolean {
    const perms = this._getPermissions();
    return permissions.some(p => perms.includes(p));
  }

  hasAllPermissions(permissions: string[]): boolean {
    const perms = this._getPermissions();
    return permissions.every(p => perms.includes(p));
  }

  hasRole(role: string): boolean {
    const roles = this._getRoles();
    return roles.includes(role);
  }

  private _syncState(): void {
    const isAuthenticated = this.oauthService.hasValidAccessToken();
    const claims = this.oauthService.getIdentityClaims() as UserProfile | null;
    this._state.set({
      isAuthenticated,
      user: claims,
      accessToken: isAuthenticated ? this.oauthService.getAccessToken() : null,
      expiresAt: this.oauthService.getAccessTokenExpiration(),
    });
  }

  private _getPermissions(): string[] {
    const user = this._state().user;
    if (!user?.permissions) return [];
    return Array.isArray(user.permissions)
      ? user.permissions
      : user.permissions.split(',').map(p => p.trim());
  }

  private _getRoles(): string[] {
    const user = this._state().user;
    if (!user?.roles) return [];
    return Array.isArray(user.roles)
      ? user.roles
      : user.roles.split(',').map(r => r.trim());
  }
}
