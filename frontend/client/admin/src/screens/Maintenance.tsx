import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { MaintenanceLogResponse, CreateMaintenanceLogRequest, VehicleResponse } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';

const COLS: ColDef<MaintenanceLogResponse>[] = [
  { key: 'vehicle',     header: 'Vehicle',      render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.vehicleModel ?? '—'}</span> },
  { key: 'plate',       header: 'Plate',        render: r => r.vehicleLicensePlate ?? '—' },
  { key: 'date',        header: 'Service Date', render: r => new Date(r.serviceDate).toLocaleDateString() },
  { key: 'description', header: 'Description',  render: r => <span style={{ fontSize: 12 }}>{r.description}</span>, width: '30%' },
  { key: 'technician',  header: 'Technician',   render: r => r.technicianName },
  { key: 'cost',        header: 'Cost',         render: r => `€${r.cost.toFixed(2)}` },
  { key: 'nextMileage', header: 'Next at',      render: r => r.nextServiceMileage != null ? `${r.nextServiceMileage.toLocaleString()} km` : '—' },
];

const INPUT_STYLE: React.CSSProperties = {
  border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
  fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
  background: '#fff', boxSizing: 'border-box',
};

function LogMaintenancePanel({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const today = new Date().toISOString().split('T')[0];
  const [form, setForm] = useState<CreateMaintenanceLogRequest>({
    vehicleId: '', serviceDate: today, description: '', technicianName: '', cost: 0,
  });
  const [vehicles, setVehicles] = useState<VehicleResponse[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminApi.vehicles.list(1, 100).then(r => setVehicles(r.items)).catch(console.error);
  }, []);

  function set<K extends keyof CreateMaintenanceLogRequest>(key: K, value: CreateMaintenanceLogRequest[K]) {
    setForm(f => ({ ...f, [key]: value }));
  }

  async function handleSave() {
    if (!form.vehicleId || !form.description.trim() || !form.technicianName.trim()) {
      setError('Vehicle, description, and technician name are required.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const { vehicleId, ...body } = form;
      await adminApi.maintenance.create(vehicleId, body);
      onSaved();
      onClose();
    } catch (e: any) {
      setError(e.message ?? 'Failed to save.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{
      position: 'fixed', top: 0, right: 0, bottom: 0, width: 440,
      background: '#fff', borderLeft: `1px solid ${ND.border}`,
      boxShadow: 'rgba(0,0,0,0.12) -8px 0 24px',
      padding: '32px 28px', display: 'flex', flexDirection: 'column', zIndex: 50,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <h2 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 20, color: '#000', margin: 0 }}>Log Maintenance</h2>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2" strokeLinecap="round">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, flex: 1, overflowY: 'auto' }}>
        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Vehicle</label>
          <select
            style={{ ...INPUT_STYLE, cursor: 'pointer' }}
            value={form.vehicleId}
            onChange={e => set('vehicleId', e.target.value)}
          >
            <option value="">Select a vehicle…</option>
            {vehicles.map(v => (
              <option key={v.id} value={v.id}>{v.model} — {v.licensePlate}</option>
            ))}
          </select>
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Service Date</label>
          <input type="date" style={INPUT_STYLE} value={form.serviceDate} onChange={e => set('serviceDate', e.target.value)} />
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Description</label>
          <textarea
            style={{ ...INPUT_STYLE, resize: 'vertical', minHeight: 80 }}
            value={form.description}
            onChange={e => set('description', e.target.value)}
            placeholder="Describe the service performed…"
          />
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Technician Name</label>
          <input style={INPUT_STYLE} value={form.technicianName} onChange={e => set('technicianName', e.target.value)} placeholder="Full name" />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Cost (€)</label>
            <input
              type="number" min="0" step="0.01" style={INPUT_STYLE}
              value={form.cost}
              onChange={e => set('cost', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Next Service (km)</label>
            <input
              type="number" min="0" style={INPUT_STYLE}
              placeholder="Optional"
              value={form.nextServiceMileage ?? ''}
              onChange={e => set('nextServiceMileage', e.target.value ? Number(e.target.value) : null)}
            />
          </div>
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
        }}>{saving ? 'Saving…' : 'Log Entry'}</button>
      </div>
    </div>
  );
}

export function Maintenance() {
  const [items, setItems] = useState<MaintenanceLogResponse[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [panelOpen, setPanelOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const PAGE_SIZE = 20;

  useEffect(() => {
    adminApi.maintenance.list(page, PAGE_SIZE)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); })
      .catch(console.error);
  }, [page, refreshKey]);

  const totalCost = items.reduce((s, r) => s + r.cost, 0);

  return (
    <div data-screen-label="Admin Maintenance" style={{ position: 'relative' }}>
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Records (page)" value={items.length} />
        <Card label="Page Cost" value={`€${totalCost.toFixed(0)}`} sub="sum of this page" />
      </div>
      <SectionCard
        title={`Maintenance Logs — ${total} total`}
        action={
          <button onClick={() => setPanelOpen(true)} style={{
            background: '#000', border: 'none', color: '#fff',
            borderRadius: 99, padding: '8px 16px', cursor: 'pointer',
            fontFamily: FONT, fontWeight: 600, fontSize: 13,
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round">
              <path d="M12 5v14M5 12h14" />
            </svg>
            Log Maintenance
          </button>
        }
      >
        <Table cols={COLS} rows={items} rowKey={r => r.id} emptyText="No maintenance logs." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>

      {panelOpen && (
        <LogMaintenancePanel
          onClose={() => setPanelOpen(false)}
          onSaved={() => setRefreshKey(k => k + 1)}
        />
      )}
    </div>
  );
}
