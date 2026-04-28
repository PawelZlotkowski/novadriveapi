import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { PaymentResponse, PaymentStatus } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';
import { ErrorBanner, apiError } from '../ui/ErrorBanner';

const STATUS_COLORS: Record<PaymentStatus, { bg: string; color: string }> = {
  Pending:    { bg: '#fef3c7', color: '#b45309' },
  Successful: { bg: '#dcfce7', color: '#16a34a' },
  Failed:     { bg: '#fee2e2', color: '#b91c1c' },
  Refunded:   { bg: '#f3f4f6', color: '#6b7280' },
};

function StatusPill({ status }: { status: PaymentStatus }) {
  const { bg, color } = STATUS_COLORS[status] ?? { bg: '#f3f4f6', color: '#6b7280' };
  return (
    <span style={{ fontSize: 11, fontWeight: 600, padding: '2px 9px', borderRadius: 99, background: bg, color, fontFamily: FONT }}>
      {status}
    </span>
  );
}

type Filter = 'All' | PaymentStatus;
const FILTERS: Filter[] = ['All', 'Pending', 'Successful', 'Failed', 'Refunded'];

export function Payments() {
  const [items, setItems]           = useState<PaymentResponse[]>([]);
  const [page, setPage]             = useState(1);
  const [total, setTotal]           = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [filter, setFilter]         = useState<Filter>('All');
  const [error, setError]           = useState<string | null>(null);
  const [refunding, setRefunding]   = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const PAGE_SIZE = 20;

  useEffect(() => { setPage(1); }, [filter]);

  useEffect(() => {
    const status = filter === 'All' ? undefined : filter;
    adminApi.payments.list(page, PAGE_SIZE, status)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); setError(null); })
      .catch(e => setError(apiError(e)));
  }, [page, filter, refreshKey]);

  async function handleRefund(id: string) {
    setRefunding(id);
    try {
      await adminApi.payments.refund(id);
      setRefreshKey(k => k + 1);
    } catch (e: any) {
      alert(e.message ?? 'Refund failed');
    } finally {
      setRefunding(null);
    }
  }

  const cols: ColDef<PaymentResponse>[] = [
    { key: 'passenger', header: 'Passenger', render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.passengerName ?? '—'}</span> },
    { key: 'amount',    header: 'Amount',    render: r => <span style={{ fontFamily: FONT, fontWeight: 600 }}>€{r.amount.toFixed(2)}</span> },
    { key: 'currency',  header: 'Currency',  render: r => r.currency },
    { key: 'status',    header: 'Status',    render: r => <StatusPill status={r.status} /> },
    { key: 'ref',       header: 'Reference', render: r => <span style={{ fontFamily: FONT, fontSize: 12, color: ND.muted }}>{r.transactionReference ?? '—'}</span> },
    { key: 'date',      header: 'Date',      render: r => new Date(r.createdAt).toLocaleDateString() },
    {
      key: 'actions', header: '',
      render: r => r.status === 'Successful' ? (
        <button
          onClick={e => { e.stopPropagation(); handleRefund(r.id); }}
          disabled={refunding === r.id}
          style={{
            background: 'none', border: `1px solid ${ND.border}`, borderRadius: 6,
            padding: '4px 10px', cursor: 'pointer', fontFamily: FONT, fontSize: 12,
            color: ND.err, opacity: refunding === r.id ? 0.5 : 1,
          }}
        >
          {refunding === r.id ? '…' : 'Refund'}
        </button>
      ) : null,
    },
  ];

  const revenue   = items.filter(p => p.status === 'Successful').reduce((s, p) => s + p.amount, 0);
  const refunded  = items.filter(p => p.status === 'Refunded').length;
  const failed    = items.filter(p => p.status === 'Failed').length;

  return (
    <div>
      {error && <ErrorBanner message={error} />}

      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Page Revenue"  value={`€${revenue.toFixed(0)}`}  sub="successful on page" subColor="#16a34a" />
        <Card label="Total (page)"  value={items.length} />
        <Card label="Refunded"      value={refunded}    sub="on page" subColor="#6b7280" />
        <Card label="Failed"        value={failed}      sub="on page" subColor="#b91c1c" />
      </div>

      <SectionCard
        title={`Payments — ${total} total`}
        action={
          <div style={{ display: 'flex', gap: 6 }}>
            {FILTERS.map(f => (
              <button key={f} onClick={() => setFilter(f)} style={{
                background: filter === f ? '#000' : ND.surface,
                color: filter === f ? '#fff' : '#000',
                border: filter === f ? '1px solid #000' : `1px solid ${ND.border}`,
                fontFamily: FONT, fontWeight: 500, fontSize: 12,
                padding: '5px 12px', borderRadius: 99, cursor: 'pointer',
              }}>{f}</button>
            ))}
          </div>
        }
      >
        <Table cols={cols} rows={items} rowKey={r => r.id} emptyText="No payments found." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>
    </div>
  );
}
