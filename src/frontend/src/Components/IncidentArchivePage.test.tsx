import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterAll, beforeAll, beforeEach, expect, it, vi } from 'vitest';
import IncidentArchivePage from './IncidentArchivePage';
import { SideBar } from '../SideBar';
import { ActiveIncidentContext } from '../hooks/useActiveIncident';
import { useRole } from '../hooks/useRole';
import { downloadArchivedIncidentPdf, getArchivedDiaryEntries, getIncidentArchive } from '../api/incidentApi';
import type { DiaryEntry, IncidentSummary } from '../api/incidentApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../AuthContext', () => ({ useAuth: () => ({ isAuthenticated: true }) }));
vi.mock('../api/incidentApi', () => ({ getIncidentArchive: vi.fn(), getArchivedDiaryEntries: vi.fn(), downloadArchivedIncidentPdf: vi.fn() }));

const incident: IncidentSummary = {
    id: 'incident-1', name: 'Wohnungsbrand', incidentNumber: '123', keyword: 'B3', status: 'Closed',
    alarmedAtUtc: '2026-10-26T08:00:00Z', createdAtUtc: '2026-10-26T08:15:00Z', closedAtUtc: '2026-10-26T10:00:00Z',
    originLatitude: 51, originLongitude: 10, revision: 3,
};
const entry: DiaryEntry = {
    id: 'entry-1', incidentId: incident.id, entryNumber: 4, currentRevision: 2, isCanceled: true,
    cancellationReason: 'Doppelte Meldung', createdAtUtc: incident.createdAtUtc, updatedAtUtc: incident.closedAtUtc!,
    currentRevisionData: {
        id: 'revision-2', revisionNumber: 2, revisionType: 'Canceled', eventTimestampUtc: incident.createdAtUtc,
        categoryCode: 'meldung', categoryNameSnapshot: 'Meldung', text: 'Wasser am Rohr',
        sender: 'Florian 1', recipient: 'Einsatzleitung', transmissionType: 'Funk',
        cancellationReason: 'Doppelte Meldung', createdAtUtc: incident.closedAtUtc!, createdByDisplayName: 'Ada Muster',
    },
    revisions: [],
};
entry.revisions = [
    { ...entry.currentRevisionData, id: 'revision-1', revisionNumber: 1, revisionType: 'Created', text: 'Ursprüngliche Meldung', cancellationReason: undefined },
    entry.currentRevisionData,
];

beforeAll(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
        configurable: true, value(this: HTMLDialogElement) { this.setAttribute('open', ''); this.querySelector<HTMLElement>('[autofocus]')?.focus(); },
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
        configurable: true, value(this: HTMLDialogElement) { this.removeAttribute('open'); },
    });
});
afterAll(() => {
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal');
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'close');
});
beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
    vi.mocked(getIncidentArchive).mockResolvedValue([incident]);
    vi.mocked(getArchivedDiaryEntries).mockResolvedValue([entry]);
    vi.mocked(downloadArchivedIncidentPdf).mockResolvedValue(undefined);
});

function show() {
    render(<MemoryRouter initialEntries={['/incident-archive']}><ActiveIncidentContext.Provider value={{
        incident: null, loading: false, error: null, refresh: vi.fn(), setActiveIncident: vi.fn(),
    }}><SideBar /><IncidentArchivePage /></ActiveIncidentContext.Provider></MemoryRouter>);
    return userEvent.setup();
}

it('shows the archive for an admin without an active incident or additional module roles', async () => {
    show();
    expect(await screen.findByRole('button', { name: incident.name })).toBeVisible();
    expect(screen.getByRole('link', { name: 'Einsatzarchiv' })).toHaveAttribute('href', '/incident-archive');
    expect(screen.getByText('123')).toBeVisible();
    expect(screen.getByText('B3')).toBeVisible();
    expect(getIncidentArchive).toHaveBeenCalledWith(1, 25);
});

