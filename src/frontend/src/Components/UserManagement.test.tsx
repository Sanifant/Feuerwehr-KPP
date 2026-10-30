import { render, screen, waitFor, within } from '@testing-library/react';
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

const existingUser = {
    id: 'user-1', email: 'ada@example.com', firstName: 'Ada', lastName: 'Muster', fullName: 'Ada Muster',
    isActive: true, passwordResetRequired: false, roles: ['Viewer'], createdAt: '2026-10-20T10:00:00Z',
    fireDepartmentId: 1 as number | null,
};
const fireDepartments = [{ id: 2, name: 'Feuerwehr B' }, { id: 1, name: 'Feuerwehr A' }];

function mockUsers(users: typeof existingUser[]) {
    vi.mocked(apiClient.get).mockImplementation(async url => ({ data: url === '/api/FireDepartment' ? fireDepartments : users }));
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
        vi.resetAllMocks();
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        mockUsers([]);
        vi.mocked(apiClient.post).mockResolvedValue({ data: {} });
        vi.mocked(apiClient.put).mockResolvedValue({ data: {} });
    });

    it('creates a user without a password and reports that an invitation was sent', async () => {
        const user = await fill();
        expect(screen.queryByLabelText(/^Passwort/)).not.toBeInTheDocument();
        await user.click(screen.getByRole('button', { name: 'Benutzer erstellen' }));
        await waitFor(() => expect(apiClient.post).toHaveBeenCalledWith('/api/Users', {
            email: 'ada@example.com', firstName: 'Ada', lastName: 'Muster', roles: ['Viewer'], fireDepartmentId: null,
        }));
        expect(await screen.findByRole('status')).toHaveTextContent('Einladung');
        expect(vi.mocked(apiClient.get).mock.calls.filter(([url]) => url === '/api/Users')).toHaveLength(2);
        expect(vi.mocked(apiClient.get).mock.calls.filter(([url]) => url === '/api/FireDepartment')).toHaveLength(2);
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

    it('resends the selected invitation and shows the account lock', async () => {
        mockUsers([existingUser, {
            ...existingUser, id: 'user-2', fullName: 'Bea Beispiel', email: 'bea@example.com',
        }]);
        const user = show();
        await user.click(await screen.findByRole('button', { name: 'Einladung für Ada Muster erneut senden' }));
        expect(apiClient.post).toHaveBeenCalledWith('/api/Users/user-1/resend-invitation');
        expect(await screen.findByRole('status')).toHaveTextContent('Einladung an ada@example.com erneut versendet');
        const adaRow = screen.getByText('Ada Muster').closest('tr')!;
        const beaRow = screen.getByText('Bea Beispiel').closest('tr')!;
        expect(within(adaRow).getByText('Gesperrt – Passwortvergabe ausstehend')).toBeInTheDocument();
        expect(within(beaRow).getByText('Aktiv')).toBeInTheDocument();
        await user.click(screen.getByRole('button', { name: 'Ada Muster bearbeiten' }));
        expect(screen.getByLabelText('Passwort (optional)')).toBeDisabled();
    });

    it('prevents duplicate sends while the request is pending and allows resending afterward', async () => {
        mockUsers([{ ...existingUser, passwordResetRequired: true }]);
        let finish!: () => void;
        vi.mocked(apiClient.post).mockImplementationOnce(() => new Promise(resolve => {
            finish = () => resolve({ data: {} });
        }));
        const user = show();
        const button = await screen.findByRole('button', { name: 'Einladung für Ada Muster erneut senden' });
        await user.click(button);
        expect(button).toBeDisabled();
        expect(button).toHaveTextContent('Wird versendet');
        await user.click(button);
        expect(apiClient.post).toHaveBeenCalledTimes(1);
        finish();
        await waitFor(() => expect(button).toBeEnabled());
    });

    it('shows a resend failure and keeps the user list available for retry', async () => {
        mockUsers([existingUser]);
        vi.mocked(apiClient.post).mockRejectedValueOnce({
            isAxiosError: true, response: { data: { message: 'Die Einladung konnte nicht versendet werden.' } },
        });
        const user = show();
        const button = await screen.findByRole('button', { name: 'Einladung für Ada Muster erneut senden' });
        await user.click(button);
        expect(await screen.findByRole('alert')).toHaveTextContent('Die Einladung konnte nicht versendet werden.');
        expect(screen.getByText('Aktiv')).toBeInTheDocument();
        expect(button).toBeEnabled();
        await user.click(button);
        expect(await screen.findByRole('status')).toHaveTextContent('erneut versendet');
        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('disables invitations for inactive accounts', async () => {
        mockUsers([{ ...existingUser, isActive: false }]);
        show();
        expect(await screen.findByRole('button', { name: 'Einladung für Ada Muster erneut senden' })).toBeDisabled();
    });

    it('loads the existing departments and submits the selected department ID when creating a user', async () => {
        const user = await fill();
        const select = screen.getByRole('combobox', { name: 'Feuerwehr' });
        expect(within(select).getAllByRole('option').map(option => option.textContent)).toEqual(['Keine Zuordnung', 'Feuerwehr A', 'Feuerwehr B']);
        await user.selectOptions(select, '2');
        await user.click(screen.getByRole('button', { name: 'Benutzer erstellen' }));
        await waitFor(() => expect(apiClient.post).toHaveBeenCalledWith('/api/Users', expect.objectContaining({ fireDepartmentId: 2 })));
    });

    it('shows the department name in the table and preselects it when editing', async () => {
        mockUsers([existingUser]); const user = show();
        const row = (await screen.findByText(existingUser.fullName)).closest('tr')!;
        expect(within(row).getByText('Feuerwehr A')).toBeVisible();
        await user.click(screen.getByRole('button', { name: 'Ada Muster bearbeiten' }));
        expect(screen.getByRole('combobox', { name: 'Feuerwehr' })).toHaveValue('1');
        await user.selectOptions(screen.getByRole('combobox', { name: 'Feuerwehr' }), '2');
        await user.click(screen.getByRole('button', { name: 'Änderungen speichern' }));
        await waitFor(() => expect(apiClient.put).toHaveBeenCalledWith('/api/Users/user-1', expect.objectContaining({ fireDepartmentId: 2, clearFireDepartment: false })));
    });

    it('can explicitly remove an existing department assignment', async () => {
        mockUsers([existingUser]); const user = show();
        await user.click(await screen.findByRole('button', { name: 'Ada Muster bearbeiten' }));
        await user.selectOptions(screen.getByRole('combobox', { name: 'Feuerwehr' }), '');
        await user.click(screen.getByRole('button', { name: 'Änderungen speichern' }));
        await waitFor(() => expect(apiClient.put).toHaveBeenCalledWith('/api/Users/user-1', expect.objectContaining({ fireDepartmentId: null, clearFireDepartment: true })));
    });

    it('shows unassigned users and explains when no departments have been created', async () => {
        vi.mocked(apiClient.get).mockImplementation(async url => ({ data: url === '/api/FireDepartment' ? [] : [{ ...existingUser, fireDepartmentId: null }] }));
        const user = show();
        const row = (await screen.findByText(existingUser.fullName)).closest('tr')!;
        expect(within(row).getByText('Keine Zuordnung')).toBeVisible();
        await user.click(screen.getByRole('button', { name: 'Ada Muster bearbeiten' }));
        expect(screen.getByRole('combobox', { name: 'Feuerwehr' })).toHaveValue('');
        expect(screen.getByText(/Es sind noch keine Feuerwehren angelegt/)).toBeVisible();
    });
});
