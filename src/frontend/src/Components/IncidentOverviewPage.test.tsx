import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { useAuth } from '../AuthContext';
import ActiveIncidentProvider from '../ActiveIncidentProvider';
import { SideBar } from '../SideBar';
import ActiveIncidentRoute from './ActiveIncidentRoute';
import IncidentOverviewPage from './IncidentOverviewPage';
import { acquireMapLease, closeIncident, createIncident, getActiveIncident } from '../api/incidentApi';
import type { EditorLease, IncidentSummary } from '../api/incidentApi';

vi.mock('../AuthContext', () => ({ useAuth: vi.fn() }));
vi.mock('../api/incidentApi', () => ({
    getActiveIncident: vi.fn(), createIncident: vi.fn(), closeIncident: vi.fn(),
    acquireMapLease: vi.fn(), buildCommandId: () => crypto.randomUUID(),
}));
vi.mock('./IncidentPositionMap', () => ({
    default: ({ onSelect }: { onSelect: (position: [number, number]) => void }) =>
        <button type="button" onClick={() => onSelect([10.25, 51.5])}>Position auf Karte wählen</button>,
}));

const sample: IncidentSummary = {
    id: 'incident-1', name: 'Wohnungsbrand', incidentNumber: '123', keyword: 'B3',
    originLatitude: 51.5, originLongitude: 10.25, status: 'Active',
    createdAtUtc: '2026-10-26T10:00:00Z', revision: 1,
    alarmedAtUtc: '2026-10-26T08:15:00Z',
};
const lease: EditorLease = {
    incidentId: sample.id, leaseToken: 'lease-token', sessionId: 'session-1', userId: 'me',
    userDisplayName: 'Ada Muster', expiresAtUtc: '2026-10-26T10:02:00Z',
    lastHeartbeatAtUtc: sample.createdAtUtc, isOwnedByCurrentSession: true, isExpired: false,
};
let active: IncidentSummary | null;
let roles: string[];

beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    active = null;
    roles = ['SituationMapEditor', 'IncidentDiaryEditor'];
    vi.mocked(useAuth).mockReturnValue({
        user: { id: 'me', fullName: 'Ada Muster', email: 'ada@example.com', roles },
        isAuthenticated: true, accessToken: 'token', refreshToken: 'refresh',
        login: vi.fn(), logout: vi.fn(), error: null, hasRole: role => roles.includes(role),
    });
    vi.mocked(getActiveIncident).mockImplementation(async () => {
        if (!active) throw { response: { status: 404 } };
        return active;
    });
    vi.mocked(createIncident).mockImplementation(async () => {
        active = sample;
        return { incident: sample, lease, serverUtc: sample.createdAtUtc };
    });
    vi.mocked(acquireMapLease).mockResolvedValue({ lease: { leaseToken: 'lease-token' } });
    vi.mocked(closeIncident).mockImplementation(async () => { active = null; });
});

function show(path = '/incidents') {
    render(<MemoryRouter initialEntries={[path]}>
        <ActiveIncidentProvider>
            <SideBar />
            <Routes>
                <Route path="/incidents" element={<IncidentOverviewPage />} />
                <Route element={<ActiveIncidentRoute />}>
                    <Route path="/incident-map" element={<h1>Aktive Lagekarte</h1>} />
                    <Route path="/incident-diary" element={<h1>Aktives Tagebuch</h1>} />
                </Route>
            </Routes>
        </ActiveIncidentProvider>
    </MemoryRouter>);
    return userEvent.setup();
}

