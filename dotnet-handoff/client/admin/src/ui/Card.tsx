import type { ReactNode } from 'react';
import { ND, FONT } from '@shared/tokens';

interface CardProps {
  label: string;
  value: string | number;
  sub?: string;
  subColor?: string;
}

export function Card({ label, value, sub, subColor }: CardProps) {
  return (
    <div style={{
      flex: 1, border: `1px solid ${ND.border}`, borderRadius: 12,
      padding: '20px 24px', background: '#fff',
    }}>
      <div style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, color: ND.muted }}>{label}</div>
      <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: 32, color: '#000', marginTop: 4, letterSpacing: -0.5 }}>
        {value}
      </div>
      {sub && (
        <div style={{ fontFamily: FONT, fontSize: 13, color: subColor ?? ND.muted, marginTop: 6 }}>{sub}</div>
      )}
    </div>
  );
}

interface SectionCardProps {
  title?: string;
  action?: ReactNode;
  children: ReactNode;
}

export function SectionCard({ title, action, children }: SectionCardProps) {
  return (
    <div style={{ border: `1px solid ${ND.border}`, borderRadius: 12, background: '#fff', overflow: 'hidden' }}>
      {(title || action) && (
        <div style={{
          padding: '14px 20px', borderBottom: `1px solid ${ND.border}`,
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        }}>
          {title && <span style={{ fontFamily: FONT, fontWeight: 600, fontSize: 14 }}>{title}</span>}
          {action}
        </div>
      )}
      {children}
    </div>
  );
}
