import { FONT } from '@shared/tokens';

type Variant = 'green' | 'red' | 'yellow' | 'blue' | 'gray';

const VARIANTS: Record<Variant, { bg: string; color: string }> = {
  green:  { bg: '#dcfce7', color: '#16a34a' },
  red:    { bg: '#fee2e2', color: '#dc2626' },
  yellow: { bg: '#fef9c3', color: '#ca8a04' },
  blue:   { bg: '#dbeafe', color: '#1d4ed8' },
  gray:   { bg: '#f3f4f6', color: '#6b7280' },
};

interface StatusPillProps {
  label: string;
  variant?: Variant;
}

export function StatusPill({ label, variant = 'gray' }: StatusPillProps) {
  const { bg, color } = VARIANTS[variant];
  return (
    <span style={{
      display: 'inline-block',
      fontFamily: FONT, fontSize: 11, fontWeight: 600,
      padding: '2px 9px', borderRadius: 99,
      background: bg, color,
    }}>
      {label}
    </span>
  );
}

// ─── Helpers for domain-specific pill colours ─────────────────────────────────

export function vehicleStatusPill(isActive: boolean) {
  return <StatusPill label={isActive ? 'Active' : 'Inactive'} variant={isActive ? 'green' : 'gray'} />;
}

export function rideStatusPill(status: string) {
  const map: Record<string, Variant> = {
    Requested: 'blue', EnRoute: 'yellow', Completed: 'green', Cancelled: 'gray',
  };
  return <StatusPill label={status} variant={map[status] ?? 'gray'} />;
}

export function ticketStatusPill(status: string) {
  const map: Record<string, Variant> = {
    Open: 'blue', InProgress: 'yellow', Resolved: 'green', Closed: 'gray',
  };
  return <StatusPill label={status} variant={map[status] ?? 'gray'} />;
}

export function ticketPriorityPill(priority: string) {
  const map: Record<string, Variant> = {
    Low: 'gray', Medium: 'blue', High: 'yellow', Critical: 'red',
  };
  return <StatusPill label={priority} variant={map[priority] ?? 'gray'} />;
}