it.each(['/incident-map', '/incident-diary'])('blocks direct access to %s without an active incident', async path => {
    show(path);
    expect(await screen.findByText(/Bitte zuerst einen aktiven Einsatz/)).toBeVisible();
    expect(screen.queryByRole('heading', { level: 1 })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Lagekarte' })).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByRole('link', { name: 'Einsatztagebuch' })).not.toHaveAttribute('href');
    expect(screen.getByRole('link', { name: 'Zur Einsatzübersicht' })).toHaveAttribute('href', '/incidents');
});

it('creates an incident with the selected map position and immediately enables both modules', async () => {
    const user = show();
    await user.type(await screen.findByLabelText('Einsatzname *'), 'Wohnungsbrand');
    await user.type(screen.getByLabelText('Einsatznummer'), '123');
    fireEvent.change(screen.getByLabelText('Alarmierungszeitpunkt *'), { target: { value: '2026-10-26T09:15' } });
    await user.click(screen.getByRole('button', { name: 'Position auf Karte wählen' }));
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(await screen.findByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
    expect(screen.getByText(new Date(sample.alarmedAtUtc).toLocaleString('de-DE'))).toBeVisible();
    expect(createIncident).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({
        name: 'Wohnungsbrand', incidentNumber: '123', originLongitude: 10.25, originLatitude: 51.5,
        sessionId: sessionStorage.getItem('incident-session-id'), commandId: expect.any(String),
        alarmedAtUtc: new Date('2026-10-26T09:15').toISOString(),
    }));
    expect(screen.getByRole('link', { name: 'Einsatztagebuch' })).toHaveAttribute('href', '/incident-diary');
    await user.click(screen.getByRole('link', { name: 'Lagekarte' }));
    expect(await screen.findByRole('heading', { name: 'Aktive Lagekarte' })).toBeVisible();
});

it('shows the created incident immediately even when a subsequent GET would fail', async () => {
    // The POST response is authoritative; no extra read is needed for the new incident.
    vi.mocked(createIncident).mockResolvedValue({ incident: sample, lease, serverUtc: sample.createdAtUtc });
    const user = show();
    await user.type(await screen.findByLabelText('Einsatzname *'), 'Wohnungsbrand');
    await user.click(screen.getByRole('button', { name: 'Position auf Karte wählen' }));
    const reads = vi.mocked(getActiveIncident).mock.calls.length;
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(await screen.findByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
    expect(getActiveIncident).toHaveBeenCalledTimes(reads);
    expect(screen.queryByLabelText('Einsatzname *')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Lagekarte' })).toHaveAttribute('href', '/incident-map');
});

it('requires an alarm date and time before submitting', async () => {
    const user = show();
    await user.type(await screen.findByLabelText('Einsatzname *'), 'Wohnungsbrand');
    await user.click(screen.getByRole('button', { name: 'Position auf Karte wählen' }));
    fireEvent.change(screen.getByLabelText('Alarmierungszeitpunkt *'), { target: { value: '' } });
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(createIncident).not.toHaveBeenCalled();
});

it('keeps the created incident when an older in-flight lookup returns no active incident', async () => {
    const user = show();
    await user.type(await screen.findByLabelText('Einsatzname *'), 'Wohnungsbrand');
    await user.click(screen.getByRole('button', { name: 'Position auf Karte wählen' }));
    let rejectLookup: (error: unknown) => void = () => {};
    vi.mocked(getActiveIncident).mockReturnValueOnce(new Promise<IncidentSummary>((_resolve, reject) => { rejectLookup = reject; }));
    fireEvent(window, new Event('focus'));
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(await screen.findByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
    await act(async () => { rejectLookup({ response: { status: 404 } }); });
    expect(screen.getByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
    expect(screen.getByRole('link', { name: 'Lagekarte' })).toHaveAttribute('href', '/incident-map');
});

it('validates the position and keeps the creation form when the request fails', async () => {
    const user = show();
    await user.type(await screen.findByLabelText('Einsatzname *'), 'Wohnungsbrand');
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(createIncident).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Position auf Karte wählen' }));
    vi.mocked(createIncident).mockRejectedValueOnce({ response: { data: { title: 'Anlegen fehlgeschlagen.' } } });
    await user.click(screen.getByRole('button', { name: 'Einsatz anlegen' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Anlegen fehlgeschlagen.');
    expect(screen.getByLabelText('Einsatzname *')).toHaveValue('Wohnungsbrand');
    expect(screen.getByRole('link', { name: 'Lagekarte' })).toHaveAttribute('aria-disabled', 'true');
});

it('requires confirmation, closes with a valid lease and disables both modules again', async () => {
    active = sample;
    const user = show();
    await user.click(await screen.findByRole('button', { name: 'Einsatz beenden' }));
    await user.click(screen.getByRole('button', { name: 'Abbrechen' }));
    expect(acquireMapLease).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Einsatz beenden' }));
    await user.click(screen.getByRole('button', { name: 'Endgültig beenden' }));
    expect(await screen.findByRole('heading', { name: 'Kein aktiver Einsatz' })).toBeVisible();
    expect(closeIncident).toHaveBeenCalledExactlyOnceWith(sample.id, {
        leaseToken: 'lease-token', sessionId: sessionStorage.getItem('incident-session-id'), commandId: expect.any(String),
    });
    expect(screen.getByRole('link', { name: 'Lagekarte' })).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByRole('link', { name: 'Einsatztagebuch' })).toHaveAttribute('aria-disabled', 'true');
});

it('does not close an incident while another session holds the editing lease', async () => {
    active = sample;
    vi.mocked(acquireMapLease).mockRejectedValueOnce({ response: { status: 409, data: {
        title: 'Lagekarte wird bereits bearbeitet.', detail: 'Aktiv: Max Muster',
    } } });
    const user = show();
    await user.click(await screen.findByRole('button', { name: 'Einsatz beenden' }));
    await user.click(screen.getByRole('button', { name: 'Endgültig beenden' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Aktiv: Max Muster');
    expect(closeIncident).not.toHaveBeenCalled();
    expect(screen.getByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
});

it('keeps viewing available for module viewers without granting incident management', async () => {
    roles = ['SituationMapViewer', 'IncidentDiaryViewer'];
    const user = show();
    expect(await screen.findByRole('heading', { name: 'Kein aktiver Einsatz' })).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Einsatz anlegen' })).not.toBeInTheDocument();
    active = sample;
    await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
    expect(await screen.findByRole('heading', { name: 'Aktiver Einsatz: Wohnungsbrand' })).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Einsatz beenden' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('link', { name: 'Einsatztagebuch' }));
    expect(await screen.findByRole('heading', { name: 'Aktives Tagebuch' })).toBeVisible();
});

it('locks an open module when a focus refresh detects that the incident was closed elsewhere', async () => {
    active = sample;
    show('/incident-diary');
    expect(await screen.findByRole('heading', { name: 'Aktives Tagebuch' })).toBeVisible();
    active = null;
    fireEvent(window, new Event('focus'));
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Aktives Tagebuch' })).not.toBeInTheDocument());
    expect(screen.getByText(/Bitte zuerst einen aktiven Einsatz/)).toBeVisible();
});

it('distinguishes server failures from no active incident and allows retrying', async () => {
    vi.mocked(getActiveIncident).mockRejectedValueOnce(new Error('Offline'));
    const user = show();
    expect(await screen.findByRole('alert')).toHaveTextContent('konnte nicht geladen werden');
    expect(screen.queryByLabelText('Einsatzname *')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
    expect(await screen.findByLabelText('Einsatzname *')).toBeVisible();
});
