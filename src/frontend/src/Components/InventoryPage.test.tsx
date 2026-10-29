import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import InventoryPage from './InventoryPage';
import { useRole } from '../hooks/useRole';
import * as api from '../api/inventoryApi';
import type { Equipment, EquipmentDetail, EquipmentType } from '../api/inventoryApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/inventoryApi', async importOriginal => ({
    ...await importOriginal<typeof import('../api/inventoryApi')>(), getInventory: vi.fn(), getInventoryCategories: vi.fn(),
    getEquipmentTypes: vi.fn(), getEquipment: vi.fn(), getEquipmentBarcodeSvg: vi.fn(), saveEquipment: vi.fn(),
    recordEquipmentInspection: vi.fn(), setEquipmentActive: vi.fn(),
}));
const category = { id: 'cat-1', name: 'Armaturen' };
const type: EquipmentType = { id: 'type-1', categoryId: category.id, categoryName: category.name, name: 'Strahlrohr', inspectionIntervalMonths: 12, inspectionInstructions: 'Dichtungen und Funktion prüfen.', version: 1 };
const equipment: Equipment = { id: 'device-1', barcode: 'KPP-001', equipmentTypeId: type.id, equipmentTypeName: type.name, categoryName: category.name,
    serialNumber: 'SN-123', manufacturer: 'Hersteller', status: 'Ready', isActive: true, createdOn: '2026-01-31', lastInspectedOn: '2026-02-28', nextInspectionOn: '2027-02-28',
    inspectionIntervalMonths: 12, inspectionInstructions: type.inspectionInstructions, version: 2 };
const detail: EquipmentDetail = { equipment, inspections: [] };

function role(manage = false, admin = false, view = true) {
    vi.mocked(useRole).mockReturnValue({ canViewInventory: view, canManageInventory: manage, isAdmin: admin } as ReturnType<typeof useRole>);
}
beforeEach(() => {
    vi.clearAllMocks(); role();
    vi.mocked(api.getInventory).mockResolvedValue([equipment]);
    vi.mocked(api.getInventoryCategories).mockResolvedValue([category]);
    vi.mocked(api.getEquipmentTypes).mockResolvedValue([type]);
    vi.mocked(api.getEquipment).mockResolvedValue(detail);
    vi.mocked(api.getEquipmentBarcodeSvg).mockResolvedValue('<svg xmlns="http://www.w3.org/2000/svg"><text>KPP-001</text></svg>');
    vi.mocked(api.saveEquipment).mockResolvedValue(equipment);
    vi.mocked(api.setEquipmentActive).mockResolvedValue({ ...detail, equipment: { ...equipment, isActive: false } });
});

it('denies unrelated roles without requesting inventory', () => {
    role(false, false, false); render(<InventoryPage />);
    expect(screen.getByRole('alert')).toHaveTextContent('Keine Berechtigung');
    expect(api.getInventory).not.toHaveBeenCalled();
});

