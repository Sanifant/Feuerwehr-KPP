import { describe, expect, it } from 'vitest';
import type CircleStyle from 'ol/style/Circle';
import { HydrantStatus } from './models/HydrantModels';
import type { HydrantStatus as HydrantStatusType } from './models/HydrantModels';
import { getHydrantStyle, getSelectedHydrantStyle } from './hydrantMapStyles';

function readStyleColors(status: HydrantStatusType) {
    const style = getHydrantStyle(status);
    const image = style.getImage() as CircleStyle;
    return {
        fill: image.getFill()?.getColor(),
        stroke: image.getStroke()?.getColor(),
    };
}

describe('HydrantOverviewMap status styles', () => {
    it.each(Object.values(HydrantStatus))('preserves the status color for selected hydrants without changing other markers: %s', status => {
        const base = getHydrantStyle(status).getImage() as CircleStyle;
        const selected = getSelectedHydrantStyle(status).getImage() as CircleStyle;
        expect(selected.getFill()?.getColor()).toEqual(base.getFill()?.getColor());
        expect(selected.getStroke()?.getColor()).toBe('#facc15');
        expect(selected.getRadius()).toBe(10);
        expect(base.getRadius()).toBe(6);
    });
    it('maps operational hydrants to the default blue marker style', () => {
        const colors = readStyleColors(HydrantStatus.Operational);

        expect(colors.fill).toBe('#2563eb');
        expect(colors.stroke).toBe('#fff');
    });

    it('maps defective and blocked hydrants to the shared red warning style', () => {
        const defective = readStyleColors(HydrantStatus.Defective);
        const blocked = readStyleColors(HydrantStatus.Blocked);

        expect(defective.fill).toBe('#ff0000');
        expect(defective.stroke).toBe('#000');
        expect(blocked).toEqual(defective);
    });

    it('maps hydrants under maintenance to orange marker style', () => {
        const colors = readStyleColors(HydrantStatus.UnderMaintenance);

        expect(colors.fill).toBe('#ff9000');
        expect(colors.stroke).toBe('#000');
    });
});
