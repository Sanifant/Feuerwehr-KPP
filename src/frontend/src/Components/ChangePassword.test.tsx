import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ChangePassword } from './ChangePassword';
import { setInvitedUserPassword } from '../api/passwordApi';

vi.mock('../api/passwordApi', () => ({ setInvitedUserPassword: vi.fn(), changeOwnPassword: vi.fn() }));
const token = '5aac0d66-a265-4f4a-90aa-fb32fb9641a4';

function show(pathToken = token) {
    render(<MemoryRouter initialEntries={[`/password/${pathToken}`]}><Routes>
        <Route path="/password/:token" element={<ChangePassword />} />
    </Routes></MemoryRouter>);
    return userEvent.setup();
}

async function fill(password = 'NewPassword123', repeat = password) {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('E-Mail-Adresse'), 'user@example.com');
    await user.type(screen.getByLabelText('Neues Passwort'), password);
    await user.type(screen.getByLabelText('Passwort wiederholen'), repeat);
    return user;
}

describe('ChangePassword invitation', () => {
    beforeEach(() => vi.mocked(setInvitedUserPassword).mockResolvedValue(undefined));

    it('submits the UUID, email and chosen password and offers login after success', async () => {
        show();
        const user = await fill();
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        await waitFor(() => expect(setInvitedUserPassword).toHaveBeenCalledWith({
            token, email: 'user@example.com', newPassword: 'NewPassword123',
        }));
        expect(await screen.findByRole('status')).toHaveTextContent('Passwort wurde gespeichert');
        expect(screen.getByRole('link', { name: 'Zur Anmeldung' })).toHaveAttribute('href', '/login');
    });

    it('requires the email address before contacting the API', async () => {
        const user = show();
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(screen.getByRole('alert')).toHaveTextContent('E-Mail-Adresse');
        expect(setInvitedUserPassword).not.toHaveBeenCalled();
    });

    it('keeps password confirmation independent and rejects a mismatch', async () => {
        show();
        const user = await fill('NewPassword123', 'DifferentPassword123');
        expect(screen.getByLabelText('Neues Passwort')).toHaveValue('NewPassword123');
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(screen.getByRole('alert')).toHaveTextContent('stimmen nicht überein');
        expect(setInvitedUserPassword).not.toHaveBeenCalled();
    });

    it('rejects a weak password locally', async () => {
        show();
        const user = await fill('weak');
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(screen.getByRole('alert')).toHaveTextContent('mindestens 8 Zeichen');
        expect(setInvitedUserPassword).not.toHaveBeenCalled();
    });

    it('does not offer a form for a malformed UUID', () => {
        show('invalid');
        expect(screen.getByRole('alert')).toHaveTextContent('ungültig');
        expect(screen.queryByRole('button')).not.toBeInTheDocument();
    });

    it('shows an invalid or expired invitation response and allows retry', async () => {
        vi.mocked(setInvitedUserPassword).mockRejectedValueOnce({
            isAxiosError: true, response: { data: { message: 'Einladungslink oder E-Mail-Adresse ungültig oder abgelaufen.' } },
        });
        show();
        const user = await fill();
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(await screen.findByRole('alert')).toHaveTextContent('abgelaufen');
        expect(screen.getByRole('button', { name: 'Passwort speichern' })).toBeEnabled();
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(await screen.findByRole('status')).toBeInTheDocument();
    });

    it('disables duplicate submissions while waiting for the API', async () => {
        let finish: (() => void) | undefined;
        vi.mocked(setInvitedUserPassword).mockReturnValueOnce(new Promise<void>(resolve => { finish = resolve; }));
        show();
        const user = await fill();
        await user.click(screen.getByRole('button', { name: 'Passwort speichern' }));
        expect(screen.getByRole('button', { name: 'Wird gespeichert…' })).toBeDisabled();
        expect(screen.getByLabelText('E-Mail-Adresse')).toBeDisabled();
        finish!();
        expect(await screen.findByRole('status')).toBeInTheDocument();
        expect(setInvitedUserPassword).toHaveBeenCalledTimes(1);
    });
});
