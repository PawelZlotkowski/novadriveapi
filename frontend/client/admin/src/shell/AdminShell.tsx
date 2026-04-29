import { useState, type ReactNode } from 'react';
import { ND, FONT } from '@shared/tokens';
import { Icons } from './Icons';
import type { NavId, NavItem } from './nav';

const NAV_TOP: NavItem[] = [
  { id: 'fleet',       label: 'Fleet Overview',  icon: Icons.grid   },
  { id: 'vehicles',    label: 'Vehicles',         icon: Icons.car    },
  { id: 'rides',       label: 'Rides',            icon: Icons.map    },
  { id: 'maintenance', label: 'Maintenance',      icon: Icons.wrench },
  { id: 'tickets',     label: 'Support Tickets',  icon: Icons.ticket },
  { id: 'telemetry',   label: 'Telemetry',        icon: Icons.signal },
];

const NAV_BOTTOM: NavItem[] = [
  { id: 'users',         label: 'Users',           icon: Icons.users   },
  { id: 'payments',      label: 'Payments',        icon: Icons.dollar  },
  { id: 'discountCodes', label: 'Discount Codes',  icon: Icons.percent },
  { id: 'diagnostics',   label: 'Diagnostics',     icon: Icons.cpu     },
];

interface ShellProps {
  active: NavId;
  onNav: (id: NavId) => void;
  title: string;
  onLogout?: () => void;
  children: ReactNode;
}

export function AdminShell({ active, onNav, title, onLogout, children }: ShellProps) {
  return (
    <div style={{
      width: 1440, height: 900, display: 'flex', background: '#fff',
      overflow: 'hidden', boxShadow: '0 20px 60px rgba(0,0,0,0.18)', borderRadius: 8,
    }}>
      <Sidebar active={active} onNav={onNav} onLogout={onLogout} />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <Header title={title} onLogout={onLogout} />
        <div style={{ flex: 1, overflow: 'auto', padding: 32, background: '#fff' }}>
          {children}
        </div>
      </div>
    </div>
  );
}

function NavButton({ item, active, onNav }: { item: NavItem; active: NavId; onNav: (id: NavId) => void }) {
  const [hover, setHover] = useState(false);
  const isActive = active === item.id;
  const c = isActive ? ND.accent : ND.muted;
  return (
    <button
      onClick={() => onNav(item.id)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        display: 'flex', alignItems: 'center', gap: 12,
        padding: '10px 12px', borderRadius: 8, border: 'none',
        background: isActive ? ND.accentBg : (hover ? 'rgba(255,255,255,0.06)' : 'transparent'),
        color: c, fontFamily: FONT, fontWeight: 500, fontSize: 14,
        cursor: 'pointer', textAlign: 'left', width: '100%',
      }}
    >
      {item.icon(c, 18)}
      <span>{item.label}</span>
    </button>
  );
}

function Sidebar({ active, onNav, onLogout }: { active: NavId; onNav: (id: NavId) => void; onLogout?: () => void }) {
  return (
    <aside style={{
      width: 240, height: '100%', background: ND.black,
      display: 'flex', flexDirection: 'column', flexShrink: 0,
    }}>
      <div style={{ padding: '24px 20px' }}>
        <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: 18, color: '#fff', letterSpacing: '0.12em' }}>NOVA</div>
        <div style={{ fontFamily: FONT, fontWeight: 400, fontSize: 11, color: ND.muted, letterSpacing: '0.15em', marginTop: 2 }}>DRIVE ADMIN</div>
        <div style={{ height: 1, background: ND.darkBorder, marginTop: 20 }} />
      </div>

      <nav style={{ padding: '0 12px', display: 'flex', flexDirection: 'column', gap: 4 }}>
        {NAV_TOP.map(n => <NavButton key={n.id} item={n} active={active} onNav={onNav} />)}
      </nav>

      <div style={{ height: 1, background: ND.darkBorder, margin: '16px 20px 12px' }} />

      <nav style={{ padding: '0 12px', display: 'flex', flexDirection: 'column', gap: 4 }}>
        {NAV_BOTTOM.map(n => <NavButton key={n.id} item={n} active={active} onNav={onNav} />)}
      </nav>

      <div style={{ marginTop: 'auto', padding: 20, display: 'flex', alignItems: 'center', gap: 10 }}>
        <div style={{
          width: 36, height: 36, borderRadius: '50%', background: ND.darkBorder,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: FONT, fontWeight: 600, fontSize: 14, color: '#fff', flexShrink: 0,
        }}>AD</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, flex: 1, minWidth: 0 }}>
          <span style={{ fontFamily: FONT, fontWeight: 500, fontSize: 13, color: '#fff', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>Admin User</span>
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: ND.accent, flexShrink: 0 }} />
        </div>
        <button onClick={onLogout} style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, display: 'flex' }}>
          {Icons.gear(ND.muted, 16)}
        </button>
      </div>
    </aside>
  );
}

function Header({ title, onLogout }: { title: string; onLogout?: () => void }) {
  return (
    <div style={{
      height: 64, borderBottom: `1px solid ${ND.border}`,
      padding: '0 32px', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      flexShrink: 0, background: '#fff',
    }}>
      <h1 style={{ fontFamily: FONT, fontWeight: 700, fontSize: 22, color: '#000', letterSpacing: -0.3, margin: 0 }}>{title}</h1>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{
          background: ND.surface, border: `1px solid ${ND.border}`,
          borderRadius: 999, padding: '10px 16px', width: 240,
          display: 'flex', alignItems: 'center', gap: 8,
        }}>
          {Icons.search(ND.muted, 16)}
          <span style={{ fontFamily: FONT, fontWeight: 400, fontSize: 14, color: ND.muted }}>Search...</span>
        </div>
        <button style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, display: 'flex' }}>
          {Icons.bell('#000', 18)}
        </button>
        <div style={{
          width: 36, height: 36, borderRadius: '50%', background: '#000',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontFamily: FONT, fontWeight: 600, fontSize: 13, color: '#fff',
        }}>AD</div>
      </div>
    </div>
  );
}
