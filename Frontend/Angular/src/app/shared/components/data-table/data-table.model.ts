import { SortDirection } from '../../models/query-params.model';

export interface TableColumn<T = Record<string, unknown>> {
  /** Column field key matching the DTO property (camelCase). */
  key: keyof T | string;
  /** Translation key for the header label. */
  headerKey: string;
  /** Custom render function for cell values. */
  render?: (row: T) => string;
  /** Whether the column is sortable. */
  sortable?: boolean;
  /** Whether the column should be hidden at a given breakpoint. */
  hideBelow?: 'sm' | 'md' | 'lg';
  /** Pipe to apply: 'date' | 'localDate' | 'fileSize' | 'translateLookup' */
  pipe?: 'date' | 'localDate' | 'fileSize' | 'translateLookup' | 'boolean';
  /** Sticky position */
  sticky?: 'start' | 'end';
  /** Width hint (CSS value, e.g. '200px' or '15%') */
  width?: string;
}

export interface TableAction<T> {
  icon: string;
  tooltipKey: string;
  permission?: string;
  color?: 'primary' | 'accent' | 'warn';
  action: (row: T) => void;
  visible?: (row: T) => boolean;
}
