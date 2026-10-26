import { act, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, expect, it, vi } from 'vitest';
import IncidentTitlebar from './IncidentTitlebar';
import { ActiveIncidentContext } from '../hooks/useActiveIncident';
import type { IncidentSummary } from '../api/incidentApi';

const incident: IncidentSummary = {
    id: 'incident-1', name: 'Wohnungsbrand', keyword: 'B3', status: 'Active',
    originLatitude: 51, originLongitude: 10, revision: 1,
    createdAtUtc: '2026-10-26T09:55:00Z', alarmedAtUtc: '2026-10-25T08:30:00Z',
};

afterEach(() => { vi.useRealTimers(); });

function show(active: IncidentSummary | null = incident) {
    return render(<MemoryRouter><ActiveIncidentContext.Provider value={{
        incident: active, loading: false, error: null, refresh: vi.fn(), setActiveIncident: vi.fn(),
    }}><IncidentTitlebar /></ActiveIncidentContext.Provider></MemoryRouter>);
}

it('shows the incident name, keyword, clock and elapsed time since alarm, including durations over 24 hours', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-10-26T10:00:00Z'));
    show();
    expect(screen.getByRole('region', { name: 'Aktiver Einsatz' })).toBeVisible();
    expect(screen.getByRole('link', { name: 'Wohnungsbrand Stichwort: B3' })).toHaveAttribute('href', '/incidents');
    expect(screen.getByText('25:30:00')).toHaveAttribute('datetime', 'PT91800S');
    expect(screen.getByText(new Date().toLocaleTimeString('de-DE', { hour12: false }))).toHaveAttribute('datetime', '2026-10-26T10:00:00.000Z');
    act(() => { vi.advanceTimersByTime(5000); });
    expect(screen.getByText('25:30:05')).toBeVisible();
    expect(screen.getByText(new Date().toLocaleTimeString('de-DE', { hour12: false }))).toHaveAttribute('datetime', '2026-10-26T10:00:05.000Z');
});

it('shows no incident information or timer when there is no active incident', () => {
    vi.useFakeTimers();
    show(null);
    expect(screen.queryByRole('region')).not.toBeInTheDocument();
    expect(vi.getTimerCount()).toBe(0);
});

it('stops its clock on unmount and handles a future alarm without a negative duration', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-10-25T08:00:00Z'));
    const view = show();
    expect(screen.getByText('00:00:00')).toBeVisible();
    view.unmount();
    expect(vi.getTimerCount()).toBe(0);
});
