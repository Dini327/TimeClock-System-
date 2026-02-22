import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import {
  Box,
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Alert,
  CircularProgress,
  InputAdornment,
  IconButton,
} from '@mui/material';
import { Visibility, VisibilityOff, AccessTime } from '@mui/icons-material';
import { login } from '../api/auth';
import type { StoredUser } from '../types';

function LoginPage() {
  const navigate = useNavigate();

  const [email, setEmail]               = useState('');
  const [password, setPassword]         = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [fieldErrors, setFieldErrors]   = useState({ email: '', password: '' });

  // Redirect if already logged in
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      const stored = localStorage.getItem('user');
      const user: StoredUser | null = stored ? (JSON.parse(stored) as StoredUser) : null;
      navigate(user?.role === 'Admin' ? '/admin' : '/dashboard', { replace: true });
    }
  }, [navigate]);

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      localStorage.setItem('token', data.token);
      const user: StoredUser = { userId: data.userId, fullName: data.fullName, role: data.role };
      localStorage.setItem('user', JSON.stringify(user));
      navigate(data.role === 'Admin' ? '/admin' : '/dashboard', { replace: true });
    },
  });

  const validate = (): boolean => {
    const errors = { email: '', password: '' };
    if (!email.trim())  errors.email    = 'Email is required.';
    if (!password)      errors.password = 'Password is required.';
    setFieldErrors(errors);
    return !errors.email && !errors.password;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    mutation.mutate({ email: email.trim(), password });
  };

  const errorMessage = mutation.isError ? getErrorMessage(mutation.error) : null;

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #0D47A1 0%, #1565C0 50%, #1976D2 100%)',
      }}
    >
      <Container maxWidth="xs">
        <Paper elevation={0} sx={{ p: 4, borderRadius: 3 }}>

          {/* Header */}
          <Box sx={{ textAlign: 'center', mb: 3 }}>
            <AccessTime sx={{ fontSize: 52, color: 'primary.main', mb: 1 }} />
            <Typography variant="h5" color="primary.dark" gutterBottom>
              TimeClock Secure Login
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sign in to access your dashboard
            </Typography>
          </Box>

          {/* Error alert */}
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errorMessage}
            </Alert>
          )}

          {/* Form */}
          <Box component="form" onSubmit={handleSubmit} noValidate>
            <TextField
              fullWidth
              size="medium"
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={!!fieldErrors.email}
              helperText={fieldErrors.email}
              autoComplete="email"
              autoFocus
              sx={{ mb: 2 }}
            />

            <TextField
              fullWidth
              size="medium"
              label="Password"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              error={!!fieldErrors.password}
              helperText={fieldErrors.password}
              autoComplete="current-password"
              sx={{ mb: 3 }}
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowPassword((v) => !v)}
                        edge="end"
                        size="small"
                      >
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />

            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              disabled={mutation.isPending}
              startIcon={
                mutation.isPending
                  ? <CircularProgress size={18} color="inherit" />
                  : undefined
              }
            >
              {mutation.isPending ? 'Signing in…' : 'Sign In'}
            </Button>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}

function getErrorMessage(error: unknown): string {
  if (
    error &&
    typeof error === 'object' &&
    'response' in error &&
    error.response &&
    typeof error.response === 'object' &&
    'status' in error.response
  ) {
    const status = (error.response as { status: number }).status;
    if (status === 401) return 'Invalid email or password. Please try again.';
    if (status === 503) return 'Service temporarily unavailable. Please try again later.';
  }
  return 'An unexpected error occurred. Please try again.';
}

export default LoginPage;
