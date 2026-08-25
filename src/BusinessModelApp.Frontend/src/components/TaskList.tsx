import React from 'react';
import { Task } from '../types';
import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';

interface TaskListProps {
    tasks: Task[];
}

const TaskList: React.FC<TaskListProps> = ({ tasks }) => {
    if (tasks.length === 0) {
        return <Typography sx={{ mt: 4, textAlign: 'center' }}>No tasks assigned yet.</Typography>;
    }

    return (
        <TableContainer component={Paper} sx={{ mt: 4 }}>
            <Typography variant="h6" component="div" sx={{ p: 2 }}>
                Assigned Tasks
            </Typography>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>Description</TableCell>
                        <TableCell>Assigned To</TableCell>
                        <TableCell>Assigned By</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Created At</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {tasks.map((task) => (
                        <TableRow key={task.id}>
                            <TableCell>{task.description}</TableCell>
                            <TableCell>{task.assignedToUserName}</TableCell>
                            <TableCell>{task.assignedByUserName}</TableCell>
                            <TableCell>{task.status}</TableCell>
                            <TableCell>{new Date(task.createdAt).toLocaleString()}</TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    );
};

export default TaskList;
