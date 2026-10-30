import apiClient from './apiClient';

export type VehicleDayStatus = 'Free' | 'Pending' | 'Booked' | 'Unavailable';
export type VehicleReservationStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Cancelled';
export interface Vehicle {
    id: string; fireDepartmentId: number; fireDepartmentName: string; radioCallSign: string;
    name: string | null; licensePlate: string | null; equipmentOfficerId: string; equipmentOfficerName: string;
    isActive: boolean; version: number; canEdit: boolean; canBookDirectly: boolean;
    vehicleTypeId: string | null; vehicleTypeName: string | null;
    vehicleStatusId: string; vehicleStatusName: string; allowsBooking: boolean;
    statusChangedAtUtc: string | null;
}
export interface VehicleType { id: string; name: string; isActive: boolean; version: number }
export interface VehicleStatus { id: string; name: string; allowsBooking: boolean; isActive: boolean; version: number; isDefault: boolean }
export interface VehicleAvailability { vehicle: Vehicle; days: { date: string; status: VehicleDayStatus }[] }
export interface VehicleManagementOptions {
    departments: { id: number; name: string }[];
    officers: { id: string; name: string; fireDepartmentId: number }[];
    defaultStatusId: string;
}
export interface SaveVehicle {
    fireDepartmentId: number; radioCallSign: string; name: string; licensePlate: string;
    equipmentOfficerId: string; expectedVersion: number;
    vehicleTypeId: string | null;
    vehicleStatusId: string | null;
}
export interface VehicleReservation {
    id: string; vehicleId: string; radioCallSign: string; fireDepartmentName: string; from: string; to: string;
    comment: string | null; status: VehicleReservationStatus; requesterName: string; createdAtUtc: string;
    decidedAtUtc: string | null; isDirectBooking: boolean; version: number; canDecide: boolean; canCancel: boolean;
}
export interface CreateVehicleReservation { commandId: string; from: string; to: string; comment: string }

export const vehicleDayLabels: Record<VehicleDayStatus, string> = { Free: 'Frei', Pending: 'Reserviert – Anfrage offen', Booked: 'Gebucht', Unavailable: 'Nicht verfügbar' };
export const reservationStatusLabels: Record<VehicleReservationStatus, string> = {
    Pending: 'Anfrage offen', Confirmed: 'Gebucht', Rejected: 'Abgelehnt', Cancelled: 'Storniert',
};
export const getVehicles = async (includeInactive = false) => (await apiClient.get<Vehicle[]>('/api/vehicles', { params: { includeInactive } })).data;
export const getVehicleTypes = async (includeInactive = false) => (await apiClient.get<VehicleType[]>('/api/vehicles/types', { params: { includeInactive } })).data;
export const getVehicleStatuses = async (includeInactive = false) => (await apiClient.get<VehicleStatus[]>('/api/vehicles/statuses', { params: { includeInactive } })).data;
export const saveVehicleStatus = async (id: string | null, request: { name: string; allowsBooking: boolean; isActive: boolean; expectedVersion: number }) =>
    (id ? await apiClient.put<VehicleStatus>(`/api/vehicles/statuses/${id}`, request) : await apiClient.post<VehicleStatus>('/api/vehicles/statuses', request)).data;
export const saveVehicleType = async (id: string | null, request: { name: string; expectedVersion: number }) =>
    (id ? await apiClient.put<VehicleType>(`/api/vehicles/types/${id}`, request) : await apiClient.post<VehicleType>('/api/vehicles/types', request)).data;
export const setVehicleTypeActive = async (type: VehicleType, isActive: boolean) =>
    (await apiClient.put<VehicleType>(`/api/vehicles/types/${type.id}/active`, { isActive, expectedVersion: type.version })).data;
export const getVehicleAvailability = async (from: string, to: string, vehicleType?: string) =>
    (await apiClient.get<VehicleAvailability[]>('/api/vehicles/availability', { params: { from, to, vehicleType } })).data;
export const getVehicleManagementOptions = async () => (await apiClient.get<VehicleManagementOptions>('/api/vehicles/management-options')).data;
export const saveVehicle = async (id: string | null, request: SaveVehicle) =>
    (id ? await apiClient.put<Vehicle>(`/api/vehicles/${id}`, request) : await apiClient.post<Vehicle>('/api/vehicles', request)).data;
export const setVehicleActive = async (vehicle: Vehicle, isActive: boolean) =>
    (await apiClient.put<Vehicle>(`/api/vehicles/${vehicle.id}/active`, { isActive, expectedVersion: vehicle.version })).data;
export const getVehicleReservations = async (scope: 'mine' | 'assigned') =>
    (await apiClient.get<VehicleReservation[]>('/api/vehicles/reservations', { params: { scope } })).data;
export const reserveVehicle = async (id: string, request: CreateVehicleReservation, direct: boolean) =>
    (await apiClient.post<VehicleReservation>(`/api/vehicles/${id}/${direct ? 'direct-bookings' : 'reservations'}`, request)).data;
export const decideVehicleReservation = async (reservation: VehicleReservation, decision: 'approve' | 'reject' | 'cancel') =>
    (await apiClient.post<VehicleReservation>(`/api/vehicles/reservations/${reservation.id}/${decision}`, { expectedVersion: reservation.version })).data;

// Date-only values stay in the local calendar, without a UTC conversion.
export function vehicleDate(value: Date): string {
    return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
}
export const formatVehicleDate = (value: string) => new Date(`${value}T12:00:00`).toLocaleDateString('de-DE');

export function vehicleCalendarRange(reference = new Date()) {
    const day = reference.getDate();
    const year = reference.getFullYear();
    const month = reference.getMonth();
    return {
        today: vehicleDate(reference),
        from: vehicleDate(new Date(year, month, day - 3)),
        to: vehicleDate(new Date(year, month, day + 20)),
        days: Array.from({ length: 24 }, (_, index) => vehicleDate(new Date(year, month, day - 3 + index))),
    };
}
