import { useEffect, useState } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { PassengerShell, type TabId } from './PassengerShell';
import { Home }       from './screens/Home';
import { Tracking }   from './screens/Tracking';
import { History }    from './screens/History';
import { Profile }    from './screens/Profile';
import { Login }      from './screens/Login';
import { Onboarding } from './screens/Onboarding';
import { authApi, passengerApi } from '@shared/api';

type Phase = 'loading' | 'login' | 'syncing' | 'onboarding' | 'ready';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

function Spinner() {
  return (
    <div style={{
      height: '100%', display: 'flex', flexDirection: 'column',
      alignItems: 'center', justifyContent: 'center', gap: 16,
      background: '#000',
    }}>
      <div style={{ fontSize: 22, fontWeight: 700, color: '#fff', letterSpacing: '0.15em', fontFamily: FONT }}>NOVA</div>
      <div style={{ width: 24, height: 24, borderRadius: '50%', border: `2px solid rgba(255,255,255,0.15)`, borderTopColor: GREEN, animation: 'ndSpin 0.8s linear infinite' }}/>
      <style>{`@keyframes ndSpin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}

export function App() {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
  const [phase, setPhase] = useState<Phase>('loading');
  const [tab, setTab]     = useState<TabId>('home');

  useEffect(() => {
    if (isLoading) return;

    if (!isAuthenticated) {
      setPhase('login');
      return;
    }

    // Authenticated — sync user then check passenger profile
    setPhase('syncing');
    authApi.sync()
      .then(() => passengerApi.profile.get())
      .then(() => setPhase('ready'))
      .catch(err => {
        const msg = err instanceof Error ? err.message : String(err);
        if (msg.includes('404')) {
          setPhase('onboarding');
        } else {
          // Other error (e.g. 403 missing permission) — still try onboarding
          setPhase('onboarding');
        }
      });
  }, [isAuthenticated, isLoading]);

  // Render pre-auth phases inside the phone frame (no nav bar)
  if (phase === 'loading' || phase === 'syncing') {
    return (
      <PassengerShell active="home" onTab={() => {}} hideNav>
        <Spinner />
      </PassengerShell>
    );
  }

  if (phase === 'login') {
    return (
      <PassengerShell active="home" onTab={() => {}} hideNav>
        <Login onLogin={() => loginWithRedirect({ authorizationParams: { redirect_uri: window.location.origin } })} />
      </PassengerShell>
    );
  }

  if (phase === 'onboarding') {
    return (
      // Light shell for the onboarding form
      <PassengerShell active="history" onTab={() => {}} hideNav>
        <Onboarding onComplete={() => setPhase('ready')} />
      </PassengerShell>
    );
  }

  return (
    <PassengerShell active={tab} onTab={setTab}>
      {tab === 'home'    && <Home    onBookRide={() => setTab('rides')} />}
      {tab === 'rides'   && <Tracking onCancel={() => setTab('home')} onComplete={() => setTab('history')} />}
      {tab === 'history' && <History  onTrack={() => setTab('rides')} />}
      {tab === 'profile' && <Profile />}
    </PassengerShell>
  );
}