it('blocks direct access and hides the navigation for non-admins without fetching archive data', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false, canViewIncidentDiary: true } as ReturnType<typeof useRole>);
    show();
    expect(screen.getByRole('alert')).toHaveTextContent('nur für Administratoren');
    expect(screen.queryByRole('link', { name: 'Einsatzarchiv' })).not.toBeInTheDocument();
    expect(getIncidentArchive).not.toHaveBeenCalled();
});

it('opens the selected incident diary in a read-only modal with revision history', async () => {
    const user = show();
    await user.click(await screen.findByRole('button', { name: incident.name }));
    const dialog = screen.getByRole('dialog', { name: 'Einsatztagebuch: Wohnungsbrand' });
    expect((await within(dialog).findAllByText('Wasser am Rohr'))[0]).toBeVisible();
    expect(within(dialog).getAllByText('Florian 1')[0]).toBeVisible();
    expect(within(dialog).getAllByText('Stornierungsgrund: Doppelte Meldung')[0]).toBeVisible();
    expect(getArchivedDiaryEntries).toHaveBeenCalledExactlyOnceWith(incident.id);
    expect(within(dialog).queryByRole('button', { name: /Bearbeiten|Speichern|Stornieren/ })).not.toBeInTheDocument();
    await user.click(within(dialog).getByText('Versionshistorie (2 Versionen)'));
    expect(within(dialog).getByText('Ursprüngliche Meldung')).toBeVisible();
    fireEvent(dialog, new Event('cancel', { bubbles: true, cancelable: true }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: incident.name })).toHaveFocus();
});

it('opens the diary by clicking the incident row and downloads its PDF without opening the dialog', async () => {
    const user = show();
    await user.click(await screen.findByRole('button', { name: /als PDF herunterladen/ }));
    expect(downloadArchivedIncidentPdf).toHaveBeenCalledExactlyOnceWith(incident.id);
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    await user.click(screen.getByText('123'));
    expect(await screen.findByRole('dialog')).toBeVisible();
});

it('shows an empty archive and supports retrying a failed archive load', async () => {
    vi.mocked(getIncidentArchive).mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce([]);
    const user = show();
    expect(await screen.findByRole('alert')).toHaveTextContent('konnte nicht geladen werden');
    await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
    expect(await screen.findByText('Keine abgeschlossenen Einsätze vorhanden.')).toBeVisible();
});

it('loads further pages while keeping the incidents already shown', async () => {
    vi.mocked(getIncidentArchive).mockResolvedValueOnce(Array.from({ length: 25 }, (_, index) => ({ ...incident, id: `incident-${index}`, name: `Einsatz ${index}` })))
        .mockResolvedValueOnce([{ ...incident, id: 'older', name: 'Älterer Einsatz' }]);
    const user = show();
    await user.click(await screen.findByRole('button', { name: 'Weitere Einsätze laden' }));
    expect(await screen.findByRole('button', { name: 'Älterer Einsatz' })).toBeVisible();
    expect(screen.getByRole('button', { name: 'Einsatz 0' })).toBeVisible();
    expect(getIncidentArchive).toHaveBeenLastCalledWith(2, 25);
    expect(screen.queryByRole('button', { name: 'Weitere Einsätze laden' })).not.toBeInTheDocument();
});

it('lets admins retry diary loading and displays an empty diary', async () => {
    vi.mocked(getArchivedDiaryEntries).mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce([]);
    const user = show();
    await user.click(await screen.findByRole('button', { name: incident.name }));
    expect(await screen.findByRole('alert')).toHaveTextContent('konnte nicht geladen werden');
    await user.click(screen.getByRole('button', { name: 'Erneut versuchen' }));
    expect(await screen.findByText('Keine Tagebucheinträge vorhanden.')).toBeVisible();
});

it('reports failed PDF downloads and leaves the incident available', async () => {
    vi.mocked(downloadArchivedIncidentPdf).mockRejectedValueOnce(new Error('offline'));
    const user = show();
    await user.click(await screen.findByRole('button', { name: /als PDF herunterladen/ }));
    expect(await screen.findByRole('alert')).toHaveTextContent('PDF konnte nicht heruntergeladen');
    expect(screen.getByRole('button', { name: /als PDF herunterladen/ })).toBeEnabled();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
