import axios from 'axios';
import { expect, it, vi } from 'vitest';
import { changeOwnPassword, setInvitedUserPassword } from './passwordApi';
import apiClient from './apiClient';

vi.mock('axios', () => ({ default: { post: vi.fn().mockResolvedValue({ data: {} }) } }));
vi.mock('./apiClient', () => ({ default: { post: vi.fn().mockResolvedValue({ data: {} }) } }));

it('posts the invitation anonymously to the reset endpoint', async () => {
    const request = { token: '5aac0d66-a265-4f4a-90aa-fb32fb9641a4', email: 'ada@example.com', newPassword: 'Password123' };
    await setInvitedUserPassword(request);
    expect(axios.post).toHaveBeenCalledWith(`${import.meta.env.VITE_API_URL || ''}/api/Auth/reset-password`, request);
});

it('changes the signed-in account using the authenticated API client', async () => {
    const request = { currentPassword: 'CurrentPassword123', newPassword: 'NewPassword123' };
    await changeOwnPassword(request);
    expect(apiClient.post).toHaveBeenCalledWith('/api/Auth/change-password', request);
});
