import apiClient from './apiClient';

export interface MapSymbol {
    id: string;
    name: string;
    category: string;
    svgContent: string;
    isActive: boolean;
}

export type SaveMapSymbol = Omit<MapSymbol, 'id'>;

export async function getMapSymbols() {
    return (await apiClient.get<MapSymbol[]>('/api/map-symbols')).data;
}

export async function getMapSymbolCategories() {
    return (await apiClient.get<string[]>('/api/map-symbols/categories')).data;
}

export async function saveMapSymbol(id: string | null, payload: SaveMapSymbol) {
    return (id ? await apiClient.put<MapSymbol>(`/api/map-symbols/${id}`, payload)
        : await apiClient.post<MapSymbol>('/api/map-symbols', payload)).data;
}

export function svgImageUrl(svg: string) {
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}
