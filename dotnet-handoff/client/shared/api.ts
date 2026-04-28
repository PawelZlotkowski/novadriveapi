// shared/api.ts
// HTTP client wired to the NovaDrive .NET backend.
import { getAccessToken } from './auth';
import type {
  PaginatedResponse,
  VehicleResponse, CreateVehicleRequest, UpdateVehicleRequest, FleetStatsResponse,
  PassengerResponse, CreatePassengerRequest, UpdatePassengerRequest,
  RideResponse, CreateRideRequest, PricingResult,
  DiscountCodeResponse, CreateDiscountCodeRequest,
  MaintenanceLogResponse, CreateMaintenanceLogRequest,
  SupportTicketResponse, CreateSupportTicketRequest,
  TelemetryResponse,
  SensorDiagnosticResponse,
  PaymentResponse,
  UserResponse,
  AuthSyncResponse,
} from './types';

const BASE = import.meta.env.VITE_API_BASE_URL ?? '';

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getAccessToken();
  const res = await fetch(`${BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
      ...(init?.headers ?? {}),
    },
    ...init,
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}: ${await res.text()}`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// Binary download (returns Blob for file downloads)
async function httpBlob(path: string): Promise<Blob> {
  const token = await getAccessToken();
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.blob();
}

// ─── Auth ─────────────────────────────────────────────────────────────────────
export const authApi = {
  sync: () => http<AuthSyncResponse>('/api/public/auth/sync', { method: 'POST' }),
};

// ─── Admin API ────────────────────────────────────────────────────────────────
export const adminApi = {
  users: {
    list: (page = 1, pageSize = 20) =>
      http<PaginatedResponse<UserResponse>>(`/api/admin/users?page=${page}&pageSize=${pageSize}`),
    get: (id: string) => http<UserResponse>(`/api/admin/users/${id}`),
    setRole: (id: string, role: string) =>
      http<{ id: string; role: string }>(`/api/admin/users/${id}/role`, { method: 'PUT', body: JSON.stringify({ role }) }),
    setStatus: (id: string, isActive: boolean) =>
      http<{ id: string; isActive: boolean }>(`/api/admin/users/${id}/status`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
  },

  vehicles: {
    list: (page = 1, pageSize = 20, isActive?: boolean) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (isActive !== undefined) params.set('isActive', String(isActive));
      return http<PaginatedResponse<VehicleResponse>>(`/api/admin/vehicles?${params}`);
    },
    get:    (id: string) => http<VehicleResponse>(`/api/admin/vehicles/${id}`),
    create: (body: CreateVehicleRequest) =>
      http<VehicleResponse>('/api/admin/vehicles', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: string, body: UpdateVehicleRequest) =>
      http<VehicleResponse>(`/api/admin/vehicles/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    delete: (id: string) =>
      http<void>(`/api/admin/vehicles/${id}`, { method: 'DELETE' }),
    setStatus: (id: string, isActive: boolean) =>
      http<void>(`/api/admin/vehicles/${id}/status`, { method: 'PATCH', body: JSON.stringify({ isActive }) }),
    stats: () => http<FleetStatsResponse>('/api/admin/vehicles/stats'),
  },

  passengers: {
    list: (page = 1, pageSize = 20) =>
      http<PaginatedResponse<PassengerResponse>>(`/api/admin/passengers?page=${page}&pageSize=${pageSize}`),
    get:  (id: string) => http<PassengerResponse>(`/api/admin/passengers/${id}`),
    adjustLoyalty: (id: string, points: number) =>
      http<{ passengerId: string; adjustedBy: number }>(`/api/admin/passengers/${id}/loyalty`, { method: 'PUT', body: JSON.stringify({ points }) }),
  },

  rides: {
    list: (page = 1, pageSize = 20, status?: string) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status) params.set('status', status);
      return http<PaginatedResponse<RideResponse>>(`/api/admin/rides?${params}`);
    },
    get:    (id: string) => http<RideResponse>(`/api/admin/rides/${id}`),
    active: () => http<RideResponse[]>('/api/admin/rides/active'),
    sendInvoice: (id: string) => http<void>(`/api/admin/rides/${id}/invoice/send`, { method: 'POST' }),
  },

  maintenance: {
    list: (page = 1, pageSize = 20) =>
      http<PaginatedResponse<MaintenanceLogResponse>>(`/api/admin/maintenance?page=${page}&pageSize=${pageSize}`),
    get:  (id: string) => http<MaintenanceLogResponse>(`/api/admin/maintenance/${id}`),
    // vehicleId goes in the URL; the rest in the body
    create: (vehicleId: string, body: Omit<CreateMaintenanceLogRequest, 'vehicleId'>) =>
      http<MaintenanceLogResponse>(`/api/admin/maintenance/vehicle/${vehicleId}`, { method: 'POST', body: JSON.stringify(body) }),
    overdue: () => http<MaintenanceLogResponse[]>('/api/admin/maintenance/overdue'),
  },

  tickets: {
    list: (page = 1, pageSize = 20, status?: string, priority?: string) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status)   params.set('status', status);
      if (priority) params.set('priority', priority);
      return http<PaginatedResponse<SupportTicketResponse>>(`/api/admin/support-tickets?${params}`);
    },
    get:            (id: string) => http<SupportTicketResponse>(`/api/admin/support-tickets/${id}`),
    updateStatus:   (id: string, status: string, adminNotes?: string | null) =>
      http<SupportTicketResponse>(`/api/admin/support-tickets/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status, adminNotes }) }),
    updatePriority: (id: string, priority: string) =>
      http<SupportTicketResponse>(`/api/admin/support-tickets/${id}/priority`, { method: 'PATCH', body: JSON.stringify({ priority }) }),
  },

  payments: {
    list: (page = 1, pageSize = 20, status?: string) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status) params.set('status', status);
      return http<PaginatedResponse<PaymentResponse>>(`/api/admin/payments?${params}`);
    },
    get:    (id: string) => http<PaymentResponse>(`/api/admin/payments/${id}`),
    refund: (id: string) => http<PaymentResponse>(`/api/admin/payments/${id}/refund`, { method: 'POST' }),
  },

  discountCodes: {
    list: (page = 1, pageSize = 20, isActive?: boolean) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (isActive !== undefined) params.set('isActive', String(isActive));
      return http<PaginatedResponse<DiscountCodeResponse>>(`/api/admin/discount-codes?${params}`);
    },
    get:    (id: string) => http<DiscountCodeResponse>(`/api/admin/discount-codes/${id}`),
    create: (body: CreateDiscountCodeRequest) =>
      http<DiscountCodeResponse>('/api/admin/discount-codes', { method: 'POST', body: JSON.stringify(body) }),
    setStatus: (id: string, isActive: boolean) =>
      http<DiscountCodeResponse>(`/api/admin/discount-codes/${id}/status`, { method: 'PATCH', body: JSON.stringify({ isActive }) }),
    delete: (id: string) =>
      http<void>(`/api/admin/discount-codes/${id}`, { method: 'DELETE' }),
  },

  telemetry: {
    latest:  (vehicleId: string) =>
      http<TelemetryResponse>(`/api/admin/telemetry/${vehicleId}/latest`),
    history: (vehicleId: string, from: string, to: string) =>
      http<TelemetryResponse[]>(`/api/admin/telemetry/${vehicleId}?from=${from}&to=${to}`),
  },

  diagnostics: {
    list:     (page = 1, pageSize = 20, severity?: string) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (severity) params.set('severity', severity);
      return http<PaginatedResponse<SensorDiagnosticResponse>>(`/api/admin/diagnostics?${params}`);
    },
    critical: (limit = 50) =>
      http<SensorDiagnosticResponse[]>(`/api/admin/diagnostics/critical?limit=${limit}`),
    byVehicle: (vehicleId: string, page = 1, pageSize = 20) =>
      http<PaginatedResponse<SensorDiagnosticResponse>>(`/api/admin/diagnostics/${vehicleId}?page=${page}&pageSize=${pageSize}`),
  },
};

