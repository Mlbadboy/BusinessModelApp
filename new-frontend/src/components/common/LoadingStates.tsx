import {
  Box,
  CircularProgress,
  Skeleton,
  Stack,
  useTheme,
} from '@mui/material';
import type { SxProps } from '@mui/material';

interface LoaderProps {
  size?: number;
  sx?: SxProps;
}

// Full-page loading spinner
export function PageLoader({ size = 40, sx }: LoaderProps) {
  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        zIndex: (theme) => theme.zIndex.modal - 1,
        ...sx,
      }}
    >
      <CircularProgress size={size} />
    </Box>
  );
}

// Content area loader
export function ContentLoader({ size = 40, sx }: LoaderProps) {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: 200,
        width: '100%',
        ...sx,
      }}
    >
      <CircularProgress size={size} />
    </Box>
  );
}

interface CardSkeletonProps {
  count?: number;
  height?: number;
}

// Skeleton loader for cards
export function CardSkeleton({ count = 1, height = 200 }: CardSkeletonProps) {
  const theme = useTheme();
  
  return (
    <Stack spacing={2}>
      {Array.from({ length: count }).map((_, index) => (
        <Skeleton
          key={index}
          variant="rectangular"
          height={height}
          sx={{
            borderRadius: theme.shape.borderRadius,
          }}
        />
      ))}
    </Stack>
  );
}

interface TableSkeletonProps {
  rowCount?: number;
  columnCount?: number;
}

// Skeleton loader for tables
export function TableSkeleton({
  rowCount = 5,
  columnCount = 4,
}: TableSkeletonProps) {
  return (
    <Box sx={{ width: '100%' }}>
      {/* Header */}
      <Box sx={{ display: 'flex', gap: 2, mb: 1 }}>
        {Array.from({ length: columnCount }).map((_, index) => (
          <Skeleton
            key={`header-${index}`}
            variant="rectangular"
            width={`${100 / columnCount}%`}
            height={40}
          />
        ))}
      </Box>

      {/* Rows */}
      {Array.from({ length: rowCount }).map((_, rowIndex) => (
        <Box
          key={`row-${rowIndex}`}
          sx={{
            display: 'flex',
            gap: 2,
            mb: 1,
          }}
        >
          {Array.from({ length: columnCount }).map((_, colIndex) => (
            <Skeleton
              key={`cell-${rowIndex}-${colIndex}`}
              variant="rectangular"
              width={`${100 / columnCount}%`}
              height={32}
            />
          ))}
        </Box>
      ))}
    </Box>
  );
}

interface ChartSkeletonProps {
  height?: number;
}

// Skeleton loader for charts
export function ChartSkeleton({ height = 300 }: ChartSkeletonProps) {
  return (
    <Box sx={{ width: '100%', mt: 2 }}>
      <Skeleton variant="rectangular" width="100%" height={height} />
      <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
        <Skeleton variant="rectangular" width={100} height={24} />
        <Skeleton variant="rectangular" width={100} height={24} />
        <Skeleton variant="rectangular" width={100} height={24} />
      </Box>
    </Box>
  );
}

// Export a default object with all loaders
export const LoadingStates = {
  PageLoader,
  ContentLoader,
  CardSkeleton,
  TableSkeleton,
  ChartSkeleton,
};

export default LoadingStates;