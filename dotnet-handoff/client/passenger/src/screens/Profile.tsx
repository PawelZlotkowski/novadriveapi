import { useEffect, useState } from 'react';
import { passengerApi } from '@shared/api';
import type { PassengerResponse, PaymentMethod, VehicleType, TicketPriority } from '@shared/types';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';

const PAYMENT_ICON: Record<PaymentMethod, string> = {
  CreditCard:   '💳',
  PayPal:       '🅿',
  BankTransfer: '🏦',
};
const PAYMENT_LAST4: Record<PaymentMethod, string> = {
  CreditCard:   '•••• 4242',
  PayPal:       'PayPal',
  BankTransfer: 'Bank Transfer',
};
const PAYMENT_BRAND: Record<PaymentMethod, string> = {
  CreditCard:   'Visa',
  PayPal:       '',
  BankTransfer: '',
};

function tierLabel(pts: number): string {
  if (pts >= 2000) return 'Platinum';
  if (pts >= 1000) return 'Gold';
  if (pts >= 500)  return 'Silver';
  return 'Bronze';
}

function tierMax(pts: number): number {
  if (pts >= 2000) return 5000;
  if (pts >= 1000) return 2000;
  if (pts >= 500)  return 1000;
  return 500;
}

interface AccountRowProps {
  label: string;
  danger?: boolean;
  onClick?: () => void;
}
function AccountRow({ label, danger, onClick }: AccountRowProps) {
  return (
    <button
      onClick={onClick}
      style={{
        width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '15px 20px', background: 'none', border: 'none',
        borderBottom: '1px solid #f0f0f0', cursor: 'pointer',
        fontFamily: FONT,
      }}
    >
      <span style={{ fontSize: 14, color: danger ? '#ef4444' : '#000' }}>{label}</span>
      {!danger && <span style={{ fontSize: 16, color: '#c7c7cc' }}>›</span>}
    </button>
  );
}

// ── Support ticket bottom sheet ───────────────────────────────────────────────

const PRIORITY_COLORS: Record<TicketPriority, string> = {
  Low: '#6b7280', Medium: '#f59e0b', High: '#ef4444', Critical: '#7c3aed',
};

