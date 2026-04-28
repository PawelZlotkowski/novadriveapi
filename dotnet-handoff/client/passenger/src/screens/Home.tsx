import { useEffect, useRef, useState, useCallback } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { passengerApi } from '@shared/api';
import type { VehicleType } from '@shared/types';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

// Howest campus / Kortrijk
const KORTRIJK: [number, number] = [3.2523, 50.8282];
const HOWEST:   [number, number] = [3.2458, 50.8218];

const ROUTE_SOURCE = 'route';
const ROUTE_LAYER  = 'route-line';

interface HomeProps {
  onBookRide: () => void;
}

export function Home({ onBookRide }: HomeProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef          = useRef<mapboxgl.Map | null>(null);
  const debounceRef     = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [from, setFrom]   = useState('Howest, Kortrijk');
  const [to, setTo]       = useState('');
  const [destCoords, setDestCoords] = useState<[number, number] | null>(null); // [lat, lng]
  const [vType, setVType] = useState<VehicleType>('Standard');
  const [booking, setBooking] = useState(false);
  const [error, setError]     = useState('');
  const [promoOpen, setPromoOpen]     = useState(false);
  const [promoCode, setPromoCode]     = useState('');
  const [promoState, setPromoState]   = useState<'idle' | 'checking' | 'valid' | 'invalid'>('idle');
  const [promoSaving, setPromoSaving] = useState(0); // discount amount
  const [appliedCode, setAppliedCode] = useState<string | null>(null);

  // Mapbox init
  useEffect(() => {
    if (!mapContainerRef.current) return;
    mapboxgl.accessToken = import.meta.env.VITE_MAPBOX_TOKEN;
    const map = new mapboxgl.Map({
      container: mapContainerRef.current,
      style: 'mapbox://styles/mapbox/dark-v11',
      center: KORTRIJK,
      zoom: 12,
      attributionControl: false,
      logoPosition: 'bottom-left',
    });
    mapRef.current = map;

    // Departure marker (green dot)
    const el = document.createElement('div');
    el.style.cssText = `width:12px;height:12px;border-radius:50%;background:${GREEN};border:2px solid #fff;box-shadow:0 0 8px ${GREEN}88;`;
    new mapboxgl.Marker({ element: el }).setLngLat(HOWEST).addTo(map);

    // Route source + layer (empty to start)
    map.on('load', () => {
      map.addSource(ROUTE_SOURCE, { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
      map.addLayer({
        id: ROUTE_LAYER,
        type: 'line',
        source: ROUTE_SOURCE,
        layout: { 'line-join': 'round', 'line-cap': 'round' },
        paint: { 'line-color': GREEN, 'line-width': 4, 'line-opacity': 0.85 },
      });
    });

    return () => { map.remove(); mapRef.current = null; };
  }, []);

  // Fetch route when destination changes (debounced geocode + directions)
  const fetchRoute = useCallback(async (destination: string) => {
    const map = mapRef.current;
    if (!map || !destination.trim()) {
      clearRoute(map);
      return;
    }

    try {
      // 1. Geocode destination with Mapbox Geocoding API
      const geoRes = await fetch(
        `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(destination)}.json` +
        `?proximity=${HOWEST[0]},${HOWEST[1]}&country=BE&limit=1&access_token=${mapboxgl.accessToken}`
      );
      const geoJson = await geoRes.json();
      const feature = geoJson.features?.[0];
      if (!feature) { clearRoute(map); return; }

      const [destLng, destLat] = feature.center as [number, number];
      setDestCoords([destLat, destLng]);

      // 2. Get driving route from Mapbox Directions API
      const dirRes = await fetch(
        `https://api.mapbox.com/directions/v5/mapbox/driving/` +
        `${HOWEST[0]},${HOWEST[1]};${destLng},${destLat}` +
        `?geometries=geojson&overview=full&access_token=${mapboxgl.accessToken}`
      );
      const dirJson = await dirRes.json();
      const route = dirJson.routes?.[0];
      if (!route) { clearRoute(map); return; }

      // 3. Draw route on map
      const src = map.getSource(ROUTE_SOURCE) as mapboxgl.GeoJSONSource | undefined;
      if (src) {
        src.setData({ type: 'Feature', geometry: route.geometry, properties: {} });
      }

      // 4. Fit map to route bounds
      const coords: [number, number][] = route.geometry.coordinates;
      const bounds = coords.reduce(
        (b, c) => b.extend(c),
        new mapboxgl.LngLatBounds(coords[0], coords[0])
      );
      map.fitBounds(bounds, { padding: 60, duration: 800 });

    } catch {
      clearRoute(map);
    }
  }, []);

  function clearRoute(map: mapboxgl.Map | null) {
    setDestCoords(null);
    if (!map) return;
    const src = map.getSource(ROUTE_SOURCE) as mapboxgl.GeoJSONSource | undefined;
    src?.setData({ type: 'FeatureCollection', features: [] });
    map.flyTo({ center: KORTRIJK, zoom: 12, duration: 600 });
  }

  function handleToChange(value: string) {
    setTo(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => fetchRoute(value), 700);
  }

  async function applyPromo() {
    if (!promoCode.trim()) return;
    setPromoState('checking');
    try {
      const result = await passengerApi.discountCodes.validate(promoCode.trim());
      if (result.isValid) {
        setPromoState('valid');
        setPromoSaving(result.discountAmount);
        setAppliedCode(promoCode.trim().toUpperCase());
      } else {
        setPromoState('invalid');
        setAppliedCode(null);
      }
    } catch {
      setPromoState('invalid');
      setAppliedCode(null);
    }
  }

  function clearPromo() {
    setPromoCode('');
    setPromoState('idle');
    setPromoSaving(0);
    setAppliedCode(null);
    setPromoOpen(false);
  }

  async function handleBook() {
    if (!to.trim()) { setError('Enter a destination'); return; }
    if (!destCoords) { setError('Could not geocode destination — try a more specific address'); return; }
    setError('');
    setBooking(true);
    try {
      await passengerApi.rides.book({
        departureAddress:     from,
        departureLatitude:    HOWEST[1],
        departureLongitude:   HOWEST[0],
        destinationAddress:   to,
        destinationLatitude:  destCoords[0],
        destinationLongitude: destCoords[1],
        vehicleType: vType,
        discountCode: appliedCode ?? undefined,
      });
      onBookRide();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Booking failed. Please try again.');
    } finally {
      setBooking(false);
    }
  }

  function swap() {
    const tmp = from;
    setFrom(to || 'Destination');
    setTo(tmp);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => fetchRoute(tmp), 700);
  }

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column', position: 'relative' }}>
      {/* Live Mapbox map */}
      <div ref={mapContainerRef} style={{ flex: 1, minHeight: 0, position: 'relative' }}>
        {/* NovaDrive badge overlay */}
        <div style={{
          position: 'absolute', top: 16, left: 16, zIndex: 10,
          background: 'rgba(0,0,0,0.6)',
          backdropFilter: 'blur(8px)',
          border: '1px solid rgba(255,255,255,0.1)',
          borderRadius: 20, padding: '6px 14px',
          display: 'flex', alignItems: 'center', gap: 6,
        }}>
          <div style={{ width: 8, height: 8, borderRadius: '50%', background: GREEN }}/>
          <span style={{ fontSize: 12, fontWeight: 600, color: '#fff', fontFamily: FONT }}>NovaDrive</span>
        </div>
      </div>

      {/* Bottom sheet */}
      <div style={{
        background: '#000',
        borderRadius: '20px 20px 0 0',
        padding: '12px 20px 28px',
        flexShrink: 0,
      }}>
        <div style={{ width: 36, height: 4, background: 'rgba(255,255,255,0.2)', borderRadius: 99, margin: '0 auto 18px' }}/>

        <div style={{ fontSize: 22, fontWeight: 700, color: '#fff', fontFamily: FONT, marginBottom: 16 }}>
          Where to?
        </div>

        {/* Route inputs */}
        <div style={{
          background: '#1a1a1a', borderRadius: 16,
          border: '1px solid rgba(255,255,255,0.07)', marginBottom: 16,
          overflow: 'hidden',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', padding: '13px 16px', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
            <div style={{ width: 10, height: 10, borderRadius: '50%', background: GREEN, flexShrink: 0, marginRight: 12 }}/>
            <input
              value={from}
              onChange={e => setFrom(e.target.value)}
              placeholder="Pickup location"
              style={{ flex: 1, background: 'none', border: 'none', outline: 'none', fontFamily: FONT, fontSize: 14, fontWeight: 500, color: '#fff' }}
            />
            <button
              onClick={swap}
              style={{
                background: 'rgba(255,255,255,0.1)', border: 'none', borderRadius: 8,
                width: 32, height: 32, display: 'flex', alignItems: 'center', justifyContent: 'center',
                cursor: 'pointer', color: '#fff', fontSize: 16, flexShrink: 0, marginLeft: 8,
              }}
            >⇅</button>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', padding: '13px 16px' }}>
            <div style={{ width: 10, height: 10, background: '#fff', flexShrink: 0, marginRight: 12, borderRadius: 2 }}/>
            <input
              value={to}
              onChange={e => handleToChange(e.target.value)}
              placeholder="Where to?"
              style={{
                flex: 1, background: 'none', border: 'none', outline: 'none',
                fontFamily: FONT, fontSize: 14, fontWeight: 500,
                color: to ? '#fff' : 'rgba(255,255,255,0.35)',
              }}
            />
          </div>
        </div>

        {/* Vehicle type chips */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
          {(['Standard', 'Van', 'Luxury'] as VehicleType[]).map(v => (
            <button
              key={v}
              onClick={() => setVType(v)}
              style={{
                flex: 1, padding: '9px 0', borderRadius: 999,
                background: vType === v ? GREEN : 'rgba(255,255,255,0.08)',
                border: 'none', cursor: 'pointer',
                fontFamily: FONT, fontSize: 12, fontWeight: 600,
                color: vType === v ? '#000' : 'rgba(255,255,255,0.6)',
              }}
            >
              {v}
            </button>
          ))}
        </div>

        {/* Promo code */}
        {!promoOpen ? (
          <button
            onClick={() => setPromoOpen(true)}
            style={{
              background: 'none', border: 'none', cursor: 'pointer', padding: '0 0 12px',
              fontFamily: FONT, fontSize: 12, color: 'rgba(255,255,255,0.45)',
              textDecoration: 'underline', textAlign: 'left',
            }}
          >Have a promo code?</button>
        ) : (
          <div style={{ marginBottom: 14 }}>
            <div style={{ display: 'flex', gap: 8, marginBottom: 6 }}>
              <input
                value={promoCode}
                onChange={e => { setPromoCode(e.target.value.toUpperCase()); setPromoState('idle'); setAppliedCode(null); }}
                placeholder="PROMO CODE"
                onKeyDown={e => e.key === 'Enter' && applyPromo()}
                style={{
                  flex: 1, background: '#1a1a1a', border: '1px solid rgba(255,255,255,0.12)',
                  borderRadius: 10, padding: '10px 14px', fontFamily: FONT, fontSize: 13,
                  fontWeight: 600, color: '#fff', outline: 'none', letterSpacing: '0.08em',
                }}
              />
              <button
                onClick={promoState === 'valid' ? clearPromo : applyPromo}
                disabled={promoState === 'checking'}
                style={{
                  background: promoState === 'valid' ? '#ef4444' : GREEN,
                  border: 'none', borderRadius: 10, padding: '10px 16px',
                  fontFamily: FONT, fontSize: 12, fontWeight: 700,
                  color: promoState === 'valid' ? '#fff' : '#000',
                  cursor: promoState === 'checking' ? 'default' : 'pointer',
                  opacity: promoState === 'checking' ? 0.6 : 1, flexShrink: 0,
                }}
              >
                {promoState === 'checking' ? '…' : promoState === 'valid' ? 'Remove' : 'Apply'}
              </button>
            </div>
            {promoState === 'valid' && (
              <div style={{ fontFamily: FONT, fontSize: 12, color: GREEN }}>
                ✓ {appliedCode} applied{promoSaving > 0 ? ` — save €${promoSaving.toFixed(2)}` : ''}
              </div>
            )}
            {promoState === 'invalid' && (
              <div style={{ fontFamily: FONT, fontSize: 12, color: '#ef4444' }}>
                Invalid or expired code
              </div>
            )}
          </div>
        )}

        {error && (
          <div style={{ fontSize: 12, color: '#ef4444', marginBottom: 10, fontFamily: FONT }}>
            {error}
          </div>
        )}

        <button
          onClick={handleBook}
          disabled={booking}
          style={{
            width: '100%', padding: '16px 0',
            background: booking ? 'rgba(34,197,94,0.5)' : GREEN,
            border: 'none', borderRadius: 999,
            fontFamily: FONT, fontSize: 16, fontWeight: 700, color: '#000',
            cursor: booking ? 'default' : 'pointer',
          }}
        >
          {booking ? 'Booking…' : 'Book Ride →'}
        </button>
      </div>
    </div>
  );
}
