import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, expect, it, vi } from 'vitest';
import SettingsPage from './SettingsPage';
import { useRole } from '../../hooks/useRole';
import * as api from './settingsApi';

vi.mock('../../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('./settingsApi', async importOriginal => ({
  ...await importOriginal<typeof import('./settingsApi')>(),
  getSettings: vi.fn(), createSetting: vi.fn(), updateSetting: vi.fn(), deleteSetting: vi.fn(),
}));

const setting: api.Setting = {
  id: 'setting-1', name: 'Hydrant Benachrichtigung', value: 'hydranten@example.org', type: 2,
  modifiedOn: '2026-11-18T08:00:00Z', modifiedBy: 'admin-1', modifiedByName: 'Ada Muster',
};

beforeAll(() => {
  HTMLDialogElement.prototype.showModal = function () { this.setAttribute('open', ''); };
  HTMLDialogElement.prototype.close = function () { this.removeAttribute('open'); };
});
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
  vi.mocked(api.getSettings).mockResolvedValue([setting]);
  vi.mocked(api.createSetting).mockResolvedValue(setting);
  vi.mocked(api.updateSetting).mockResolvedValue(setting);
  vi.mocked(api.deleteSetting).mockResolvedValue(undefined);
});

it('shows the parameter value, modification time and readable editor in a table', async () => {
  render(<SettingsPage />);
  const row = (await screen.findByText('Hydrant Benachrichtigung')).closest('tr')!;
  expect(within(row).getByText('hydranten@example.org')).toBeVisible();
  expect(within(row).getByText('Ada Muster')).toBeVisible();
  expect(row.querySelector('time')).toHaveAttribute('datetime', setting.modifiedOn);
});

it('creates a parameter with a selected validation type', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await screen.findByText('Ada Muster');
  await user.click(screen.getByRole('button', { name: 'Parameter anlegen' }));
  const dialog = screen.getByRole('dialog');
  await user.type(within(dialog).getByLabelText('Parametername'), 'Prüfintervall');
  await user.selectOptions(within(dialog).getByLabelText('Typ'), '1');
  await user.type(within(dialog).getByLabelText('Wert'), '12');
  await user.click(within(dialog).getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.createSetting).toHaveBeenCalledWith({ name: 'Prüfintervall', value: '12', type: 1 }));
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
});

it('forces the email type for the hydrant notification parameter', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await screen.findByText('Ada Muster');
  await user.click(screen.getByRole('button', { name: 'Parameter anlegen' }));
  await user.type(screen.getByLabelText('Parametername'), 'Hydrant Benachrichtigung');
  expect(screen.getByLabelText('Typ')).toHaveValue('2');
  expect(screen.getByLabelText('Typ')).toBeDisabled();
  expect(screen.getByLabelText('Wert')).toHaveAttribute('type', 'email');
  await user.type(screen.getByLabelText('Wert'), 'pruefung@example.org');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.createSetting).toHaveBeenCalledWith({ name: setting.name, value: 'pruefung@example.org', type: 2 }));
});

it('edits only the value and keeps the name and type immutable', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Hydrant Benachrichtigung bearbeiten' }));
  expect(screen.getByLabelText('Parametername')).toHaveAttribute('readonly');
  expect(screen.getByLabelText('Typ')).toBeDisabled();
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), 'neu@example.org');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.updateSetting).toHaveBeenCalledWith('setting-1', 'neu@example.org'));
  expect(api.createSetting).not.toHaveBeenCalled();
});

it.each(['0', '-1', '1.5', '1,5', '+1', '1e3', '   '])('does not submit an invalid number: %s', async value => {
  const user = userEvent.setup();
  vi.mocked(api.getSettings).mockResolvedValue([{ ...setting, name: 'Anzahl', type: 1, value: '1' }]);
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Anzahl bearbeiten' }));
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), value);
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  expect(api.updateSetting).not.toHaveBeenCalled();
  expect(screen.getByRole('dialog')).toBeVisible();
});

it('rejects a whitespace-only text value', async () => {
  const user = userEvent.setup();
  vi.mocked(api.getSettings).mockResolvedValue([{ ...setting, name: 'Text', type: 0 }]);
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Text bearbeiten' }));
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), '   ');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  expect(api.updateSetting).not.toHaveBeenCalled();
  expect(screen.getByRole('alert')).toHaveTextContent('Bitte Name und Wert angeben.');
});

it('confirms deletion and reloads the table', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Hydrant Benachrichtigung löschen' }));
  expect(api.deleteSetting).not.toHaveBeenCalled();
  vi.mocked(api.getSettings).mockResolvedValue([]);
  await user.click(screen.getByRole('button', { name: 'Löschen bestätigen' }));
  await waitFor(() => expect(api.deleteSetting).toHaveBeenCalledWith('setting-1'));
  expect(await screen.findByText('Noch keine Parameter vorhanden.')).toBeVisible();
});

it('keeps the editor open when saving fails', async () => {
  const user = userEvent.setup();
  vi.mocked(api.updateSetting).mockRejectedValue(new Error('Save failed'));
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Hydrant Benachrichtigung bearbeiten' }));
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  expect(await screen.findByRole('alert')).toBeVisible();
  expect(screen.getByRole('dialog')).toBeVisible();
  expect(screen.getByLabelText('Wert')).toHaveValue(setting.value);
});

it('does not load or display settings for non-admins', () => {
  vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
  render(<SettingsPage />);
  expect(screen.getByRole('alert')).toHaveTextContent('Nur Administratoren');
  expect(api.getSettings).not.toHaveBeenCalled();
  expect(screen.queryByRole('table')).not.toBeInTheDocument();
});
