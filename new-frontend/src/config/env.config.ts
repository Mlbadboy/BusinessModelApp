// Environment configuration utility

interface EnvConfig {
  api: {
    baseUrl: string;
    errorReportingUrl: string;
  };
  auth: {
    tokenKey: string;
  };
  features: {
    analytics: boolean;
    notifications: boolean;
    errorReporting: boolean;
  };
  app: {
    environment: string;
    name: string;
  };
}

export const config: EnvConfig = {
  api: {
    // A relative URL lets Vite's development proxy and a same-origin production
    // deployment use the API without machine-specific configuration.
    baseUrl: import.meta.env.VITE_API_BASE_URL || '/api',
    errorReportingUrl: import.meta.env.VITE_ERROR_REPORTING_URL || '',
  },
  auth: {
    tokenKey: import.meta.env.VITE_AUTH_TOKEN_KEY || 'auth_token',
  },
  features: {
    analytics: import.meta.env.VITE_ENABLE_ANALYTICS === 'true',
    notifications: import.meta.env.VITE_ENABLE_NOTIFICATIONS === 'true',
    errorReporting: import.meta.env.VITE_ENABLE_ERROR_REPORTING === 'true',
  },
  app: {
    environment: import.meta.env.VITE_APP_ENV || import.meta.env.MODE,
    name: import.meta.env.VITE_APP_NAME || 'Business Model Management',
  },
};

export const isProduction = config.app.environment === 'production';
export const isDevelopment = config.app.environment === 'development';

export default config;
