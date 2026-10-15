import axios from 'axios';
import { expect, it, vi } from 'vitest';
import { setInvitedUserPassword } from './passwordApi';

vi.mock('axios', () => ({ default: { post: vi.fn().mockResolvedValue({ data: {} }) } }));

it('posts the invitation anonymously to the reset endpoint', async () => {
    const request = { token: '5aac0d66-a265-4f4a-90aa-fb32fb9641a4', email: 'ada@example.com', newPassword: 'Password123' };
    await setInvitedUserPassword(request);
    expect(axios.post).toHaveBeenCalledWith(`${import.meta.env.VITE_API_URL || ''}/api/Auth/reset-password`, request);
});
