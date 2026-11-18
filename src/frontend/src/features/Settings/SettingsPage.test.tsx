import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, expect, it, vi } from 'vitest';
import SettingsPage from './SettingsPage';
import { useRole } from '../../hooks/useRole';
import * as api from './settingsApi';

vi.mock('../../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('./settingsApi', async importOriginal => ({
  ...await importOriginal<typeof import('./settingsApi')>(),
  getSettings: vi.fn(), getSettingDefinitions: vi.fn(), createSetting: vi.fn(), updateSetting: vi.fn(), deleteSetting: vi.fn(),
}));

const setting: api.Setting = {
  id: 'setting-1', key: 'TEST_EMAIL', name: 'Benachrichtigung', value: 'hydranten@example.org', type: 2,
  modifiedOn: '2026-11-18T08:00:00Z', modifiedBy: 'admin-1', modifiedByName: 'Ada Muster',
};
const definitions: api.SettingDefinition[] = [
  { key: 'TEST_EMAIL', name: 'Benachrichtigung', moduleId: 'test', moduleName: 'Testmodul', type: 2, mandatory: true },
  { key: 'TEST_NUMBER', name: 'Prüfintervall', moduleId: 'test', moduleName: 'Testmodul', type: 1, mandatory: false },
];

beforeAll(() => {
  HTMLDialogElement.prototype.showModal = function () { this.setAttribute('open', ''); };
  HTMLDialogElement.prototype.close = function () { this.removeAttribute('open'); };
});
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
  vi.mocked(api.getSettings).mockResolvedValue([setting]);
  vi.mocked(api.getSettingDefinitions).mockResolvedValue(definitions);
  vi.mocked(api.createSetting).mockResolvedValue(setting);
  vi.mocked(api.updateSetting).mockResolvedValue(setting);
  vi.mocked(api.deleteSetting).mockResolvedValue(undefined);
});

it('shows the parameter value, modification time and readable editor in a table', async () => {
  render(<SettingsPage />);
  const row = (await screen.findByText('Benachrichtigung')).closest('tr')!;
  expect(within(row).getByText('hydranten@example.org')).toBeVisible();
  expect(within(row).getByText('Ada Muster')).toBeVisible();
  expect(row.querySelector('time')).toHaveAttribute('datetime', setting.modifiedOn);
});

it('creates only a predefined parameter and derives its type from the catalog', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await screen.findByText('Ada Muster');
  await user.click(screen.getByRole('button', { name: 'Parameter anlegen' }));
  const dialog = screen.getByRole('dialog');
  expect(within(dialog).queryByRole('option', { name: /Benachrichtigung/ })).not.toBeInTheDocument();
  await user.selectOptions(within(dialog).getByLabelText('Parametername'), 'TEST_NUMBER');
  expect(within(dialog).getByLabelText('Typ')).toHaveValue('Zahl');
  await user.type(within(dialog).getByLabelText('Wert'), '12');
  await user.click(within(dialog).getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.createSetting).toHaveBeenCalledWith({ key: 'TEST_NUMBER', value: '12' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
});

it('uses the email metadata without allowing a user to select a type', async () => {
  const user = userEvent.setup();
  vi.mocked(api.getSettings).mockResolvedValue([]);
  render(<SettingsPage />);
  await screen.findByText('Noch keine Parameter vorhanden.');
  await user.click(screen.getByRole('button', { name: 'Parameter anlegen' }));
  await user.selectOptions(screen.getByLabelText('Parametername'), 'TEST_EMAIL');
  expect(screen.getByLabelText('Typ')).toHaveValue('E-Mail');
  expect(screen.getByLabelText('Typ')).toHaveAttribute('readonly');
  expect(screen.getByLabelText('Wert')).toHaveAttribute('inputmode', 'email');
  await user.type(screen.getByLabelText('Wert'), 'pruefung@example.org');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.createSetting).toHaveBeenCalledWith({ key: 'TEST_EMAIL', value: 'pruefung@example.org' }));
});

it('edits only the value and keeps the name and type immutable', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Benachrichtigung bearbeiten' }));
  expect(screen.getByLabelText('Parametername')).toHaveAttribute('readonly');
  expect(screen.getByLabelText('Typ')).toHaveAttribute('readonly');
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), 'neu@example.org');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  await waitFor(() => expect(api.updateSetting).toHaveBeenCalledWith('setting-1', 'neu@example.org'));
  expect(api.createSetting).not.toHaveBeenCalled();
});

it.each(['0', '-1', '1.5', '1,5', '+1', '1e3', '   '])('delegates validation to the server and retains rejected input: %s', async value => {
  const user = userEvent.setup();
  vi.mocked(api.getSettings).mockResolvedValue([{ ...setting, name: 'Anzahl', type: 1, value: '1' }]);
  vi.mocked(api.updateSetting).mockRejectedValue(new Error('Invalid number'));
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Anzahl bearbeiten' }));
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), value);
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  await screen.findByRole('alert');
  expect(api.updateSetting).toHaveBeenCalledWith('setting-1', value);
  expect(screen.getByLabelText('Wert')).toHaveValue(value);
  expect(screen.getByRole('dialog')).toBeVisible();
});

it('displays validation errors from the server for whitespace-only text', async () => {
  const user = userEvent.setup();
  vi.mocked(api.getSettings).mockResolvedValue([{ ...setting, name: 'Text', type: 0 }]);
  vi.mocked(api.updateSetting).mockRejectedValue(new Error('Invalid text'));
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Text bearbeiten' }));
  await user.clear(screen.getByLabelText('Wert'));
  await user.type(screen.getByLabelText('Wert'), '   ');
  await user.click(screen.getByRole('button', { name: 'Speichern' }));
  expect(await screen.findByRole('alert')).toBeVisible();
  expect(api.updateSetting).toHaveBeenCalledWith('setting-1', '   ');
});

it('confirms deletion and reloads the table', async () => {
  const user = userEvent.setup();
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Benachrichtigung löschen' }));
  expect(api.deleteSetting).not.toHaveBeenCalled();
  vi.mocked(api.getSettings).mockResolvedValue([]);
  await user.click(screen.getByRole('button', { name: 'Löschen bestätigen' }));
  await waitFor(() => expect(api.deleteSetting).toHaveBeenCalledWith('setting-1'));
  expect(await screen.findByText('Noch keine Parameter vorhanden.')).toBeVisible();
  await user.click(screen.getByRole('button', { name: 'Parameter anlegen' }));
  expect(screen.getByRole('option', { name: 'Testmodul: Benachrichtigung (Pflichtparameter)' })).toBeVisible();
});

it('keeps the editor open when saving fails', async () => {
  const user = userEvent.setup();
  vi.mocked(api.updateSetting).mockRejectedValue(new Error('Save failed'));
  render(<SettingsPage />);
  await user.click(await screen.findByRole('button', { name: 'Benachrichtigung bearbeiten' }));
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
  expect(api.getSettingDefinitions).not.toHaveBeenCalled();
  expect(screen.queryByRole('table')).not.toBeInTheDocument();
});

it('has no selectable parameters when no module declares settings', async () => {
  vi.mocked(api.getSettings).mockResolvedValue([]);
  vi.mocked(api.getSettingDefinitions).mockResolvedValue([]);
  render(<SettingsPage />);
  expect(await screen.findByText('Es sind noch keine Parameter durch Module definiert.')).toBeVisible();
  expect(screen.getByRole('button', { name: 'Parameter anlegen' })).toBeDisabled();
});
