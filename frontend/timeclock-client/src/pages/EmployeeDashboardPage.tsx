import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  Container,
  Card,
  CardContent,
  Button,
  CircularProgress,
  Alert,
  Chip,
  List,
  ListItem,
  ListItemText,
  Divider,
  IconButton,
  Skeleton,
} from '@mui/material';
import { AccessTime, Logout, PlayArrow, Stop } from '@mui/icons-material';
import { getStatus, getHistory, clockIn, clockOut } from '../api/attendance';
import type { StoredUser, AttendanceLog, EventType } from '../types';

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatElapsed(totalSeconds: number): string {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  return [h, m, s].map((v) => String(v).padStart(2, '0')).join(':');
}

function formatTimestamp(iso: string): string {
  return new Date(iso).toLocaleString('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}

const EVENT_LABELS: Record<EventType, string> = {
  ClockIn:     'Clock In',
  ClockOut:    'Clock Out',
  ManualClose: 'Admin Closed',
};

const EVENT_COLORS: Record<EventType, 'success' | 'primary' | 'warning'> = {
  ClockIn:     'success',
  ClockOut:    'primary',
  ManualClose: 'warning',
};

function getApiErrorMessage(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const r = (error as { response?: { status?: number; data?: { message?: string } } }).response;
    if (r?.status === 503)
      return 'The time verification service is unavailable. Clock operations are blocked to protect payroll accuracy. Please try again shortly.';
    if (r?.status === 409)
      return r.data?.message ?? 'Operation not allowed in current state.';
  }
  return 'An unexpected error occurred. Please try again.';
}

// ── Component ─────────────────────────────────────────────────────────────────

function EmployeeDashboardPage() {
  const navigate     = useNavigate();
  const queryClient  = useQueryClient();

  const stored      = localStorage.getItem('user');
  const currentUser = stored ? (JSON.parse(stored) as StoredUser) : null;

  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [clockError, setClockError]         = useState<string | null>(null);

  // ── Queries ───────────────────────────────────────────────────────────────

  const statusQuery = useQuery({
    queryKey: ['attendance', 'status'],
    queryFn:  getStatus,
    refetchInterval: 60_000,
  });

  const historyQuery = useQuery({
    queryKey: ['attendance', 'history'],
    queryFn:  getHistory,
    select:   (data) => data.slice(0, 5),
  });

  // ── Live timer ────────────────────────────────────────────────────────────

  const clockInTimestamp =
    statusQuery.data?.status === 'ClockedIn'
      ? statusQuery.data.lastEvent?.officialTimestamp
      : undefined;

  useEffect(() => {
    if (!clockInTimestamp) {
      setElapsedSeconds(0);
      return;
    }
    const start = new Date(clockInTimestamp).getTime();
    const tick  = () => setElapsedSeconds(Math.max(0, Math.floor((Date.now() - start) / 1000)));
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [clockInTimestamp]);

  // ── Mutations ─────────────────────────────────────────────────────────────

  const invalidate = useCallback(() => {
    setClockError(null);
    void queryClient.invalidateQueries({ queryKey: ['attendance'] });
  }, [queryClient]);

  const clockInMutation  = useMutation({
    mutationFn: clockIn,
    onSuccess:  invalidate,
    onError:    (err) => setClockError(getApiErrorMessage(err)),
  });

  const clockOutMutation = useMutation({
    mutationFn: clockOut,
    onSuccess:  invalidate,
    onError:    (err) => setClockError(getApiErrorMessage(err)),
  });

  const isMutating  = clockInMutation.isPending || clockOutMutation.isPending;
  const isClockedIn = statusQuery.data?.status === 'ClockedIn';

  // ── Logout ────────────────────────────────────────────────────────────────

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/login', { replace: true });
  };

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>

      {/* ── AppBar ── */}
      <AppBar position="static" color="primary" elevation={2}>
        <Toolbar>
          <AccessTime sx={{ mr: 1 }} />
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            TimeClock
          </Typography>
          {currentUser && (
            <Typography variant="body2" sx={{ mr: 2, opacity: 0.9 }}>
              Welcome back, {currentUser.fullName}
            </Typography>
          )}
          <IconButton color="inherit" onClick={handleLogout} title="Logout">
            <Logout />
          </IconButton>
        </Toolbar>
      </AppBar>

      <Container maxWidth="sm" sx={{ py: 4 }}>

        {/* Error alert */}
        {clockError && (
          <Alert severity="error" onClose={() => setClockError(null)} sx={{ mb: 3 }}>
            {clockError}
          </Alert>
        )}

        {/* ── Clock Card ── */}
        <Card sx={{ mb: 3 }}>
          <CardContent sx={{ textAlign: 'center', py: 5 }}>
            {statusQuery.isLoading ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
                <Skeleton variant="rounded" width={110} height={32} />
                <Skeleton variant="rounded" width={200} height={60} />
              </Box>
            ) : (
              <>
                {/* Status chip */}
                <Chip
                  label={isClockedIn ? 'Clocked In' : 'Clocked Out'}
                  color={isClockedIn ? 'success' : 'default'}
                  sx={{ mb: 2, fontWeight: 700, fontSize: '0.85rem', px: 1 }}
                />

                {/* Live timer */}
                {isClockedIn && (
                  <Typography
                    variant="h3"
                    fontFamily="monospace"
                    fontWeight={700}
                    color="success.main"
                    sx={{ mb: 1, letterSpacing: 3 }}
                  >
                    {formatElapsed(elapsedSeconds)}
                  </Typography>
                )}

                {/* Last event info */}
                {statusQuery.data?.lastEvent && (
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                    {isClockedIn ? 'Since' : 'Last event'}:{' '}
                    {formatTimestamp(statusQuery.data.lastEvent.officialTimestamp)}
                    {' · '}
                    {statusQuery.data.lastEvent.timeSource}
                  </Typography>
                )}

                {/* Clock In / Clock Out button */}
                <Button
                  variant="contained"
                  size="large"
                  color={isClockedIn ? 'error' : 'success'}
                  disabled={isMutating}
                  startIcon={
                    isMutating
                      ? <CircularProgress size={20} color="inherit" />
                      : isClockedIn
                      ? <Stop />
                      : <PlayArrow />
                  }
                  onClick={() =>
                    isClockedIn ? clockOutMutation.mutate() : clockInMutation.mutate()
                  }
                  sx={{ minWidth: 200, py: 1.5, fontSize: '1.05rem' }}
                >
                  {isMutating
                    ? (isClockedIn ? 'Clocking Out…' : 'Clocking In…')
                    : (isClockedIn ? 'Clock Out' : 'Clock In')}
                </Button>
              </>
            )}
          </CardContent>
        </Card>

        {/* ── History Card ── */}
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Recent Activity
            </Typography>

            {historyQuery.isLoading ? (
              Array.from({ length: 3 }, (_, i) => (
                <Skeleton key={i} variant="text" height={48} />
              ))
            ) : !historyQuery.data?.length ? (
              <Typography variant="body2" color="text.secondary">
                No activity recorded yet.
              </Typography>
            ) : (
              <List disablePadding>
                {historyQuery.data.map((log: AttendanceLog, idx: number) => (
                  <Box key={log.id}>
                    {idx > 0 && <Divider />}
                    <ListItem disablePadding sx={{ py: 1 }}>
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Chip
                              label={EVENT_LABELS[log.eventType]}
                              color={EVENT_COLORS[log.eventType]}
                              size="small"
                            />
                            <Typography variant="body2" color="text.secondary">
                              {formatTimestamp(log.officialTimestamp)}
                            </Typography>
                          </Box>
                        }
                        secondary={
                          log.isManuallyClosed && log.manualCloseReason
                            ? `${log.timeSource} · Reason: ${log.manualCloseReason}`
                            : log.timeSource
                        }
                      />
                    </ListItem>
                  </Box>
                ))}
              </List>
            )}
          </CardContent>
        </Card>

      </Container>
    </Box>
  );
}

export default EmployeeDashboardPage;
