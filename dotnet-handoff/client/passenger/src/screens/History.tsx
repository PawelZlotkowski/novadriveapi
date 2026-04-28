import { useEffect, useState } from 'react';
import { passengerApi } from '@shared/api';
import type { RideResponse, RideStatus } from '@shared/types';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

type Filter = 'All' | RideStatus;

const BADGE: Record<RideStatus, { bg: string; color: string; label: string }> = {
  Requested: { bg: '#dbeafe', color: '#1d4ed8', label: 'Requested' },
  EnRoute:   { bg: '#fef3c7', color: '#b45309', label: 'En Route'  },
  Completed: { bg: '#dcfce7', color: '#15803d', label: 'Completed' },
  Cancelled: { bg: '#fee2e2', color: '#b91c1c', label: 'Cancelled' },
};

const FILTERS: Filter[] = ['All', 'Completed', 'Cancelled', 'EnRoute'];
const FILTER_LABEL: Partial<Record<Filter, string>> = { EnRoute: 'En Route' };

interface HistoryProps {
  onTrack: () => void;
}

export function History({ onTrack }: HistoryProps) {
  const [items, setItems]     = useState<RideResponse[]>([]);
  const [filter, setFilter]   = useState<Filter>('All');
  const [page, setPage]       = useState(1);
  const [totalPages, setTotal]= useState(1);
  const [loading, setLoading] = useState(true);
  const PAGE_SIZE = 10;

  useEffect(() => {
    setLoading(true);
    passengerApi.rides.history(page, PAGE_SIZE)
      .then(r => { setItems(r.items); setTotal(r.totalPages); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [page]);

  const visible = filter === 'All' ? items : items.filter(r => r.status === filter);

  return (
    <div style={{ background: '#f8f8f8', minHeight: '100%', fontFamily: FONT }}>
      {/* Header */}
      <div style={{
        background: '#fff',
        padding: '20px 20px 0',
        borderBottom: '1px solid #f0f0f0',
      }}>
        <div style={{ fontSize: 24, fontWeight: 700, color: '#000', marginBottom: 16 }}>My Rides</div>

        {/* Filter chips */}
        <div style={{ display: 'flex', gap: 8, overflowX: 'auto', paddingBottom: 14 }}>
          {FILTERS.map(f => {
            const active = filter === f;
            return (
              <button
                key={f}
                onClick={() => setFilter(f)}
                style={{
                  padding: '7px 16px', borderRadius: 999, border: 'none',
                  background: active ? '#000' : '#f0f0f0',
                  color: active ? '#fff' : '#555',
                  fontFamily: FONT, fontSize: 12, fontWeight: 600,
                  cursor: 'pointer', flexShrink: 0,
                  whiteSpace: 'nowrap',
                }}
              >
                {FILTER_LABEL[f] ?? f}
              </button>
            );
          })}
        </div>
      </div>

      {/* List */}
      <div style={{ padding: '16px 16px 24px' }}>
        {loading ? (
          <div style={{ fontSize: 13, color: '#afafaf', textAlign: 'center', padding: '32px 0' }}>Loading…</div>
        ) : visible.length === 0 ? (
          <div style={{ fontSize: 13, color: '#afafaf', textAlign: 'center', padding: '32px 0' }}>No rides found.</div>
        ) : (
          visible.map(ride => {
            const badge = BADGE[ride.status] ?? BADGE.Cancelled;
            return (
              <div
                key={ride.id}
                style={{
                  background: '#fff', borderRadius: 16,
                  padding: '16px', marginBottom: 10,
                  boxShadow: '0 1px 4px rgba(0,0,0,0.06)',
                }}
              >
                {/* Route */}
                <div style={{ marginBottom: 10 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 7 }}>
                    <div style={{ width: 8, height: 8, borderRadius: '50%', background: GREEN, flexShrink: 0 }}/>
                    <span style={{ fontSize: 13, fontWeight: 600, color: '#000' }}>{ride.departureAddress}</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{ width: 8, height: 8, background: '#000', flexShrink: 0, borderRadius: 2 }}/>
                    <span style={{ fontSize: 13, color: '#555' }}>{ride.destinationAddress}</span>
                  </div>
                </div>

                {/* Meta row */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap', marginBottom: 10 }}>
                  {ride.vehicleModel && (
                    <span style={{ fontSize: 11, color: '#afafaf' }}>{ride.vehicleModel}</span>
                  )}
                  <span style={{ fontSize: 11, color: '#afafaf' }}>·</span>
                  <span style={{ fontSize: 11, color: '#afafaf' }}>
                    {new Date(ride.requestedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                  </span>
                </div>

                {/* Bottom row: price + badge + actions */}
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    {ride.finalPrice != null && (
                      <span style={{ fontSize: 15, fontWeight: 700, color: '#000' }}>€{ride.finalPrice.toFixed(2)}</span>
                    )}
                    <span style={{
                      fontSize: 11, fontWeight: 600, padding: '3px 10px', borderRadius: 999,
                      background: badge.bg, color: badge.color,
                    }}>
                      {badge.label}
                    </span>
                  </div>

                  <div style={{ display: 'flex', gap: 8 }}>
                    {ride.status === 'Completed' && (
                      <button
                        onClick={async () => {
                          try {
                            const blob = await passengerApi.rides.invoice(ride.id);
                            const url  = URL.createObjectURL(blob);
                            const a    = document.createElement('a');
                            a.href     = url;
                            a.download = `NovaDrive-Invoice-${ride.id.slice(0, 8)}.pdf`;
                            a.click();
                            URL.revokeObjectURL(url);
                          } catch { /* ignore */ }
                        }}
                        style={{
                          background: 'none', border: 'none', padding: 0,
                          fontSize: 12, color: GREEN, fontFamily: FONT, fontWeight: 600,
                          cursor: 'pointer', textDecoration: 'underline',
                        }}
                      >
                        Invoice ↓
                      </button>
                    )}
                    {(ride.status === 'EnRoute' || ride.status === 'Requested') && (
                      <button
                        onClick={onTrack}
                        style={{
                          background: GREEN, border: 'none', borderRadius: 999,
                          padding: '6px 14px',
                          fontFamily: FONT, fontSize: 12, fontWeight: 700, color: '#000',
                          cursor: 'pointer',
                        }}
                      >
                        Track →
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 10, marginTop: 8 }}>
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page <= 1}
              style={{
                padding: '8px 18px', borderRadius: 999, border: '1px solid #e5e7eb',
                background: '#fff', fontFamily: FONT, fontSize: 13, cursor: page <= 1 ? 'default' : 'pointer',
                color: page <= 1 ? '#afafaf' : '#000',
              }}
            >←</button>
            <span style={{ fontSize: 13, color: '#afafaf' }}>{page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              style={{
                padding: '8px 18px', borderRadius: 999, border: '1px solid #e5e7eb',
                background: '#fff', fontFamily: FONT, fontSize: 13, cursor: page >= totalPages ? 'default' : 'pointer',
                color: page >= totalPages ? '#afafaf' : '#000',
              }}
            >→</button>
          </div>
        )}
      </div>
    </div>
  );
}
