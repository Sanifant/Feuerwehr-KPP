
// --- TypeScript Werte (Exakt wie in C#) ---
export const HydrantStatus = {
    Operational: 1,
    Defective: 2,
    Blocked: 3,
    UnderMaintenance: 4
} as const;

export type HydrantStatus = typeof HydrantStatus[keyof typeof HydrantStatus];

export const HydrantType = {
    Underground: 1,
    AbovegroundWithoutJack: 2,
    AbovegroundWithJack: 3,
    WallHydrant: 4
} as const;

export type HydrantType = typeof HydrantType[keyof typeof HydrantType];

export const WaterSource = {
    WaterGrid: 1,
    FirePond: 2,
    FireWell: 3,
    Cistern: 4
} as const;

export type WaterSource = typeof WaterSource[keyof typeof WaterSource];

// --- TypeScript Interfaces ---
export interface Address {
    street: string;
    houseNumber: string;
    postalCode: string;
    city: string;
    additionalInfo?: string | null;
}

export interface Hydrant {
    id: number;
    address: Address;
    latitude: number;
    longitude: number;
    nominalDiameter: number;
    type: HydrantType;
    waterSource: WaterSource;
    status: HydrantStatus;
    notes?: string | null;
}

