import { useState } from 'react';
import { passengerApi } from '@shared/api';
import type { PaymentMethod } from '@shared/types';

const FONT  = "'Inter', system-ui, sans-serif";
const GREEN = '#22C55E';
const BORDER = '#e5e5e5';

const PAYMENT_OPTIONS: { value: PaymentMethod; label: string; icon: string }[] = [
  { value: 'CreditCard',   label: 'Credit Card',    icon: '💳' },
  { value: 'PayPal',       label: 'PayPal',          icon: '🅿' },
  { value: 'BankTransfer', label: 'Bank Transfer',   icon: '🏦' },
];

const INPUT: React.CSSProperties = {
  border: `1px solid ${BORDER}`, borderRadius: 12, padding: '13px 16px',
  fontFamily: FONT, fontSize: 14, color: '#000', width: '100%', outline: 'none',
  background: '#fff', boxSizing: 'border-box',
};

interface OnboardingProps {
  onComplete: () => void;
}

export function Onboarding({ onComplete }: OnboardingProps) {
  const [firstName, setFirstName]   = useState('');
  const [lastName, setLastName]     = useState('');
  const [homeAddress, setHome]      = useState('');
  const [payment, setPayment]       = useState<PaymentMethod>('CreditCard');
  const [saving, setSaving]         = useState(false);
  const [error, setError]           = useState('');

  async function handleSubmit() {
    if (!firstName.trim() || !lastName.trim()) {
      setError('First name and last name are required.');
      return;
    }
    setError('');
    setSaving(true);
    try {
      await passengerApi.profile.create({
        firstName: firstName.trim(),
        lastName:  lastName.trim(),
        homeAddress: homeAddress.trim() || null,
        preferredPaymentMethod: payment,
      });
      onComplete();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to create profile. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{
      minHeight: '100%', background: '#fff',
      display: 'flex', flexDirection: 'column',
      padding: '32px 24px 40px',
      fontFamily: FONT,
      overflowY: 'auto',
    }}>
      {/* Header */}
      <div style={{ marginBottom: 32 }}>
        <div style={{
          width: 48, height: 48, borderRadius: 14, background: GREEN,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          marginBottom: 20,
          fontSize: 22,
        }}>👋</div>
        <div style={{ fontSize: 24, fontWeight: 800, color: '#000', marginBottom: 6 }}>Welcome aboard!</div>
        <div style={{ fontSize: 14, color: '#afafaf', lineHeight: 1.5 }}>
          Set up your profile to start booking rides.
        </div>
      </div>

      {/* Form */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 16, flex: 1 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label style={{ fontSize: 12, fontWeight: 600, color: '#555', display: 'block', marginBottom: 6 }}>First Name *</label>
            <input
              style={INPUT}
              value={firstName}
              onChange={e => setFirstName(e.target.value)}
              placeholder="Pawel"
            />
          </div>
          <div>
            <label style={{ fontSize: 12, fontWeight: 600, color: '#555', display: 'block', marginBottom: 6 }}>Last Name *</label>
            <input
              style={INPUT}
              value={lastName}
              onChange={e => setLastName(e.target.value)}
              placeholder="Kowalski"
            />
          </div>
        </div>

        <div>
          <label style={{ fontSize: 12, fontWeight: 600, color: '#555', display: 'block', marginBottom: 6 }}>Home Address</label>
          <input
            style={INPUT}
            value={homeAddress}
            onChange={e => setHome(e.target.value)}
            placeholder="Graaf Karel de Goedelaan 5, 8500 Kortrijk"
          />
        </div>

        <div>
          <label style={{ fontSize: 12, fontWeight: 600, color: '#555', display: 'block', marginBottom: 10 }}>Preferred Payment</label>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {PAYMENT_OPTIONS.map(opt => (
              <button
                key={opt.value}
                onClick={() => setPayment(opt.value)}
                style={{
                  display: 'flex', alignItems: 'center', gap: 12,
                  padding: '13px 16px', borderRadius: 12, border: 'none',
                  background: payment === opt.value ? '#000' : '#f8f8f8',
                  cursor: 'pointer', textAlign: 'left',
                  transition: 'background .15s',
                }}
              >
                <span style={{ fontSize: 20 }}>{opt.icon}</span>
                <span style={{ fontFamily: FONT, fontSize: 14, fontWeight: 500, color: payment === opt.value ? '#fff' : '#000' }}>
                  {opt.label}
                </span>
                {payment === opt.value && (
                  <span style={{ marginLeft: 'auto', color: GREEN, fontSize: 18 }}>✓</span>
                )}
              </button>
            ))}
          </div>
        </div>

        {error && (
          <div style={{ fontSize: 13, color: '#ef4444', background: '#fef2f2', borderRadius: 8, padding: '10px 14px' }}>
            {error}
          </div>
        )}
      </div>

      <button
        onClick={handleSubmit}
        disabled={saving}
        style={{
          marginTop: 32,
          width: '100%', padding: '17px 0',
          background: saving ? 'rgba(34,197,94,0.5)' : GREEN,
          border: 'none', borderRadius: 999,
          fontFamily: FONT, fontSize: 16, fontWeight: 700, color: '#000',
          cursor: saving ? 'default' : 'pointer',
        }}
      >
        {saving ? 'Creating profile…' : 'Get Started →'}
      </button>
    </div>
  );
}
