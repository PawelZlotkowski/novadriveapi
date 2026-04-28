import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { DiscountCodeResponse, DiscountType, CreateDiscountCodeRequest } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';

const INPUT_STYLE: React.CSSProperties = {
  border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
  fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
  background: '#fff', boxSizing: 'border-box',
};

function TypePill({ type }: { type: DiscountType }) {
  const isPercent = type === 'Percentage';
  return (
    <span style={{
      fontFamily: FONT, fontSize: 11, fontWeight: 600, padding: '2px 8px',
      borderRadius: 99, background: isPercent ? '#dbeafe' : '#dcfce7',
      color: isPercent ? '#1d4ed8' : '#16a34a',
    }}>{isPercent ? '%' : '€ Flat'}</span>
  );
}

function ActiveToggle({ code, onToggled }: { code: DiscountCodeResponse; onToggled: (c: DiscountCodeResponse) => void }) {
  const [loading, setLoading] = useState(false);
  async function toggle() {
    setLoading(true);
    try {
      const updated = await adminApi.discountCodes.setStatus(code.id, !code.isActive);
      onToggled(updated);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  }
  return (
    <button
      onClick={e => { e.stopPropagation(); toggle(); }}
      disabled={loading}
      style={{
        width: 44, height: 24, borderRadius: 999, border: 'none', cursor: loading ? 'default' : 'pointer',
        background: code.isActive ? '#22c55e' : '#d1d5db',
        position: 'relative', transition: 'background 0.2s',
        opacity: loading ? 0.6 : 1, flexShrink: 0,
      }}
    >
      <div style={{
        position: 'absolute', top: 3, left: code.isActive ? 23 : 3,
        width: 18, height: 18, borderRadius: '50%', background: '#fff',
        transition: 'left 0.2s', boxShadow: '0 1px 3px rgba(0,0,0,0.2)',
      }}/>
    </button>
  );
}

function CreatePanel({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const oneYearFromNow = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);
  const [form, setForm] = useState({
    code: '', type: 'Percentage' as DiscountType,
    value: 10, minimumRideValue: 0, expiresAt: oneYearFromNow, maxUses: '' as string | number,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  function set<K extends keyof typeof form>(k: K, v: typeof form[K]) {
    setForm(f => ({ ...f, [k]: v }));
  }

  async function handleSave() {
    if (!form.code.trim()) { setError('Code is required.'); return; }
    if (form.value <= 0)   { setError('Value must be greater than 0.'); return; }
    setSaving(true); setError(null);
    try {
      const body: CreateDiscountCodeRequest = {
        code: form.code.trim().toUpperCase(),
        type: form.type,
        value: Number(form.value),
        minimumRideValue: Number(form.minimumRideValue) || 0,
        expiresAt: new Date(form.expiresAt).toISOString(),
        maxUses: form.maxUses !== '' ? Number(form.maxUses) : null,
      };
      await adminApi.discountCodes.create(body);
      onSaved(); onClose();
    } catch (e: any) {
      setError(e.message ?? 'Failed to create code.');
    } finally { setSaving(false); }
  }

  return (
    <div style={{
      position: 'fixed', top: 0, right: 0, bottom: 0, width: 420,
      background: '#fff', borderLeft: `1px solid ${ND.border}`,
      boxShadow: 'rgba(0,0,0,0.12) -8px 0 24px',
      padding: '32px 28px', display: 'flex', flexDirection: 'column', zIndex: 50,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <h2 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 20, color: '#000', margin: 0 }}>Create Discount Code</h2>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2" strokeLinecap="round"><path d="M6 6l12 12M18 6L6 18"/></svg>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, flex: 1, overflowY: 'auto' }}>
        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Code</label>
          <input style={INPUT_STYLE} placeholder="e.g. SAVE20" value={form.code}
            onChange={e => set('code', e.target.value.toUpperCase())} />
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 8 }}>Type</label>
          <div style={{ display: 'inline-flex', background: '#f0f0f0', borderRadius: 99, padding: 3 }}>
            {(['Percentage', 'Flat'] as DiscountType[]).map(t => (
              <button key={t} onClick={() => set('type', t)} style={{
                border: 'none', cursor: 'pointer',
                background: form.type === t ? '#000' : 'transparent',
                color: form.type === t ? '#fff' : '#000',
                fontFamily: FONT, fontWeight: 500, fontSize: 13, padding: '8px 20px', borderRadius: 99,
              }}>{t === 'Percentage' ? '% Percentage' : '€ Flat Amount'}</button>
            ))}
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>
              Value {form.type === 'Percentage' ? '(%)' : '(€)'}
            </label>
            <input type="number" style={INPUT_STYLE} value={form.value} min={0}
              onChange={e => set('value', Number(e.target.value))} />
          </div>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Min Ride Value (€)</label>
            <input type="number" style={INPUT_STYLE} value={form.minimumRideValue} min={0}
              onChange={e => set('minimumRideValue', Number(e.target.value))} />
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Expires At</label>
            <input type="date" style={INPUT_STYLE} value={form.expiresAt}
              onChange={e => set('expiresAt', e.target.value)} />
          </div>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Max Uses (blank = ∞)</label>
            <input type="number" style={INPUT_STYLE} placeholder="∞" value={form.maxUses}
              onChange={e => set('maxUses', e.target.value)} min={1} />
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
        }}>{saving ? 'Creating…' : 'Create Code'}</button>
      </div>
    </div>
  );
}

