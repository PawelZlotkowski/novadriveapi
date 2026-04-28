import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { RideResponse, RideStatus } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { rideStatusPill } from '../ui/StatusPill';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';

const STATUS_OPTIONS: Array<RideStatus | ''> = ['', 'Requested', 'EnRoute', 'Completed', 'Cancelled'];

const COLS: ColDef<RideResponse>[] = [
  { key: 'passenger', header: 'Passenger',    render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.passengerName}</span> },
  { key: 'from',      header: 'From',         render: r => <span style={{ fontSize: 12 }}>{r.departureAddress}</span>, width: '20%' },
  { key: 'to',        header: 'To',           render: r => <span style={{ fontSize: 12 }}>{r.destinationAddress}</span>, width: '20%' },
  { key: 'vehicle',   header: 'Vehicle',      render: r => r.vehicleModel ? `${r.vehicleModel} (${r.vehicleLicensePlate})` : '—' },
  { key: 'price',     header: 'Price',        render: r => r.finalPrice != null ? `€${r.finalPrice.toFixed(2)}` : '—' },
  { key: 'status',    header: 'Status',       render: r => rideStatusPill(r.status) },
  { key: 'date',      header: 'Requested',    render: r => new Date(r.requestedAt).toLocaleDateString() },
];

export function Rides() {
  const [items, setItems] = useState<RideResponse[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState<RideStatus | ''>('');
  const PAGE_SIZE = 20;

  useEffect(() => {
    setPage(1);
  }, [statusFilter]);

  useEffect(() => {
    adminApi.rides.list(page, PAGE_SIZE, statusFilter || undefined)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); })
      .catch(console.error);
  }, [page, statusFilter]);

  const byStatus = (s: RideStatus) => items.filter(r => r.status === s).length;

  return (
    <div data-screen-label="Admin Rides">
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Total (page)" value={items.length} />
        <Card label="Requested" value={byStatus('Requested')} subColor="#1d4ed8" />
        <Card label="En Route"  value={byStatus('EnRoute')}   subColor="#ca8a04" />
        <Card label="Completed" value={byStatus('Completed')} subColor="#16a34a" />
      </div>
      <SectionCard
        title={`Rides — ${total} total`}
        action={
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value as RideStatus | '')}
            style={{ fontFamily: FONT, fontSize: 12, padding: '4px 8px', borderRadius: 6, border: `1px solid ${ND.border}` }}
          >
            {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s || 'All statuses'}</option>)}
          </select>
        }
      >
        <Table cols={COLS} rows={items} rowKey={r => r.id} emptyText="No rides found." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>
    </div>
  );
}
