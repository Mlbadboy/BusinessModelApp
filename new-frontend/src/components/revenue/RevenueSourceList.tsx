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
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { useRevenueSourcePerformance, useAddRevenueSource, useUpdateRevenueSource } from '../../api/hooks';
import type { RevenueSourcePerformance } from '../../types';

interface EditDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (data: Partial<RevenueSourcePerformance>) => void;
  initialData?: Partial<RevenueSourcePerformance>;
  title: string;
}

function EditRevenueSourceDialog({
  open,
  onClose,
  onSave,
  initialData,
  title,
}: EditDialogProps) {
  const [formData, setFormData] = useState<Partial<RevenueSourcePerformance>>(
    initialData || {
      sourceName: '',
      revenue: 0,
      growth: 0,
      contribution: 0,
      profitMargin: 0,
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
              label="Source Name"
              value={formData.sourceName}
              onChange={(e) =>
                setFormData((prev) => ({ ...prev, sourceName: e.target.value }))
              }
              fullWidth
              required
            />
            <TextField
              label="Revenue"
              type="number"
              value={formData.revenue}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  revenue: parseFloat(e.target.value) || 0,
                }))
              }
              fullWidth
              required
            />
            <TextField
              label="Growth (%)"
              type="number"
              value={formData.growth}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  growth: parseFloat(e.target.value) || 0,
                }))
              }
              fullWidth
              required
            />
            <TextField
              label="Profit Margin (%)"
              type="number"
              value={formData.profitMargin}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  profitMargin: parseFloat(e.target.value) || 0,
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

export function RevenueSourceList() {
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [selectedSource, setSelectedSource] = useState<RevenueSourcePerformance | null>(null);

  const { data: sources, isLoading, error } = useRevenueSourcePerformance();
  const addRevenueMutation = useAddRevenueSource();
  const updateRevenueMutation = useUpdateRevenueSource();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !sources) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load revenue sources
        </Typography>
      </Box>
    );
  }

  const handleEdit = (source: RevenueSourcePerformance) => {
    setSelectedSource(source);
    setEditDialogOpen(true);
  };

  const handleAdd = () => {
    setSelectedSource(null);
    setAddDialogOpen(true);
  };

  const handleSaveAdd = async (data: Partial<RevenueSourcePerformance>) => {
    try {
      await addRevenueMutation.mutateAsync(data);
      setAddDialogOpen(false);
    } catch (error) {
      console.error('Failed to add revenue source:', error);
    }
  };

  const handleSaveEdit = async (data: Partial<RevenueSourcePerformance>) => {
    if (!selectedSource?.sourceId) return;

    try {
      await updateRevenueMutation.mutateAsync({
        id: selectedSource.sourceId,
        updates: data,
      });
      setEditDialogOpen(false);
    } catch (error) {
      console.error('Failed to update revenue source:', error);
    }
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h6">Revenue Sources</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleAdd}
        >
          Add Revenue Source
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Source Name</TableCell>
              <TableCell align="right">Revenue</TableCell>
              <TableCell align="right">Growth</TableCell>
              <TableCell align="right">Contribution</TableCell>
              <TableCell align="right">Profit Margin</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sources.data.map((source) => (
              <TableRow key={source.sourceId}>
                <TableCell>{source.sourceName}</TableCell>
                <TableCell align="right">
                  {new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: 'USD',
                  }).format(source.revenue)}
                </TableCell>
                <TableCell align="right">{`${source.growth.toFixed(1)}%`}</TableCell>
                <TableCell align="right">{`${source.contribution.toFixed(1)}%`}</TableCell>
                <TableCell align="right">{`${source.profitMargin.toFixed(1)}%`}</TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    onClick={() => handleEdit(source)}
                  >
                    <EditIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <EditRevenueSourceDialog
        open={addDialogOpen}
        onClose={() => setAddDialogOpen(false)}
        onSave={handleSaveAdd}
        title="Add Revenue Source"
      />

      <EditRevenueSourceDialog
        open={editDialogOpen}
        onClose={() => setEditDialogOpen(false)}
        onSave={handleSaveEdit}
        initialData={selectedSource || undefined}
        title="Edit Revenue Source"
      />
    </Box>
  );
}