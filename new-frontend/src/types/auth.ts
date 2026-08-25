export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  permissions: Permission[];
}

export interface AuthResponse {
  user: AuthUser;
  token: string;
}

export enum UserRole {
  Admin = 'Admin',
  Manager = 'Manager',
  Analyst = 'Analyst',
  User = 'User'
}

export enum Permission {
  ViewAnalytics = 'ViewAnalytics',
  ManageRisks = 'ManageRisks',
  ManageStrategy = 'ManageStrategy',
  ManageRevenue = 'ManageRevenue',
  ManageExpenses = 'ManageExpenses',
  ManageUsers = 'ManageUsers',
  ExportReports = 'ExportReports'
}

export interface AuthState {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface AuthContextType extends AuthState {
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => void;
  clearError: () => void;
}