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
    baseUrl: import.meta.env.VITE_API_BASE_URL,
    errorReportingUrl: import.meta.env.VITE_ERROR_REPORTING_URL,
  },
  auth: {
    tokenKey: import.meta.env.VITE_AUTH_TOKEN_KEY,
  },
  features: {
    analytics: import.meta.env.VITE_ENABLE_ANALYTICS === 'true',
    notifications: import.meta.env.VITE_ENABLE_NOTIFICATIONS === 'true',
    errorReporting: import.meta.env.VITE_ENABLE_ERROR_REPORTING === 'true',
  },
  app: {
    environment: import.meta.env.VITE_APP_ENV,
    name: import.meta.env.VITE_APP_NAME,
  },
};

export const isProduction = config.app.environment === 'production';
export const isDevelopment = config.app.environment === 'development';

export default config;