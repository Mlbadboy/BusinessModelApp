import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  TextField,
  MenuItem,
  CircularProgress,
  Alert,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import PhoneIcon from '@mui/icons-material/Phone';
import StarIcon from '@mui/icons-material/Star';
import LanguageIcon from '@mui/icons-material/Language';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import api from '../../utils/api';

interface DiscoveredBusiness {
  id: string;
  name: string;
  category: string;
  city: string;
  address: string;
  websiteUrl: string;
  phone: string;
  googleRating: number;
  reviewCount: number;
  googlePlacesId: string;
}

interface WebsiteAuditReport {
  overallScore: number;
  mobileUXScore: number;
  performanceScore: number;
  seoScore: number;
  hasModernFunnel: boolean;
  hasWhatsAppLeadCapture: boolean;
  detectedTechnologies: string[];
  criticalPainPoints: string[];
  auditSummary: string;
}

interface OpportunityHypothesis {
  id: string;
  businessName: string;
  hypothesisTitle: string;
  problemDescription: string;
  proposedSolution: string;
  commercialPackageName: string;
  estimatedValueINR: number;
  opportunityScore: number;
  evidenceKey: string;
  status: number;
}

interface OpportunityPackage {
  business: DiscoveredBusiness;
  audit: WebsiteAuditReport;
  hypothesis: OpportunityHypothesis;
}

