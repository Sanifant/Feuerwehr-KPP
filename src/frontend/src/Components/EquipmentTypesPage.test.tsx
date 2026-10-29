import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import EquipmentTypesPage from './EquipmentTypesPage';
import { useRole } from '../hooks/useRole';
import { getEquipmentTypes, getInventoryCategories, saveEquipmentType } from '../api/inventoryApi';
import type { EquipmentType } from '../api/inventoryApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/inventoryApi', () => ({ getEquipmentTypes: vi.fn(), getInventoryCategories: vi.fn(), saveEquipmentType: vi.fn() }));
const category = { id: 'cat-1', name: 'Armaturen' };
const type: EquipmentType = { id: 'type-1', categoryId: category.id, categoryName: category.name, name: 'Strahlrohr', inspectionIntervalMonths: 12, inspectionInstructions: 'Dichtungen prüfen.', version: 4 };

beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useRole).mockReturnValue({ canManageInventory: true } as ReturnType<typeof useRole>);
    vi.mocked(getInventoryCategories).mockResolvedValue([category]);
    vi.mocked(getEquipmentTypes).mockResolvedValue([type]);
    vi.mocked(saveEquipmentType).mockResolvedValue(type);
});

it('denies read-only users without fetching the administration data', () => {
    vi.mocked(useRole).mockReturnValue({ canManageInventory: false } as ReturnType<typeof useRole>);
    render(<EquipmentTypesPage />);
    expect(screen.getByRole('alert')).toHaveTextContent('Nur für Gerätewarte und Administratoren');
    expect(getEquipmentTypes).not.toHaveBeenCalled(); expect(getInventoryCategories).not.toHaveBeenCalled();
});

it('creates a type with category, instruction and monthly interval on its own page', async () => {
    const user = userEvent.setup(); render(<EquipmentTypesPage />);
    await screen.findByText(type.name); await user.click(screen.getByRole('button', { name: 'Gerätetyp anlegen' }));
    await user.type(screen.getByLabelText('Typbezeichnung'), 'Scheinwerfer');
    await user.selectOptions(screen.getByLabelText('Typkategorie'), category.id);
    fireEvent.change(screen.getByLabelText('Prüfintervall in Monaten'), { target: { value: '6' } });
    await user.type(screen.getByLabelText('Prüfanweisung'), 'Gehäuse und Funktion prüfen.');
    await user.click(screen.getByRole('button', { name: 'Prüfplan speichern' }));
    await waitFor(() => expect(saveEquipmentType).toHaveBeenCalledWith(null, { categoryId: category.id, name: 'Scheinwerfer', inspectionIntervalMonths: 6, inspectionInstructions: 'Gehäuse und Funktion prüfen.', expectedVersion: 1 }));
    expect(await screen.findByRole('status')).toHaveTextContent('Gerätetyp und Prüfplan gespeichert.');
});

it('edits the existing plan with its version and displays the saved result', async () => {
    const user = userEvent.setup(); render(<EquipmentTypesPage />);
    vi.mocked(saveEquipmentType).mockResolvedValue({ ...type, inspectionIntervalMonths: 6, version: 5 });
    await user.click(await screen.findByRole('button', { name: 'Strahlrohr Prüfplan bearbeiten' }));
    expect(screen.getByLabelText('Prüfanweisung')).toHaveValue(type.inspectionInstructions);
    fireEvent.change(screen.getByLabelText('Prüfintervall in Monaten'), { target: { value: '6' } });
    await user.click(screen.getByRole('button', { name: 'Prüfplan speichern' }));
    await waitFor(() => expect(saveEquipmentType).toHaveBeenCalledWith(type.id, expect.objectContaining({ expectedVersion: 4, inspectionIntervalMonths: 6 })));
    expect(await screen.findByText('6 Monate')).toBeVisible(); expect(screen.queryByLabelText('Typbezeichnung')).not.toBeInTheDocument();
});

it('keeps the edited plan when saving fails', async () => {
    const user = userEvent.setup(); render(<EquipmentTypesPage />);
    vi.mocked(saveEquipmentType).mockRejectedValue(new Error('offline'));
    await user.click(await screen.findByRole('button', { name: 'Strahlrohr Prüfplan bearbeiten' }));
    fireEvent.change(screen.getByLabelText('Prüfintervall in Monaten'), { target: { value: '6' } });
    await user.click(screen.getByRole('button', { name: 'Prüfplan speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('konnte nicht gespeichert');
    expect(screen.getByLabelText('Prüfintervall in Monaten')).toHaveValue(6);
});
