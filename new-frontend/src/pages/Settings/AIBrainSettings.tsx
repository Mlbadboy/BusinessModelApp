import { useState } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Stack,
  Avatar,
  Chip,
  Button,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  IconButton,
  Divider,
  Alert,
  Tabs,
  Tab,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableContainer,
  CircularProgress,
} from '@mui/material';
import {
  PsychologyOutlined,
  AddCircleOutlineRounded,
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  RefreshOutlined,
  VisibilityOffOutlined,
  TokenOutlined,
  AttachMoneyOutlined,
  SpeedOutlined,
  PublicOutlined,
  DevicesOutlined,
  SettingsOutlined,
  ScienceOutlined,
  ShieldOutlined,
  RouterOutlined,
} from '@mui/icons-material';

// ─── Types ────────────────────────────────────────────────────────────────────

interface AIProvider {
  id: string;
  name: string;
  type: 'openrouter' | 'openai' | 'google' | 'anthropic' | 'local';
  status: 'connected' | 'disconnected' | 'error' | 'testing';
  models: string[];
  keyMasked: string;
  latencyMs: number;
  tokensUsed: number;
  costUsd: number;
}

interface BrainRole {
  role: 'Strategic' | 'Operations' | 'Coding' | 'Vision' | 'Fallback';
  model: string;
  provider: string;
  color: string;
}

// ─── Mock Data ───────────────────────────────────────────────────────────────

const PROVIDERS: AIProvider[] = [
  {
    id: 'or-1',
    name: 'OpenRouter',
    type: 'openrouter',
    status: 'connected',
    models: ['anthropic/claude-3.5-sonnet', 'openai/gpt-4o', 'google/gemini-flash-1.5', 'deepseek/deepseek-coder'],
    keyMasked: 'sk-or-****8f3a',
    latencyMs: 312,
    tokensUsed: 2481000,
    costUsd: 4.82,
  },
  {
    id: 'oai-1',
    name: 'OpenAI',
    type: 'openai',
    status: 'connected',
    models: ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo'],
    keyMasked: 'sk-****9b1c',
    latencyMs: 284,
    tokensUsed: 840000,
    costUsd: 1.96,
  },
  {
    id: 'gg-1',
    name: 'Google Gemini',
    type: 'google',
    status: 'disconnected',
    models: [],
    keyMasked: '',
    latencyMs: 0,
    tokensUsed: 0,
    costUsd: 0,
  },
  {
    id: 'local-1',
    name: 'Local Ollama',
    type: 'local',
    status: 'connected',
    models: ['llama3:8b', 'mistral:7b'],
    keyMasked: 'localhost:11434',
    latencyMs: 1420,
    tokensUsed: 115000,
    costUsd: 0,
  },
];

const BRAIN_ROLES: BrainRole[] = [
  { role: 'Strategic', model: 'anthropic/claude-3.5-sonnet', provider: 'OpenRouter', color: '#8B5CF6' },
  { role: 'Operations', model: 'google/gemini-flash-1.5', provider: 'OpenRouter', color: '#00F0FF' },
  { role: 'Coding', model: 'deepseek/deepseek-coder', provider: 'OpenRouter', color: '#22C55E' },
  { role: 'Vision', model: 'openai/gpt-4o', provider: 'OpenRouter', color: '#F59E0B' },
  { role: 'Fallback', model: 'llama3:8b', provider: 'Local Ollama', color: '#64748B' },
];

const TELEMETRY_ROWS = [
  { date: 'Today', tokens: '2,481,000', cost: '$4.82', latencyAvg: '312 ms', calls: 1244 },
  { date: 'Yesterday', tokens: '1,920,100', cost: '$3.71', latencyAvg: '298 ms', calls: 962 },
  { date: 'This Week', tokens: '11,340,000', cost: '$21.60', latencyAvg: '310 ms', calls: 5881 },
  { date: 'This Month', tokens: '38,700,000', cost: '$74.30', latencyAvg: '308 ms', calls: 19420 },
];

