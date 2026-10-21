import axios from 'axios';
import apiClient from './apiClient';

export interface ResetRequest {
    token: string;
    email: string;
    newPassword: string;
}

// Invitation redemption is anonymous and must not trigger the authenticated client's refresh flow.
export async function setInvitedUserPassword(request: ResetRequest): Promise<void> {
    await axios.post(`${import.meta.env.VITE_API_URL || ''}/api/Auth/reset-password`, request);
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export async function changeOwnPassword(request: ChangePasswordRequest): Promise<void> {
    await apiClient.post('/api/Auth/change-password', request);
}
