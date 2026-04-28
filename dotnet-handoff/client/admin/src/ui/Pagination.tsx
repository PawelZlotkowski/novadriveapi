import { ND, FONT } from '@shared/tokens';

interface PaginationProps {
  page: number;
  totalPages: number;
  total: number;
  onPage: (p: number) => void;
}

export function Pagination({ page, totalPages, total, onPage }: PaginationProps) {
  if (totalPages <= 1) return null;
  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '12px 16px', borderTop: `1px solid ${ND.border}`,
      fontFamily: FONT, fontSize: 13, color: ND.muted,
    }}>
      <span>{total} records</span>
      <div style={{ display: 'flex', gap: 6 }}>
        <PageBtn label="←" disabled={page <= 1} onClick={() => onPage(page - 1)} />
        <span style={{ padding: '4px 10px', background: ND.surface, borderRadius: 6, color: '#000', fontWeight: 600 }}>
          {page} / {totalPages}
        </span>
        <PageBtn label="→" disabled={page >= totalPages} onClick={() => onPage(page + 1)} />
      </div>
    </div>
  );
}

function PageBtn({ label, disabled, onClick }: { label: string; disabled: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        padding: '4px 10px', borderRadius: 6, border: `1px solid ${ND.border}`,
        background: disabled ? ND.surface : '#fff', color: disabled ? ND.muted : '#000',
        fontFamily: FONT, fontSize: 13, cursor: disabled ? 'default' : 'pointer',
      }}
    >
      {label}
    </button>
  );
}
