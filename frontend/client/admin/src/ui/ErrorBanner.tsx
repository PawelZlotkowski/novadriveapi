import { FONT, ND } from '@shared/tokens';

export function apiError(e: unknown): string {
  const msg = e instanceof Error ? e.message : String(e);
  if (msg.includes('403')) return '403 Forbidden — your token is missing the manage:admin permission. In Auth0: create an "Admin" role → assign manage:admin permission → assign the role to your user → re-login.';
  if (msg.includes('401')) return '401 Unauthorized — session expired. Sign out and sign in again.';
  return msg;
}

export function ErrorBanner({ message }: { message: string }) {
  return (
    <div style={{
      background: '#fef2f2', border: `1px solid ${ND.err}44`, borderRadius: 10,
      padding: '12px 16px', marginBottom: 20,
      fontFamily: FONT, fontSize: 13, color: '#b91c1c', lineHeight: 1.6,
    }}>
      ⚠ {message}
    </div>
  );
}
