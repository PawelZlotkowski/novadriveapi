import type { ReactNode } from 'react';

export type NavId = 'fleet' | 'vehicles' | 'rides' | 'maintenance' | 'tickets' | 'telemetry' | 'users' | 'payments' | 'diagnostics' | 'discountCodes';

export interface NavItem {
  id: NavId;
  label: string;
  icon: (color: string, size?: number) => ReactNode;
}
