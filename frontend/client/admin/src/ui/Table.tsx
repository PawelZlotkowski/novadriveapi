import type { ReactNode } from 'react';
import { ND, FONT } from '@shared/tokens';

export interface ColDef<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  width?: number | string;
}

interface TableProps<T> {
  cols: ColDef<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  emptyText?: string;
  onRowClick?: (row: T) => void;
}

export function Table<T>({ cols, rows, rowKey, emptyText = 'No records.', onRowClick }: TableProps<T>) {
  return (
    <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontFamily: FONT, fontSize: 13 }}>
        <thead>
          <tr style={{ background: ND.surface, borderBottom: `1px solid ${ND.border}` }}>
            {cols.map(c => (
              <th
                key={c.key}
                style={{
                  padding: '10px 16px', textAlign: 'left', fontWeight: 600,
                  fontSize: 12, color: ND.muted, width: c.width,
                  textTransform: 'uppercase', letterSpacing: '0.05em',
                }}
              >
                {c.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td
                colSpan={cols.length}
                style={{ padding: '32px 16px', textAlign: 'center', color: ND.muted }}
              >
                {emptyText}
              </td>
            </tr>
          ) : (
            rows.map(row => (
              <tr
                key={rowKey(row)}
                style={{ borderBottom: `1px solid ${ND.border}`, cursor: onRowClick ? 'pointer' : undefined }}
                onClick={() => onRowClick?.(row)}
                onMouseEnter={e => (e.currentTarget.style.background = ND.surface)}
                onMouseLeave={e => (e.currentTarget.style.background = '')}
              >
                {cols.map(c => (
                  <td key={c.key} style={{ padding: '11px 16px' }}>
                    {c.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
