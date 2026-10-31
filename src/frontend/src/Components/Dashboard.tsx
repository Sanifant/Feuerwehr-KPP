import VehicleCalendar from './VehicleCalendar';
import VehicleStatusOverview from './VehicleStatusOverview';
import './Dashboard.css';

export function Dashboard() {
  return (
    <div className="dashboard-overview"><header className="page-header"><h1>Dashboard Klein Parin Pohnsdorf</h1></header>
      <VehicleStatusOverview />
      <VehicleCalendar />
    </div>
  );
}