export function DiscountCodes() {
  const [items, setItems] = useState<DiscountCodeResponse[]>([]);
  const [page, setPage]   = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [panelOpen, setPanelOpen]   = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const PAGE_SIZE = 20;

  useEffect(() => {
    adminApi.discountCodes.list(page, PAGE_SIZE)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); })
      .catch(console.error);
  }, [page, refreshKey]);

  function handleToggled(updated: DiscountCodeResponse) {
    setItems(prev => prev.map(c => c.id === updated.id ? updated : c));
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this discount code?')) return;
    try {
      await adminApi.discountCodes.delete(id);
      setRefreshKey(k => k + 1);
    } catch (e) { console.error(e); }
  }

  const active   = items.filter(c => c.isActive).length;
  const inactive = items.length - active;
  const totalUses = items.reduce((sum, c) => sum + c.timesUsed, 0);

  const COLS: ColDef<DiscountCodeResponse>[] = [
    {
      key: 'code', header: 'Code',
      render: r => (
        <span style={{
          fontFamily: 'monospace', fontSize: 13, fontWeight: 700,
          background: '#f3f4f6', padding: '2px 8px', borderRadius: 6, color: '#111',
        }}>{r.code}</span>
      ),
    },
    { key: 'type',  header: 'Type',  render: r => <TypePill type={r.type} /> },
    {
      key: 'value', header: 'Value',
      render: r => (
        <span style={{ fontFamily: FONT, fontWeight: 600, fontSize: 14 }}>
          {r.type === 'Percentage' ? `${r.value}%` : `€${r.value.toFixed(2)}`}
        </span>
      ),
    },
    { key: 'min',     header: 'Min Ride', render: r => r.minimumRideValue ? `€${r.minimumRideValue}` : '—' },
    {
      key: 'expires', header: 'Expires',
      render: r => r.expiresAt
        ? <span style={{ color: new Date(r.expiresAt) < new Date() ? ND.err : ND.body, fontSize: 13 }}>
            {new Date(r.expiresAt).toLocaleDateString()}
          </span>
        : '—',
    },
    {
      key: 'uses', header: 'Uses',
      render: r => <span style={{ fontFamily: FONT, fontSize: 13 }}>{r.timesUsed}{r.maxUses ? `/${r.maxUses}` : ''}</span>,
    },
    { key: 'active', header: 'Active', render: r => <ActiveToggle code={r} onToggled={handleToggled} /> },
    {
      key: 'delete', header: '',
      render: r => (
        <button
          onClick={e => { e.stopPropagation(); handleDelete(r.id); }}
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: ND.err, padding: '4px 8px', fontSize: 13, fontFamily: FONT }}
        >Delete</button>
      ),
    },
  ];

  return (
    <div data-screen-label="Admin Discount Codes" style={{ position: 'relative' }}>
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Total (page)" value={items.length} />
        <Card label="Active"   value={active}   subColor="#16a34a" sub="enabled" />
        <Card label="Inactive" value={inactive} subColor="#6b7280" sub="disabled" />
        <Card label="Total Uses" value={totalUses} subColor="#3b82f6" sub="all time" />
      </div>

      <SectionCard
        title={`Discount Codes — ${total} total`}
        action={
          <button onClick={() => setPanelOpen(true)} style={{
            background: '#000', border: 'none', color: '#fff',
            borderRadius: 99, padding: '8px 16px', cursor: 'pointer',
            fontFamily: FONT, fontWeight: 600, fontSize: 13,
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.4" strokeLinecap="round">
              <path d="M12 5v14M5 12h14"/>
            </svg>
            Create Code
          </button>
        }
      >
        <Table cols={COLS} rows={items} rowKey={r => r.id} emptyText="No discount codes found." />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>

      {panelOpen && (
        <CreatePanel
          onClose={() => setPanelOpen(false)}
          onSaved={() => setRefreshKey(k => k + 1)}
        />
      )}
    </div>
  );
}