it('shows the last inspection, interval and next date for a reader, without maintenance controls', async () => {
    render(<InventoryPage />);
    expect(await screen.findByText('KPP-001')).toBeVisible();
    expect(screen.getByText('28.2.2026')).toBeVisible();
    expect(screen.getByText('28.2.2027')).toHaveAttribute('dateTime', '2027-02-28');
    expect(screen.getByText('12 Monate')).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Gerät anlegen' })).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'KPP-001 anzeigen' }));
    expect(await screen.findByRole('heading', { name: 'Prüfanweisung' })).toBeVisible();
    expect(await screen.findByRole('img', { name: 'Barcode KPP-001' })).toBeVisible();
    expect(screen.getByRole('link', { name: 'Barcodeetikett herunterladen' })).toHaveAttribute('download', 'KPP-001.svg');
    expect(screen.queryByRole('button', { name: 'Prüfung speichern' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Inventarkategorien verwalten' })).not.toBeInTheDocument();
});

it('lets an equipment officer create a device, with server-generated barcode and type plan', async () => {
    role(true); const user = userEvent.setup(); render(<InventoryPage />);
    await screen.findByText('KPP-001'); await user.click(screen.getByRole('button', { name: 'Gerät anlegen' }));
    await user.selectOptions(screen.getByLabelText('Gerätetyp'), type.id);
    await user.type(screen.getByLabelText('Hersteller'), 'Rosenbauer'); await user.type(screen.getByLabelText('Seriennummer'), 'SN-999');
    await user.click(screen.getByRole('button', { name: 'Gerät speichern' }));
    await waitFor(() => expect(api.saveEquipment).toHaveBeenCalledWith(null, { equipmentTypeId: type.id, manufacturer: 'Rosenbauer', serialNumber: 'SN-999', status: 'Ready', expectedVersion: 1 }));
    expect(await screen.findByRole('status')).toHaveTextContent('Gerät gespeichert');
});

it('records inspection status/comment and displays the updated due date and history', async () => {
    role(true); const user = userEvent.setup(); render(<InventoryPage />);
    vi.mocked(api.recordEquipmentInspection).mockResolvedValue({ equipment: { ...equipment, status: 'Defective', nextInspectionOn: '2027-10-29', version: 3 }, inspections: [{ id: 'entry', inspectedOn: '2026-10-29', recordedAtUtc: '2026-10-29T12:00:00Z', inspectorName: 'Ada', comment: 'Dichtung defekt', status: 'Defective', equipmentTypeName: type.name, inspectionIntervalMonths: 12, inspectionInstructions: type.inspectionInstructions }] });
    await user.click(await screen.findByRole('button', { name: 'KPP-001 anzeigen' }));
    await screen.findByLabelText('Prüfdatum'); fireEvent.change(screen.getByLabelText('Prüfdatum'), { target: { value: '2026-10-29' } });
    await user.selectOptions(screen.getByLabelText('Status nach Prüfung'), 'Defective'); await user.type(screen.getByLabelText('Kommentar zur Prüfung'), 'Dichtung defekt');
    await user.click(screen.getByRole('button', { name: 'Prüfung speichern' }));
    await waitFor(() => expect(api.recordEquipmentInspection).toHaveBeenCalledWith(equipment.id, expect.objectContaining({ inspectedOn: '2026-10-29', status: 'Defective', comment: 'Dichtung defekt', expectedVersion: 2, commandId: expect.any(String) })));
    expect(await screen.findByText('Geprüft von Ada')).toBeVisible(); expect(screen.getByText('Dichtung defekt')).toBeVisible();
});

it('keeps entered inspection data and the command ID for retry after a network error', async () => {
    role(true); const user = userEvent.setup(); render(<InventoryPage />);
    vi.mocked(api.recordEquipmentInspection).mockRejectedValue(new Error('offline'));
    await user.click(await screen.findByRole('button', { name: 'KPP-001 anzeigen' }));
    await user.type(await screen.findByLabelText('Kommentar zur Prüfung'), 'Funktionsprüfung');
    await user.click(screen.getByRole('button', { name: 'Prüfung speichern' }));
    expect(await screen.findByRole('alert')).toBeVisible();
    expect(screen.getByLabelText('Kommentar zur Prüfung')).toHaveValue('Funktionsprüfung');
    await user.click(screen.getByRole('button', { name: 'Prüfung speichern' }));
    await waitFor(() => expect(api.recordEquipmentInspection).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.recordEquipmentInspection).mock.calls[0][1].commandId).toBe(vi.mocked(api.recordEquipmentInspection).mock.calls[1][1].commandId);
});

it('deactivates with a version and hides the maintenance form afterwards', async () => {
    role(true); const user = userEvent.setup(); render(<InventoryPage />);
    await user.click(await screen.findByRole('button', { name: 'KPP-001 anzeigen' }));
    await user.click(await screen.findByRole('button', { name: 'Gerät deaktivieren' }));
    await waitFor(() => expect(api.setEquipmentActive).toHaveBeenCalledWith(equipment, false));
    expect(await screen.findByRole('button', { name: 'Gerät wieder aktivieren' })).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Prüfung speichern' })).not.toBeInTheDocument();
});

it('keeps category and type administration off the equipment overview, including for admins', async () => {
    role(true, true); render(<InventoryPage />); await screen.findByText('KPP-001');
    expect(screen.queryByRole('button', { name: 'Kategorie anlegen' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Gerätetyp anlegen' })).not.toBeInTheDocument();
});

it('filters by barcode and requests inactive devices when selected', async () => {
    const user = userEvent.setup(); render(<InventoryPage />); await screen.findByText('KPP-001');
    await user.type(screen.getByLabelText('Suche'), 'KPP-777'); expect(screen.getByText('Keine Geräte gefunden.')).toBeVisible();
    await user.click(screen.getByLabelText('Deaktivierte anzeigen')); await waitFor(() => expect(api.getInventory).toHaveBeenCalledWith(true));
});
