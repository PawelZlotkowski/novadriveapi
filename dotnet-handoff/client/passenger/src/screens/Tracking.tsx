import { useEffect, useRef, useState, useCallback } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { passengerApi } from '@shared/api';
import type { RideResponse } from '@shared/types';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';
const KORTRIJK: [number, number] = [3.2523, 50.8282];

// ── Geo helpers ───────────────────────────────────────────────────────────────
function haversineKm(lat1: number, lng1: number, lat2: number, lng2: number): number {
  const R = 6371;
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function lerp(a: number, b: number, t: number) { return a + (b - a) * t; }

function estimateFare(ride: RideResponse): number {
  const dist = haversineKm(ride.departureLatitude, ride.departureLongitude, ride.destinationLatitude, ride.destinationLongitude);
  const duration = (dist / 30) * 60;
  const multiplier = ride.vehicleType === 'Van' ? 1.5 : ride.vehicleType === 'Luxury' ? 2.2 : 1.0;
  const base = (2.50 + dist * 1.10 + duration * 0.30) * multiplier;
  const hour = new Date().getHours();
  const subtotal = base + (hour >= 22 || hour < 6 ? base * 0.15 : 0);
  return Math.max(5.00, Math.round(subtotal * 1.21 * 100) / 100);
}

function etaMinutes(fromLat: number, fromLng: number, toLat: number, toLng: number, speedKmh: number): number {
  const dist = haversineKm(fromLat, fromLng, toLat, toLng);
  return Math.round((dist / Math.max(speedKmh, 10)) * 60);
}

// ── Map constants ─────────────────────────────────────────────────────────────
const ROUTE_SRC   = 'tracking-route';
const ROUTE_LAYER = 'tracking-route-line';

interface VehiclePos {
  latitude: number;
  longitude: number;
  speedKmh: number;
  batteryPct: number;
  rideStatus: string;
}

interface TrackingProps { onCancel: () => void; onComplete?: () => void; }

export function Tracking({ onCancel, onComplete }: TrackingProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef          = useRef<mapboxgl.Map | null>(null);
  const vehicleRef      = useRef<mapboxgl.Marker | null>(null);
  const pickupRef       = useRef<mapboxgl.Marker | null>(null);
  const destRef         = useRef<mapboxgl.Marker | null>(null);
  const mapReadyRef     = useRef(false);

  // Interpolation state — kept in refs so rAF closure always reads latest
  const fromPosRef      = useRef<[number, number] | null>(null); // [lng, lat]
  const toPosRef        = useRef<[number, number] | null>(null);
  const interpStartRef  = useRef<number>(0);
  const INTERP_MS       = 1800; // slightly shorter than poll interval for smooth arrivals
  const rafRef          = useRef<number>(0);
  const routeDrawnRef   = useRef('');

  const [ride, setRide]         = useState<RideResponse | null>(null);
  const [pos, setPos]           = useState<VehiclePos | null>(null);
  const [loading, setLoading]   = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const hadRideRef = useRef(false); // detect ride completion

  // ── Map init ──────────────────────────────────────────────────────────────
  useEffect(() => {
    if (!mapContainerRef.current) return;
    mapboxgl.accessToken = import.meta.env.VITE_MAPBOX_TOKEN;

    const map = new mapboxgl.Map({
      container: mapContainerRef.current,
      style: 'mapbox://styles/mapbox/dark-v11',
      center: KORTRIJK, zoom: 13,
      attributionControl: false,
    });
    mapRef.current = map;

    map.on('load', () => {
      map.addSource(ROUTE_SRC, { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
      map.addLayer({
        id: ROUTE_LAYER, type: 'line', source: ROUTE_SRC,
        layout: { 'line-join': 'round', 'line-cap': 'round' },
        paint: { 'line-color': GREEN, 'line-width': 4, 'line-opacity': 0.9, 'line-dasharray': [2, 1.5] },
      });

      // Vehicle marker
      const carEl = document.createElement('div');
      const style = document.createElement('style');
      style.textContent = `@keyframes nd-pulse { 0%,100%{box-shadow:0 0 20px ${GREEN}99,0 0 40px ${GREEN}44} 50%{box-shadow:0 0 30px ${GREEN}cc,0 0 60px ${GREEN}66} }`;
      document.head.appendChild(style);
      carEl.style.cssText = `width:22px;height:22px;border-radius:50%;background:${GREEN};border:3px solid #fff;box-shadow:0 0 20px ${GREEN}99,0 0 40px ${GREEN}44;animation:nd-pulse 1.4s ease-in-out infinite;`;
      vehicleRef.current = new mapboxgl.Marker({ element: carEl }).setLngLat(KORTRIJK).addTo(map);

      // Pickup marker
      const pickEl = document.createElement('div');
      pickEl.style.cssText = `width:14px;height:14px;border-radius:50%;background:${GREEN};border:3px solid #fff;box-shadow:0 0 10px ${GREEN}88;`;
      pickupRef.current = new mapboxgl.Marker({ element: pickEl }).setLngLat(KORTRIJK).addTo(map);
      pickupRef.current.getElement().style.display = 'none';

      // Destination marker
      const destEl = document.createElement('div');
      destEl.style.cssText = `width:14px;height:14px;background:#fff;border-radius:3px;border:2px solid rgba(255,255,255,0.5);`;
      destRef.current = new mapboxgl.Marker({ element: destEl }).setLngLat(KORTRIJK).addTo(map);
      destRef.current.getElement().style.display = 'none';

      mapReadyRef.current = true;
    });

    return () => {
      cancelAnimationFrame(rafRef.current);
      map.remove();
      mapRef.current = vehicleRef.current = pickupRef.current = destRef.current = null;
      mapReadyRef.current = false;
    };
  }, []);

  // ── Smooth interpolation loop (rAF) ───────────────────────────────────────
  // Runs continuously; when new GPS fix arrives, resets the tween from current
  // interpolated position to the new target — no visible jump.
  useEffect(() => {
    function frame(now: number) {
      rafRef.current = requestAnimationFrame(frame);
      if (!vehicleRef.current || !fromPosRef.current || !toPosRef.current) return;

      const elapsed = now - interpStartRef.current;
      const t       = Math.min(elapsed / INTERP_MS, 1);
      const eased   = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t; // ease-in-out

      const lng = lerp(fromPosRef.current[0], toPosRef.current[0], eased);
      const lat = lerp(fromPosRef.current[1], toPosRef.current[1], eased);
      vehicleRef.current.setLngLat([lng, lat]);
    }
    rafRef.current = requestAnimationFrame(frame);
    return () => cancelAnimationFrame(rafRef.current);
  }, []);

  // ── Fetch Mapbox route ────────────────────────────────────────────────────
  const drawRoute = useCallback(async (fromLng: number, fromLat: number, toLng: number, toLat: number) => {
    const map = mapRef.current;
    if (!map || !mapReadyRef.current) return;
    try {
      const res = await fetch(
        `https://api.mapbox.com/directions/v5/mapbox/driving/` +
        `${fromLng},${fromLat};${toLng},${toLat}` +
        `?geometries=geojson&overview=full&access_token=${mapboxgl.accessToken}`
      );
      const json = await res.json();
      const route = json.routes?.[0];
      if (!route) return;
      (map.getSource(ROUTE_SRC) as mapboxgl.GeoJSONSource | undefined)
        ?.setData({ type: 'Feature', geometry: route.geometry, properties: {} });
    } catch { /* silent */ }
  }, []);

  // ── Update markers + route when position changes ──────────────────────────
  useEffect(() => {
    if (!mapReadyRef.current || !pos || !ride) return;

    const vLng = pos.longitude;
    const vLat = pos.latitude;

    // Feed new GPS fix into interpolator — tween FROM current interpolated pos
    const cur = vehicleRef.current?.getLngLat();
    fromPosRef.current   = cur ? [cur.lng, cur.lat] : [vLng, vLat];
    toPosRef.current     = [vLng, vLat];
    interpStartRef.current = performance.now();

    const enRoute = pos.rideStatus === 'EnRoute' || ride.status === 'EnRoute';

    if (pickupRef.current) {
      pickupRef.current.setLngLat([ride.departureLongitude, ride.departureLatitude]);
      pickupRef.current.getElement().style.display = enRoute ? 'none' : 'block';
    }
    if (destRef.current) {
      destRef.current.setLngLat([ride.destinationLongitude, ride.destinationLatitude]);
      destRef.current.getElement().style.display = 'block';
    }

    // Re-fetch route only when vehicle moves noticeably (> ~22 m)
    const toLng = enRoute ? ride.destinationLongitude : ride.departureLongitude;
    const toLat = enRoute ? ride.destinationLatitude  : ride.departureLatitude;
    const routeKey = `${vLng.toFixed(3)},${vLat.toFixed(3)}-${toLng},${toLat}`;
    if (routeDrawnRef.current !== routeKey) {
      routeDrawnRef.current = routeKey;
      drawRoute(vLng, vLat, toLng, toLat);
    }

    // Keep vehicle in view
    const map = mapRef.current;
    if (map && map.getZoom() <= 15) {
      map.easeTo({ center: [vLng, vLat], duration: 600 });
    }
  }, [pos, ride, drawRoute]);

  // ── Poll active ride (8 s) ────────────────────────────────────────────────
  useEffect(() => {
    let alive = true;
    async function poll() {
      try {
        const r = await passengerApi.rides.active();
        if (!alive) return;
        setRide(r); setNotFound(false);
        hadRideRef.current = true;
      } catch {
        if (!alive) return;
        setNotFound(true);
        // If we previously had a ride and it's gone → completed → go to History
        if (hadRideRef.current && onComplete) {
          setTimeout(onComplete, 2500);
        }
      } finally {
        if (alive) setLoading(false);
      }
    }
    poll();
    const id = setInterval(poll, 8000);
    return () => { alive = false; clearInterval(id); };
  }, [onComplete]);

  // ── Poll vehicle position (2 s) ───────────────────────────────────────────
  useEffect(() => {
    let alive = true;
    async function poll() {
      try {
        const p = await passengerApi.rides.vehiclePosition();
        if (alive) setPos(p);
      } catch { /* no vehicle yet */ }
    }
    poll();
    const id = setInterval(poll, 2000);
    return () => { alive = false; clearInterval(id); };
  }, []);

  async function handleCancel() {
    if (!ride) { onCancel(); return; }
    setCancelling(true);
    try { await passengerApi.rides.cancel(ride.id); } catch { /* already done */ }
    onCancel();
  }

  // ── Derived display values ────────────────────────────────────────────────
  const enRoute     = pos?.rideStatus === 'EnRoute' || ride?.status === 'EnRoute';
  const statusLabel = enRoute ? 'En Route' : 'Finding Driver';
  const statusColor = enRoute ? '#f59e0b' : GREEN;

  // Use interpolated marker position for ETA/distance so UI updates every frame — but
  // that would cause constant re-renders. Use pos instead; UI updates on each poll.
  const etaMin = pos && ride
    ? etaMinutes(pos.latitude, pos.longitude,
        enRoute ? ride.destinationLatitude  : ride.departureLatitude,
        enRoute ? ride.destinationLongitude : ride.departureLongitude,
        pos.speedKmh)
    : null;

  const distKm = pos && ride
    ? Math.round(haversineKm(
        pos.latitude, pos.longitude,
        enRoute ? ride.destinationLatitude  : ride.departureLatitude,
        enRoute ? ride.destinationLongitude : ride.departureLongitude,
      ) * 10) / 10
    : null;

  const fareDisplay = ride
    ? (ride.finalPrice != null
        ? { label: 'Final fare',     value: `€${ride.finalPrice.toFixed(2)}` }
        : { label: 'Estimated fare', value: `€${estimateFare(ride).toFixed(2)}` })
    : null;

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column', position: 'relative' }}>
      <div ref={mapContainerRef} style={{ flex: 1, minHeight: 0 }} />

      {/* Top status bar */}
      <div style={{
        position: 'absolute', top: 12, left: 16, right: 16,
        background: 'rgba(10,10,10,0.82)', backdropFilter: 'blur(16px)',
        border: '1px solid rgba(255,255,255,0.08)',
        borderRadius: 20, padding: '12px 16px',
        display: 'flex', alignItems: 'center', gap: 10, zIndex: 10,
      }}>
        {loading ? (
          <span style={{ fontSize: 13, color: 'rgba(255,255,255,0.5)', fontFamily: FONT, flex: 1 }}>Checking…</span>
        ) : notFound ? (
          <span style={{ fontSize: 13, color: 'rgba(255,255,255,0.5)', fontFamily: FONT, flex: 1 }}>No active ride</span>
        ) : ride ? (
          <>
            <div style={{
              background: statusColor + '22', border: `1px solid ${statusColor}55`,
              borderRadius: 999, padding: '5px 12px',
              fontSize: 11, fontWeight: 700, color: statusColor, fontFamily: FONT, flexShrink: 0,
            }}>{statusLabel}</div>
            <span style={{ fontSize: 13, fontWeight: 600, color: '#fff', fontFamily: FONT, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {ride.vehicleModel ?? 'Vehicle assigned'}
            </span>
            {etaMin !== null && (
              <div style={{ background: 'rgba(255,255,255,0.08)', borderRadius: 10, padding: '4px 10px', flexShrink: 0 }}>
                <span style={{ fontSize: 12, fontWeight: 700, color: '#fff', fontFamily: FONT }}>{etaMin} min</span>
              </div>
            )}
          </>
        ) : null}
        <button onClick={handleCancel} disabled={cancelling} style={{
          background: 'rgba(239,68,68,0.15)', border: '1px solid rgba(239,68,68,0.3)',
          borderRadius: 12, padding: '7px 14px',
          fontFamily: FONT, fontSize: 12, fontWeight: 600, color: '#ef4444',
          cursor: cancelling ? 'default' : 'pointer', flexShrink: 0,
        }}>
          {cancelling ? '…' : ride ? 'Cancel' : '← Back'}
        </button>
      </div>

      {/* Bottom sheet */}
      <div style={{
        position: 'absolute', bottom: 0, left: 0, right: 0,
        background: '#111', borderRadius: '20px 20px 0 0',
        border: '1px solid rgba(255,255,255,0.07)',
        padding: '12px 20px 32px', zIndex: 10,
      }}>
        <div style={{ width: 36, height: 4, background: 'rgba(255,255,255,0.15)', borderRadius: 99, margin: '0 auto 16px' }} />

        {loading ? (
          <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.4)', fontFamily: FONT, textAlign: 'center', padding: '16px 0' }}>Loading…</div>
        ) : notFound ? (
          <div style={{ textAlign: 'center', padding: '8px 0' }}>
            <div style={{ fontSize: 15, fontWeight: 600, color: '#fff', fontFamily: FONT, marginBottom: 6 }}>No active ride</div>
            <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.4)', fontFamily: FONT, marginBottom: 20 }}>Book a ride from the Home tab.</div>
            <button onClick={onCancel} style={{ background: GREEN, border: 'none', borderRadius: 999, padding: '13px 32px', fontFamily: FONT, fontSize: 14, fontWeight: 700, color: '#000', cursor: 'pointer' }}>← Book a Ride</button>
          </div>
        ) : ride ? (
          <>
            {/* Vehicle card */}
            <div style={{ background: 'rgba(255,255,255,0.05)', borderRadius: 14, padding: '14px 16px', marginBottom: 12, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontSize: 14, fontWeight: 700, color: '#fff', fontFamily: FONT }}>{ride.vehicleModel ?? 'Assigned Vehicle'}</div>
                <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.45)', fontFamily: FONT, marginTop: 2 }}>
                  {ride.vehicleLicensePlate ?? '—'}
                  {pos && <span style={{ marginLeft: 8, color: 'rgba(255,255,255,0.3)' }}>· {Math.round(pos.speedKmh)} km/h · 🔋 {Math.round(pos.batteryPct)}%</span>}
                </div>
              </div>
              {etaMin !== null && (
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: 20, fontWeight: 700, color: GREEN, fontFamily: FONT }}>{etaMin} min</div>
                  <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.4)', fontFamily: FONT }}>{enRoute ? 'to destination' : 'to pickup'}</div>
                </div>
              )}
            </div>

            {/* Distance */}
            {distKm !== null && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, background: 'rgba(255,255,255,0.04)', borderRadius: 10, padding: '8px 14px', marginBottom: 12 }}>
                <div style={{ width: 6, height: 6, borderRadius: '50%', background: statusColor, flexShrink: 0 }} />
                <span style={{ fontSize: 12, color: 'rgba(255,255,255,0.6)', fontFamily: FONT, flex: 1 }}>
                  {enRoute ? `${distKm} km remaining to destination` : `Vehicle is ${distKm} km from your pickup`}
                </span>
              </div>
            )}

            {/* Route stops */}
            <div style={{ marginBottom: 14 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
                <div style={{ width: 8, height: 8, borderRadius: '50%', background: GREEN, flexShrink: 0 }} />
                <span style={{ fontSize: 13, color: 'rgba(255,255,255,0.7)', fontFamily: FONT, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{ride.departureAddress}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{ width: 8, height: 8, background: '#fff', flexShrink: 0, borderRadius: 2 }} />
                <span style={{ fontSize: 13, color: 'rgba(255,255,255,0.7)', fontFamily: FONT, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{ride.destinationAddress}</span>
              </div>
            </div>

            {/* Fare */}
            {fareDisplay && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.35)', fontFamily: FONT, marginBottom: 2 }}>{fareDisplay.label}</div>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#fff', fontFamily: FONT }}>{fareDisplay.value}</div>
                </div>
                <div style={{ background: `${GREEN}15`, border: `1px solid ${GREEN}30`, borderRadius: 999, padding: '6px 14px' }}>
                  <span style={{ fontSize: 12, fontWeight: 600, color: GREEN, fontFamily: FONT }}>+ pts earned</span>
                </div>
              </div>
            )}
          </>
        ) : null}
      </div>
    </div>
  );
}
