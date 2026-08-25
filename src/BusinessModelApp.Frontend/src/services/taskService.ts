import axios from 'axios';
import { Task } from '../types';

const API_URL = 'http://localhost:5055/api';

const apiClient = axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

export const getTasks = async (): Promise<Task[]> => {
    const response = await apiClient.get('/tasks');
    return response.data;
};

export const createTask = async (title: string, description: string, assignedToUserId: string): Promise<Task> => {
    const response = await apiClient.post('/tasks', { title, description, assignedToUserId });
    return response.data;
};

export const updateTaskStatus = async (id: number, status: string): Promise<void> => {
    await apiClient.put(`/tasks/${id}/status`, `"${status}"`); // Send status as a raw string in the body
};
