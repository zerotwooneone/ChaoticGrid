import { Injectable, computed, signal } from '@angular/core';

import { SystemPermission } from '../models/permissions.enum';

export interface CurrentUser {
  id: string;
  nickname: string;
  permissions: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly jwtInternal = signal<string | null>(this.getStoredJwt());

  readonly jwt = this.jwtInternal.asReadonly();

  readonly currentUser = computed<CurrentUser | null>(() => {
    const token = this.jwtInternal();
    if (!token) {
      return null;
    }

    const payload = this.tryDecodePayload(token);
    if (!payload) {
      return null;
    }

    const id = typeof payload.sub === 'string' ? payload.sub : null;
    const nickname = typeof payload.nickname === 'string' ? payload.nickname : null;
    const permsRaw = payload['x-permissions'];
    const permissions = typeof permsRaw === 'string' ? Number(permsRaw) : typeof permsRaw === 'number' ? permsRaw : NaN;

    if (!id || !nickname || Number.isNaN(permissions)) {
      return null;
    }

    return { id, nickname, permissions };
  });

  setJwt(jwt: string | null): void {
    this.jwtInternal.set(jwt);

    if (jwt) {
      localStorage.setItem('cg.jwt', jwt);
    } else {
      localStorage.removeItem('cg.jwt');
    }
  }

  hasPermission(required: SystemPermission): boolean {
    const user = this.currentUser();
    if (!user) {
      return false;
    }

    return ((user.permissions as number) & required) === required;
  }

  getAuthorizationHeaderValue(): string | null {
    const token = this.jwtInternal();
    return token ? `Bearer ${token}` : null;
  }

  private getStoredJwt(): string | null {
    try {
      return localStorage.getItem('cg.jwt');
    } catch {
      return null;
    }
  }

  private tryDecodePayload(token: string): any | null {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const payloadJson = this.base64UrlDecode(parts[1]);
      return JSON.parse(payloadJson);
    } catch {
      return null;
    }
  }

  private base64UrlDecode(value: string): string {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    return decodeURIComponent(
      atob(padded)
        .split('')
        .map(c => `%${c.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join('')
    );
  }
}
