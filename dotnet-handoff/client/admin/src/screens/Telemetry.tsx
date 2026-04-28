import { useEffect, useRef, useState } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { ND, FONT } from '@shared/tokens';
import { adminApi } from '@shared/api';
import type { TelemetryResponse, VehicleResponse } from '@shared/types';

mapboxgl.accessToken = import.meta.env.VITE_MAPBOX_TOKEN as string;

export function Telemetry() {
  const [vehicles, setVehicles] = useState<VehicleResponse[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [latest, setLatest] = useState<TelemetryResponse | null>(null);
  const [history, setHistory] = useState<TelemetryResponse[]>([]);

  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef          = useRef<mapboxgl.Map | null>(null);
  const markerRef       = useRef<mapboxgl.Marker | null>(null);

  // Load vehicles list
  useEffect(() => {
    adminApi.vehicles.list(1, 100).then(r => {
      setVehicles(r.items);
      if (r.items.length > 0) setSelectedId(r.items[0].id);
    }).catch(console.error);
  }, []);

  // Poll latest telemetry every 2 s for the selected vehicle
  useEffect(() => {
    if (!selectedId) return;
    let alive = true;
    const poll = () =>
      adminApi.telemetry.latest(selectedId).then(d => { if (alive) setLatest(d); }).catch(() => {});
    poll();
    const id = setInterval(poll, 2000);
    return () => { alive = false; clearInterval(id); };
  }, [selectedId]);

  // Reload history trail every 30 s
  useEffect(() => {
    if (!selectedId) return;
    let alive = true;
    const load = () => {
      const to   = new Date().toISOString();
      const from = new Date(Date.now() - 60 * 60 * 1000).toISOString();
      adminApi.telemetry.history(selectedId, from, to).then(d => { if (alive) setHistory(d); }).catch(() => {});
    };
    load();
    const id = setInterval(load, 30_000);
    return () => { alive = false; clearInterval(id); };
  }, [selectedId]);

  // Init map
  useEffect(() => {
    if (!mapContainerRef.current) return;
    const map = new mapboxgl.Map({
      container: mapContainerRef.current,
      style: 'mapbox://styles/mapbox/dark-v11',
      center: [3.2523, 50.8282], // Kortrijk
      zoom: 12,
    });
    map.addControl(new mapboxgl.NavigationControl(), 'top-right');
    mapRef.current = map;

    map.on('load', () => {
      map.addSource('trail', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } });
      map.addLayer({
        id: 'trail-line',
        type: 'line',
        source: 'trail',
        paint: { 'line-color': '#3b82f6', 'line-width': 3, 'line-opacity': 0.8 },
      });
    });

    return () => { map.remove(); mapRef.current = null; };
  }, []);

  // Update marker + trail when telemetry loads
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !latest) return;

    // Marker
    if (markerRef.current) markerRef.current.remove();
    const el = document.createElement('div');
    el.style.cssText = 'width:16px;height:16px;border-radius:50%;background:#3b82f6;border:2px solid #fff;box-shadow:0 0 8px #3b82f688';
    markerRef.current = new mapboxgl.Marker({ element: el })
      .setLngLat([latest.longitude, latest.latitude])
      .setPopup(new mapboxgl.Popup({ offset: 16, closeButton: false })
        .setHTML(`<div style="font-family:Inter,sans-serif;font-size:13px">${latest.speedKmh} km/h · ${latest.batteryPercentage}% · ${latest.hardwareTemperatureCelsius}°C</div>`)
      )
      .addTo(map);

    map.flyTo({ center: [latest.longitude, latest.latitude], zoom: 13, speed: 1.2 });
  }, [latest]);

  // Update trail when history loads
  useEffect(() => {
    const map = mapRef.current;
    if (!map || history.length === 0) return;
    const updateTrail = () => {
      const source = map.getSource('trail') as mapboxgl.GeoJSONSource | undefined;
      if (!source) return;
      source.setData({
        type: 'FeatureCollection',
        features: [{
          type: 'Feature',
          geometry: {
            type: 'LineString',
            coordinates: history.map(t => [t.longitude, t.latitude]),
          },
          properties: {},
        }],
      });
    };
    if (map.isStyleLoaded()) updateTrail(); else map.once('load', updateTrail);
  }, [history]);

  const selected = vehicles.find(v => v.id === selectedId);

  return (
    <div data-screen-label="Admin Telemetry">
      {/* Header + vehicle picker */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 20 }}>
        <h2 style={{ fontFamily: FONT, fontSize: 18, margin: 0 }}>Telemetry</h2>
        <select
          value={selectedId}
          onChange={e => setSelectedId(e.target.value)}
          style={{ fontFamily: FONT, fontSize: 13, padding: '6px 10px', borderRadius: 8, border: `1px solid ${ND.border}` }}
        >
          {vehicles.map(v => (
            <option key={v.id} value={v.id}>{v.model} — {v.licensePlate}</option>
          ))}
        </select>
      </div>

      {/* Stats */}
      {latest && (
        <div style={{ display: 'flex', gap: 12, marginBottom: 20 }}>
          {[
            { label: 'Speed',       value: `${latest.speedKmh} km/h`          },
            { label: 'Battery',     value: `${latest.batteryPercentage}%`      },
            { label: 'Temp',        value: `${latest.hardwareTemperatureCelsius}°C` },
            { label: 'Last seen',   value: new Date(latest.timestamp).toLocaleTimeString() },
          ].map(s => (
            <div key={s.label} style={{ flex: 1, border: `1px solid ${ND.border}`, borderRadius: 10, padding: '14px 18px' }}>
              <div style={{ fontFamily: FONT, fontSize: 12, color: ND.muted }}>{s.label}</div>
              <div style={{ fontFamily: FONT, fontSize: 22, fontWeight: 700, marginTop: 2 }}>{s.value}</div>
            </div>
          ))}
        </div>
      )}

      {/* Map */}
      <div ref={mapContainerRef} style={{ width: '100%', height: 400, borderRadius: 12, overflow: 'hidden', border: `1px solid ${ND.border}` }} />
      {selected && (
        <p style={{ fontFamily: FONT, fontSize: 12, color: ND.muted, marginTop: 8 }}>
          Blue line = last hour of travel · Dot = current position · Vehicle: {selected.model} ({selected.vin})
        </p>
      )}
    </div>
  );
}

