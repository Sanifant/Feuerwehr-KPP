export const mapSymbolDragType = 'application/x-feuerwehr-map-symbol';

export function isInsideMap(point: unknown, bounds: Pick<DOMRect, 'left' | 'right' | 'top' | 'bottom'>) {
    if (!point || typeof point !== 'object' || !('clientX' in point) || !('clientY' in point)
        || typeof point.clientX !== 'number' || typeof point.clientY !== 'number') return false;
    return Number.isFinite(point.clientX) && Number.isFinite(point.clientY)
        && point.clientX >= bounds.left && point.clientX < bounds.right
        && point.clientY >= bounds.top && point.clientY < bounds.bottom;
}
