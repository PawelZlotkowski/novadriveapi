// shared/auth.ts
// Auth0 token helpers shared by both apps.
// Each app wraps its own Auth0Provider with the correct clientId + scope.
import { useAuth0 } from '@auth0/auth0-react';

const AUDIENCE = import.meta.env.VITE_AUTH0_AUDIENCE as string;

/**
 * React hook — returns a function that resolves to a fresh access token.
 * Use inside components that need to call the API imperatively.
 */
export function useGetAccessToken() {
  const { getAccessTokenSilently } = useAuth0();
  return () =>
    getAccessTokenSilently({
      authorizationParams: { audience: AUDIENCE },
    });
}

/**
 * Standalone helper for use outside React components (e.g. the api module).
 * Must be initialised once at app boot by calling `setTokenGetter`.
 */
let _tokenGetter: (() => Promise<string>) | null = null;

export function setTokenGetter(fn: () => Promise<string>) {
  _tokenGetter = fn;
}

export async function getAccessToken(): Promise<string> {
  if (!_tokenGetter) throw new Error('Token getter not initialised — call setTokenGetter first');
  return _tokenGetter();
}
