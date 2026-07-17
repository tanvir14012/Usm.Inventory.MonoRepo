import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/http/api.service';

export interface SidebarMenuItemDto {
  id: string;
  parentSidebarMenuItemId: string | null;
  systemName: string;
  menuId: string;
  localizedName: string;
  displayOrder: number;
  materialIconName: string;
  isActive: boolean;
  children: SidebarMenuItemDto[];
}

export interface ModuleNavigationDto {
  id: string;
  buildingBlockType: number;
  systemName: string;
  menuId: string;
  localizedName: string;
  displayOrder: number;
  materialIconName: string;
  isActive: boolean;
  sidebarItems: SidebarMenuItemDto[];
}

export interface SidebarMenuItemInput {
  id?: string | null;
  parentSidebarMenuItemId?: string | null;
  systemName: string;
  menuId: string;
  localizedName: string;
  displayOrder: number;
  materialIconName: string;
  isActive: boolean;
  children?: SidebarMenuItemInput[];
}

export interface ModuleNavigationInput {
  id?: string | null;
  systemName: string;
  menuId: string;
  localizedName: string;
  displayOrder: number;
  materialIconName: string;
  isActive: boolean;
  sidebarItems?: SidebarMenuItemInput[];
}

export interface ImportModuleNavigationsInput {
  buildingBlockType: number;
  modules: ModuleNavigationInput[];
  useDerivedSidebarWhenEmpty?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ModuleNavigationService {
  private readonly api = inject(ApiService);
  private readonly path = 'iam/navigation/modules';

  get(buildingBlockType?: number): Observable<ModuleNavigationDto[]> {
    const params = buildingBlockType ? { buildingBlockType: `${buildingBlockType}` } : undefined;
    return this.api.get<ModuleNavigationDto[]>(this.path, params);
  }

  import(payload: ImportModuleNavigationsInput): Observable<ModuleNavigationDto[]> {
    return this.api.post<ModuleNavigationDto[]>(`${this.path}/import`, payload);
  }

  create(buildingBlockType: number, module: ModuleNavigationInput): Observable<ModuleNavigationDto> {
    return this.api.post<ModuleNavigationDto>(this.path, { buildingBlockType, module });
  }

  update(moduleId: string, module: ModuleNavigationInput): Observable<ModuleNavigationDto> {
    return this.api.put<ModuleNavigationDto>(`${this.path}/${moduleId}`, { module });
  }

  export(buildingBlockType: number): Observable<ModuleNavigationDto[]> {
    return this.api.get<ModuleNavigationDto[]>(`${this.path}/export`, { buildingBlockType: `${buildingBlockType}` });
  }
}
