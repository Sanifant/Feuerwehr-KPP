import axios from 'axios';

export interface ResetRequest {
    token: string;
    email: string;
    newPassword: string;
}

// Invitation redemption is anonymous and must not trigger the authenticated client's refresh flow.
export async function setInvitedUserPassword(request: ResetRequest): Promise<void> {
    await axios.post(`${import.meta.env.VITE_API_URL || ''}/api/Auth/reset-password`, request);
}
