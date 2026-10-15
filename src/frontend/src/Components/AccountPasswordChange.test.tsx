import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { ChangePassword } from './ChangePassword';
import { useAuth } from '../AuthContext';
import { changeOwnPassword, setInvitedUserPassword } from '../api/passwordApi';

vi.mock('../AuthContext', () => ({ useAuth: vi.fn() }));
vi.mock('../api/passwordApi', () => ({ changeOwnPassword: vi.fn(), setInvitedUserPassword: vi.fn() }));
const logout = vi.fn().mockResolvedValue(undefined);

beforeEach(() => {
    vi.mocked(useAuth).mockReturnValue({
        user: { id: 'me', fullName: 'Ada Muster', email: 'ada@example.com', roles: ['Viewer'] }, logout,
        isAuthenticated: true, accessToken: 'token', refreshToken: 'refresh', login: vi.fn(), error: null, hasRole: () => false,
    });
    vi.mocked(changeOwnPassword).mockResolvedValue(undefined);
});

function show() {
    render(<MemoryRouter initialEntries={['/password']}><Routes>
        <Route path="/password" element={<ChangePassword />} />
    </Routes></MemoryRouter>);
    return userEvent.setup();
}

it('requires the current password and never submits a different account identifier', async () => {
    const user = show();
    await user.type(screen.getByLabelText('Neues Passwort'), 'NewPassword123');
    await user.type(screen.getByLabelText('Passwort wiederholen'), 'NewPassword123');
    await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
    expect(screen.getByRole('alert')).toHaveTextContent('aktuelles Passwort');
    expect(changeOwnPassword).not.toHaveBeenCalled();
    await user.type(screen.getByLabelText('Aktuelles Passwort'), 'CurrentPassword123');
    await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Passwort wurde gespeichert');
    expect(changeOwnPassword).toHaveBeenCalledWith({ currentPassword: 'CurrentPassword123', newPassword: 'NewPassword123' });
    expect(setInvitedUserPassword).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Erneut anmelden' }));
    expect(logout).toHaveBeenCalledOnce();
});

it('shows an incorrect current-password error and leaves the form available', async () => {
    vi.mocked(changeOwnPassword).mockRejectedValueOnce({ isAxiosError: true, response: { data: { message: 'Das aktuelle Passwort ist falsch.' } } });
    const user = show();
    await user.type(screen.getByLabelText('Aktuelles Passwort'), 'WrongPassword123');
    await user.type(screen.getByLabelText('Neues Passwort'), 'NewPassword123');
    await user.type(screen.getByLabelText('Passwort wiederholen'), 'NewPassword123');
    await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('aktuelle Passwort ist falsch');
    expect(screen.getByRole('button', { name: 'Passwort speichern' })).toBeEnabled();
});
