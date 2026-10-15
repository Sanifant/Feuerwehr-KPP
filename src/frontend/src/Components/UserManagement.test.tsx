import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import UserManagement from './UserManagement';
import apiClient from '../api/apiClient';
import { useRole } from '../hooks/useRole';

vi.mock('../api/apiClient', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }));
vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));

function show() {
    render(<MemoryRouter initialEntries={['/users']}><Routes>
        <Route path="/users" element={<UserManagement />} />
        <Route path="/dashboard" element={<p>Dashboard</p>} />
    </Routes></MemoryRouter>);
    return userEvent.setup();
}

async function fill() {
    const user = show();
    await user.click(await screen.findByRole('button', { name: '+ Neuen Benutzer erstellen' }));
    await user.type(screen.getByLabelText('Vorname *'), 'Ada');
    await user.type(screen.getByLabelText('Nachname *'), 'Muster');
    await user.type(screen.getByLabelText('E-Mail-Adresse *'), 'ada@example.com');
    await user.click(screen.getByRole('checkbox', { name: 'Viewer' }));
    return user;
}

describe('UserManagement invitations', () => {
    beforeEach(() => {
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        vi.mocked(apiClient.get).mockResolvedValue({ data: [] });
        vi.mocked(apiClient.post).mockResolvedValue({ data: {} });
    });

    it('creates a user without a password and reports that an invitation was sent', async () => {
        const user = await fill();
        expect(screen.queryByLabelText(/^Passwort/)).not.toBeInTheDocument();
        await user.click(screen.getByRole('button', { name: 'Benutzer erstellen' }));
        await waitFor(() => expect(apiClient.post).toHaveBeenCalledWith('/api/Users', {
            email: 'ada@example.com', firstName: 'Ada', lastName: 'Muster', roles: ['Viewer'],
        }));
        expect(await screen.findByRole('status')).toHaveTextContent('Einladung');
        expect(apiClient.get).toHaveBeenCalledTimes(2);
    });

    it('keeps the form open and displays a mail delivery failure', async () => {
        vi.mocked(apiClient.post).mockRejectedValueOnce({
            isAxiosError: true, response: { data: { message: 'Die Einladung konnte nicht versendet werden.' } },
        });
        const user = await fill();
        await user.click(screen.getByRole('button', { name: 'Benutzer erstellen' }));
        expect(await screen.findByText('Die Einladung konnte nicht versendet werden.')).toBeInTheDocument();
        expect(screen.getByLabelText('E-Mail-Adresse *')).toHaveValue('ada@example.com');
        expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('requires personal details and a role before sending', async () => {
        const user = show();
        await user.click(await screen.findByRole('button', { name: '+ Neuen Benutzer erstellen' }));
        await user.click(screen.getByRole('button', { name: 'Benutzer erstellen' }));
        expect(screen.getByText('Vorname ist erforderlich.')).toBeInTheDocument();
        expect(apiClient.post).not.toHaveBeenCalled();
    });

    it('redirects a non-admin without loading users', async () => {
        vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
        show();
        expect(await screen.findByText('Dashboard')).toBeInTheDocument();
        expect(apiClient.get).not.toHaveBeenCalled();
    });
});
