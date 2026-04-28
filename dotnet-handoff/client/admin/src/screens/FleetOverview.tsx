import { useEffect, useRef, useState } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { ND, FONT } from '@shared/tokens';
import { adminApi } from '@shared/api';
import type { VehicleResponse, FleetStatsResponse } from '@shared/types';

mapboxgl.accessToken = import.meta.env.VITE_MAPBOX_TOKEN as string;

const KORTRIJK: [number, number] = [3.2523, 50.8282];

export function FleetOverview() {
  const [vehicles, setVehicles]     = useState<VehicleResponse[]>([]);
  const [stats, setStats]           = useState<FleetStatsResponse | null>(null);
  const [openTickets, setOpenTickets] = useState<number>(0);
  const [error, setError]           = useState<string | null>(null);
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef          = useRef<mapboxgl.Map | null>(null);
  const markersRef      = useRef<mapboxgl.Marker[]>([]);

  useEffect(() => {
    adminApi.vehicles.list(1, 100)
      .then(r => setVehicles(r.items))
      .catch(e => setError(apiError(e)));

    adminApi.vehicles.stats()
      .then(setStats)
      .catch(console.error);

    adminApi.tickets.list(1, 1, 'Open')
      .then(r => setOpenTickets(r.totalCount))
      .catch(console.error);
  }, []);

  // Init Mapbox
  useEffect(() => {
    if (!mapContainerRef.current) return;
    const map = new mapboxgl.Map({
      container: mapContainerRef.current,
      style: 'mapbox://styles/mapbox/dark-v11',
      center: KORTRIJK,
      zoom: 11,
    });
    map.addControl(new mapboxgl.NavigationControl(), 'top-right');
    mapRef.current = map;
    return () => { map.remove(); mapRef.current = null; };
  }, []);

  // Vehicle markers
  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    markersRef.current.forEach(m => m.remove());
    markersRef.current = [];

    vehicles.forEach(v => {
      if (v.currentLatitude == null || v.currentLongitude == null) return;
      const color = v.isActive
        ? v.vehicleType === 'Luxury' ? '#f5c518'
        : v.vehicleType === 'Van'    ? '#3b82f6'
        : '#22c55e'
        : '#6b7280';

      const el = document.createElement('div');
      el.style.cssText = `width:14px;height:14px;border-radius:50%;background:${color};border:2px solid #fff;box-shadow:0 0 6px ${color}88;cursor:pointer;`;

      const popup = new mapboxgl.Popup({ offset: 16, closeButton: false }).setHTML(`
        <div style="font-family:Inter,sans-serif;font-size:13px;line-height:1.6">
          <strong>${v.model}</strong><br/>
          ${v.licensePlate} &middot; ${v.vehicleType}<br/>
          ${v.currentBatteryPercentage != null ? `🔋 ${v.currentBatteryPercentage}%` : ''}
          ${v.currentMileage != null ? ` &middot; ${v.currentMileage.toLocaleString()} km` : ''}
        </div>
      `);

      const marker = new mapboxgl.Marker({ element: el })
        .setLngLat([v.currentLongitude, v.currentLatitude])
        .setPopup(popup)
        .addTo(map);
      markersRef.current.push(marker);
    });
  }, [vehicles]);

  return (
    <div data-screen-label="Admin Fleet Overview">
      {error && <ErrorBanner message={error} />}

      {/* Stats row */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <Stat label="Total Vehicles"    value={String(stats?.totalVehicles ?? vehicles.length)} sub="in fleet" />
        <Stat label="Active"            value={String(stats?.activeVehicles ?? '—')}             sub="on road"    subColor="#22c55e" />
        <Stat label="Avg Battery"       value={stats ? `${stats.averageBatteryPercentage.toFixed(0)}%` : '—'} sub="active vehicles" subColor="#3b82f6" />
        <Stat label="Open Tickets"      value={String(openTickets)}                              sub="unresolved" subColor="#f59e0b" />
      </div>

      {/* Map + list */}
      <div style={{ display: 'flex', gap: 24 }}>
        <div
          ref={mapContainerRef}
          style={{ flex: 1, height: 600, borderRadius: 12, overflow: 'hidden', border: `1px solid ${ND.border}` }}
        />
        <aside style={{ width: 280, border: `1px solid ${ND.border}`, borderRadius: 12, background: '#fff', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
          <div style={{ padding: '14px 18px', borderBottom: `1px solid ${ND.border}`, fontFamily: FONT, fontWeight: 600, fontSize: 14, flexShrink: 0 }}>
            Vehicle List ({vehicles.length})
          </div>
          <div style={{ overflowY: 'auto', maxHeight: 554 }}>
            {vehicles.length === 0 && !error && (
              <div style={{ padding: '20px 18px', fontFamily: FONT, fontSize: 13, color: ND.muted }}>No vehicles loaded.</div>
            )}
            {vehicles.map(v => (
              <div
                key={v.id}
                style={{ padding: '10px 18px', borderBottom: '1px solid #f5f5f5', display: 'flex', justifyContent: 'space-between', alignItems: 'center', cursor: 'pointer' }}
                onClick={() => {
                  if (v.currentLatitude != null && v.currentLongitude != null && mapRef.current) {
                    mapRef.current.flyTo({ center: [v.currentLongitude, v.currentLatitude], zoom: 14, speed: 1.4 });
                  }
                }}
              >
                <div>
                  <div style={{ fontFamily: FONT, fontSize: 13, fontWeight: 500 }}>{v.model}</div>
                  <div style={{ fontFamily: FONT, fontSize: 12, color: ND.muted }}>{v.licensePlate}</div>
                </div>
                <span style={{
                  fontFamily: FONT, fontSize: 11, padding: '2px 8px', borderRadius: 99,
                  background: v.isActive ? '#dcfce7' : '#f3f4f6',
                  color: v.isActive ? '#16a34a' : '#6b7280',
                }}>
                  {v.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            ))}
          </div>
        </aside>
      </div>
    </div>
  );
}

function Stat({ label, value, sub, subColor }: { label: string; value: string; sub: string; subColor?: string }) {
  return (
    <div style={{ flex: 1, border: `1px solid ${ND.border}`, borderRadius: 12, padding: '20px 24px' }}>
      <div style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, color: ND.muted }}>{label}</div>
      <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: 36, color: '#000', marginTop: 4, letterSpacing: -0.5 }}>{value}</div>
      <div style={{ fontFamily: FONT, fontSize: 13, color: subColor ?? ND.muted, marginTop: 6 }}>{sub}</div>
    </div>
  );
}

function apiError(e: unknown): string {
  const msg = e instanceof Error ? e.message : String(e);
  if (msg.includes('403')) return '403 Forbidden — your token is missing the manage:admin permission. Complete Auth0 RBAC setup: create an "Admin" role, assign manage:admin permission, then assign the role to your user.';
  if (msg.includes('401')) return '401 Unauthorized — session expired. Sign out and sign in again.';
  return msg;
}

function ErrorBanner({ message }: { message: string }) {
  return (
    <div style={{
      background: '#fef2f2', border: '1px solid #fecaca', borderRadius: 10,
      padding: '12px 16px', marginBottom: 20,
      fontFamily: FONT, fontSize: 13, color: '#b91c1c', lineHeight: 1.5,
    }}>
      ⚠ {message}
    </div>
  );
}
