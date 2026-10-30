import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { SideBar } from './SideBar';
import { useRole } from './hooks/useRole';

vi.mock('./AuthContext', () => ({ useAuth: () => ({ isAuthenticated: true }) }));
vi.mock('./hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('./hooks/useActiveIncident', () => ({ useActiveIncident: () => ({ incident: null, loading: false, error: null }) }));

beforeEach(() => {
    vi.mocked(useRole).mockReturnValue({ canViewInventory: true, canManageInventory: true, isAdmin: false } as ReturnType<typeof useRole>);
});
function show(path = '/dashboard') { render(<MemoryRouter initialEntries={[path]}><SideBar /></MemoryRouter>); return userEvent.setup(); }

it('opens and closes the inventory tree and navigates to the dedicated type page', async () => {
    const user = show(); const trigger = screen.getByRole('button', { name: 'Inventar' });
    expect(trigger).toHaveAttribute('aria-expanded', 'false'); expect(screen.queryByRole('link', { name: 'Inventarübersicht' })).not.toBeInTheDocument();
    await user.click(trigger); expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Inventarübersicht' })).toHaveAttribute('href', '/inventory');
    await user.click(screen.getByRole('link', { name: 'Gerätetypen und Prüfpläne' }));
    expect(screen.getByRole('link', { name: 'Gerätetypen und Prüfpläne' })).toHaveAttribute('aria-current', 'page');
    await user.click(trigger); expect(screen.queryByRole('link', { name: 'Gerätetypen und Prüfpläne' })).not.toBeInTheDocument();
});

it('opens the inventory tree automatically on a direct type-page visit', () => {
    show('/inventory/types');
    expect(screen.getByRole('button', { name: 'Inventar' })).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Gerätetypen und Prüfpläne' })).toHaveAttribute('aria-current', 'page');
});

it('puts inventory categories in administration and activates that group for direct visits', () => {
    vi.mocked(useRole).mockReturnValue({ canViewInventory: true, canManageInventory: true, isAdmin: true } as ReturnType<typeof useRole>);
    show('/inventory/categories');
    expect(screen.getByRole('button', { name: 'Verwaltung' })).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('button', { name: 'Inventar' })).toHaveAttribute('aria-expanded', 'false');
    expect(screen.getByRole('link', { name: 'Inventarkategorien' })).toHaveAttribute('aria-current', 'page');
});

it('limits read-only viewers to the inventory overview', async () => {
    vi.mocked(useRole).mockReturnValue({ canViewInventory: true, canManageInventory: false, isAdmin: false } as ReturnType<typeof useRole>);
    const user = show('/inventory'); expect(screen.getByRole('link', { name: 'Inventarübersicht' })).toBeVisible();
    expect(screen.queryByRole('link', { name: 'Gerätetypen und Prüfpläne' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Verwaltung' }));
    expect(screen.queryByRole('link', { name: 'Inventarkategorien' })).not.toBeInTheDocument();
});

it('offers the MTW overview and opens vehicle management under administration for equipment officers', () => {
    vi.mocked(useRole).mockReturnValue({ canManageVehicles: true } as ReturnType<typeof useRole>);
    show('/vehicles/manage');
    expect(screen.getByRole('link', { name: 'MTW-Übersicht' })).toHaveAttribute('href', '/vehicles');
    expect(screen.getByRole('button', { name: 'Verwaltung' })).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Fahrzeug Verwaltung' })).toHaveAttribute('aria-current', 'page');
    expect(screen.queryByRole('link', { name: 'Fahrzeugtypen' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Fahrzeugstatus' })).not.toBeInTheDocument();
});

it('puts vehicle types under administration for administrators', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: true, canManageVehicles: true } as ReturnType<typeof useRole>);
    show('/vehicle-types');
    expect(screen.getByRole('button', { name: 'Verwaltung' })).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Fahrzeugtypen' })).toHaveAttribute('aria-current', 'page');
});

it('offers the extensible vehicle-status list to administrators', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: true, canManageVehicles: true } as ReturnType<typeof useRole>);
    show('/vehicle-statuses');
    expect(screen.getByRole('button', { name: 'Verwaltung' })).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('link', { name: 'Fahrzeugstatus' })).toHaveAttribute('aria-current', 'page');
});
