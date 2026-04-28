import { useEffect, useState } from 'react';
import { adminApi } from '@shared/api';
import type { SupportTicketResponse, TicketStatus, TicketPriority } from '@shared/types';
import { Table, type ColDef } from '../ui/Table';
import { ticketStatusPill, ticketPriorityPill } from '../ui/StatusPill';
import { Card, SectionCard } from '../ui/Card';
import { Pagination } from '../ui/Pagination';
import { FONT, ND } from '@shared/tokens';

type StatusFilter = TicketStatus | '';
type PriorityFilter = TicketPriority | '';

const STATUS_OPTIONS: StatusFilter[] = ['', 'Open', 'InProgress', 'Resolved', 'Closed'];
const PRIORITY_OPTIONS: PriorityFilter[] = ['', 'Low', 'Medium', 'High', 'Critical'];

const COLS: ColDef<SupportTicketResponse>[] = [
  { key: 'subject',   header: 'Subject',     render: r => <span style={{ fontFamily: FONT, fontWeight: 500 }}>{r.subject}</span>, width: '25%' },
  { key: 'passenger', header: 'Passenger',   render: r => r.passengerName },
  { key: 'priority',  header: 'Priority',    render: r => ticketPriorityPill(r.priority) },
  { key: 'status',    header: 'Status',      render: r => ticketStatusPill(r.status) },
  { key: 'created',   header: 'Created',     render: r => new Date(r.createdAt).toLocaleDateString() },
  { key: 'resolved',  header: 'Resolved',    render: r => r.resolvedAt ? new Date(r.resolvedAt).toLocaleDateString() : '—' },
  { key: 'notes',     header: 'Admin Notes', render: r => <span style={{ fontSize: 12 }}>{r.adminNotes ?? '—'}</span>, width: '20%' },
];

const SELECT_STYLE: React.CSSProperties = {
  border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
  fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
  background: '#fff', cursor: 'pointer',
};

function TicketDetailPanel({
  ticket, onClose, onUpdated,
}: { ticket: SupportTicketResponse; onClose: () => void; onUpdated: (t: SupportTicketResponse) => void }) {
  const [status, setStatus] = useState<TicketStatus>(ticket.status);
  const [priority, setPriority] = useState<TicketPriority>(ticket.priority);
  const [adminNotes, setAdminNotes] = useState(ticket.adminNotes ?? '');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      let updated = ticket;
      if (status !== ticket.status || adminNotes !== (ticket.adminNotes ?? '')) {
        updated = await adminApi.tickets.updateStatus(ticket.id, status, adminNotes || null);
      }
      if (priority !== ticket.priority) {
        updated = await adminApi.tickets.updatePriority(ticket.id, priority);
      }
      onUpdated(updated);
      onClose();
    } catch (e: any) {
      setError(e.message ?? 'Failed to update ticket.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{
      position: 'fixed', top: 0, right: 0, bottom: 0, width: 460,
      background: '#fff', borderLeft: `1px solid ${ND.border}`,
      boxShadow: 'rgba(0,0,0,0.12) -8px 0 24px',
      padding: '32px 28px', display: 'flex', flexDirection: 'column', zIndex: 50,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <h2 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 18, color: '#000', margin: 0, flex: 1, marginRight: 16 }}>
          {ticket.subject}
        </h2>
        <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, flexShrink: 0 }}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2" strokeLinecap="round">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 16, flex: 1, overflowY: 'auto' }}>
        <div style={{ background: ND.surface, borderRadius: 8, padding: '14px 16px' }}>
          <div style={{ fontFamily: FONT, fontSize: 12, color: ND.muted, marginBottom: 4 }}>
            {ticket.passengerName} · {new Date(ticket.createdAt).toLocaleDateString()}
          </div>
          <div style={{ fontFamily: FONT, fontSize: 14, color: ND.body, lineHeight: 1.6 }}>{ticket.description}</div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Status</label>
            <select style={SELECT_STYLE} value={status} onChange={e => setStatus(e.target.value as TicketStatus)}>
              {(['Open', 'InProgress', 'Resolved', 'Closed'] as TicketStatus[]).map(s => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
          <div>
            <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Priority</label>
            <select style={SELECT_STYLE} value={priority} onChange={e => setPriority(e.target.value as TicketPriority)}>
              {(['Low', 'Medium', 'High', 'Critical'] as TicketPriority[]).map(p => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, display: 'block', marginBottom: 6 }}>Admin Notes</label>
          <textarea
            style={{
              border: `1px solid ${ND.border}`, borderRadius: 8, padding: '10px 12px',
              fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
              background: '#fff', resize: 'vertical', minHeight: 100, boxSizing: 'border-box',
            }}
            value={adminNotes}
            onChange={e => setAdminNotes(e.target.value)}
            placeholder="Internal notes visible only to admins…"
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
        }}>{saving ? 'Saving…' : 'Save Changes'}</button>
      </div>
    </div>
  );
}

export function Tickets() {
  const [items, setItems] = useState<SupportTicketResponse[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('');
  const [priorityFilter, setPriorityFilter] = useState<PriorityFilter>('');
  const [selected, setSelected] = useState<SupportTicketResponse | null>(null);
  const PAGE_SIZE = 20;

  useEffect(() => { setPage(1); }, [statusFilter, priorityFilter]);

  useEffect(() => {
    adminApi.tickets.list(page, PAGE_SIZE, statusFilter || undefined, priorityFilter || undefined)
      .then(r => { setItems(r.items); setTotal(r.totalCount); setTotalPages(r.totalPages); })
      .catch(console.error);
  }, [page, statusFilter, priorityFilter]);

  function handleUpdated(updated: SupportTicketResponse) {
    setItems(prev => prev.map(t => t.id === updated.id ? updated : t));
  }

  const open       = items.filter(t => t.status === 'Open').length;
  const inProgress = items.filter(t => t.status === 'InProgress').length;

  return (
    <div data-screen-label="Admin Support Tickets" style={{ position: 'relative' }}>
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Card label="Total (page)" value={items.length} />
        <Card label="Open"        value={open}       subColor="#1d4ed8" />
        <Card label="In Progress" value={inProgress} subColor="#ca8a04" />
        <Card label="Critical"    value={items.filter(t => t.priority === 'Critical').length} subColor="#dc2626" />
      </div>
      <SectionCard
        title={`Support Tickets — ${total} total`}
        action={
          <div style={{ display: 'flex', gap: 8 }}>
            <select
              value={statusFilter}
              onChange={e => setStatusFilter(e.target.value as StatusFilter)}
              style={{ fontFamily: FONT, fontSize: 12, padding: '4px 8px', borderRadius: 6, border: `1px solid ${ND.border}` }}
            >
              {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s || 'All statuses'}</option>)}
            </select>
            <select
              value={priorityFilter}
              onChange={e => setPriorityFilter(e.target.value as PriorityFilter)}
              style={{ fontFamily: FONT, fontSize: 12, padding: '4px 8px', borderRadius: 6, border: `1px solid ${ND.border}` }}
            >
              {PRIORITY_OPTIONS.map(p => <option key={p} value={p}>{p || 'All priorities'}</option>)}
            </select>
          </div>
        }
      >
        <Table
          cols={COLS} rows={items} rowKey={r => r.id}
          emptyText="No tickets found."
          onRowClick={setSelected}
        />
        <Pagination page={page} totalPages={totalPages} total={total} onPage={setPage} />
      </SectionCard>

      {selected && (
        <TicketDetailPanel
          ticket={selected}
          onClose={() => setSelected(null)}
          onUpdated={handleUpdated}
        />
      )}
    </div>
  );
}
