import React from 'react';
import ReactDOM from 'react-dom/client';
import { Auth0Provider, useAuth0 } from '@auth0/auth0-react';
import { setTokenGetter } from '@shared/auth';
import { App } from './App';

const domain   = import.meta.env.VITE_AUTH0_DOMAIN as string;
const clientId = import.meta.env.VITE_AUTH0_PASSENGER_CLIENT_ID as string;
const audience = import.meta.env.VITE_AUTH0_AUDIENCE as string;

/** Wires up the module-level token getter once Auth0 is available. */
function TokenBridge({ children }: { children: React.ReactNode }) {
  const { getAccessTokenSilently } = useAuth0();
  setTokenGetter(() => getAccessTokenSilently({ authorizationParams: { audience } }));
  return <>{children}</>;
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      cacheLocation="localstorage"
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience,
        scope: 'read:passenger',
      }}
    >
      <TokenBridge>
        <App />
      </TokenBridge>
    </Auth0Provider>
  </React.StrictMode>,
);

