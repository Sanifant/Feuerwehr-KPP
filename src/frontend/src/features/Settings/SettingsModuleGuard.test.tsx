import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { useRole } from '../../hooks/useRole';
import SettingsModuleGuard from './SettingsModuleGuard';
import { getSettingsModuleStatuses } from './settingsApi';

vi.mock('../../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('./settingsApi', () => ({ getSettingsModuleStatuses: vi.fn() }));
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
});

function show(path = '/test-module') {
  render(<MemoryRouter initialEntries={[path]}><Routes><Route element={<SettingsModuleGuard />}>
    <Route path="*" element={<p>Modulinhalt</p>} />
  </Route></Routes></MemoryRouter>);
}

it('allows pages when no modules have parameter definitions', async () => {
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([]);
  show();
  expect(await screen.findByText('Modulinhalt')).toBeVisible();
});

it('blocks the entire declared module and offers administrators a configuration link', async () => {
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([
    { id: 'test', name: 'Testmodul', pagePaths: ['/test-module'], isAvailable: false, missingParameters: ['E-Mail-Empfänger'] },
  ]);
  show('/test-module/details');
  expect(await screen.findByRole('alert')).toHaveTextContent('Testmodul');
  expect(screen.getByText('E-Mail-Empfänger')).toBeVisible();
  expect(screen.queryByText('Modulinhalt')).not.toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Settings konfigurieren' })).toHaveAttribute('href', '/settings');
});

it('restores module access after configuration is corrected', async () => {
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([
    { id: 'test', name: 'Testmodul', pagePaths: ['/test-module'], isAvailable: false, missingParameters: ['Empfänger'] },
  ]);
  show();
  await screen.findByRole('alert');
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([
    { id: 'test', name: 'Testmodul', pagePaths: ['/test-module'], isAvailable: true, missingParameters: [] },
  ]);
  await userEvent.setup().click(screen.getByRole('button', { name: 'Erneut prüfen' }));
  expect(await screen.findByText('Modulinhalt')).toBeVisible();
});

it('does not block unrelated routes or a similarly named route', async () => {
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([
    { id: 'test', name: 'Testmodul', pagePaths: ['/test-module'], isAvailable: false, missingParameters: ['Empfänger'] },
  ]);
  show('/test-module-other');
  expect(await screen.findByText('Modulinhalt')).toBeVisible();
});

it.each(['/settings', '/Settings/'])('leaves settings administration accessible even if the availability request would fail: %s', path => {
  show(path);
  expect(screen.getByText('Modulinhalt')).toBeVisible();
  expect(getSettingsModuleStatuses).not.toHaveBeenCalled();
});

it('keeps module content mounted while rechecking availability after window focus', async () => {
  vi.mocked(getSettingsModuleStatuses).mockResolvedValue([]);
  show();
  const content = await screen.findByText('Modulinhalt');
  vi.mocked(getSettingsModuleStatuses).mockReturnValue(new Promise(() => {}));
  fireEvent.focus(window);
  await waitFor(() => expect(getSettingsModuleStatuses).toHaveBeenCalledTimes(2));
  expect(content).toBeInTheDocument();
});

it('shows a retryable error without rendering module content when availability cannot be checked', async () => {
  vi.mocked(getSettingsModuleStatuses).mockRejectedValue(new Error('Unavailable'));
  vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
  show();
  expect(await screen.findByRole('alert')).toHaveTextContent('konnte nicht geprüft werden');
  expect(screen.queryByText('Modulinhalt')).not.toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Settings konfigurieren' })).not.toBeInTheDocument();
});
