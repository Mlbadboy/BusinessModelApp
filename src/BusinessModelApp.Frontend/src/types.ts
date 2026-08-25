export interface User {
    id: string;
    name: string;
    email: string;
    role: string;
}

export interface Task {
    id: string;
    title: string;
    description: string;
    assignedToUserId: string;
    assignedToUserName: string;
    assignedByUserId: string;
    assignedByUserName: string;
    status: string;
    createdAt: string; // ISO 8601 date string
    completedAt?: string; // ISO 8601 date string
}

export interface Role {
    id: number;
    name: string;
}

export interface PerformanceMetric {
    label: string;
    value: string;
    change?: string;
}

export interface Agent {
    name: string;
    status: 'Online' | 'Offline';
}

export interface Insight {
    text: string;
    confidence?: number;
}

export interface Project {
    name: string;
    status: string;
}

export interface AgentActivity {
    id: string;
    agentId: string;
    agentName: string;
    activityType: string;
    description: string;
    timestamp: string;
    details?: string;
}
