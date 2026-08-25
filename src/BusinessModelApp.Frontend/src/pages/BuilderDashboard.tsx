import React, { useState, useEffect } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import LiveTerminal from '../components/LiveTerminal';
import { api } from '../services/api';

const BuilderDashboard: React.FC = () => {
    const [logs, setLogs] = useState<string[]>([]);
    const [goal, setGoal] = useState('');
    const [isBuilding, setIsBuilding] = useState(false);
    const [connection, setConnection] = useState<any>(null);

    useEffect(() => {
        const newConnection = new HubConnectionBuilder()
            .withUrl('http://localhost:5055/agentHub')
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('Connected to SignalR Hub');
                    connection.on('ReceiveLog', (message: string) => {
                        setLogs(prev => [...prev, message]);
                    });
                })
                .catch((err: any) => console.error('Connection failed: ', err));
        }
    }, [connection]);

    const handleBuild = async () => {
        if (!goal) return;
        setIsBuilding(true);
        setLogs(prev => [...prev, `[System] Starting build task: ${goal}`]);

        try {
            const data = await api.builder.build(goal);
            setLogs(prev => [...prev, `[System] Task Completed: ${data.result}`]);
        } catch (error) {
            console.error('Build failed:', error);
            setLogs(prev => [...prev, `[System] Error: ${error}`]);
        } finally {
            setIsBuilding(false);
        }
    };

    const handleClear = () => {
        setLogs([]);
    };

    return (
        <div style={{ padding: '20px', maxWidth: '1200px', margin: '0 auto' }}>
            <h1 style={{ color: '#fff', marginBottom: '20px' }}>Autonomous Software Builder</h1>

            <div style={{ marginBottom: '20px', display: 'flex', gap: '10px' }}>
                <input
                    type="text"
                    value={goal}
                    onChange={(e) => setGoal(e.target.value)}
                    placeholder="Describe what you want to build..."
                    style={{
                        flex: 1,
                        padding: '10px',
                        borderRadius: '4px',
                        border: '1px solid #444',
                        backgroundColor: '#2d2d2d',
                        color: '#fff'
                    }}
                    disabled={isBuilding}
                />
                <button
                    onClick={handleBuild}
                    disabled={isBuilding || !goal}
                    style={{
                        padding: '10px 20px',
                        borderRadius: '4px',
                        border: 'none',
                        backgroundColor: isBuilding ? '#666' : '#007bff',
                        color: '#fff',
                        cursor: isBuilding ? 'not-allowed' : 'pointer'
                    }}
                >
                    {isBuilding ? 'Building...' : 'Start Build'}
                </button>
                <button
                    onClick={handleClear}
                    style={{
                        padding: '10px 20px',
                        borderRadius: '4px',
                        border: '1px solid #666',
                        backgroundColor: 'transparent',
                        color: '#ccc',
                        cursor: 'pointer'
                    }}
                >
                    Clear
                </button>
            </div>

            <div style={{ border: '1px solid #444', borderRadius: '8px', overflow: 'hidden' }}>
                <div style={{ padding: '10px', backgroundColor: '#333', color: '#ccc', fontSize: '12px' }}>
                    Live Agent Terminal
                </div>
                <LiveTerminal logs={logs} />
            </div>
        </div>
    );
};

export default BuilderDashboard;
