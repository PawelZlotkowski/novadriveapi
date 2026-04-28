import { useEffect, useState } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { AdminShell } from './shell/AdminShell';
import type { NavId } from './shell/nav';
import { FleetOverview } from './screens/FleetOverview';
import { Vehicles }      from './screens/Vehicles';
import { Rides }         from './screens/Rides';
import { Maintenance }   from './screens/Maintenance';
import { Tickets }       from './screens/Tickets';
import { Telemetry }     from './screens/Telemetry';
import { Users }         from './screens/Users';
import { Payments }      from './screens/Payments';
import { Diagnostics }    from './screens/Diagnostics';
import { DiscountCodes }  from './screens/DiscountCodes';
import { authApi }        from '@shared/api';
import { FONT, ND }      from '@shared/tokens';

const TITLES: Record<NavId, string> = {
  fleet:         'Fleet Overview',
  vehicles:      'Vehicles',
  rides:         'Rides',
  maintenance:   'Maintenance',
  tickets:       'Support Tickets',
  telemetry:     'Telemetry',
  users:         'Users',
  payments:      'Payments',
  discountCodes: 'Discount Codes',
  diagnostics:   'Diagnostics',
};

export function App() {
  const { isAuthenticated, isLoading, loginWithRedirect, logout } = useAuth0();
  const [active, setActive] = useState<NavId>('fleet');

  // Sync Auth0 user to local DB once authenticated
  useEffect(() => {
    if (isAuthenticated) {
      authApi.sync().catch(console.error);
    }
  }, [isAuthenticated]);

  if (isLoading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', background: '#000' }}>
        <div style={{ fontFamily: FONT, color: ND.muted, fontSize: 14 }}>Loading…</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', background: '#000' }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: 24, color: '#fff', letterSpacing: '0.12em' }}>NOVA</div>
          <div style={{ fontFamily: FONT, fontSize: 11, color: ND.muted, letterSpacing: '0.18em', marginTop: 2 }}>DRIVE ADMIN</div>
          <button
            onClick={() => loginWithRedirect()}
            style={{
              marginTop: 40, padding: '12px 32px', borderRadius: 8,
              background: ND.accent, color: '#000', border: 'none',
              fontFamily: FONT, fontWeight: 600, fontSize: 14, cursor: 'pointer',
            }}
          >
            Sign in
          </button>
        </div>
      </div>
    );
  }

  return (
    <AdminShell
      active={active}
      onNav={setActive}
      title={TITLES[active]}
      onLogout={() => logout({ logoutParams: { returnTo: window.location.origin } })}
    >
      {active === 'fleet'       && <FleetOverview />}
      {active === 'vehicles'    && <Vehicles />}
      {active === 'rides'       && <Rides />}
      {active === 'maintenance' && <Maintenance />}
      {active === 'tickets'     && <Tickets />}
      {active === 'telemetry'   && <Telemetry />}
      {active === 'users'       && <Users />}
      {active === 'payments'      && <Payments />}
      {active === 'discountCodes' && <DiscountCodes />}
      {active === 'diagnostics'   && <Diagnostics />}
    </AdminShell>
  );
}
