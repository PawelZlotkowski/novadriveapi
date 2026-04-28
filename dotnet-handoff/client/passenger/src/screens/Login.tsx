const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

interface LoginProps {
  onLogin: () => void;
}

export function Login({ onLogin }: LoginProps) {
  return (
    <div style={{
      height: '100%', backgroundColor: '#000',
      display: 'flex', flexDirection: 'column',
      alignItems: 'center', justifyContent: 'center',
      padding: '0 32px',
    }}>
      {/* Logo mark */}
      <div style={{ marginBottom: 48, textAlign: 'center' }}>
        <div style={{
          width: 64, height: 64, borderRadius: 20,
          background: GREEN,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          margin: '0 auto 20px',
          boxShadow: `0 0 40px ${GREEN}44`,
        }}>
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#000" strokeWidth="2.5" strokeLinecap="round">
            <path d="M3 13l2-5a2 2 0 012-2h10a2 2 0 012 2l2 5"/>
            <path d="M3 13h18v5a1 1 0 01-1 1h-2a1 1 0 01-1-1v-1H7v1a1 1 0 01-1 1H4a1 1 0 01-1-1v-5z"/>
            <circle cx="7.5" cy="16.5" r="1" fill="#000"/>
            <circle cx="16.5" cy="16.5" r="1" fill="#000"/>
          </svg>
        </div>
        <div style={{ fontSize: 28, fontWeight: 800, color: '#fff', fontFamily: FONT, letterSpacing: '-0.5px' }}>
          NovaDrive
        </div>
        <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)', fontFamily: FONT, marginTop: 6 }}>
          Your autonomous ride companion
        </div>
      </div>

      {/* Feature pills */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 48, width: '100%' }}>
        {[
          { icon: '⚡', text: 'Instant matching with nearby vehicles' },
          { icon: '📍', text: 'Real-time ride tracking' },
          { icon: '🎯', text: 'Earn loyalty points every ride' },
        ].map(f => (
          <div key={f.text} style={{
            display: 'flex', alignItems: 'center', gap: 12,
            background: 'rgba(255,255,255,0.05)',
            border: '1px solid rgba(255,255,255,0.08)',
            borderRadius: 12, padding: '12px 16px',
          }}>
            <span style={{ fontSize: 20 }}>{f.icon}</span>
            <span style={{ fontSize: 13, color: 'rgba(255,255,255,0.7)', fontFamily: FONT }}>{f.text}</span>
          </div>
        ))}
      </div>

      {/* CTA */}
      <button
        onClick={onLogin}
        style={{
          width: '100%', padding: '17px 0',
          background: GREEN, border: 'none', borderRadius: 999,
          fontFamily: FONT, fontSize: 16, fontWeight: 700, color: '#000',
          cursor: 'pointer',
          boxShadow: `0 4px 24px ${GREEN}44`,
        }}
      >
        Sign In to Continue
      </button>

      <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.25)', fontFamily: FONT, marginTop: 20, textAlign: 'center' }}>
        By signing in you agree to our Terms of Service
      </div>
    </div>
  );
}
