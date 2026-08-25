import axios from 'axios';
import { Role } from '../types';

const API_URL = 'http://localhost:5055/api';

const apiClient = axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

export const getRoles = async (): Promise<Role[]> => {
    try {
        const response = await apiClient.get('/roles');
        return response.data;
    } catch (error) {
        console.error("Failed to fetch roles:", error);
        return [];
    }
};
