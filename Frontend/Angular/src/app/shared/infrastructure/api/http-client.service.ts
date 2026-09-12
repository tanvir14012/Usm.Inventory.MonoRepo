import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IAPIResponse } from '../../core/adapter/api-adapter.class';

/**
 * Standard HTTP options interface aligned with Angular's HttpClient signatures.
 */
export interface HttpMethodOptions {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  context?: HttpContext;
  observe?: 'body';
  params?: HttpParams | { [param: string]: string | number | boolean | ReadonlyArray<string | number | boolean> };
  reportProgress?: boolean;
  responseType?: 'json';
  withCredentials?: boolean;
}

/**
 * HTTP Client service for API communication
 */
@Injectable({ providedIn: 'root' })
export class APIClientService {
  constructor(private http: HttpClient) {}

  get<T>(url: string, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.get<IAPIResponse<T>>(url, options);
  }

  post<T>(url: string, body: unknown, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.post<IAPIResponse<T>>(url, body, options);
  }

  put<T>(url: string, body: unknown, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.put<IAPIResponse<T>>(url, body, options);
  }

  patch<T>(url: string, body: unknown, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.patch<IAPIResponse<T>>(url, body, options);
  }

  delete<T>(url: string, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.delete<IAPIResponse<T>>(url, options);
  }

  head<T>(url: string, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.head<IAPIResponse<T>>(url, options);
  }

  options<T>(url: string, options?: HttpMethodOptions): Observable<IAPIResponse<T>> {
    return this.http.options<IAPIResponse<T>>(url, options);
  }
}