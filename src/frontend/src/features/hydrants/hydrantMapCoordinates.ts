export function hasMappableCoordinates(latitude: number, longitude: number): boolean {
    return Number.isFinite(latitude) && Number.isFinite(longitude)
        && Math.abs(latitude) <= 85.0511287798066 && Math.abs(longitude) <= 180;
}
