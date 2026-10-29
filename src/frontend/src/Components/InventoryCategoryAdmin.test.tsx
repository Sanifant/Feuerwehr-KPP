import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import InventoryCategoryAdmin from './InventoryCategoryAdmin';
import { useRole } from '../hooks/useRole';
import { createInventoryCategory, getInventoryCategories } from '../api/inventoryApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/inventoryApi', () => ({ getInventoryCategories: vi.fn(), createInventoryCategory: vi.fn() }));

beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
    vi.mocked(getInventoryCategories).mockResolvedValue([{ id: 'cat-1', name: 'Armaturen' }]);
    vi.mocked(createInventoryCategory).mockResolvedValue({ id: 'cat-2', name: 'Werkzeug' });
});

it('denies non-administrators without loading categories', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
    render(<InventoryCategoryAdmin />);
    expect(screen.getByRole('alert')).toHaveTextContent('Nur für Administratoren'); expect(getInventoryCategories).not.toHaveBeenCalled();
});

it('shows existing categories and lets an administrator create another on the dedicated page', async () => {
    const user = userEvent.setup(); render(<InventoryCategoryAdmin />); await screen.findByText('Armaturen');
    await user.type(screen.getByLabelText('Neue Kategorie'), ' Werkzeug ');
    await user.click(screen.getByRole('button', { name: 'Kategorie anlegen' }));
    await waitFor(() => expect(createInventoryCategory).toHaveBeenCalledWith('Werkzeug'));
    expect(await screen.findByText('Werkzeug')).toBeVisible(); expect(screen.getByLabelText('Neue Kategorie')).toHaveValue('');
});
