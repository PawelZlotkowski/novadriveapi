import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { VehicleResponse, VehicleType, CreateVehicleRequest } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { vehicleStatusPill } from '../ui/StatusPill';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';

const TYPE_COLORS: Record<VehicleType, string> = {
  Standard: '#6b7280', Van: '#3b82f6', Luxury: '#f5c518',
};

function BatteryBar({ pct }: { pct: number | null }) {
  if (pct == null) return <span style={{ color: ND.muted, fontSize: 14 }}>—</span>;
  const color = pct >= 50 ? ND.accent : pct >= 25 ? ND.warn : ND.err;
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <div style={{ width: 72, height: 6, background: ND.border, borderRadius: 99, overflow: 'hidden' }}>
        <div style={{ width: `${pct}%`, height: '100%', background: color, borderRadius: 99 }} />
      </div>
      <span style={{ fontFamily: FONT, fontSize: 12, color: ND.body, minWidth: 28 }}>{pct}%</span>
    </div>
  );
}

const COLS: ColDef<VehicleResponse>[] = [
  { key: 'model',   header: 'Model',   render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.model}</span> },
  { key: 'vin',     header: 'VIN',     render: r => <span style={{ fontFamily: FONT, fontSize: 12, color: ND.muted }}>{r.vin}</span> },
  { key: 'plate',   header: 'Plate',   render: r => r.licensePlate },
  {
    key: 'type', header: 'Type',
    render: r => (
      <span style={{
        fontFamily: FONT, fontSize: 11, fontWeight: 600, padding: '2px 9px',
        borderRadius: 99, background: TYPE_COLORS[r.vehicleType] + '22', color: TYPE_COLORS[r.vehicleType],
      }}>{r.vehicleType}</span>
    ),
  },
  { key: 'year',    header: 'Year',    render: r => r.yearOfManufacture },
  { key: 'battery', header: 'Battery', render: r => <BatteryBar pct={r.currentBatteryPercentage} /> },
  { key: 'mileage', header: 'Mileage', render: r => r.currentMileage != null ? `${r.currentMileage.toLocaleString()} km` : '—' },
  { key: 'status',  header: 'Status',  render: r => vehicleStatusPill(r.isActive) },
];

type Filter = 'All' | 'Active' | 'Inactive';
const FILTERS: Filter[] = ['All', 'Active', 'Inactive'];

const INPUT_STYLE: React.CSSProperties = {
  border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
  fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
  background: '#fff', boxSizing: 'border-box',
};

