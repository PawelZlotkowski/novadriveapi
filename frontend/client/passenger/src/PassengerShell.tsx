import type { ReactNode } from 'react';

export type TabId = 'home' | 'rides' | 'history' | 'profile';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

interface ShellProps {
  active: TabId;
  onTab: (t: TabId) => void;
  children: ReactNode;
  hideNav?: boolean;
}

export function PassengerShell({ active, onTab, children, hideNav = false }: ShellProps) {
  const isDark = active === 'home' || active === 'rides';
  const ink    = isDark ? '#fff' : '#000';
  const dim    = isDark ? 'rgba(255,255,255,0.3)' : '#afafaf';

  return (
    <div style={{
      width: 393, height: 852,
      background: isDark ? '#000' : '#f8f8f8',
      borderRadius: 48, overflow: 'hidden',
      display: 'flex', flexDirection: 'column',
      boxShadow: '0 24px 64px rgba(0,0,0,0.28)',
      fontFamily: FONT,
      transform: 'translateZ(0)',
    }}>
      {/* iOS status bar */}
      <div style={{
        height: 44, flexShrink: 0,
        background: isDark ? '#000' : '#fff',
        display: 'flex', alignItems: 'center',
        padding: '0 28px', justifyContent: 'space-between',
        position: 'relative', zIndex: 20,
      }}>
        <span style={{ fontSize: 15, fontWeight: 600, color: ink }}>9:41</span>
        <div style={{ display: 'flex', gap: 5, alignItems: 'center' }}>
          <svg width="17" height="12" viewBox="0 0 17 12">
            <rect x="0"    y="6"  width="3" height="6"  rx="1" fill={ink} opacity="0.35"/>
            <rect x="4.5"  y="4"  width="3" height="8"  rx="1" fill={ink} opacity="0.6"/>
            <rect x="9"    y="2"  width="3" height="10" rx="1" fill={ink}/>
            <rect x="13.5" y="0"  width="3" height="12" rx="1" fill={ink}/>
          </svg>
          <svg width="16" height="12" viewBox="0 0 16 12" fill="none">
            <path d="M8 9a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z" fill={ink}/>
            <path d="M8 6c-1.5 0-2.8.6-3.8 1.5L3 6.3A7 7 0 0 1 8 4.5c2 0 3.8.8 5 2L11.8 7.5C10.8 6.6 9.5 6 8 6Z" fill={ink} opacity="0.6"/>
            <path d="M8 3C5.2 3 2.7 4 .9 5.8L0 4.9A10 10 0 0 1 8 1.5c3.2 0 6 1.3 8 3.4l-.9.9A8.5 8.5 0 0 0 8 3Z" fill={ink} opacity="0.35"/>
          </svg>
          <svg width="25" height="12" viewBox="0 0 25 12" fill="none">
            <rect x="0.5" y="0.5" width="21" height="11" rx="3.5" stroke={ink} strokeOpacity="0.35"/>
            <rect x="2"   y="2"   width="16" height="8" rx="2" fill={ink}/>
            <path d="M23 4.5v3C23.8 7.2 24.5 6.7 24.5 6c0-.7-.7-1.2-1.5-1.5Z" fill={ink} fillOpacity="0.4"/>
          </svg>
        </div>
      </div>

      {/* Content */}
      <div style={{
        flex: 1,
        overflow: isDark ? 'hidden' : 'auto',
        position: 'relative',
      }}>
        {children}
      </div>

      {/* Bottom tab bar — hidden on pre-auth screens */}
      {!hideNav && (
        <div style={{
          height: 83, flexShrink: 0,
          background: isDark ? '#111' : '#fff',
          borderTop: isDark ? '1px solid rgba(255,255,255,0.07)' : '1px solid #f0f0f0',
          display: 'flex', alignItems: 'flex-start', paddingTop: 10,
        }}>
          {([
            { id: 'home'    as TabId, label: 'Home',    glyph: '⊞' },
            { id: 'rides'   as TabId, label: 'Rides',   glyph: '⊙' },
            { id: 'history' as TabId, label: 'History', glyph: '☰' },
            { id: 'profile' as TabId, label: 'Profile', glyph: '◉' },
          ]).map(t => {
            const on = active === t.id;
            return (
              <button
                key={t.id}
                onClick={() => onTab(t.id)}
                style={{
                  flex: 1, background: 'none', border: 'none', cursor: 'pointer',
                  display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 3,
                }}
              >
                <span style={{ fontSize: 22, color: on ? GREEN : dim }}>{t.glyph}</span>
                <span style={{ fontSize: 10, fontWeight: on ? 600 : 400, color: on ? GREEN : dim }}>{t.label}</span>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
