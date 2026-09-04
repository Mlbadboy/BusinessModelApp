import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Chip,
  Button,
  Tabs,
  Tab,
  LinearProgress,
  Alert,
  CircularProgress,
  Stack,
  Divider,
  Paper
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
import CameraAltIcon from '@mui/icons-material/CameraAlt';
import SyncProblemIcon from '@mui/icons-material/SyncProblem';
import ShieldIcon from '@mui/icons-material/Shield';

export interface DigitalTwinField {
  fieldPath: string;
  fieldName: string;
  dimension: number;
  displayValue: string;
  classification: number; // 1=Fact, 2=Estimate, 3=Hypothesis, 4=Observation, 5=Learning, 6=Unknown
  confidence: number;
  evidenceRecordIds: string[];
  groundingEvidenceHashes: string[];
  source: string;
  observedAt: string;
  freshness: number; // 1=Verified, 2=Aging, 3=Stale, 4=Unknown
  isDisputed: boolean;
  activeConflictId?: string;
  note: string;
  isGroundedFact: boolean;
}

export interface DigitalTwinDimensionState {
  dimension: number;
  dimensionName: string;
  fields: DigitalTwinField[];
  totalFields: number;
  knownFieldsCount: number;
  unknownFieldsCount: number;
  staleFieldsCount: number;
  disputedFieldsCount: number;
  averageConfidence: number;
}

export interface DigitalTwinConflict {
  id: string;
  workspaceId: string;
  fieldPath: string;
  sourceA: string;
  valueA: string;
  confidenceA: number;
  sourceB: string;
  valueB: string;
  confidenceB: number;
  status: number;
  detectedAt: string;
  resolutionNote?: string;
}

export interface DigitalTwinHealthReport {
  evidenceCoveragePercent: number;
  freshnessPercent: number;
  unknownRatioPercent: number;
  staleRatioPercent: number;
  conflictRatioPercent: number;
  averageConfidence: number;
  activeConnectorCount: number;
  totalTrackedFields: number;
}

export interface DigitalTwinStateResponse {
  workspaceId: string;
  asOfUtc: string;
  dimensions: Record<string, DigitalTwinDimensionState>;
  activeConflicts: DigitalTwinConflict[];
  healthReport: DigitalTwinHealthReport;
  totalTrackedFields: number;
}

const CLASSIFICATION_MAP: Record<number, { label: string; color: 'success' | 'info' | 'secondary' | 'warning' | 'default'; bg: string; text: string }> = {
  1: { label: 'FACT (VERIFIED)', color: 'success', bg: '#ecfdf5', text: '#065f46' },
  2: { label: 'ESTIMATE', color: 'info', bg: '#eff6ff', text: '#1e40af' },
  3: { label: 'HYPOTHESIS', color: 'secondary', bg: '#faf5ff', text: '#6b21a8' },
  4: { label: 'OBSERVATION', color: 'warning', bg: '#fffbeb', text: '#92400e' },
  5: { label: 'LEARNING', color: 'default', bg: '#f1f5f9', text: '#334155' },
  6: { label: 'UNKNOWN', color: 'default', bg: '#f8fafc', text: '#64748b' }
};

