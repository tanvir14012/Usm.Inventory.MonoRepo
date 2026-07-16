import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/http/api.service';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { QueryParams } from '../../../shared/models/query-params.model';
import { HttpCacheService } from '../../../core/services/http-cache.service';

export interface DepartmentDto {
  id: string;
  nameEn: string;
  nameAr: string;
  code: string;
  parentId: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateDepartmentCommand {
  nameEn: string;
  nameAr: string;
  code: string;
  parentId?: string | null;
}

export interface UpdateDepartmentCommand extends CreateDepartmentCommand {
  id: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentsService {
  private readonly api = inject(ApiService);
  private readonly cache = inject(HttpCacheService);
  private readonly path = 'administration/departments';

  getAll(query: QueryParams): Observable<PagedResult<DepartmentDto>> {
    return this.api.getPaged<DepartmentDto>(this.path, query);
  }

  getById(id: string): Observable<DepartmentDto> {
    return this.api.get<DepartmentDto>(`${this.path}/${id}`);
  }

  create(command: CreateDepartmentCommand): Observable<DepartmentDto> {
    return this.api.post<DepartmentDto>(this.path, command);
  }

  update(command: UpdateDepartmentCommand): Observable<DepartmentDto> {
    return this.api.put<DepartmentDto>(`${this.path}/${command.id}`, command);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`${this.path}/${id}`);
  }
}
