import { Card, CardContent, Typography, Box } from '@mui/material';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import { MetricValue } from '../../types';

interface FinancialMetricsCardProps {
  title: string;
  value: MetricValue;
  subtitle?: string;
  icon?: React.ReactNode;
}

export function FinancialMetricsCard({
  title,
  value,
  subtitle,
  icon,
}: FinancialMetricsCardProps) {
  const isPositiveTrend = value.trend === 'up';
  const changeColor = isPositiveTrend ? 'success.main' : 'error.main';
  const TrendIcon = isPositiveTrend ? TrendingUpIcon : TrendingDownIcon;

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
          <Typography variant="subtitle2" color="textSecondary">
            {title}
          </Typography>
          {icon && <Box color="primary.main">{icon}</Box>}
        </Box>

        <Typography variant="h4" component="div" gutterBottom>
          {typeof value.value === 'number' 
            ? new Intl.NumberFormat('en-US', {
                style: value.unit === '%' ? 'percent' : 'currency',
                currency: 'USD',
                minimumFractionDigits: 0,
                maximumFractionDigits: 2,
              }).format(value.unit === '%' ? value.value / 100 : value.value)
            : value.value}
        </Typography>

        {(value.change !== undefined || value.trend) && (
          <Box display="flex" alignItems="center" mt={1}>
            <TrendIcon fontSize="small" sx={{ color: changeColor, mr: 0.5 }} />
            <Typography variant="body2" color={changeColor}>
              {value.change !== undefined && (
                <span>
                  {value.change > 0 ? '+' : ''}
                  {value.change.toFixed(1)}%
                </span>
              )}
            </Typography>
          </Box>
        )}

        {subtitle && (
          <Typography variant="caption" color="textSecondary" display="block">
            {subtitle}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}