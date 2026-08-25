import { useState } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Button,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  LinearProgress,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { useExpenseCategories, useAddExpense, useUpdateExpense, useDeleteExpense } from '../../api/hooks';
import type { ExpenseCategory } from '../../types';

interface EditDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (data: Partial<ExpenseCategory>) => void;
  initialData?: Partial<ExpenseCategory>;
  title: string;
}

function EditExpenseCategoryDialog({
  open,
  onClose,
  onSave,
  initialData,
  title,
}: EditDialogProps) {
  const [formData, setFormData] = useState<Partial<ExpenseCategory>>(
    initialData || {
      name: '',
      description: '',
      totalAmount: 0,
      budget: 0,
    }
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Category Name"
              value={formData.name}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, name: e.target.value }))
              }
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={formData.description}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, description: e.target.value }))
              }
              fullWidth
              multiline
              rows={2}
            />
            <TextField
              label="Budget"
              type="number"
              value={formData.budget}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  budget: parseFloat(e.target.value) || 0,
                }))
              }
              fullWidth
              required
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained">
            Save
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

function BudgetProgressBar({ spent, budget }: { spent: number; budget: number }) {
  const percentage = (spent / budget) * 100;
  const color = percentage > 90 ? 'error' : percentage > 75 ? 'warning' : 'success';

  return (
    <Box sx={{ width: '100%', mr: 1 }}>
      <LinearProgress
        variant="determinate"
        value={Math.min(percentage, 100)}
        color={color}
        sx={{ height: 8, borderRadius: 4 }}
      />
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          mt: 0.5,
          fontSize: '0.75rem',
        }}
      >
        <Typography variant="caption">
          {new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
          }).format(spent)}
        </Typography>
        <Typography variant="caption" color={color}>
          {percentage.toFixed(1)}%
        </Typography>
      </Box>
    </Box>
  );
}

export function ExpenseCategoryList() {
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<ExpenseCategory | null>(null);

  const { data: categories, isLoading, error } = useExpenseCategories();
  const addExpenseMutation = useAddExpense();
  const updateExpenseMutation = useUpdateExpense();
  const deleteExpenseMutation = useDeleteExpense();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !categories) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load expense categories
        </Typography>
      </Box>
    );
  }

  const handleEdit = (category: ExpenseCategory) => {
    setSelectedCategory(category);
    setEditDialogOpen(true);
  };

  const handleAdd = () => {
    setSelectedCategory(null);
    setAddDialogOpen(true);
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this expense category?')) {
      try {
        await deleteExpenseMutation.mutateAsync(id);
      } catch (error) {
        console.error('Failed to delete expense category:', error);
      }
    }
  };

  const handleSaveAdd = async (data: Partial<ExpenseCategory>) => {
    try {
      await addExpenseMutation.mutateAsync(data);
      setAddDialogOpen(false);
    } catch (error) {
      console.error('Failed to add expense category:', error);
    }
  };

  const handleSaveEdit = async (data: Partial<ExpenseCategory>) => {
    if (!selectedCategory?.id) return;

    try {
      await updateExpenseMutation.mutateAsync({
        id: selectedCategory.id,
        updates: data,
      });
      setEditDialogOpen(false);
    } catch (error) {
      console.error('Failed to update expense category:', error);
    }
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h6">Expense Categories</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleAdd}
        >
          Add Category
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Category</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Budget Usage</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {categories.data.map((category) => (
              <TableRow key={category.id}>
                <TableCell>{category.name}</TableCell>
                <TableCell>{category.description}</TableCell>
                <TableCell sx={{ width: '30%' }}>
                  <BudgetProgressBar
                    spent={category.totalAmount}
                    budget={category.budget}
                  />
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    onClick={() => handleEdit(category)}
                  >
                    <EditIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    onClick={() => handleDelete(category.id)}
                    color="error"
                  >
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <EditExpenseCategoryDialog
        open={addDialogOpen}
        onClose={() => setAddDialogOpen(false)}
        onSave={handleSaveAdd}
        title="Add Expense Category"
      />

      <EditExpenseCategoryDialog
        open={editDialogOpen}
        onClose={() => setEditDialogOpen(false)}
        onSave={handleSaveEdit}
        initialData={selectedCategory || undefined}
        title="Edit Expense Category"
      />
    </Box>
  );
}