import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { OperationsService } from '../operations/operations.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isLoading()) {
      <mat-progress-bar mode="indeterminate" class="mb-4" />
    }

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      @for (card of statCards(); track card.titleKey) {
        <mat-card class="stat-card">
          <mat-card-content class="flex items-center gap-4 p-4">
            <div class="stat-icon rounded-full p-3 text-white" [style.background]="card.color">
              <mat-icon>{{ card.icon }}</mat-icon>
            </div>
            <div>
              <p class="text-sm text-gray-500">{{ card.titleKey }}</p>
              <p class="text-2xl font-bold">{{ card.value }}</p>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-3 gap-4">
      <mat-card>
        <mat-card-header><mat-card-title>Procurement posture</mat-card-title></mat-card-header>
        <mat-card-content class="pt-4">
          @for (order of procurementFocus(); track order.id) {
            <div class="flex items-center justify-between py-2 border-b border-slate-100">
              <div>
                <div class="font-medium">{{ order.orderNumber }}</div>
                <div class="text-xs text-gray-500">{{ order.supplierName }}</div>
              </div>
              <div class="text-sm font-medium">{{ order.status }}</div>
            </div>
          }
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header><mat-card-title>Supply pressure points</mat-card-title></mat-card-header>
        <mat-card-content class="pt-4">
          @for (item of lowStockItems(); track item.id) {
            <div class="flex items-center justify-between py-2 border-b border-slate-100">
              <div>
                <div class="font-medium">{{ item.nameEn }}</div>
                <div class="text-xs text-gray-500">{{ item.code }}</div>
              </div>
              <div class="text-sm font-medium text-amber-700">{{ item.currentQuantity }} / {{ item.reorderLevel }}</div>
            </div>
          }
        </mat-card-content>
      </mat-card>

      <mat-card>
        <mat-card-header><mat-card-title>Security & maintenance alerts</mat-card-title></mat-card-header>
        <mat-card-content class="pt-4">
          @for (alert of alerts(); track alert.id) {
            <div class="flex items-center justify-between py-2 border-b border-slate-100">
              <div>
                <div class="font-medium">{{ alert.title }}</div>
                <div class="text-xs text-gray-500">{{ alert.subtitle }}</div>
              </div>
              <div class="text-sm font-medium" [class.text-red-600]="alert.severity === 'High'" [class.text-amber-700]="alert.severity === 'Medium'">
                {{ alert.severity }}
              </div>
            </div>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class DashboardComponent {
  private readonly operationsService = inject(OperationsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(true);
  readonly procurementFocus = signal<Array<{ id: string; orderNumber: string; supplierName: string; status: string }>>([]);
  readonly lowStockItems = signal<Array<{ id: string; nameEn: string; code: string; currentQuantity: number; reorderLevel: number }>>([]);
  readonly alerts = signal<Array<{ id: string; title: string; subtitle: string; severity: 'High' | 'Medium' }>>([]);
  readonly statCards = signal([
    { titleKey: 'Approved purchase orders', icon: 'shopping_cart', value: '0', color: '#3f51b5' },
    { titleKey: 'Low stock alerts', icon: 'warehouse', value: '0', color: '#e65100' },
    { titleKey: 'Movement transactions', icon: 'swap_horiz', value: '0', color: '#7b1fa2' },
    { titleKey: 'Open repairs', icon: 'build', value: '0', color: '#00897b' },
  ]);

  constructor() {
    forkJoin({
      purchaseOrders: this.operationsService.getPurchaseOrders(),
      inventoryItems: this.operationsService.getInventoryItems(),
      transactions: this.operationsService.getTransactions(),
      repairOrders: this.operationsService.getRepairOrders(),
      safetyRecords: this.operationsService.getVehicleSafetyRecords(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        const approvedOrders = result.purchaseOrders.filter(order => order.status === 'Approved' || order.status === 'Delivered');
        const lowStock = result.inventoryItems.filter(item => item.isBelowReorderLevel);
        const openRepairs = result.repairOrders.filter(order => order.status !== 'Completed');

        this.statCards.set([
          { titleKey: 'Approved purchase orders', icon: 'shopping_cart', value: `${approvedOrders.length}`, color: '#3f51b5' },
          { titleKey: 'Low stock alerts', icon: 'warehouse', value: `${lowStock.length}`, color: '#e65100' },
          { titleKey: 'Movement transactions', icon: 'swap_horiz', value: `${result.transactions.length}`, color: '#7b1fa2' },
          { titleKey: 'Open repairs', icon: 'build', value: `${openRepairs.length}`, color: '#00897b' },
        ]);

        this.procurementFocus.set(result.purchaseOrders.slice(0, 3));
        this.lowStockItems.set(lowStock.slice(0, 3));
        this.alerts.set([
          ...result.safetyRecords
            .filter(record => record.status !== 'Passed')
            .map(record => ({
              id: record.id,
              title: record.vehicleRegistrationNumber,
              subtitle: record.remarks || 'Safety review required',
              severity: record.status === 'Failed' ? 'High' as const : 'Medium' as const,
            })),
          ...openRepairs.slice(0, 2).map(order => ({
            id: order.id,
            title: order.orderNumber,
            subtitle: order.description,
            severity: 'Medium' as const,
          })),
        ].slice(0, 5));

        this.isLoading.set(false);
      });
  }
}
