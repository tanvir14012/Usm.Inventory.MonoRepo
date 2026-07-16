import { Injectable, signal } from '@angular/core';
import { HttpResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface CacheEntry {
  response: HttpResponse<unknown>;
  expiresAt: number;
}

@Injectable({ providedIn: 'root' })
export class HttpCacheService {
  private readonly cache = new Map<string, CacheEntry>();
  private readonly ttl = environment.cacheTtlMs;

  get(url: string): HttpResponse<unknown> | null {
    const entry = this.cache.get(url);
    if (!entry) return null;
    if (Date.now() > entry.expiresAt) {
      this.cache.delete(url);
      return null;
    }
    return entry.response;
  }

  set(url: string, response: HttpResponse<unknown>): void {
    this.cache.set(url, { response, expiresAt: Date.now() + this.ttl });
  }

  invalidate(urlPattern?: string): void {
    if (!urlPattern) {
      this.cache.clear();
      return;
    }
    for (const key of this.cache.keys()) {
      if (key.includes(urlPattern)) this.cache.delete(key);
    }
  }
}
