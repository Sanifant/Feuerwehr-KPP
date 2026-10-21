import { isAxiosError } from 'axios';

interface ApiError {
    message?: string;
    title?: string;
    errors?: Array<{ description: string }> | Record<string, string[]>;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
    if (!isAxiosError<ApiError>(error)) return fallback;
    const data = error.response?.data;
    if (Array.isArray(data?.errors)) {
        return data.errors.map(item => item.description).join(' ') || fallback;
    }
    if (data?.errors) return Object.values(data.errors).flat().join(' ') || fallback;
    return data?.message || data?.title || fallback;
}
