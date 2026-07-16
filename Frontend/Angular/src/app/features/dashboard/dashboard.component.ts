import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TranslateModule, MatCardModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      @for (card of statCards; track card.titleKey) {
        <mat-card class="stat-card">
          <mat-card-content class="flex items-center gap-4 p-4">
            <div class="stat-icon rounded-full p-3 text-white" [style.background]="card.color">
              <mat-icon>{{ card.icon }}</mat-icon>
            </div>
            <div>
              <p class="text-sm text-gray-500">{{ card.titleKey | translate }}</p>
              <p class="text-2xl font-bold">{{ card.value }}</p>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
})
export class DashboardComponent {
  readonly statCards = [
    { titleKey: 'navigation.departments', icon: 'account_tree', value: '—', color: '#3f51b5' },
    { titleKey: 'navigation.procurement', icon: 'shopping_cart', value: '—', color: '#00897b' },
    { titleKey: 'navigation.storeHouse', icon: 'warehouse', value: '—', color: '#e65100' },
    { titleKey: 'navigation.issueReceipt', icon: 'swap_horiz', value: '—', color: '#7b1fa2' },
  ];
}