export const OpportunityDiscoveryPage: React.FC = () => {
  const [city, setCity] = useState<string>('Pune');
  const [industry, setIndustry] = useState<string>('Real Estate Developer');
  const [results, setResults] = useState<OpportunityPackage[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [quoteSuccessMsg, setQuoteSuccessMsg] = useState<string | null>(null);

  useEffect(() => {
    handleSearch();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSearch = async () => {
    setLoading(true);
    setQuoteSuccessMsg(null);
    try {
      const res = await api.post('/opportunitydiscovery/search', {
        city,
        industry,
        targetCount: 10
      });
      setResults(res.data.results || []);
    } catch (err) {
      console.error('Opportunity discovery failed', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateQuote = async (pkg: OpportunityPackage) => {
    try {
      await api.post('/commercialtransactions/quotes', {
        opportunityHypothesisId: pkg.hypothesis.id,
        amountINR: pkg.hypothesis.estimatedValueINR,
        title: pkg.hypothesis.hypothesisTitle,
        deliverables: [
          'Headless Next.js Real-Estate Portal',
          'Instant WhatsApp Lead Routing & Chatbot',
          'Automated CRM Opportunity Integration'
        ]
      });
      setQuoteSuccessMsg(`Commercial Proposal Quote created for ${pkg.business.name} (₹${(pkg.hypothesis.estimatedValueINR / 100000).toFixed(2)}L). Sent to executive approval queue!`);
    } catch (err) {
      console.error('Failed to create proposal quote', err);
    }
  };

  return (
    <Box sx={{ p: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
          <SearchIcon sx={{ fontSize: 32, color: 'primary.main' }} />
          <Typography variant="h4" fontWeight={700} sx={{ letterSpacing: '-0.5px' }}>
            Real Business Opportunity Discovery Engine
          </Typography>
        </Box>
        <Typography variant="body1" color="text.secondary">
          Discover real companies via Google Places and public business signals. Charlie audits web performance, mobile UX, and lead conversion to formulate evidence-grounded commercial opportunities.
        </Typography>
      </Box>

      {/* Search Filter Bar */}
      <Card sx={{ mb: 4, p: 2 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={4}>
            <TextField
              select
              fullWidth
              label="Target Geography / City"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              size="small"
            >
              <MenuItem value="Pune">Pune IT & Real Estate Corridor</MenuItem>
              <MenuItem value="Mumbai">Mumbai Metropolitan Region</MenuItem>
              <MenuItem value="Bengaluru">Bengaluru Tech Hub</MenuItem>
              <MenuItem value="Delhi NCR">Delhi NCR Commercial</MenuItem>
            </TextField>
          </Grid>
          <Grid item xs={12} sm={5}>
            <TextField
              select
              fullWidth
              label="Target Industry Vertical"
              value={industry}
              onChange={(e) => setIndustry(e.target.value)}
              size="small"
            >
              <MenuItem value="Real Estate Developer">Real Estate Developers & Builders</MenuItem>
              <MenuItem value="Healthcare & Dental">Healthcare & Dental Clinics</MenuItem>
              <MenuItem value="Education & EdTech">Private Education & Academies</MenuItem>
              <MenuItem value="BFSI FinTech">BFSI & Financial Consultants</MenuItem>
            </TextField>
          </Grid>
          <Grid item xs={12} sm={3}>
            <Button
              fullWidth
              variant="contained"
              startIcon={<SearchIcon />}
              onClick={handleSearch}
              disabled={loading}
              sx={{ height: 40 }}
            >
              {loading ? 'Analyzing...' : 'Discover Opportunities'}
            </Button>
          </Grid>
        </Grid>
      </Card>

      {quoteSuccessMsg && (
        <Alert severity="success" sx={{ mb: 4 }} onClose={() => setQuoteSuccessMsg(null)}>
          {quoteSuccessMsg}
        </Alert>
      )}

      {/* Results */}
      {loading ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', py: 8 }}>
          <CircularProgress sx={{ mb: 2 }} />
          <Typography variant="body2" color="text.secondary">
            Ingesting Google Places data & auditing live website presence...
          </Typography>
        </Box>
      ) : (
        <Grid container spacing={3}>
          {results.map((pkg) => (
            <Grid item xs={12} md={6} key={pkg.business.id}>
              <Card
                sx={{
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  border: '1px solid',
                  borderColor: 'divider',
                  bgcolor: 'background.paper',
                  p: 2
                }}
              >
                <CardContent sx={{ p: 1 }}>
                  {/* Business Meta Header */}
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                    <Box>
                      <Typography variant="h6" fontWeight={700}>
                        {pkg.business.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {pkg.business.category} • {pkg.business.city}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <StarIcon sx={{ color: '#f59e0b', fontSize: 18 }} />
                      <Typography variant="body2" fontWeight={700}>
                        {pkg.business.googleRating}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        ({pkg.business.reviewCount})
                      </Typography>
                    </Box>
                  </Box>

                  <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <LanguageIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                      <Typography variant="caption" color="primary.main">
                        {pkg.business.websiteUrl}
                      </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <PhoneIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                      <Typography variant="caption" color="text.secondary">
                        {pkg.business.phone}
                      </Typography>
                    </Box>
                  </Box>

                  {/* Presence Audit Scorecard */}
                  <Box sx={{ bgcolor: 'rgba(243, 244, 246, 0.05)', p: 1.5, borderRadius: 1, mb: 2, border: '1px solid rgba(255,255,255,0.05)' }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Typography variant="caption" fontWeight={700} sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                        Website Presence Audit
                      </Typography>
                      <Chip
                        label={`Health: ${pkg.audit.overallScore}/100`}
                        size="small"
                        color={pkg.audit.overallScore < 50 ? 'error' : 'warning'}
                      />
                    </Box>

                    <Grid container spacing={1} sx={{ mb: 1 }}>
                      <Grid item xs={4}>
                        <Typography variant="caption" color="text.secondary">Mobile UX</Typography>
                        <Typography variant="body2" fontWeight={700} color="error.main">{pkg.audit.mobileUXScore}/100 (Poor)</Typography>
                      </Grid>
                      <Grid item xs={4}>
                        <Typography variant="caption" color="text.secondary">Speed</Typography>
                        <Typography variant="body2" fontWeight={700} color="warning.main">{pkg.audit.performanceScore}/100</Typography>
                      </Grid>
                      <Grid item xs={4}>
                        <Typography variant="caption" color="text.secondary">SEO</Typography>
                        <Typography variant="body2" fontWeight={700}>{pkg.audit.seoScore}/100</Typography>
                      </Grid>
                    </Grid>

                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                      Critical Conversion Pain Points:
                    </Typography>
                    {pkg.audit.criticalPainPoints.map((point, idx) => (
                      <Typography key={idx} variant="caption" sx={{ display: 'block', color: '#ef4444' }}>
                        • {point}
                      </Typography>
                    ))}
                  </Box>

                  {/* Opportunity Hypothesis Box */}
                  <Box sx={{ bgcolor: 'rgba(16, 185, 129, 0.05)', p: 1.5, borderRadius: 1, border: '1px solid rgba(16, 185, 129, 0.2)' }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                      <Typography variant="subtitle2" fontWeight={700} color="success.main">
                        Opportunity Hypothesis
                      </Typography>
                      <Chip label={pkg.hypothesis.evidenceKey} size="small" variant="outlined" />
                    </Box>

                    <Typography variant="body2" fontWeight={600} sx={{ mb: 0.5 }}>
                      {pkg.hypothesis.hypothesisTitle}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                      Package: <strong>{pkg.hypothesis.commercialPackageName}</strong>
                    </Typography>

                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <Box>
                        <Typography variant="caption" color="text.secondary">Estimated Deal Value</Typography>
                        <Typography variant="h6" fontWeight={700} color="success.main">
                          ₹{(pkg.hypothesis.estimatedValueINR / 100000).toFixed(2)} Lakhs
                        </Typography>
                      </Box>
                      <Chip
                        icon={<CheckCircleIcon />}
                        label={`Fit Score: ${pkg.hypothesis.opportunityScore}%`}
                        color="success"
                        size="small"
                      />
                    </Box>
                  </Box>
                </CardContent>

                <Box sx={{ p: 1, pt: 0 }}>
                  <Button
                    fullWidth
                    variant="contained"
                    startIcon={<RocketLaunchIcon />}
                    onClick={() => handleCreateQuote(pkg)}
                  >
                    Generate Proposal Quote (₹{(pkg.hypothesis.estimatedValueINR / 100000).toFixed(2)}L)
                  </Button>
                </Box>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
};

export default OpportunityDiscoveryPage;
