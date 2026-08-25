import { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  IconButton,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
  LinearProgress,
  Chip,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import AddIcon from '@mui/icons-material/Add';
import { 
  useStrategyRisks,
  useCreateRisk,
  useUpdateRisk,
  useUpdateMitigationStatus,
} from '../../api/hooks';
import { 
  StrategyRisk,
  RiskSeverity,
  RiskProbability,
  MitigationStatus,
} from '../../types';

interface RiskFormDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (risk: Partial<StrategyRisk>) => void;
  initialData?: Partial<StrategyRisk>;
  title: string;
}

function RiskFormDialog({
  open,
  onClose,
  onSave,
  initialData,
  title,
}: RiskFormDialogProps) {
  const [formData, setFormData] = useState<Partial<StrategyRisk>>(
    initialData || {
      name: '',
      description: '',
      severity: RiskSeverity.Low,
      probability: RiskProbability.Low,
      category: '',
      mitigationStrategy: '',
      mitigationCost: 0,
      mitigationStatus: 'Not Started' as MitigationStatus,
      owner: '',
      isActive: true,
      affectedAreas: [],
    }
  );

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <Grid container spacing={3} sx={{ mt: 0 }}>
            <Grid item xs={12}>
              <TextField
                label="Risk Name"
                value={formData.name}
                onChange={(e) => setFormData((prev) => ({ ...prev, name: e.target.value }))}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                label="Description"
                value={formData.description}
                onChange={(e) => setFormData((prev) => ({ ...prev, description: e.target.value }))}
                fullWidth
                multiline
                rows={3}
                required
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth required>
                <InputLabel>Severity</InputLabel>
                <Select
                  value={formData.severity}
                  onChange={(e) => setFormData((prev) => ({ ...prev, severity: e.target.value as RiskSeverity }))}
                  label="Severity"
                >
                  {Object.values(RiskSeverity).map((severity) => (
                    <MenuItem key={severity} value={severity}>
                      {severity}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth required>
                <InputLabel>Probability</InputLabel>
                <Select
                  value={formData.probability}
                  onChange={(e) => setFormData((prev) => ({ ...prev, probability: e.target.value as RiskProbability }))}
                  label="Probability"
                >
                  {Object.values(RiskProbability).map((probability) => (
                    <MenuItem key={probability} value={probability}>
                      {probability}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Category"
                value={formData.category}
                onChange={(e) => setFormData((prev) => ({ ...prev, category: e.target.value }))}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Risk Owner"
                value={formData.owner}
                onChange={(e) => setFormData((prev) => ({ ...prev, owner: e.target.value }))}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                label="Mitigation Strategy"
                value={formData.mitigationStrategy}
                onChange={(e) => setFormData((prev) => ({ ...prev, mitigationStrategy: e.target.value }))}
                fullWidth
                multiline
                rows={2}
                required
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Mitigation Cost"
                type="number"
                value={formData.mitigationCost}
                onChange={(e) => setFormData((prev) => ({ ...prev, mitigationCost: parseFloat(e.target.value) || 0 }))}
                fullWidth
                required
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth required>
                <InputLabel>Mitigation Status</InputLabel>
                <Select
                  value={formData.mitigationStatus}
                  onChange={(e) => setFormData((prev) => ({ ...prev, mitigationStatus: e.target.value as MitigationStatus }))}
                  label="Mitigation Status"
                >
                  <MenuItem value="Not Started">Not Started</MenuItem>
                  <MenuItem value="In Progress">In Progress</MenuItem>
                  <MenuItem value="Completed">Completed</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
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

function RiskCard({ risk }: { risk: StrategyRisk }) {
  const updateMitigation = useUpdateMitigationStatus();
  const updateRisk = useUpdateRisk();
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  const handleStatusChange = async (status: MitigationStatus) => {
    try {
      await updateMitigation.mutateAsync({ id: risk.id, status });
    } catch (error) {
      console.error('Failed to update mitigation status:', error);
    }
  };

  const handleEdit = async (updates: Partial<StrategyRisk>) => {
    try {
      await updateRisk.mutateAsync({ id: risk.id, updates });
      setEditDialogOpen(false);
    } catch (error) {
      console.error('Failed to update risk:', error);
    }
  };

  const severityColor = {
    Low: 'success',
    Medium: 'warning',
    High: 'error',
    Critical: 'error',
  }[risk.severity] as 'success' | 'warning' | 'error';

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="flex-start">
          <Typography variant="h6" gutterBottom>
            {risk.name}
          </Typography>
          <IconButton size="small" onClick={() => setEditDialogOpen(true)}>
            <EditIcon />
          </IconButton>
        </Box>

        <Typography variant="body2" color="text.secondary" paragraph>
          {risk.description}
        </Typography>

        <Grid container spacing={2}>
          <Grid item xs={12} sm={6}>
            <Chip
              label={`Severity: ${risk.severity}`}
              color={severityColor}
              size="small"
              sx={{ mr: 1 }}
            />
            <Chip
              label={`Probability: ${risk.probability}`}
              color="primary"
              size="small"
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="body2">
              Category: {risk.category}
            </Typography>
            <Typography variant="body2">
              Owner: {risk.owner}
            </Typography>
          </Grid>
        </Grid>

        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            Mitigation Progress
          </Typography>
          <LinearProgress
            variant="determinate"
            value={
              risk.mitigationStatus === 'Completed' ? 100 :
              risk.mitigationStatus === 'In Progress' ? 50 : 0
            }
            sx={{ height: 8, borderRadius: 4 }}
          />
          <Box display="flex" justifyContent="space-between" mt={1}>
            <FormControl size="small">
              <Select
                value={risk.mitigationStatus}
                onChange={(e) => handleStatusChange(e.target.value as MitigationStatus)}
                sx={{ minWidth: 120 }}
              >
                <MenuItem value="Not Started">Not Started</MenuItem>
                <MenuItem value="In Progress">In Progress</MenuItem>
                <MenuItem value="Completed">Completed</MenuItem>
              </Select>
            </FormControl>
            <Typography variant="body2" color="text.secondary">
              Cost: {new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD',
              }).format(risk.mitigationCost)}
            </Typography>
          </Box>
        </Box>
      </CardContent>

      <RiskFormDialog
        open={editDialogOpen}
        onClose={() => setEditDialogOpen(false)}
        onSave={handleEdit}
        initialData={risk}
        title="Edit Risk"
      />
    </Card>
  );
}

export function RiskManagement() {
  const { data: risks, isLoading, error } = useStrategyRisks();
  const createRisk = useCreateRisk();
  const [addDialogOpen, setAddDialogOpen] = useState(false);

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={3}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !risks) {
    return (
      <Box p={3}>
        <Typography color="error">
          Failed to load strategic risks
        </Typography>
      </Box>
    );
  }

  const handleCreateRisk = async (risk: Partial<StrategyRisk>) => {
    try {
      await createRisk.mutateAsync(risk);
      setAddDialogOpen(false);
    } catch (error) {
      console.error('Failed to create risk:', error);
    }
  };

  const activeRisks = risks.data.filter((risk) => risk.isActive);
  const sortedRisks = [...activeRisks].sort((a, b) => {
    const severityOrder = { Critical: 3, High: 2, Medium: 1, Low: 0 };
    return severityOrder[b.severity] - severityOrder[a.severity];
  });

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Strategic Risk Management</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setAddDialogOpen(true)}
        >
          Add Risk
        </Button>
      </Box>

      <Grid container spacing={3}>
        {sortedRisks.map((risk) => (
          <Grid item xs={12} md={6} key={risk.id}>
            <RiskCard risk={risk} />
          </Grid>
        ))}
      </Grid>

      <RiskFormDialog
        open={addDialogOpen}
        onClose={() => setAddDialogOpen(false)}
        onSave={handleCreateRisk}
        title="Add New Risk"
      />
    </Box>
  );
}