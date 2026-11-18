import { HydrantStatus } from './HydrantModels';

export const hydrantStatusLabels: Record<HydrantStatus, string> = {
  [HydrantStatus.Operational]: 'Betriebsbereit',
  [HydrantStatus.Defective]: 'Defekt',
  [HydrantStatus.Blocked]: 'Gesperrt',
  [HydrantStatus.UnderMaintenance]: 'In Wartung',
};
