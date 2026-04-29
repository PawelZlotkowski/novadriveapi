import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { UserResponse, UserRole } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';
import { ErrorBanner, apiError } from '../ui/ErrorBanner';

const ROLE_COLORS: Record<UserRole, { bg: string; color: string }> = {
  Admin:     { bg: '#fef3c7', color: '#b45309' },
  Passenger: { bg: '#dbeafe', color: '#1d4ed8' },
};

function RolePill({ role }: { role: UserRole }) {
  const { bg, color } = ROLE_COLORS[role] ?? { bg: '#f3f4f6', color: '#6b7280' };
  return (
    <span style={{ fontSize: 11, fontWeight: 600, padding: '2px 9px', borderRadius: 99, background: bg, color, fontFamily: FONT }}>
      {role}
    </span>
  );
}

function StatusDot({ active }: { active: boolean }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
      <div style={{ width: 7, height: 7, borderRadius: '50%', background: active ? '#22c55e' : '#6b7280' }} />
      <span style={{ fontFamily: FONT, fontSize: 13, color: active ? '#16a34a' : '#6b7280' }}>
        {active ? 'Active' : 'Inactive'}
      </span>
    </div>
  );
}

const COLS: ColDef<UserResponse>[] = [
  { key: 'email',       header: 'Email',       render: r => <span style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13 }}>{r.email}</span> },
  { key: 'role',        header: 'Role',        render: r => <RolePill role={r.role} /> },
  { key: 'status',      header: 'Status',      render: r => <StatusDot active={r.isActive} /> },
  { key: 'lastLogin',   header: 'Last Login',  render: r => r.lastLoginAt ? new Date(r.lastLoginAt).toLocaleString() : <span style={{ color: ND.muted }}>Never</span> },
  { key: 'created',     header: 'Joined',      render: r => new Date(r.createdAt).toLocaleDateString() },
];

export function Users() {
  const [items, setItems]         = useState<UserResponse[]>([]);
  const [page, setPage]           = useState(1);
  const [total, setTotal]         = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [error, setError]         = useState<string | null>(null);
  const [selected, setSelected]   = useState<UserResponse | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const PAGE_SIZE = 20;

  useEffect(() => {
    adminApi.users.list(page, PAGE_SIZE)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); setError(null); })
      .catch(e => setError(apiError(e)));
  }, [page, refreshKey]);

  const admins     = items.filter(u => u.role === 'Admin').length;
  const passengers = items.filter(u => u.role === 'Passenger').length;
  const active     = items.filter(u => u.isActive).length;

  return (
    <div style={{ position: 'relative' }}>
      {error && <ErrorBanner message={error} />}

      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Total (page)" value={items.length} />
        <Card label="Admins"     value={admins}     subColor="#b45309" sub="on page" />
        <Card label="Passengers" value={passengers} subColor="#1d4ed8" sub="on page" />
        <Card label="Active"     value={active}     subColor="#16a34a" sub="on page" />
      </div>

      <SectionCard title={`Users — ${total} total`}>
        <Table
          cols={COLS}
          rows={items}
          rowKey={r => r.id}
          emptyText="No users found."
          onRowClick={setSelected}
        />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>

      {selected && (
        <UserPanel
          user={selected}
          onClose={() => setSelected(null)}
          onSaved={() => { setSelected(null); setRefreshKey(k => k + 1); }}
        />
      )}
    </div>
  );
}

function UserPanel({ user, onClose, onSaved }: { user: UserResponse; onClose: () => void; onSaved: () => void }) {
  const [role, setRole]       = useState<UserRole>(user.role);
  const [active, setActive]   = useState(user.isActive);
  const [saving, setSaving]   = useState(false);
  const [error, setError]     = useState<string | null>(null);

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      if (role !== user.role)     await adminApi.users.setRole(user.id, role);
      if (active !== user.isActive) await adminApi.users.setStatus(user.id, active);
      onSaved();
    } catch (e: any) {
      setError(e.message ?? 'Failed to update user.');
    } finally {
      setSaving(false);
    }
  }

  const INPUT: React.CSSProperties = {
    border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
    fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
    background: '#fff', boxSizing: 'border-box', cursor: 'pointer',
  };

  return (
    <div style={{
      position: 'fixed', top: 0, right: 0, bottom: 0, width: 400,
      background: '#fff', borderLeft: `1px solid ${ND.border}`,
      boxShadow: '-8px 0 24px rgba(0,0,0,0.10)',
      padding: '32px 28px', display: 'flex', flexDirection: 'column', zIndex: 50,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <h2 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 20, color: '#000', margin: 0 }}>Edit User</h2>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2" strokeLinecap="round"><path d="M6 6l12 12M18 6L6 18" /></svg>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16, flex: 1 }}>
        <div>
          <div style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, color: ND.muted, marginBottom: 4 }}>Email</div>
          <div style={{ fontFamily: FONT, fontSize: 14, color: '#000' }}>{user.email}</div>
        </div>
        <div>
          <div style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, color: ND.muted, marginBottom: 4 }}>Joined</div>
          <div style={{ fontFamily: FONT, fontSize: 14, color: '#000' }}>{new Date(user.createdAt).toLocaleDateString()}</div>
        </div>
        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Role</label>
          <select style={INPUT} value={role} onChange={e => setRole(e.target.value as UserRole)}>
            <option value="Passenger">Passenger</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 8 }}>Status</label>
          <div style={{ display: 'inline-flex', background: '#f0f0f0', borderRadius: 99, padding: 3 }}>
            {[true, false].map(v => (
              <button key={String(v)} onClick={() => setActive(v)} style={{
                border: 'none', cursor: 'pointer',
                background: active === v ? '#000' : 'transparent',
                color: active === v ? '#fff' : '#000',
                fontFamily: FONT, fontWeight: 500, fontSize: 13,
                padding: '8px 20px', borderRadius: 99,
              }}>{v ? 'Active' : 'Inactive'}</button>
            ))}
          </div>
        </div>
        {error && <div style={{ color: ND.err, fontFamily: FONT, fontSize: 13 }}>{error}</div>}
      </div>

      <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
        <button onClick={onClose} style={{ flex: 1, background: ND.surface, border: `1px solid ${ND.border}`, color: '#000', borderRadius: 99, padding: '12px 20px', cursor: 'pointer', fontFamily: FONT, fontWeight: 500, fontSize: 14 }}>Cancel</button>
        <button onClick={handleSave} disabled={saving} style={{ flex: 1, background: '#000', border: 'none', color: '#fff', borderRadius: 99, padding: '12px 20px', cursor: saving ? 'not-allowed' : 'pointer', fontFamily: FONT, fontWeight: 600, fontSize: 14, opacity: saving ? 0.6 : 1 }}>
          {saving ? 'Saving…' : 'Save Changes'}
        </button>
      </div>
    </div>
  );
}