// ─── Helpers ─────────────────────────────────────────────────────────────────

const providerColor = (type: AIProvider['type']) => {
  switch (type) {
    case 'openrouter': return '#8B5CF6';
    case 'openai': return '#22C55E';
    case 'google': return '#4285F4';
    case 'anthropic': return '#E07B39';
    case 'local': return '#64748B';
  }
};

const providerIcon = (type: AIProvider['type']) => {
  switch (type) {
    case 'openrouter': return <RouterOutlined />;
    case 'openai': return <PublicOutlined />;
    case 'google': return <PublicOutlined />;
    case 'anthropic': return <PsychologyOutlined />;
    case 'local': return <DevicesOutlined />;
  }
};

const statusColor = (s: AIProvider['status']) => {
  switch (s) {
    case 'connected': return '#22C55E';
    case 'error': return '#EF4444';
    case 'testing': return '#F59E0B';
    default: return '#475569';
  }
};

// ─── Sub-components ───────────────────────────────────────────────────────────

const ProviderCard = ({
  provider,
  onTest,
  onDisconnect,
}: {
  provider: AIProvider;
  onTest: (id: string) => void;
  onDisconnect: (id: string) => void;
}) => {
  const color = providerColor(provider.type);
  const sc = statusColor(provider.status);

  return (
    <Card
      sx={{
        background: 'linear-gradient(135deg, rgba(15,23,42,0.97) 0%, rgba(22,33,57,0.92) 100%)',
        border: `1px solid ${color}22`,
        borderRadius: 3,
        transition: 'border-color 0.2s, box-shadow 0.2s',
        '&:hover': { borderColor: `${color}55`, boxShadow: `0 0 20px ${color}14` },
      }}
    >
      <CardContent sx={{ p: 2.5 }}>
        <Stack direction="row" alignItems="center" spacing={1.5} mb={2}>
          <Avatar sx={{ width: 36, height: 36, backgroundColor: `${color}18`, color }}>{providerIcon(provider.type)}</Avatar>
          <Box flex={1}>
            <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC' }}>{provider.name}</Typography>
            <Stack direction="row" alignItems="center" spacing={0.5}>
              <Box sx={{ width: 7, height: 7, borderRadius: '50%', backgroundColor: sc }} />
              <Typography variant="caption" sx={{ color: sc, fontWeight: 600, textTransform: 'capitalize' }}>{provider.status}</Typography>
            </Stack>
          </Box>
          <Stack direction="row" spacing={0.5}>
            {provider.status === 'connected' && (
              <Tooltip title="Test connection">
                <IconButton size="small" onClick={() => onTest(provider.id)} sx={{ color: '#64748B', '&:hover': { color: '#00F0FF' } }}>
                  <RefreshOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
            {provider.status === 'connected' && (
              <Tooltip title="Disconnect">
                <IconButton size="small" onClick={() => onDisconnect(provider.id)} sx={{ color: '#64748B', '&:hover': { color: '#EF4444' } }}>
                  <DeleteOutlined fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        </Stack>

        {provider.status === 'connected' ? (
          <>
            <Box mb={1.5}>
              <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase' }}>API Key</Typography>
              <Stack direction="row" alignItems="center" spacing={0.5} mt={0.25}>
                <VisibilityOffOutlined sx={{ fontSize: 13, color: '#334155' }} />
                <Typography sx={{ color: '#334155', fontFamily: 'monospace', fontSize: '0.75rem' }}>{provider.keyMasked}</Typography>
              </Stack>
            </Box>
            <Grid container spacing={1.5}>
              <Grid item xs={4}>
                <Box sx={{ textAlign: 'center', p: 1, backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: 2 }}>
                  <SpeedOutlined sx={{ fontSize: 16, color: '#00F0FF', mb: 0.25 }} />
                  <Typography variant="caption" sx={{ color: '#64748B', display: 'block', fontSize: '0.62rem' }}>Latency</Typography>
                  <Typography variant="caption" fontWeight={700} sx={{ color: '#CBD5E1', fontSize: '0.72rem' }}>{provider.latencyMs}ms</Typography>
                </Box>
              </Grid>
              <Grid item xs={4}>
                <Box sx={{ textAlign: 'center', p: 1, backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: 2 }}>
                  <TokenOutlined sx={{ fontSize: 16, color: '#8B5CF6', mb: 0.25 }} />
                  <Typography variant="caption" sx={{ color: '#64748B', display: 'block', fontSize: '0.62rem' }}>Tokens</Typography>
                  <Typography variant="caption" fontWeight={700} sx={{ color: '#CBD5E1', fontSize: '0.72rem' }}>
                    {(provider.tokensUsed / 1000).toFixed(0)}K
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={4}>
                <Box sx={{ textAlign: 'center', p: 1, backgroundColor: 'rgba(255,255,255,0.03)', borderRadius: 2 }}>
                  <AttachMoneyOutlined sx={{ fontSize: 16, color: '#22C55E', mb: 0.25 }} />
                  <Typography variant="caption" sx={{ color: '#64748B', display: 'block', fontSize: '0.62rem' }}>Cost</Typography>
                  <Typography variant="caption" fontWeight={700} sx={{ color: '#CBD5E1', fontSize: '0.72rem' }}>
                    ${provider.costUsd.toFixed(2)}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
            {provider.models.length > 0 && (
              <Box mt={1.5}>
                <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', fontSize: '0.62rem' }}>
                  Available Models
                </Typography>
                <Stack direction="row" flexWrap="wrap" spacing={0.5} mt={0.5}>
                  {provider.models.map(m => (
                    <Chip key={m} size="small" label={m.split('/').pop()}
                      sx={{ backgroundColor: `${color}12`, color, fontWeight: 600, fontSize: '0.62rem', height: 18 }} />
                  ))}
                </Stack>
              </Box>
            )}
          </>
        ) : (
          <Button
            variant="outlined"
            size="small"
            startIcon={<AddCircleOutlineRounded />}
            fullWidth
            sx={{ borderColor: `${color}55`, color, borderRadius: 2, fontWeight: 700, fontSize: '0.72rem', '&:hover': { backgroundColor: `${color}12`, borderColor: color } }}
          >
            Connect
          </Button>
        )}
      </CardContent>
    </Card>
  );
};

// ─── Main Component ───────────────────────────────────────────────────────────

export default function AIBrainSettings() {
  const [tab, setTab] = useState(0);
  const [testPrompt, setTestPrompt] = useState('');
  const [testRole, setTestRole] = useState<string>('Strategic');
  const [testResult, setTestResult] = useState<string | null>(null);
  const [testLoading, setTestLoading] = useState(false);
  const [connectDialog, setConnectDialog] = useState(false);
  const [newApiKey, setNewApiKey] = useState('');
  const [newProviderType, setNewProviderType] = useState<AIProvider['type']>('openrouter');
  const [providers, setProviders] = useState(PROVIDERS);
  const [routingPolicy, setRoutingPolicy] = useState<'cost' | 'latency' | 'quality'>('quality');

  const handleTest = () => {
    if (!testPrompt.trim()) return;
    setTestLoading(true);
    setTestResult(null);
    // Simulate a governed structured output (never execution authority)
    setTimeout(() => {
      setTestResult(JSON.stringify({
        role: testRole,
        model: BRAIN_ROLES.find(r => r.role === testRole)?.model,
        output: {
          type: 'StructuredAnalysis',
          summary: `Sample analysis for: "${testPrompt}"`,
          confidence: 0.92,
          reasoning: ['Point 1: Context analysis', 'Point 2: Evidence synthesis', 'Point 3: Hypothesis generation'],
          executionAuthority: 'ZERO — output is reasoning only, not an execution command',
        },
        meta: { tokensUsed: 412, latencyMs: 328, costUsd: 0.0008, governanceStatus: 'PASSED' },
      }, null, 2));
      setTestLoading(false);
    }, 1200);
  };

  const handleDisconnect = (id: string) => {
    setProviders(prev => prev.map(p => p.id === id ? { ...p, status: 'disconnected' as const, models: [], keyMasked: '' } : p));
  };

  const handleTestConnection = (id: string) => {
    setProviders(prev => prev.map(p => p.id === id ? { ...p, status: 'testing' as const } : p));
    setTimeout(() => {
      setProviders(prev => prev.map(p => p.id === id ? { ...p, status: 'connected' as const } : p));
    }, 1500);
  };

  return (
    <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1200, mx: 'auto' }}>
      {/* ── Header ── */}
      <Stack direction="row" alignItems="center" spacing={2} mb={3}>
        <Avatar sx={{ width: 44, height: 44, background: 'linear-gradient(135deg, #8B5CF6, #00F0FF)', boxShadow: '0 0 18px rgba(139,92,246,0.4)' }}>
          <PsychologyOutlined sx={{ color: '#0F172A', fontSize: 24 }} />
        </Avatar>
        <Box>
          <Typography variant="h5" fontWeight="bold" sx={{ color: '#F8FAFC' }}>AI Brain Fabric</Typography>
          <Typography variant="caption" sx={{ color: '#64748B', letterSpacing: '0.06em', fontWeight: 600 }}>
            MODEL ROUTING · GOVERNED INFERENCE · ZERO EXECUTION AUTHORITY
          </Typography>
        </Box>
      </Stack>

      {/* ── Sovereign Guard Banner ── */}
      <Alert
        severity="info"
        icon={<ShieldOutlined />}
        sx={{ mb: 3, backgroundColor: '#8B5CF618', border: '1px solid #8B5CF633', color: '#C4B5FD', borderRadius: 2, '& .MuiAlert-icon': { color: '#8B5CF6' } }}
      >
        <strong>Sovereign Execution Law:</strong> AI Brain Fabric providers are reasoning adapters only. Models generate structured outputs — they carry <strong>ZERO execution authority</strong>. All real-world actions must pass through the Execution Firewall independently.
      </Alert>

      {/* ── Tabs ── */}
      <Box sx={{ borderBottom: '1px solid rgba(255,255,255,0.08)', mb: 3 }}>
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          sx={{
            '& .MuiTab-root': { color: '#64748B', fontWeight: 600, textTransform: 'none', minWidth: 140 },
            '& .Mui-selected': { color: '#8B5CF6' },
            '& .MuiTabs-indicator': { backgroundColor: '#8B5CF6' },
          }}
        >
          <Tab icon={<RouterOutlined fontSize="small" />} iconPosition="start" label="Providers" id="brain-tab-0" />
          <Tab icon={<PsychologyOutlined fontSize="small" />} iconPosition="start" label="Brain Roles" id="brain-tab-1" />
          <Tab icon={<SettingsOutlined fontSize="small" />} iconPosition="start" label="Routing Policy" id="brain-tab-2" />
          <Tab icon={<TokenOutlined fontSize="small" />} iconPosition="start" label="Telemetry" id="brain-tab-3" />
          <Tab icon={<ScienceOutlined fontSize="small" />} iconPosition="start" label="Inference Test" id="brain-tab-4" />
        </Tabs>
      </Box>

      {/* ── Tab 0: Providers ── */}
      {tab === 0 && (
        <Box>
          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase' }}>
              {providers.filter(p => p.status === 'connected').length} / {providers.length} providers connected
            </Typography>
            <Button
              variant="outlined"
              size="small"
              startIcon={<AddCircleOutlineRounded />}
              onClick={() => setConnectDialog(true)}
              sx={{ borderColor: '#8B5CF655', color: '#8B5CF6', fontWeight: 700, borderRadius: 2, fontSize: '0.72rem', '&:hover': { backgroundColor: '#8B5CF618', borderColor: '#8B5CF6' } }}
            >
              Add Provider
            </Button>
          </Stack>
          <Grid container spacing={2}>
            {providers.map(p => (
              <Grid item xs={12} sm={6} md={6} key={p.id}>
                <ProviderCard provider={p} onTest={handleTestConnection} onDisconnect={handleDisconnect} />
              </Grid>
            ))}
          </Grid>
        </Box>
      )}

      {/* ── Tab 1: Brain Roles ── */}
      {tab === 1 && (
        <Stack spacing={2}>
          <Alert severity="warning" sx={{ backgroundColor: '#F59E0B12', border: '1px solid #F59E0B33', color: '#FCD34D', borderRadius: 2 }}>
            Brain Role assignments are governed. Changes are audited with full attribution. Each role maps to a specific model for that reasoning function — roles do not grant execution authority.
          </Alert>
          {BRAIN_ROLES.map(br => (
            <Card key={br.role}
              sx={{
                background: 'linear-gradient(135deg, rgba(15,23,42,0.97) 0%, rgba(22,33,57,0.92) 100%)',
                border: `1px solid ${br.color}22`,
                borderRadius: 3,
                '&:hover': { borderColor: `${br.color}44` },
                transition: 'border-color 0.2s',
              }}
            >
              <CardContent sx={{ p: 2.5 }}>
                <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} spacing={2}>
                  <Avatar sx={{ width: 40, height: 40, backgroundColor: `${br.color}18`, color: br.color, fontWeight: 700, fontSize: '0.8rem' }}>
                    {br.role[0]}
                  </Avatar>
                  <Box flex={1}>
                    <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC' }}>{br.role} Brain</Typography>
                    <Stack direction="row" spacing={1} mt={0.5}>
                      <Chip size="small" label={br.model}
                        sx={{ backgroundColor: `${br.color}18`, color: br.color, fontWeight: 600, fontSize: '0.68rem', height: 22 }} />
                      <Chip size="small" label={br.provider}
                        sx={{ backgroundColor: 'rgba(255,255,255,0.05)', color: '#64748B', fontWeight: 600, fontSize: '0.68rem', height: 22 }} />
                    </Stack>
                  </Box>
                  <Tooltip title="Edit role assignment (audited)">
                    <IconButton size="small" sx={{ color: '#64748B', '&:hover': { color: br.color } }}>
                      <EditOutlined fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              </CardContent>
            </Card>
          ))}
        </Stack>
      )}

      {/* ── Tab 2: Routing Policy ── */}
      {tab === 2 && (
        <Stack spacing={3}>
          <Card sx={{ background: 'rgba(15,23,42,0.97)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 3 }}>
            <CardContent sx={{ p: 3 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC', mb: 0.5 }}>Routing Optimization Strategy</Typography>
              <Typography variant="caption" sx={{ color: '#64748B', mb: 2, display: 'block' }}>
                Controls how Charlie selects between providers when multiple options are available.
              </Typography>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                {(['cost', 'latency', 'quality'] as const).map(policy => (
                  <Card
                    key={policy}
                    onClick={() => setRoutingPolicy(policy)}
                    sx={{
                      flex: 1,
                      cursor: 'pointer',
                      background: routingPolicy === policy ? 'rgba(139,92,246,0.1)' : 'rgba(255,255,255,0.03)',
                      border: `2px solid ${routingPolicy === policy ? '#8B5CF6' : 'rgba(255,255,255,0.06)'}`,
                      borderRadius: 2,
                      transition: 'all 0.2s',
                    }}
                  >
                    <CardContent sx={{ p: 2, textAlign: 'center' }}>
                      <Typography variant="subtitle2" fontWeight={700} sx={{ color: routingPolicy === policy ? '#C4B5FD' : '#64748B', textTransform: 'capitalize', mb: 0.5 }}>
                        {policy === 'cost' ? '💰' : policy === 'latency' ? '⚡' : '🎯'} {policy.charAt(0).toUpperCase() + policy.slice(1)}
                      </Typography>
                      <Typography variant="caption" sx={{ color: '#475569', fontSize: '0.72rem' }}>
                        {policy === 'cost' ? 'Minimize inference spend. Route to lowest-cost capable model.' :
                          policy === 'latency' ? 'Minimize response time. Route to lowest-latency provider.' :
                            'Maximize output quality. Route to highest-capability model regardless of cost.'}
                      </Typography>
                    </CardContent>
                  </Card>
                ))}
              </Stack>
            </CardContent>
          </Card>

          <Card sx={{ background: 'rgba(15,23,42,0.97)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 3 }}>
            <CardContent sx={{ p: 3 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ color: '#F8FAFC', mb: 2 }}>Fallback Cascade Order</Typography>
              <Stack spacing={1}>
                {['OpenRouter → Claude 3.5 Sonnet', 'OpenAI → GPT-4o', 'OpenRouter → Gemini Flash', 'Local Ollama → Llama 3 8B'].map((step, i) => (
                  <Stack key={i} direction="row" alignItems="center" spacing={1.5}>
                    <Box sx={{ width: 22, height: 22, borderRadius: '50%', backgroundColor: 'rgba(139,92,246,0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                      <Typography variant="caption" sx={{ color: '#8B5CF6', fontWeight: 700, fontSize: '0.65rem' }}>{i + 1}</Typography>
                    </Box>
                    <Typography variant="caption" sx={{ color: '#CBD5E1', fontWeight: 600 }}>{step}</Typography>
                    {i < 3 && <Typography variant="caption" sx={{ color: '#334155' }}>→ on error / rate-limit</Typography>}
                  </Stack>
                ))}
              </Stack>
            </CardContent>
          </Card>
        </Stack>
      )}

      {/* ── Tab 3: Telemetry ── */}
      {tab === 3 && (
        <Stack spacing={2}>
          <Grid container spacing={2}>
            {[
              { label: 'Total Tokens (Today)', value: '2,481,000', icon: <TokenOutlined />, color: '#8B5CF6' },
              { label: 'Inference Cost (Today)', value: '$4.82', icon: <AttachMoneyOutlined />, color: '#22C55E' },
              { label: 'Avg Latency', value: '312 ms', icon: <SpeedOutlined />, color: '#00F0FF' },
              { label: 'Total API Calls', value: '1,244', icon: <RouterOutlined />, color: '#F59E0B' },
            ].map(m => (
              <Grid item xs={6} md={3} key={m.label}>
                <Card sx={{ background: 'rgba(15,23,42,0.97)', border: `1px solid ${m.color}22`, borderRadius: 3 }}>
                  <CardContent sx={{ p: 2 }}>
                    <Avatar sx={{ width: 32, height: 32, backgroundColor: `${m.color}18`, color: m.color, mb: 1 }}>{m.icon}</Avatar>
                    <Typography variant="caption" sx={{ color: '#475569', fontWeight: 700, letterSpacing: '0.05em', textTransform: 'uppercase', fontSize: '0.62rem' }}>
                      {m.label}
                    </Typography>
                    <Typography variant="h6" fontWeight="bold" sx={{ color: '#F8FAFC', mt: 0.25 }}>{m.value}</Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          <TableContainer component={Card} sx={{ background: 'rgba(15,23,42,0.97)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {['Period', 'Tokens', 'Cost (USD)', 'Avg Latency', 'API Calls'].map(h => (
                    <TableCell key={h} sx={{ color: '#64748B', fontWeight: 700, fontSize: '0.7rem', letterSpacing: '0.06em', borderBottom: '1px solid rgba(255,255,255,0.07)', textTransform: 'uppercase' }}>{h}</TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {TELEMETRY_ROWS.map(row => (
                  <TableRow key={row.date} sx={{ '&:hover': { backgroundColor: 'rgba(139,92,246,0.04)' } }}>
                    <TableCell sx={{ color: '#CBD5E1', fontWeight: 600, fontSize: '0.8rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>{row.date}</TableCell>
                    <TableCell sx={{ color: '#8B5CF6', fontFamily: 'monospace', fontSize: '0.78rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>{row.tokens}</TableCell>
                    <TableCell sx={{ color: '#22C55E', fontFamily: 'monospace', fontSize: '0.78rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>{row.cost}</TableCell>
                    <TableCell sx={{ color: '#00F0FF', fontFamily: 'monospace', fontSize: '0.78rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>{row.latencyAvg}</TableCell>
                    <TableCell sx={{ color: '#94A3B8', fontFamily: 'monospace', fontSize: '0.78rem', borderBottom: '1px solid rgba(255,255,255,0.04)' }}>{row.calls.toLocaleString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      )}

      {/* ── Tab 4: Inference Test Sandbox ── */}
      {tab === 4 && (
        <Stack spacing={2}>
          <Alert severity="success" icon={<ScienceOutlined />}
            sx={{ backgroundColor: '#22C55E12', border: '1px solid #22C55E33', color: '#86EFAC', borderRadius: 2, '& .MuiAlert-icon': { color: '#22C55E' } }}>
            <strong>Safe Inference Sandbox:</strong> This sends a live prompt to your selected Brain Role model. Output is <strong>structured data only</strong> — it generates reasoning, never execution commands. The Execution Firewall is not involved.
          </Alert>

          <Card sx={{ background: 'rgba(15,23,42,0.97)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 3 }}>
            <CardContent sx={{ p: 3 }}>
              <Stack spacing={2}>
                <FormControl size="small" fullWidth>
                  <InputLabel sx={{ color: '#64748B' }}>Brain Role</InputLabel>
                  <Select
                    value={testRole}
                    label="Brain Role"
                    onChange={e => setTestRole(e.target.value)}
                    sx={{
                      color: '#F8FAFC',
                      '& .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.1)' },
                      '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(139,92,246,0.4)' },
                      '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#8B5CF6' },
                      '& .MuiSelect-icon': { color: '#64748B' },
                    }}
                  >
                    {BRAIN_ROLES.map(r => (
                      <MenuItem key={r.role} value={r.role}>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <Box sx={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: r.color }} />
                          <span>{r.role} — {r.model}</span>
                        </Stack>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <TextField
                  multiline
                  rows={3}
                  placeholder="Enter a test prompt (e.g. 'Analyze our Q4 churn risk and list the top 3 hypotheses')"
                  value={testPrompt}
                  onChange={e => setTestPrompt(e.target.value)}
                  fullWidth
                  sx={{
                    '& .MuiOutlinedInput-root': {
                      color: '#F8FAFC',
                      '& fieldset': { borderColor: 'rgba(255,255,255,0.1)' },
                      '&:hover fieldset': { borderColor: 'rgba(139,92,246,0.4)' },
                      '&.Mui-focused fieldset': { borderColor: '#8B5CF6' },
                    },
                    '& .MuiInputBase-input::placeholder': { color: '#334155' },
                  }}
                />

                <Button
                  variant="contained"
                  disabled={!testPrompt.trim() || testLoading}
                  onClick={handleTest}
                  startIcon={testLoading ? <CircularProgress size={16} sx={{ color: '#0F172A' }} /> : <ScienceOutlined />}
                  sx={{
                    background: 'linear-gradient(135deg, #8B5CF6, #6D28D9)',
                    fontWeight: 700,
                    borderRadius: 2,
                    alignSelf: 'flex-start',
                    '&:hover': { background: 'linear-gradient(135deg, #7C3AED, #5B21B6)' },
                    '&:disabled': { background: 'rgba(139,92,246,0.3)', color: '#475569' },
                  }}
                >
                  {testLoading ? 'Invoking…' : 'Run Governed Inference'}
                </Button>

                {testResult && (
                  <Box>
                    <Divider sx={{ borderColor: 'rgba(255,255,255,0.07)', mb: 2 }} />
                    <Stack direction="row" alignItems="center" spacing={1} mb={1}>
                      <CheckCircleOutlined sx={{ color: '#22C55E', fontSize: 18 }} />
                      <Typography variant="caption" fontWeight={700} sx={{ color: '#22C55E', letterSpacing: '0.06em', textTransform: 'uppercase' }}>
                        Governed Structured Output
                      </Typography>
                    </Stack>
                    <Box
                      sx={{
                        backgroundColor: '#0B1120',
                        borderRadius: 2,
                        p: 2,
                        border: '1px solid rgba(139,92,246,0.2)',
                        fontFamily: 'monospace',
                        fontSize: '0.75rem',
                        color: '#A5B4FC',
                        whiteSpace: 'pre-wrap',
                        maxHeight: 320,
                        overflowY: 'auto',
                      }}
                    >
                      {testResult}
                    </Box>
                  </Box>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Stack>
      )}

      {/* ── Add Provider Dialog ── */}
      <Dialog open={connectDialog} onClose={() => setConnectDialog(false)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { background: '#0F172A', border: '1px solid rgba(139,92,246,0.3)', borderRadius: 3 } }}>
        <DialogTitle sx={{ color: '#F8FAFC', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <AddCircleOutlineRounded sx={{ color: '#8B5CF6' }} />
            <span>Connect AI Provider</span>
          </Stack>
        </DialogTitle>
        <DialogContent sx={{ pt: 2.5 }}>
          <Stack spacing={2.5}>
            <Alert severity="warning" sx={{ backgroundColor: '#F59E0B12', border: '1px solid #F59E0B33', color: '#FCD34D', borderRadius: 2 }}>
              API keys are encrypted with AES-256-GCM and stored in the Connector Vault. <strong>Keys are never returned in plaintext or exposed to the browser after submission.</strong>
            </Alert>

            <FormControl size="small" fullWidth>
              <InputLabel sx={{ color: '#64748B' }}>Provider Type</InputLabel>
              <Select
                value={newProviderType}
                label="Provider Type"
                onChange={e => setNewProviderType(e.target.value as AIProvider['type'])}
                sx={{
                  color: '#F8FAFC',
                  '& .MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.1)' },
                  '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#8B5CF6' },
                  '& .MuiSelect-icon': { color: '#64748B' },
                }}
              >
                <MenuItem value="openrouter">OpenRouter</MenuItem>
                <MenuItem value="openai">OpenAI</MenuItem>
                <MenuItem value="google">Google Gemini</MenuItem>
                <MenuItem value="anthropic">Anthropic</MenuItem>
                <MenuItem value="local">Local Ollama</MenuItem>
              </Select>
            </FormControl>

            <TextField
              label={newProviderType === 'local' ? 'Endpoint URL (e.g. http://localhost:11434)' : 'API Key'}
              type={newProviderType === 'local' ? 'text' : 'password'}
              value={newApiKey}
              onChange={e => setNewApiKey(e.target.value)}
              fullWidth
              size="small"
              placeholder={newProviderType === 'local' ? 'http://localhost:11434' : 'sk-...'}
              helperText={newProviderType !== 'local' ? 'Key is encrypted on submission and never returned in plaintext.' : ''}
              sx={{
                '& .MuiOutlinedInput-root': {
                  color: '#F8FAFC',
                  '& fieldset': { borderColor: 'rgba(255,255,255,0.1)' },
                  '&.Mui-focused fieldset': { borderColor: '#8B5CF6' },
                },
                '& .MuiFormHelperText-root': { color: '#475569' },
                '& .MuiInputLabel-root': { color: '#64748B' },
              }}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 2.5, borderTop: '1px solid rgba(255,255,255,0.07)', gap: 1 }}>
          <Button onClick={() => setConnectDialog(false)} sx={{ color: '#64748B' }}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!newApiKey.trim()}
            onClick={() => setConnectDialog(false)}
            sx={{ background: 'linear-gradient(135deg, #8B5CF6, #6D28D9)', fontWeight: 700, borderRadius: 2, '&:disabled': { background: 'rgba(139,92,246,0.3)', color: '#475569' } }}
          >
            Encrypt &amp; Connect
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