export const DigitalTwinExplorer: React.FC = () => {
  const [twinState, setTwinState] = useState<DigitalTwinStateResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [snapshotLoading, setSnapshotLoading] = useState<boolean>(false);
  const [snapshotSuccess, setSnapshotSuccess] = useState<string | null>(null);
  const [selectedDimensionKey, setSelectedDimensionKey] = useState<string>('1');
  const [filterMode, setFilterMode] = useState<'all' | 'unknowns' | 'facts' | 'disputed'>('all');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const fetchTwinState = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const token = localStorage.getItem('token');
      const res = await fetch('/api/digital-twin/state', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (!res.ok) {
        throw new Error(`Failed to load Digital Twin: HTTP ${res.status}`);
      }
      const data: DigitalTwinStateResponse = await res.json();
      setTwinState(data);
      const firstKey = Object.keys(data.dimensions)[0];
      if (firstKey && !data.dimensions[selectedDimensionKey]) {
        setSelectedDimensionKey(firstKey);
      }
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to connect to Digital Twin service');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTwinState();
  }, []);

  const handleCaptureSnapshot = async () => {
    setSnapshotLoading(true);
    setSnapshotSuccess(null);
    try {
      const token = localStorage.getItem('token');
      const res = await fetch('/api/digital-twin/snapshot', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (!res.ok) throw new Error('Snapshot creation failed.');
      const snap = await res.json();
      setSnapshotSuccess(`Point-in-time Snapshot ${snap.id.substring(0, 8)} captured. Hash: ${snap.integrityHash.substring(0, 16)}...`);
      fetchTwinState();
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to capture snapshot.');
    } finally {
      setSnapshotLoading(false);
    }
  };

  if (loading && !twinState) {
    return (
      <Box sx={{ p: 4, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 300 }}>
        <CircularProgress size={36} sx={{ mb: 2 }} />
        <Typography variant="body2" color="textSecondary">
          Projecting Company Digital Twin from verified Evidence Graph...
        </Typography>
      </Box>
    );
  }

  const dimensionsList = twinState ? Object.values(twinState.dimensions) : [];
  const currentDim = twinState?.dimensions[selectedDimensionKey] || dimensionsList[0];

  let displayFields: DigitalTwinField[] = [];
  if (filterMode === 'unknowns') {
    displayFields = dimensionsList.flatMap(d => d.fields).filter(f => f.classification === 6);
  } else if (filterMode === 'facts') {
    displayFields = dimensionsList.flatMap(d => d.fields).filter(f => f.classification === 1);
  } else if (filterMode === 'disputed') {
    displayFields = dimensionsList.flatMap(d => d.fields).filter(f => f.isDisputed);
  } else {
    displayFields = currentDim?.fields || [];
  }

  const health = twinState?.healthReport;

  return (
    <Box sx={{ width: '100%' }}>
      {/* HEADER & HEALTH METROLOGY */}
      <Paper sx={{ p: 3, mb: 3, borderRadius: 2, border: '1px solid #e2e8f0', background: 'linear-gradient(135deg, #f8fafc 0%, #ffffff 100%)' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 2 }}>
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <ShieldIcon color="primary" />
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a' }}>
                Company Digital Twin & Reality Metrology
              </Typography>
              <Chip label="Phase 2 Certified" color="success" size="small" sx={{ fontWeight: 700, fontSize: '0.7rem' }} />
            </Box>
            <Typography variant="caption" color="textSecondary">
              Authoritative point-in-time projection across 22 business dimensions. Strict reality classification with zero ungrounded defaults.
            </Typography>
          </Box>
          <Stack direction="row" spacing={1.5}>
            <Button
              variant="outlined"
              size="small"
              startIcon={<RefreshIcon />}
              onClick={fetchTwinState}
              disabled={loading}
              sx={{ fontWeight: 700 }}
            >
              Refresh Twin
            </Button>
            <Button
              variant="contained"
              size="small"
              startIcon={snapshotLoading ? <CircularProgress size={14} color="inherit" /> : <CameraAltIcon />}
              onClick={handleCaptureSnapshot}
              disabled={snapshotLoading}
              sx={{ fontWeight: 700, bgcolor: '#0f172a', '&:hover': { bgcolor: '#1e293b' } }}
            >
              Capture Snapshot
            </Button>
          </Stack>
        </Box>

        {snapshotSuccess && (
          <Alert severity="success" sx={{ mb: 2, py: 0.5 }} onClose={() => setSnapshotSuccess(null)}>
            {snapshotSuccess}
          </Alert>
        )}

        {errorMsg && (
          <Alert severity="error" sx={{ mb: 2, py: 0.5 }} onClose={() => setErrorMsg(null)}>
            {errorMsg}
          </Alert>
        )}

        {/* HEALTH METRICS CARDS */}
        <Grid container spacing={2}>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Evidence Coverage</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#059669' }}>
                {Math.round((health?.evidenceCoveragePercent || 0) * 100)}%
              </Typography>
              <LinearProgress variant="determinate" value={(health?.evidenceCoveragePercent || 0) * 100} color="success" sx={{ height: 4, borderRadius: 2, mt: 0.5 }} />
            </Box>
          </Grid>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Reality Freshness</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#0284c7' }}>
                {Math.round((health?.freshnessPercent || 0) * 100)}%
              </Typography>
              <LinearProgress variant="determinate" value={(health?.freshnessPercent || 0) * 100} color="info" sx={{ height: 4, borderRadius: 2, mt: 0.5 }} />
            </Box>
          </Grid>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Unknown Ratio</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#64748b' }}>
                {Math.round((health?.unknownRatioPercent || 0) * 100)}%
              </Typography>
              <LinearProgress variant="determinate" value={(health?.unknownRatioPercent || 0) * 100} color="inherit" sx={{ height: 4, borderRadius: 2, mt: 0.5 }} />
            </Box>
          </Grid>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Active Conflicts</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: (twinState?.activeConflicts?.length || 0) > 0 ? '#dc2626' : '#059669' }}>
                {twinState?.activeConflicts?.length || 0}
              </Typography>
              <Typography variant="caption" color="textSecondary" sx={{ fontSize: '0.65rem' }}>
                {(twinState?.activeConflicts?.length || 0) > 0 ? 'Disputed records' : 'Zero disputes'}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Average Confidence</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#4f46e5' }}>
                {Math.round((health?.averageConfidence || 0) * 100)}%
              </Typography>
              <LinearProgress variant="determinate" value={(health?.averageConfidence || 0) * 100} color="primary" sx={{ height: 4, borderRadius: 2, mt: 0.5 }} />
            </Box>
          </Grid>
          <Grid item xs={6} md={2}>
            <Box sx={{ p: 1.5, bgcolor: '#ffffff', borderRadius: 1.5, border: '1px solid #e2e8f0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 600 }}>Tracked Fields</Typography>
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                {twinState?.totalTrackedFields || 0}
              </Typography>
              <Typography variant="caption" color="textSecondary" sx={{ fontSize: '0.65rem' }}>
                22 Dimensions
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* DISPUTED / CONFLICT ALERT */}
      {twinState && twinState.activeConflicts && twinState.activeConflicts.length > 0 && (
        <Alert
          severity="error"
          icon={<SyncProblemIcon fontSize="inherit" />}
          sx={{ mb: 3, border: '1px solid #fca5a5', '& .MuiAlert-message': { width: '100%' } }}
        >
          <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 0.5 }}>
            Active Reality Discrepancy Detected ({twinState.activeConflicts.length})
          </Typography>
          {twinState.activeConflicts.map((c) => (
            <Box key={c.id} sx={{ bgcolor: '#fff', p: 1.5, borderRadius: 1, border: '1px solid #fee2e2', mt: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 700, color: '#991b1b' }}>
                Field: {c.fieldPath}
              </Typography>
              <Grid container spacing={2} sx={{ mt: 0.5 }}>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">Source A ({c.sourceA}):</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 800 }}>{c.valueA}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">Source B ({c.sourceB}):</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 800 }}>{c.valueB}</Typography>
                </Grid>
              </Grid>
              {c.resolutionNote && (
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 1, fontStyle: 'italic' }}>
                  {c.resolutionNote}
                </Typography>
              )}
            </Box>
          ))}
        </Alert>
      )}

      {/* CLASSIFICATION FILTER TOGGLES */}
      <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap', alignItems: 'center' }}>
        <Typography variant="caption" color="textSecondary" sx={{ fontWeight: 700, mr: 1 }}>
          VIEW MODE:
        </Typography>
        <Chip
          label="All Dimensions"
          color={filterMode === 'all' ? 'primary' : 'default'}
          onClick={() => setFilterMode('all')}
          clickable
          size="small"
          sx={{ fontWeight: 700 }}
        />
        <Chip
          label="Grounded Facts Only"
          color={filterMode === 'facts' ? 'success' : 'default'}
          onClick={() => setFilterMode('facts')}
          clickable
          size="small"
          sx={{ fontWeight: 700 }}
        />
        <Chip
          label={`Unknowns (${twinState ? dimensionsList.flatMap(d => d.fields).filter(f => f.classification === 6).length : 0})`}
          color={filterMode === 'unknowns' ? 'secondary' : 'default'}
          onClick={() => setFilterMode('unknowns')}
          clickable
          size="small"
          sx={{ fontWeight: 700 }}
        />
        {twinState && twinState.activeConflicts.length > 0 && (
          <Chip
            label={`Disputed / Conflicts (${twinState.activeConflicts.length})`}
            color={filterMode === 'disputed' ? 'error' : 'default'}
            onClick={() => setFilterMode('disputed')}
            clickable
            size="small"
            sx={{ fontWeight: 700 }}
          />
        )}
      </Box>

      {/* 22-DIMENSION TABS (when in 'all' mode) */}
      {filterMode === 'all' && (
        <Paper sx={{ mb: 3, borderRadius: 1.5, border: '1px solid #e2e8f0', bgcolor: '#fff' }}>
          <Tabs
            value={selectedDimensionKey}
            onChange={(_, val) => setSelectedDimensionKey(val)}
            variant="scrollable"
            scrollButtons="auto"
            indicatorColor="primary"
            textColor="primary"
            sx={{
              minHeight: 44,
              '& .MuiTab-root': {
                minHeight: 44,
                fontWeight: 700,
                fontSize: '0.8rem',
                textTransform: 'none',
                py: 1
              }
            }}
          >
            {dimensionsList.map((d) => (
              <Tab
                key={d.dimension}
                value={String(d.dimension)}
                label={`${d.dimensionName} (${d.fields.length})`}
              />
            ))}
          </Tabs>
        </Paper>
      )}

      {/* DIMENSION FIELDS GRID */}
      <Grid container spacing={2}>
        {displayFields.map((field) => {
          const classMeta = CLASSIFICATION_MAP[field.classification] || CLASSIFICATION_MAP[6];
          const isStale = field.freshness === 3;
          const isUnknown = field.classification === 6;

          return (
            <Grid item xs={12} sm={6} md={4} key={field.fieldPath}>
              <Card
                sx={{
                  height: '100%',
                  borderRadius: 2,
                  border: field.isDisputed
                    ? '1.5px solid #ef4444'
                    : isStale
                    ? '1.5px solid #f59e0b'
                    : isUnknown
                    ? '1px dashed #94a3b8'
                    : '1px solid #e2e8f0',
                  bgcolor: isUnknown ? '#f8fafc' : '#ffffff',
                  boxShadow: 'none',
                  transition: 'all 0.2s',
                  '&:hover': {
                    boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
                  }
                }}
              >
                <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1.5 }}>
                    <Box>
                      <Typography variant="caption" color="textSecondary" sx={{ fontSize: '0.7rem', display: 'block' }}>
                        {field.fieldPath}
                      </Typography>
                      <Typography variant="subtitle2" sx={{ fontWeight: 800, color: '#1e293b' }}>
                        {field.fieldName}
                      </Typography>
                    </Box>
                    <Chip
                      label={classMeta.label}
                      size="small"
                      sx={{
                        bgcolor: classMeta.bg,
                        color: classMeta.text,
                        fontWeight: 800,
                        fontSize: '0.65rem',
                        height: 20
                      }}
                    />
                  </Box>

                  {/* DISPLAY VALUE */}
                  <Box sx={{ my: 1.5 }}>
                    <Typography
                      variant="h5"
                      sx={{
                        fontWeight: 900,
                        color: isUnknown ? '#64748b' : field.isDisputed ? '#dc2626' : '#0f172a',
                        fontStyle: isUnknown ? 'italic' : 'normal',
                        fontSize: isUnknown ? '1.1rem' : '1.35rem'
                      }}
                    >
                      {field.displayValue}
                    </Typography>
                    {isUnknown && (
                      <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 0.5, fontStyle: 'italic' }}>
                        Ungrounded — Awaiting external telemetry or financial evidence.
                      </Typography>
                    )}
                  </Box>

                  <Divider sx={{ my: 1 }} />

                  {/* PROVENANCE & METROLOGY */}
                  <Stack spacing={0.75}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Confidence:</Typography>
                      <Typography variant="caption" sx={{ fontWeight: 700 }}>
                        {Math.round(field.confidence * 100)}%
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="textSecondary">Provenance Source:</Typography>
                      <Typography variant="caption" sx={{ fontWeight: 700, maxWidth: 180, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {field.source}
                      </Typography>
                    </Box>

                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography variant="caption" color="textSecondary">Freshness:</Typography>
                      <Chip
                        label={isStale ? 'STALE' : field.freshness === 1 ? 'FRESH' : field.freshness === 2 ? 'AGING' : 'UNKNOWN'}
                        size="small"
                        color={isStale ? 'warning' : field.freshness === 1 ? 'success' : 'default'}
                        sx={{ fontSize: '0.6rem', height: 18, fontWeight: 700 }}
                      />
                    </Box>

                    {field.evidenceRecordIds && field.evidenceRecordIds.length > 0 && (
                      <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                        <Typography variant="caption" color="textSecondary">Evidence Chain:</Typography>
                        <Typography variant="caption" sx={{ fontWeight: 700, color: '#059669' }}>
                          {field.evidenceRecordIds.length} Verified Records
                        </Typography>
                      </Box>
                    )}

                    {field.note && (
                      <Typography variant="caption" color="textSecondary" sx={{ fontSize: '0.68rem', mt: 0.5, fontStyle: 'italic', display: 'block' }}>
                        {field.note}
                      </Typography>
                    )}
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          );
        })}

        {displayFields.length === 0 && (
          <Grid item xs={12}>
            <Box sx={{ p: 4, textAlign: 'center' }}>
              <Typography variant="body2" color="textSecondary">
                No fields match the selected filter.
              </Typography>
            </Box>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};
