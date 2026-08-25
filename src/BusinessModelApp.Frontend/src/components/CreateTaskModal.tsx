import React, { useState, useEffect } from 'react';
import { Modal, Box, Typography, TextField, Button, Select, MenuItem, FormControl, InputLabel, CircularProgress } from '@mui/material';
import { User } from '../types';
import { getUsers } from '../services/userService';
import { createTask } from '../services/taskService';

const style = {
    position: 'absolute' as 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 400,
    bgcolor: 'background.paper',
    border: '2px solid #000',
    boxShadow: 24,
    p: 4,
};

interface CreateTaskModalProps {
    open: boolean;
    onClose: () => void;
    onTaskCreated: () => void;
}

const CreateTaskModal: React.FC<CreateTaskModalProps> = ({ open, onClose, onTaskCreated }) => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [assignedToUserId, setAssignedToUserId] = useState<string>('');
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const fetchExecutiveUsers = async () => {
            if (open) {
                console.log("CreateTaskModal opened, fetching executive users...");
                setLoading(true);

                try {
                    const fetchedUsers = await getUsers();
                    console.log("API response received:", fetchedUsers);
                    const executiveRoleNames = ['ceo', 'cbo', 'cfo', 'chro', 'cto'];

                    const executiveUsers = fetchedUsers.filter(user =>
                        user.role && executiveRoleNames.includes(user.role.toLowerCase())
                    );

                    if (executiveUsers.length > 0) {
                        console.log("Using API users:", executiveUsers);
                        setUsers(executiveUsers);
                        console.log("No executive users from API.");
                        // Keep fallback users if API returns nothing
                        // setUsers([]); 
                    }
                } catch (error) {
                    console.error("Failed to fetch users from API:", error);
                } finally {
                    setLoading(false);
                }
            }
        };

        fetchExecutiveUsers();
    }, [open]);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!title || !description || !assignedToUserId) {
            alert('Please fill out all fields.');
            return;
        }
        try {
            await createTask(title, description, assignedToUserId);
            onTaskCreated();
            onClose(); // Close modal on success
            // Reset state
            setTitle('');
            setDescription('');
            setAssignedToUserId('');
        } catch (error) {
            console.error('Failed to create task:', error);
            alert('Failed to create task. See console for details.');
        }
    };

    return (
        <Modal
            open={open}
            onClose={onClose}
            aria-labelledby="create-task-modal-title"
        >
            <Box sx={style} component="form" onSubmit={handleSubmit}>
                <Typography id="create-task-modal-title" variant="h6" component="h2">
                    Create a New Task
                </Typography>
                <TextField
                    margin="normal"
                    required
                    fullWidth
                    id="title"
                    label="Task Title"
                    name="title"
                    autoFocus
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                />
                <TextField
                    margin="normal"
                    required
                    fullWidth
                    id="description"
                    label="Task Description"
                    name="description"
                    multiline
                    rows={4}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                />
                <FormControl fullWidth margin="normal" required>
                    <InputLabel id="assign-to-label">Assign To</InputLabel>
                    <Select
                        labelId="assign-to-label"
                        id="assign-to"
                        value={assignedToUserId}
                        label="Assign To"
                        onChange={(e) => setAssignedToUserId(e.target.value as string)}
                        disabled={loading || users.length === 0}
                    >
                        {loading && <MenuItem value=""><CircularProgress size={24} /></MenuItem>}
                        {!loading && users.length === 0 && <MenuItem value="" disabled>No agents available</MenuItem>}
                        {users.map((user) => (
                            <MenuItem key={user.id} value={user.id}>
                                {user.name} ({user.role})
                            </MenuItem>
                        ))}
                    </Select>
                </FormControl>
                <Button
                    type="submit"
                    fullWidth
                    variant="contained"
                    sx={{ mt: 3, mb: 2 }}
                    disabled={loading || !assignedToUserId}
                >
                    Create Task
                </Button>
            </Box>
        </Modal>
    );
};

export default CreateTaskModal;
