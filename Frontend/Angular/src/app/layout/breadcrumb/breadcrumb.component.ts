import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { filter, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

interface Breadcrumb {
  labelKey: string;
  route?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (breadcrumbs().length > 1) {
      <nav aria-label="breadcrumb" class="mb-4">
        <ol class="flex items-center gap-1 text-sm text-gray-500">
          @for (crumb of breadcrumbs(); track $index; let last = $last) {
            @if (!last) {
              <li>
                <a [routerLink]="crumb.route" class="hover:text-primary transition-colors">
                  {{ crumb.labelKey | translate }}
                </a>
              </li>
              <li><mat-icon class="!text-sm !w-4 !h-4">chevron_right</mat-icon></li>
            } @else {
              <li class="text-gray-800 dark:text-gray-200 font-medium">
                {{ crumb.labelKey | translate }}
              </li>
            }
          }
        </ol>
      </nav>
    }
  `,
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly breadcrumbs = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(() => this._buildBreadcrumbs(this.activatedRoute.root)),
    ),
    { initialValue: [] as Breadcrumb[] },
  );

  private _buildBreadcrumbs(
    route: ActivatedRoute,
    crumbs: Breadcrumb[] = [{ labelKey: 'navigation.dashboard', route: '/' }],
  ): Breadcrumb[] {
    if (route.firstChild) {
      const data = route.firstChild.snapshot.data;
      if (data['breadcrumb']) {
        crumbs.push({
          labelKey: data['breadcrumb'] as string,
          route: route.firstChild.snapshot.pathFromRoot
            .map(r => r.url.map(u => u.path).join('/'))
            .join('/'),
        });
      }
      return this._buildBreadcrumbs(route.firstChild, crumbs);
    }
    return crumbs;
  }
}
