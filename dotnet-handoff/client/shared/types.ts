// shared/types.ts
// TypeScript mirrors of the real NovaDrive .NET backend DTOs.

export type VehicleType    = 'Standard' | 'Van' | 'Luxury';
export type RideStatus     = 'Requested' | 'EnRoute' | 'Completed' | 'Cancelled';
export type PaymentStatus  = 'Pending' | 'Successful' | 'Failed' | 'Refunded';
export type PaymentMethod  = 'CreditCard' | 'PayPal' | 'BankTransfer';
export type DiscountType   = 'Percentage' | 'Flat';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TicketStatus   = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type UserRole       = 'Passenger' | 'Admin';
export type DiagSeverity   = 'Info' | 'Warning' | 'Critical';

// ─── Pagination ───────────────────────────────────────────────────────────────
export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ─── User ─────────────────────────────────────────────────────────────────────
export interface UserResponse {
  id: string;
  auth0UserId: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

// ─── Vehicle ──────────────────────────────────────────────────────────────────
export interface VehicleResponse {
  id: string;
  vin: string;
  licensePlate: string;
  model: string;
  vehicleType: VehicleType;
  yearOfManufacture: number;
  isActive: boolean;
  currentLatitude: number | null;
  currentLongitude: number | null;
  currentBatteryPercentage: number | null;
  currentMileage: number | null;
}

export interface CreateVehicleRequest {
  vin: string;
  licensePlate: string;
  model: string;
  vehicleType: VehicleType;
  yearOfManufacture: number;
  latitude?: number | null;
  longitude?: number | null;
}

export interface UpdateVehicleRequest {
  licensePlate?: string | null;
  model?: string | null;
  vehicleType?: VehicleType | null;
  isActive?: boolean | null;
}

export interface FleetStatsResponse {
  totalVehicles: number;
  activeVehicles: number;
  inactiveVehicles: number;
  standardCount: number;
  vanCount: number;
  luxuryCount: number;
  averageBatteryPercentage: number;
  activeRides: number;
}

// ─── Passenger ────────────────────────────────────────────────────────────────
export interface PassengerResponse {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  homeAddress: string | null;
  loyaltyPoints: number;
  preferredPaymentMethod: PaymentMethod;
  createdAt: string;
}

export interface CreatePassengerRequest {
  firstName: string;
  lastName: string;
  homeAddress?: string | null;
  preferredPaymentMethod: PaymentMethod;
}

export interface UpdatePassengerRequest {
  firstName?: string | null;
  lastName?: string | null;
  homeAddress?: string | null;
  preferredPaymentMethod?: PaymentMethod | null;
}

// ─── Ride ─────────────────────────────────────────────────────────────────────
export interface RideResponse {
  id: string;
  passengerId: string;
  passengerName: string;
  vehicleId: string | null;
  vehicleModel: string | null;
  vehicleLicensePlate: string | null;
  departureAddress: string;
  departureLatitude: number;
  departureLongitude: number;
  destinationAddress: string;
  destinationLatitude: number;
  destinationLongitude: number;
  vehicleType: VehicleType | null;
  status: RideStatus;
  requestedAt: string;
  startedAt: string | null;
  completedAt: string | null;
  distanceKm: number | null;
  durationMinutes: number | null;
  finalPrice: number | null;
  vatAmount: number | null;
  discountCode: string | null;
}

export interface CreateRideRequest {
  departureAddress: string;
  departureLatitude: number;
  departureLongitude: number;
  destinationAddress: string;
  destinationLatitude: number;
  destinationLongitude: number;
  vehicleType?: VehicleType | null;
  discountCode?: string | null;
}

export interface PricingResult {
  basePrice: number;
  vehicleMultiplier: number;
  afterMultiplier: number;
  isNightRate: boolean;
  nightSurcharge: number;
  subTotalBeforeDiscounts: number;
  loyaltyDiscount: number;
  loyaltyPointsUsed: number;
  codeDiscount: number;
  codeApplied: string | null;
  subTotalBeforeVat: number;
  vatRate: number;
  vatAmount: number;
  finalPrice: number;
}

// ─── Payment ──────────────────────────────────────────────────────────────────
export interface PaymentResponse {
  id: string;
  rideId: string;
  passengerId: string;
  passengerName: string | null;
  amount: number;
  currency: string;
  status: PaymentStatus;
  transactionReference: string | null;
  createdAt: string;
  paidAt: string | null;
}

// ─── Discount Code ────────────────────────────────────────────────────────────
export interface DiscountCodeResponse {
  id: string;
  code: string;
  type: DiscountType;
  value: number;
  minimumRideValue: number | null;
  expiresAt: string | null;
  isActive: boolean;
  maxUses: number | null;
  timesUsed: number;
}

export interface CreateDiscountCodeRequest {
  code: string;
  type: DiscountType;
  value: number;
  minimumRideValue: number;
  expiresAt: string;
  maxUses?: number | null;
}

// ─── Maintenance ──────────────────────────────────────────────────────────────
export interface MaintenanceLogResponse {
  id: string;
  vehicleId: string;
  vehicleModel: string | null;
  vehicleLicensePlate: string | null;
  serviceDate: string;
  description: string;
  technicianName: string;
  cost: number;
  nextServiceMileage: number | null;
}

export interface CreateMaintenanceLogRequest {
  vehicleId: string;
  serviceDate: string;
  description: string;
  technicianName: string;
  cost: number;
  nextServiceMileage?: number | null;
}

// ─── Support Ticket ───────────────────────────────────────────────────────────
export interface SupportTicketResponse {
  id: string;
  passengerId: string;
  passengerName: string;
  rideId: string | null;
  subject: string;
  description: string;
  priority: TicketPriority;
  status: TicketStatus;
  createdAt: string;
  resolvedAt: string | null;
  adminNotes: string | null;
}

export interface CreateSupportTicketRequest {
  subject: string;
  description: string;
  rideId?: string | null;
  priority: TicketPriority;
}

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
  adminNotes?: string | null;
}

export interface UpdateTicketPriorityRequest {
  priority: TicketPriority;
}

// ─── Telemetry ────────────────────────────────────────────────────────────────
export interface TelemetryResponse {
  vehicleId: string;
  latitude: number;
  longitude: number;
  speedKmh: number;
  batteryPercentage: number;
  hardwareTemperatureCelsius: number;
  timestamp: string;
}

// ─── Sensor Diagnostics ───────────────────────────────────────────────────────
export interface SensorDiagnosticResponse {
  id: string;
  vehicleId: string;
  vehicleModel: string | null;
  vehicleLicensePlate: string | null;
  severity: DiagSeverity;
  sensorType: string;
  errorCode: string;
  message: string;
  timestamp: string;
}

// ─── Auth Sync ────────────────────────────────────────────────────────────────
export interface AuthSyncResponse {
  userId: string;
  role: UserRole;
}