// ─── Passenger (public) API ───────────────────────────────────────────────────
export const passengerApi = {
  profile: {
    get:    () => http<PassengerResponse>('/api/public/passengers/me'),
    create: (body: CreatePassengerRequest) =>
      http<PassengerResponse>('/api/public/passengers', { method: 'POST', body: JSON.stringify(body) }),
    update: (body: UpdatePassengerRequest) =>
      http<PassengerResponse>('/api/public/passengers/me', { method: 'PUT', body: JSON.stringify(body) }),
  },

  rides: {
    history: (page = 1, pageSize = 20) =>
      http<PaginatedResponse<RideResponse>>(`/api/public/rides/history?page=${page}&pageSize=${pageSize}`),
    active:  () => http<RideResponse>('/api/public/rides/active'),
    vehiclePosition: () => http<{ latitude: number; longitude: number; speedKmh: number; batteryPct: number; timestamp: string; rideStatus: string }>('/api/public/rides/active/vehicle-position'),
    get:     (id: string) => http<RideResponse>(`/api/public/rides/${id}`),
    book:    (body: CreateRideRequest) =>
      http<RideResponse>('/api/public/rides', { method: 'POST', body: JSON.stringify(body) }),
    cancel:  (id: string) =>
      http<RideResponse>(`/api/public/rides/${id}/cancel`, { method: 'POST' }),
    estimate: (body: CreateRideRequest) =>
      http<PricingResult>('/api/public/rides/estimate', { method: 'POST', body: JSON.stringify(body) }),
    invoice: (id: string) => httpBlob(`/api/public/rides/${id}/invoice`),
  },

  vehicles: {
    nearest: (lat: number, lng: number, type?: string) => {
      const params = new URLSearchParams({ lat: String(lat), lng: String(lng) });
      if (type) params.set('type', type);
      return http<VehicleResponse>(`/api/public/vehicles/nearest?${params}`);
    },
  },

  discountCodes: {
    validate: (code: string, estimatedRideValue?: number) =>
      http<{ isValid: boolean; discountAmount: number }>(
        `/api/public/discount-codes/validate`,
        { method: 'POST', body: JSON.stringify({ code, estimatedRideValue: estimatedRideValue ?? 0 }) }
      ),
  },

  pricing: {
    estimate: (body: CreateRideRequest) =>
      http<PricingResult>('/api/public/pricing/estimate', { method: 'POST', body: JSON.stringify(body) }),
  },

  tickets: {
    list:   (page = 1, pageSize = 10) =>
      http<PaginatedResponse<SupportTicketResponse>>(`/api/public/support-tickets?page=${page}&pageSize=${pageSize}`),
    create: (body: CreateSupportTicketRequest) =>
      http<SupportTicketResponse>('/api/public/support-tickets', { method: 'POST', body: JSON.stringify(body) }),
  },
};
