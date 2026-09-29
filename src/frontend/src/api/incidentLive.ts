import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

let connection: HubConnection | null = null;

export async function ensureIncidentConnection() {
    if (connection && connection.state !== HubConnectionState.Disconnected) {
        return connection;
    }

    const token = localStorage.getItem('accessToken') ?? '';
    const apiBase = import.meta.env.VITE_API_URL || '';

    connection = new HubConnectionBuilder()
        .withUrl(`${apiBase}/hubs/incidents`, {
            accessTokenFactory: () => localStorage.getItem('accessToken') ?? token,
            withCredentials: true,
        })
        .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
        .configureLogging(LogLevel.Warning)
        .build();

    await connection.start();
    return connection;
}

export function getIncidentConnection() {
    return connection;
}

export async function stopIncidentConnection() {
    if (connection) {
        await connection.stop();
        connection = null;
    }
}
