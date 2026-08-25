import axios from 'axios';
import { API_URL } from './userService';

export interface Metric {
    metricName: string;
    value: number;
    unit: string;
    trend: string;
}

export const getRevenueMetrics = async (): Promise<Metric[]> => {
    const response = await axios.get(`${API_URL}/business/revenue/metrics`);
    return response.data;
};

export const getExpenseMetrics = async (): Promise<Metric[]> => {
    const response = await axios.get(`${API_URL}/business/expense/metrics`);
    return response.data;
};
