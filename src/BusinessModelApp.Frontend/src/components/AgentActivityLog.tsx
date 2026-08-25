import React, { useEffect, useState } from 'react';
import { Box, Typography, Paper, List, ListItem, ListItemText, Divider, Chip } from '@mui/material';
import axios from 'axios';
import { AgentActivity } from '../types';

const AgentActivityLog: React.FC = () => {
    const [activities, setActivities] = useState<AgentActivity[]>([]);

    useEffect(() => {
        const fetchActivities = async () => {
            try {
                const response = await axios.get<AgentActivity[]>('http://localhost:5055/api/agents/activities');
                setActivities(response.data);
            } catch (error) {
                console.error('Error fetching agent activities:', error);
            }
        };

        fetchActivities();
        const interval = setInterval(fetchActivities, 5000); // Poll every 5 seconds

        return () => clearInterval(interval);
    }, []);

    return (
        <Paper elevation={3} sx={{ p: 2, height: '100%', overflow: 'auto' }}>
            <Typography variant="h6" gutterBottom>
                Agent Activity Log
            </Typography>
            <List>
                {activities.length === 0 ? (
                    <ListItem>
                        <ListItemText primary="No activity recorded yet." />
                    </ListItem>
                ) : (
                    activities.map((activity, index) => (
                        <React.Fragment key={activity.id}>
                            <ListItem alignItems="flex-start">
                                <ListItemText
                                    primary={
                                        <Box display="flex" justifyContent="space-between" alignItems="center">
                                            <Typography variant="subtitle1" component="span">
                                                {activity.agentName}
                                            </Typography>
                                            <Typography variant="caption" color="text.secondary">
                                                {new Date(activity.timestamp).toLocaleTimeString()}
                                            </Typography>
                                        </Box>
                                    }
                                    secondary={
                                        <React.Fragment>
                                            <Typography
                                                component="span"
                                                variant="body2"
                                                color="text.primary"
                                            >
                                                {activity.description}
                                            </Typography>
                                            {activity.details && (
                                                <Typography variant="caption" display="block" color="text.secondary">
                                                    {activity.details}
                                                </Typography>
                                            )}
                                            <Box mt={1}>
                                                <Chip label={activity.activityType} size="small" color="primary" variant="outlined" />
                                            </Box>
                                        </React.Fragment>
                                    }
                                    secondaryTypographyProps={{ component: 'div' }}
                                />
                            </ListItem>
                            {index < activities.length - 1 && <Divider component="li" />}
                        </React.Fragment>
                    ))
                )}
            </List>
        </Paper>
    );
};

export default AgentActivityLog;
