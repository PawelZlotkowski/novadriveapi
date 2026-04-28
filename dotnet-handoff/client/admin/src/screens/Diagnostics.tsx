import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { SensorDiagnosticResponse, DiagSeverity, VehicleResponse } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';
import { ErrorBanner, apiError } from '../ui/ErrorBanner';

const SEV_COLORS: Record<DiagSeverity, { bg: string; color: string }> = {
  Info:     { bg: '#dbeafe', color: '#1d4ed8' },
  Warning:  { bg: '#fef3c7', color: '#b45309' },
  Critical: { bg: '#fee2e2', color: '#b91c1c' },
};

function SevPill({ severity }: { severity: DiagSeverity }) {
  const { bg, color } = SEV_COLORS[severity] ?? { bg: '#f3f4f6', color: '#6b7280' };
  return (
    <span style={{ fontSize: 11, fontWeight: 700, padding: '2px 9px', borderRadius: 99, background: bg, color, fontFamily: FONT }}>
      {severity}
    </span>
  );
}

type Filter = 'All' | DiagSeverity;
const FILTERS: Filter[] = ['All', 'Critical', 'Warning', 'Info'];

const COLS: ColDef<SensorDiagnosticResponse>[] = [
  { key: 'sev',       header: 'Severity',    render: r => <SevPill severity={r.severity} /> },
  { key: 'vehicle',   header: 'Vehicle',     render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.vehicleModel ?? '—'}</span> },
  { key: 'plate',     header: 'Plate',       render: r => <span style={{ fontFamily: FONT, fontSize: 12, color: ND.muted }}>{r.vehicleLicensePlate ?? '—'}</span> },
  { key: 'sensor',    header: 'Sensor',      render: r => r.sensorType },
  { key: 'code',      header: 'Error Code',  render: r => <code style={{ fontSize: 12, background: '#f5f5f5', padding: '1px 5px', borderRadius: 4 }}>{r.errorCode}</code> },
  { key: 'message',   header: 'Message',     render: r => <span style={{ fontSize: 12, color: ND.body }}>{r.message}</span>, width: '30%' },
  { key: 'timestamp', header: 'Time',        render: r => new Date(r.timestamp).toLocaleString() },
];

export function Diagnostics() {
  const [items, setItems]           = useState<SensorDiagnosticResponse[]>([]);
  const [critical, setCritical]     = useState<SensorDiagnosticResponse[]>([]);
  const [page, setPage]             = useState(1);
  const [total, setTotal]           = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [filter, setFilter]         = useState<Filter>('All');
  const [vehicle, setVehicle]       = useState<string>('');
  const [vehicles, setVehicles]     = useState<VehicleResponse[]>([]);
  const [error, setError]           = useState<string | null>(null);
  const PAGE_SIZE = 20;

  useEffect(() => {
    adminApi.vehicles.list(1, 100).then(r => setVehicles(r.items)).catch(console.error);
    adminApi.diagnostics.critical(10).then(setCritical).catch(console.error);
  }, []);

  useEffect(() => { setPage(1); }, [filter, vehicle]);

  useEffect(() => {
    const sev = filter === 'All' ? undefined : filter;
    const req = vehicle
      ? adminApi.diagnostics.byVehicle(vehicle, page, PAGE_SIZE)
      : adminApi.diagnostics.list(page, PAGE_SIZE, sev);
    req
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); setError(null); })
      .catch(e => setError(apiError(e)));
  }, [page, filter, vehicle]);

  const critCount = critical.length;
  const warnCount = items.filter(d => d.severity === 'Warning').length;

  const SELECT: React.CSSProperties = {
    border: `1px solid ${ND.border}`, borderRadius: 8, padding: '6px 10px',
    fontFamily: FONT, fontSize: 13, color: '#000', outline: 'none',
    background: '#fff', cursor: 'pointer',
  };

  return (
    <div>
      {error && <ErrorBanner message={error} />}

      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Critical Alerts" value={critCount}    sub="recent" subColor="#b91c1c" />
        <Card label="Warnings"        value={warnCount}    sub="on page" subColor="#b45309" />
        <Card label="Total (page)"    value={items.length} />
        <Card label="Vehicles"        value={vehicles.length} sub="monitored" />
      </div>

      {/* Critical alert strip */}
      {critical.length > 0 && (
        <div style={{
          background: '#fff5f5', border: '1px solid #fecaca', borderRadius: 10,
          padding: '14px 16px', marginBottom: 20,
        }}>
          <div style={{ fontFamily: FONT, fontWeight: 600, fontSize: 13, color: '#b91c1c', marginBottom: 8 }}>
            🚨 Recent Critical Alerts
          </div>
          {critical.slice(0, 5).map(d => (
            <div key={d.id} style={{ display: 'flex', gap: 12, padding: '6px 0', borderBottom: '1px solid #fecaca', alignItems: 'center' }}>
              <span style={{ fontFamily: FONT, fontSize: 12, fontWeight: 600, color: '#000', minWidth: 140 }}>{d.vehicleModel ?? d.vehicleId}</span>
              <code style={{ fontSize: 11, background: '#fee2e2', padding: '1px 6px', borderRadius: 4, color: '#b91c1c' }}>{d.errorCode}</code>
              <span style={{ fontFamily: FONT, fontSize: 12, color: ND.body, flex: 1 }}>{d.message}</span>
              <span style={{ fontFamily: FONT, fontSize: 11, color: ND.muted }}>{new Date(d.timestamp).toLocaleTimeString()}</span>
            </div>
          ))}
        </div>
      )}

      <SectionCard
        title={`Diagnostics — ${total} total`}
        action={
          <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
            <select style={SELECT} value={vehicle} onChange={e => setVehicle(e.target.value)}>
              <option value="">All Vehicles</option>
              {vehicles.map(v => <option key={v.id} value={v.id}>{v.model} — {v.licensePlate}</option>)}
            </select>
            {!vehicle && (
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
            )}
          </div>
        }
      >
        <Table cols={COLS} rows={items} rowKey={r => r.id} emptyText="No diagnostics found." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>
    </div>
  );
}
