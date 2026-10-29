import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import Navbar from './Navbar';
import { useAuth } from './AuthContext';
import { ChangePassword } from './Components/ChangePassword';
import { ActiveIncidentContext } from './hooks/useActiveIncident';

vi.mock('./AuthContext', () => ({ useAuth: vi.fn() }));
vi.mock('./api/passwordApi', () => ({ changeOwnPassword: vi.fn(), setInvitedUserPassword: vi.fn() }));
const logout = vi.fn().mockResolvedValue(undefined);

beforeEach(() => {
    vi.mocked(useAuth).mockReturnValue({
        isAuthenticated: true, logout, user: { id: 'me', fullName: 'Ada Muster', email: 'ada@example.com', roles: ['Viewer'] },
        accessToken: 'token', refreshToken: 'refresh', login: vi.fn(), error: null, hasRole: () => false,
    });
});

function show() {
    render(<MemoryRouter><ActiveIncidentContext.Provider value={{ incident: null, loading: false, error: null, refresh: vi.fn(), setActiveIncident: vi.fn() }}><Navbar /><button>Außerhalb</button><Routes>
        <Route path="/password" element={<ChangePassword />} />
    </Routes></ActiveIncidentContext.Provider></MemoryRouter>);
    return userEvent.setup();
}

it('opens the account menu without logging out', async () => {
    const user = show();
    const trigger = screen.getByRole('button', { name: 'Benutzermenü' });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    await user.click(trigger);
    expect(screen.getByRole('menuitem', { name: 'Passwort ändern' })).toBeVisible();
    expect(screen.getByRole('menuitem', { name: 'Abmelden' })).toBeVisible();
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(logout).not.toHaveBeenCalled();
});

it('navigates to password change with the signed-in email prefilled', async () => {
    const user = show();
    await user.click(screen.getByRole('button', { name: 'Benutzermenü' }));
    await user.click(screen.getByRole('menuitem', { name: 'Passwort ändern' }));
    expect(screen.getByRole('heading', { name: 'Passwort ändern' })).toBeVisible();
    expect(screen.getByLabelText('E-Mail-Adresse')).toHaveValue('ada@example.com');
    expect(screen.getByLabelText('E-Mail-Adresse')).toHaveAttribute('readonly');
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
});

it('logs out only when the logout item is selected', async () => {
    const user = show();
    await user.click(screen.getByRole('button', { name: 'Benutzermenü' }));
    await user.click(screen.getByRole('menuitem', { name: 'Abmelden' }));
    expect(logout).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
});

it('closes on outside clicks and Escape, restoring focus on Escape', async () => {
    const user = show();
    const trigger = screen.getByRole('button', { name: 'Benutzermenü' });
    await user.click(trigger);
    await user.click(screen.getByRole('button', { name: 'Außerhalb' }));
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    await user.click(trigger);
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
});

it('supports keyboard opening and arrow navigation', async () => {
    const user = show();
    screen.getByRole('button', { name: 'Benutzermenü' }).focus();
    await user.keyboard('{ArrowDown}');
    expect(screen.getByRole('menuitem', { name: 'Passwort ändern' })).toHaveFocus();
    await user.keyboard('{ArrowDown}');
    expect(screen.getByRole('menuitem', { name: 'Abmelden' })).toHaveFocus();
    await user.keyboard('{Home}');
    expect(screen.getByRole('menuitem', { name: 'Dokumentation' })).toHaveFocus();
});

it('shows login instead of the menu when signed out', () => {
    vi.mocked(useAuth).mockReturnValue({ isAuthenticated: false, logout, user: null,
        accessToken: null, refreshToken: null, login: vi.fn(), error: null, hasRole: () => false });
    show();
    expect(screen.getByRole('link', { name: 'Anmelden' })).toHaveAttribute('href', '/login');
    expect(screen.queryByRole('button', { name: 'Benutzermenü' })).not.toBeInTheDocument();
});
