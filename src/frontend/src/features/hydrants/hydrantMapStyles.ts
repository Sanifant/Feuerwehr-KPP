import { Circle as CircleStyle, Fill, Stroke, Style } from 'ol/style';
import { HydrantStatus } from './models/HydrantModels';

function marker(fill: string, stroke: string): Style {
  return new Style({ image: new CircleStyle({ radius: 6, fill: new Fill({ color: fill }), stroke: new Stroke({ color: stroke, width: 2 }) }) });
}

const operational = marker('#2563eb', '#fff');
const defective = marker('#ff0000', '#000');
const styles: Record<HydrantStatus, Style> = {
  [HydrantStatus.Operational]: operational,
  [HydrantStatus.Defective]: defective,
  [HydrantStatus.Blocked]: defective,
  [HydrantStatus.UnderMaintenance]: marker('#ff9000', '#000'),
};

export function getHydrantStyle(status: HydrantStatus): Style {
  return styles[status] || operational;
}

export function getSelectedHydrantStyle(status: HydrantStatus): Style {
  const style = getHydrantStyle(status).clone();
  const circle = style.getImage() as CircleStyle;
  circle.setRadius(10);
  circle.setStroke(new Stroke({ color: '#facc15', width: 3 }));
  style.setZIndex(1);
  return style;
}
