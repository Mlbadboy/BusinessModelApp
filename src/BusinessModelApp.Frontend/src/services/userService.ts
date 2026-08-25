import axios from 'axios';
import { User } from '../types';

export const API_URL = 'http://localhost:5055/api';

const apiClient = axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

export const getUsers = async (): Promise<User[]> => {
    try {
        const response = await apiClient.get('/users');
        return response.data;
    } catch (error) {
        console.error("Failed to fetch users:", error);
        return [];
    }
};
