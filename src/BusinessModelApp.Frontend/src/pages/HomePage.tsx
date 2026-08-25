import React, { useState, useEffect } from 'react';
import '../App.css';
import { PerformanceMetric, Agent, Insight, Project, Task } from '../types';
import { getTasks } from '../services/taskService';
import { getUsers } from '../services/userService';
import { getRevenueMetrics, getExpenseMetrics } from '../services/analyticsService';
import { Button } from '@mui/material';
import TaskList from '../components/TaskList';
import CreateTaskModal from '../components/CreateTaskModal';
import AgentActivityLog from '../components/AgentActivityLog';
import { useNavigate } from 'react-router-dom';

const HomePage: React.FC = () => {
    const navigate = useNavigate();
    // --- State Management for Dynamic Data ---
    const [performanceData, setPerformanceData] = useState<PerformanceMetric[]>([
        { label: 'Revenue', value: '$1.24M', change: '(+12%)' },
        { label: 'Operational Efficiency', value: '92%' },
        { label: 'Task Completion', value: '84%' },
        { label: 'Customer Growth', value: '18%' },
    ]);

    const [agents, setAgents] = useState<Agent[]>([]);

    const [insights, setInsights] = useState<Insight[]>([
        { text: 'Expand to Southeast Asia', confidence: 87 },
        { text: 'Launch premium tier - projected 35% revenue lift' },
    ]);

    const [projects, setProjects] = useState<Project[]>([
        { name: 'Product Launch', status: '(75% complete)' },
        { name: 'Marketing Campaign', status: '(In progress)' },
    ]);

    const [tasks, setTasks] = useState<Task[]>([]);
    const [isModalOpen, setModalOpen] = useState(false);

    // --- Data Fetching ---
    const fetchTasks = async () => {
        try {
            const fetchedTasks = await getTasks();
            setTasks(fetchedTasks);
        } catch (error) {
            console.error("Failed to fetch tasks:", error);
        }
    };

    const fetchAgents = async () => {
        try {
            const users = await getUsers();
            const fetchedAgents = users.map(user => ({
                name: user.name,
                status: 'Online' as 'Online' | 'Offline'
            }));
            setAgents(fetchedAgents);
        } catch (error) {
            console.error("Failed to fetch agents:", error);
        }
    };

    const fetchMetrics = async () => {
        try {
            const [revenueMetrics, expenseMetrics] = await Promise.all([
                getRevenueMetrics(),
                getExpenseMetrics()
            ]);

            const totalRevenue = revenueMetrics.find(m => m.metricName === 'Total Revenue');
            const totalExpenses = expenseMetrics.find(m => m.metricName === 'Total Expenses');

            setPerformanceData([
                { label: 'Revenue', value: totalRevenue ? `$${totalRevenue.value.toLocaleString()}` : '$0', change: totalRevenue?.trend },
                { label: 'Expenses', value: totalExpenses ? `$${totalExpenses.value.toLocaleString()}` : '$0', change: totalExpenses?.trend },
                { label: 'Operational Efficiency', value: '92%' }, // Mock for now
                { label: 'Task Completion', value: '84%' }, // Mock for now
            ]);
        } catch (error) {
            console.error("Failed to fetch metrics:", error);
        }
    };

    useEffect(() => {
        fetchTasks();
        fetchAgents();
        fetchMetrics();

        // Poll for task updates every 2 seconds to reflect simulation changes
        const interval = setInterval(() => {
            fetchTasks();
            fetchMetrics();
        }, 2000);
        return () => clearInterval(interval);
    }, []);

    // --- Event Handlers ---
    const handleAgentClick = (agentName: string) => {
        const newAgents = [...agents];
        const agentIndex = agents.findIndex(a => a.name === agentName);
        if (agentIndex !== -1) {
            newAgents[agentIndex].status = newAgents[agentIndex].status === 'Online' ? 'Offline' : 'Online';
            setAgents(newAgents);
        }
    };

    const handleTaskCreated = () => {
        fetchTasks();
    };

    const activeAgentsCount = agents.filter(agent => agent.status === 'Online').length;
    const totalAgents = agents.length;

    const performanceMetrics = [
        ...performanceData,
        { label: 'Active Agents', value: `${activeAgentsCount} / ${totalAgents}` }
    ];

    return (
        <div className="App">
            <header>
                <div className="logo-container">
                    <img src="/logo1.png" alt="Logo 1" className="logo logo-left" />
                    <h1>QuantumExecutive AI</h1>
                    <img src="/logo2.png" alt="Logo 2" className="logo logo-right" />
                </div>
                <div className="header-actions">
                    <Button variant="contained" color="secondary" onClick={() => navigate('/builder')} style={{ marginRight: '10px' }}>
                        Builder Mode
                    </Button>
                    <Button variant="contained" color="secondary" onClick={() => navigate('/models')} style={{ marginRight: '10px' }}>
                        Model Manager
                    </Button>
                    <Button variant="contained" color="primary" onClick={() => setModalOpen(true)}>
                        Create New Task
                    </Button>
                </div>
            </header>

            <main className="dashboard-container">
                <section className="performance-summary">
                    {performanceMetrics.map((metric, index) => (
                        <div className="tile" key={index}>
                            <strong>{metric.label}:</strong> {metric.value} {metric.change && <span>{metric.change}</span>}
                        </div>
                    ))}
                </section>

                <section className="agent-management">
                    <h2>Agent Management</h2>
                    {agents.map((agent, index) => (
                        <div className="agent" key={index} onClick={() => handleAgentClick(agent.name)}>
                            <span className="agent-name">{agent.name}</span>
                            <span className={`status ${agent.status.toLowerCase()}`}>{agent.status}</span>
                        </div>
                    ))}
                </section>

                <section className="agent-activity-log" style={{ gridColumn: '1 / -1', maxHeight: '400px' }}>
                    <AgentActivityLog />
                </section>

                <section className="ai-insights">
                    <h2>AI Insights & Opportunities</h2>
                    <ul>
                        {insights.map((insight, index) => (
                            <li key={index}>
                                {insight.text} {insight.confidence && <strong>{`- ${insight.confidence}% confidence`}</strong>}
                            </li>
                        ))}
                    </ul>
                </section>

                <section className="project-status">
                    <h2>Project Status</h2>
                    {projects.map((project, index) => (
                        <div className="project" key={index}>
                            <span>{project.name} <strong>{project.status}</strong></span>
                        </div>
                    ))}
                </section>

                <section className="tasks-section">
                    <TaskList tasks={tasks} />
                </section>
            </main>

            <div className="footer-logo">
                <img src="/logo1.png" alt="Footer Logo" className="footer-logo-img" />
            </div>

            <CreateTaskModal
                open={isModalOpen}
                onClose={() => setModalOpen(false)}
                onTaskCreated={handleTaskCreated}
            />
        </div>
    );
}

export default HomePage;