function AddVehiclePanel({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState<CreateVehicleRequest>({
    vin: '', licensePlate: '', model: '', vehicleType: 'Standard',
    yearOfManufacture: new Date().getFullYear(),
    latitude: 50.8218,   // Howest campus, Kortrijk
    longitude: 3.2458,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function set<K extends keyof CreateVehicleRequest>(key: K, value: CreateVehicleRequest[K]) {
    setForm(f => ({ ...f, [key]: value }));
  }

  async function handleSave() {
    if (!form.vin.trim() || !form.licensePlate.trim() || !form.model.trim()) {
      setError('VIN, license plate, and model are required.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await adminApi.vehicles.create(form);
      onSaved();
      onClose();
    } catch (e: any) {
      setError(e.message ?? 'Failed to save vehicle.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{
      position: 'fixed', top: 0, right: 0, bottom: 0, width: 420,
      background: '#fff', borderLeft: `1px solid ${ND.border}`,
      boxShadow: 'rgba(0,0,0,0.12) -8px 0 24px',
      padding: '32px 28px', display: 'flex', flexDirection: 'column', zIndex: 50,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <h2 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 20, color: '#000', margin: 0 }}>Add Vehicle</h2>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2" strokeLinecap="round">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, flex: 1, overflowY: 'auto' }}>
        {([
          { label: 'VIN', key: 'vin', placeholder: 'e.g. 1HGCM82633A000000' },
          { label: 'License Plate', key: 'licensePlate', placeholder: 'e.g. 1-ABC-234' },
          { label: 'Model', key: 'model', placeholder: 'e.g. Tesla Model Y' },
        ] as { label: string; key: 'vin' | 'licensePlate' | 'model'; placeholder: string }[]).map(({ label, key, placeholder }) => (
          <div key={key}>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>{label}</label>
            <input
              style={INPUT_STYLE} placeholder={placeholder} value={form[key]}
              onChange={e => set(key, e.target.value)}
            />
          </div>
        ))}

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 8 }}>Vehicle Type</label>
          <div style={{ display: 'inline-flex', background: '#f0f0f0', borderRadius: 99, padding: 3 }}>
            {(['Standard', 'Van', 'Luxury'] as VehicleType[]).map(t => (
              <button key={t} onClick={() => set('vehicleType', t)} style={{
                border: 'none', cursor: 'pointer',
                background: form.vehicleType === t ? '#000' : 'transparent',
                color: form.vehicleType === t ? '#fff' : '#000',
                fontFamily: FONT, fontWeight: 500, fontSize: 13, padding: '8px 16px', borderRadius: 99,
              }}>{t}</button>
            ))}
          </div>
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Year of Manufacture</label>
          <input
            type="number" style={INPUT_STYLE} value={form.yearOfManufacture}
            onChange={e => set('yearOfManufacture', Number(e.target.value))}
          />
        </div>

        {error && <div style={{ color: ND.err, fontFamily: FONT, fontSize: 13 }}>{error}</div>}
      </div>

      <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
        <button onClick={onClose} style={{
          flex: 1, background: ND.surface, border: `1px solid ${ND.border}`, color: '#000',
          borderRadius: 99, padding: '12px 20px', cursor: 'pointer', fontFamily: FONT, fontWeight: 500, fontSize: 14,
        }}>Cancel</button>
        <button onClick={handleSave} disabled={saving} style={{
          flex: 1, background: '#000', border: 'none', color: '#fff',
          borderRadius: 99, padding: '12px 20px', cursor: saving ? 'not-allowed' : 'pointer',
          fontFamily: FONT, fontWeight: 600, fontSize: 14, opacity: saving ? 0.6 : 1,
        }}>{saving ? 'Saving…' : 'Save Vehicle'}</button>
      </div>
    </div>
  );
}

export function Vehicles() {
  const [items, setItems] = useState<VehicleResponse[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [filter, setFilter] = useState<Filter>('All');
  const [panelOpen, setPanelOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const PAGE_SIZE = 20;

  const isActive = filter === 'All' ? undefined : filter === 'Active';

  useEffect(() => { setPage(1); }, [filter]);

  useEffect(() => {
    adminApi.vehicles.list(page, PAGE_SIZE, isActive)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); })
      .catch(console.error);
  }, [page, filter, refreshKey]);

  // Auto-refresh battery / position every 10 s
  useEffect(() => {
    const id = setInterval(() => setRefreshKey(k => k + 1), 10_000);
    return () => clearInterval(id);
  }, []);

  const active   = items.filter(v => v.isActive).length;
  const inactive = items.length - active;

  return (
    <div data-screen-label="Admin Vehicles" style={{ position: 'relative' }}>
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Total (page)" value={items.length} />
        <Card label="Active"   value={active}   subColor="#16a34a" sub="on road" />
        <Card label="Inactive" value={inactive} subColor="#6b7280" sub="offline" />
        <Card label="Luxury"   value={items.filter(v => v.vehicleType === 'Luxury').length} subColor="#f5c518" sub="premium" />
      </div>

      <SectionCard
        title={`Vehicles — ${total} total`}
        action={
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
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
            <button onClick={() => setPanelOpen(true)} style={{
              background: '#000', border: 'none', color: '#fff',
              borderRadius: 99, padding: '8px 16px', cursor: 'pointer',
              fontFamily: FONT, fontWeight: 600, fontSize: 13,
              display: 'flex', alignItems: 'center', gap: 6,
            }}>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round">
                <path d="M12 5v14M5 12h14" />
              </svg>
              Add Vehicle
            </button>
          </div>
        }
      >
        <Table cols={COLS} rows={items} rowKey={r => r.id} emptyText="No vehicles found." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>

      {panelOpen && (
        <AddVehiclePanel
          onClose={() => setPanelOpen(false)}
          onSaved={() => setRefreshKey(k => k + 1)}
        />
      )}
    </div>
  );
}