function SupportSheet({ onClose }: { onClose: () => void }) {
  const [subject, setSubject]       = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority]     = useState<TicketPriority>('Medium');
  const [status, setStatus]         = useState<'idle' | 'sending' | 'sent' | 'error'>('idle');
  const [errMsg, setErrMsg]         = useState('');

  async function handleSubmit() {
    if (!subject.trim() || !description.trim()) return;
    setStatus('sending');
    try {
      await passengerApi.tickets.create({ subject: subject.trim(), description: description.trim(), priority });
      setStatus('sent');
    } catch (e: any) {
      setErrMsg(e.message ?? 'Failed to send ticket.');
      setStatus('error');
    }
  }

  return (
    <>
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.55)', zIndex: 100 }}
      />
      {/* Sheet */}
      <div style={{
        position: 'fixed', left: 0, right: 0, bottom: 0,
        background: '#fff', borderRadius: '20px 20px 0 0',
        padding: '0 20px 32px', zIndex: 101,
        maxHeight: '90%', display: 'flex', flexDirection: 'column',
      }}>
        {/* Handle */}
        <div style={{ display: 'flex', justifyContent: 'center', padding: '12px 0 8px' }}>
          <div style={{ width: 36, height: 4, borderRadius: 99, background: '#e5e7eb' }} />
        </div>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
          <div style={{ fontSize: 17, fontWeight: 700, color: '#000', fontFamily: FONT }}>Help & Support</div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: 22, color: '#9ca3af', cursor: 'pointer', lineHeight: 1, padding: 0 }}>×</button>
        </div>

        {status === 'sent' ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <div style={{ fontSize: 40, marginBottom: 12 }}>✅</div>
            <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: 16, marginBottom: 6 }}>Ticket Submitted</div>
            <div style={{ fontFamily: FONT, fontSize: 13, color: '#6b7280', marginBottom: 28 }}>
              We'll get back to you as soon as possible.
            </div>
            <button onClick={onClose} style={{
              background: '#000', color: '#fff', border: 'none', borderRadius: 999,
              padding: '12px 32px', fontFamily: FONT, fontWeight: 600, fontSize: 14, cursor: 'pointer',
            }}>Done</button>
          </div>
        ) : (
          <div style={{ overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div>
              <div style={{ fontFamily: FONT, fontSize: 12, fontWeight: 600, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>Subject</div>
              <input
                value={subject}
                onChange={e => setSubject(e.target.value)}
                placeholder="e.g. Driver was late, App issue…"
                style={{
                  width: '100%', boxSizing: 'border-box', padding: '11px 14px',
                  border: '1px solid #e5e7eb', borderRadius: 12,
                  fontFamily: FONT, fontSize: 14, color: '#000', outline: 'none',
                  background: '#f9f9f9',
                }}
              />
            </div>

            <div>
              <div style={{ fontFamily: FONT, fontSize: 12, fontWeight: 600, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>Description</div>
              <textarea
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="Please describe your issue in detail…"
                rows={4}
                style={{
                  width: '100%', boxSizing: 'border-box', padding: '11px 14px',
                  border: '1px solid #e5e7eb', borderRadius: 12,
                  fontFamily: FONT, fontSize: 14, color: '#000', outline: 'none',
                  background: '#f9f9f9', resize: 'none',
                }}
              />
            </div>

            <div>
              <div style={{ fontFamily: FONT, fontSize: 12, fontWeight: 600, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>Priority</div>
              <div style={{ display: 'flex', gap: 8 }}>
                {(['Low', 'Medium', 'High'] as TicketPriority[]).map(p => (
                  <button
                    key={p}
                    onClick={() => setPriority(p)}
                    style={{
                      flex: 1, padding: '8px 0', borderRadius: 999, cursor: 'pointer',
                      fontFamily: FONT, fontSize: 13, fontWeight: 600,
                      border: priority === p ? 'none' : '1px solid #e5e7eb',
                      background: priority === p ? PRIORITY_COLORS[p] : '#f9f9f9',
                      color: priority === p ? '#fff' : '#6b7280',
                    }}
                  >{p}</button>
                ))}
              </div>
            </div>

            {status === 'error' && (
              <div style={{ fontFamily: FONT, fontSize: 13, color: '#ef4444' }}>{errMsg}</div>
            )}

            <button
              onClick={handleSubmit}
              disabled={status === 'sending' || !subject.trim() || !description.trim()}
              style={{
                background: '#000', color: '#fff', border: 'none', borderRadius: 999,
                padding: '14px', fontFamily: FONT, fontWeight: 700, fontSize: 15,
                cursor: status === 'sending' ? 'not-allowed' : 'pointer',
                opacity: status === 'sending' || !subject.trim() || !description.trim() ? 0.5 : 1,
                marginTop: 4,
              }}
            >
              {status === 'sending' ? 'Sending…' : 'Submit Ticket'}
            </button>
          </div>
        )}
      </div>
    </>
  );
}

// ── Profile screen ────────────────────────────────────────────────────────────

export function Profile() {
  const [me, setMe]           = useState<PassengerResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [prefVehicle, setPrefVehicle] = useState<VehicleType>('Standard');
  const [showSupport, setShowSupport] = useState(false);

  useEffect(() => {
    passengerApi.profile.get()
      .then(setMe)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const initials = me
    ? `${me.firstName[0] ?? ''}${me.lastName[0] ?? ''}`.toUpperCase()
    : '?';
  const pts  = me?.loyaltyPoints ?? 0;
  const tier = tierLabel(pts);
  const max  = tierMax(pts);
  const pct  = Math.min(100, (pts / max) * 100);

  return (
    <div style={{ fontFamily: FONT, background: '#f8f8f8', minHeight: '100%' }}>
      {/* Black hero zone */}
      <div style={{
        background: '#000',
        padding: '28px 20px 48px',
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10,
      }}>
        {/* Avatar */}
        <div style={{
          width: 72, height: 72, borderRadius: '50%',
          background: '#1a1a1a',
          border: '2px solid rgba(255,255,255,0.12)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: 26, fontWeight: 700, color: '#fff',
          letterSpacing: 1,
        }}>
          {loading ? '?' : initials}
        </div>

        {loading ? (
          <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)' }}>Loading…</div>
        ) : me ? (
          <>
            <div style={{ fontSize: 20, fontWeight: 700, color: '#fff' }}>{me.firstName} {me.lastName}</div>
            <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.45)' }}>{me.email}</div>
          </>
        ) : (
          <div style={{ fontSize: 14, color: 'rgba(255,255,255,0.4)' }}>Could not load profile</div>
        )}
      </div>

      {/* White overlay card */}
      <div style={{
        background: '#fff',
        borderRadius: '20px 20px 0 0',
        marginTop: -20,
        paddingBottom: 16,
      }}>
        {/* Loyalty card */}
        <div style={{ padding: '20px 20px 0' }}>
          <div style={{
            background: 'linear-gradient(135deg, #111 0%, #1e1e1e 100%)',
            borderRadius: 16, padding: '18px 18px 16px',
            marginBottom: 20,
          }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 14 }}>
              <div>
                <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.4)', fontFamily: FONT, marginBottom: 3 }}>Loyalty Points</div>
                <div style={{ fontSize: 28, fontWeight: 700, color: '#fff', fontFamily: FONT }}>{pts.toLocaleString()}</div>
              </div>
              <div style={{
                background: 'rgba(34,197,94,0.15)', border: '1px solid rgba(34,197,94,0.3)',
                borderRadius: 999, padding: '5px 12px',
              }}>
                <span style={{ fontSize: 11, fontWeight: 700, color: GREEN, fontFamily: FONT }}>★ {tier} Member</span>
              </div>
            </div>

            {/* Progress bar */}
            <div>
              <div style={{ height: 5, background: 'rgba(255,255,255,0.1)', borderRadius: 99, overflow: 'hidden', marginBottom: 5 }}>
                <div style={{
                  width: `${pct}%`, height: '100%', borderRadius: 99,
                  background: `linear-gradient(90deg, ${GREEN} 0%, #4ade80 100%)`,
                }}/>
              </div>
              <div style={{ fontSize: 11, color: 'rgba(255,255,255,0.35)', fontFamily: FONT }}>
                {Math.max(0, max - pts).toLocaleString()} pts to {tierLabel(max)}
              </div>
            </div>
          </div>

          {/* Payment method */}
          {me && (
            <div style={{
              background: '#f9f9f9', borderRadius: 14, padding: '14px 16px', marginBottom: 20,
              display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span style={{ fontSize: 22 }}>{PAYMENT_ICON[me.preferredPaymentMethod]}</span>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: '#000' }}>
                    {PAYMENT_LAST4[me.preferredPaymentMethod]}
                    {PAYMENT_BRAND[me.preferredPaymentMethod] && (
                      <span style={{ fontSize: 11, color: '#afafaf', marginLeft: 6 }}>
                        {PAYMENT_BRAND[me.preferredPaymentMethod]}
                      </span>
                    )}
                  </div>
                  <div style={{ fontSize: 11, color: '#afafaf' }}>Payment method</div>
                </div>
              </div>
              <span style={{ fontSize: 16, color: '#c7c7cc' }}>›</span>
            </div>
          )}

          {/* Preferred vehicle */}
          <div style={{ marginBottom: 4 }}>
            <div style={{ fontSize: 11, fontWeight: 600, color: '#afafaf', letterSpacing: '0.07em', textTransform: 'uppercase', marginBottom: 10 }}>
              Preferred Vehicle
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              {(['Standard', 'Van', 'Luxury'] as VehicleType[]).map(v => (
                <button
                  key={v}
                  onClick={() => setPrefVehicle(v)}
                  style={{
                    flex: 1, padding: '9px 0', borderRadius: 999,
                    background: prefVehicle === v ? '#000' : '#f0f0f0',
                    border: 'none', cursor: 'pointer',
                    fontFamily: FONT, fontSize: 12, fontWeight: 600,
                    color: prefVehicle === v ? '#fff' : '#555',
                  }}
                >
                  {v}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Divider */}
        <div style={{ height: 8, background: '#f8f8f8', margin: '20px 0 0' }}/>

        {/* Account rows */}
        <div>
          <AccountRow label="Notifications" />
          <AccountRow label="Privacy & Safety" />
          <AccountRow label="Help & Support" onClick={() => setShowSupport(true)} />
          <AccountRow label="Sign Out" danger onClick={() => window.location.reload()} />
        </div>
      </div>

      {showSupport && <SupportSheet onClose={() => setShowSupport(false)} />}
    </div>
  );
}
