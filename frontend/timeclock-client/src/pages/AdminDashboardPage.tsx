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
  Divider,
  IconButton,
  Skeleton,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  List,
  ListItem,
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Tooltip,
} from '@mui/material';
import {
  AccessTime,
  Logout,
  Notifications,
  StopCircle,
  AdminPanelSettings,
  Warning,
} from '@mui/icons-material';
import { getLiveStatus, getAlerts, closeShift } from '../api/admin';
import type { StoredUser, AttendanceLog, AlertSeverity, ManualShiftClose } from '../types';

// ── Constants ─────────────────────────────────────────────────────────────────

const ORPHAN_HOURS = 12;

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

/** Convert a JS Date to a local datetime-local string (yyyy-MM-ddTHH:mm) */
function toDatetimeLocalString(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function isOrphanShift(officialTimestamp: string): boolean {
  return (Date.now() - new Date(officialTimestamp).getTime()) > ORPHAN_HOURS * 60 * 60 * 1000;
}

const SEVERITY_COLOR: Record<AlertSeverity, 'error' | 'warning' | 'info'> = {
  Critical: 'error',
  Warning:  'warning',
  Info:     'info',
};

const SEVERITY_BGCOLOR: Record<AlertSeverity, string> = {
  Critical: '#FFF0F0',
  Warning:  '#FFFBF0',
  Info:     '#F0F7FF',
};

function getApiErrorMessage(error: unknown): string {
  if (error && typeof error === 'object' && 'response' in error) {
    const r = (error as { response?: { data?: { message?: string } } }).response;
    return r?.data?.message ?? 'Operation failed. Please try again.';
  }
  return 'An unexpected error occurred. Please try again.';
}

// ── Force-Close Modal ─────────────────────────────────────────────────────────

interface ForceCloseModalProps {
  open:     boolean;
  log:      AttendanceLog | null;
  onClose:  () => void;
  onSubmit: (data: ManualShiftClose) => void;
  loading:  boolean;
}

function ForceCloseModal({ open, log, onClose, onSubmit, loading }: ForceCloseModalProps) {
  const [endTime, setEndTime] = useState('');
  const [reason, setReason]   = useState('');
  const [errors, setErrors]   = useState<{ endTime?: string; reason?: string }>({});

  // Pre-fill end time with current local time whenever the modal opens
  useEffect(() => {
    if (open) {
      setEndTime(toDatetimeLocalString(new Date()));
      setReason('');
      setErrors({});
    }
  }, [open]);

  const validate = () => {
    const e: typeof errors = {};
    if (!endTime) e.endTime = 'End time is required.';
    if (!reason.trim()) e.reason = 'Reason is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = () => {
    if (!validate() || !log) return;
    onSubmit({
      userId:        log.userId,
      manualEndTime: new Date(endTime).toISOString(),
      reason:        reason.trim(),
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <AdminPanelSettings color="error" />
        Force-Close Shift
      </DialogTitle>
      <DialogContent dividers>
        {log && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            You are about to force-close the shift for <strong>{log.fullName}</strong> (opened{' '}
            {formatTimestamp(log.officialTimestamp)}). This action is logged and cannot be undone.
          </Alert>
        )}

        <TextField
          label="End Time"
          type="datetime-local"
          fullWidth
          value={endTime}
          onChange={(e) => setEndTime(e.target.value)}
          error={!!errors.endTime}
          helperText={errors.endTime ?? 'Select the exact time to record as the shift end.'}
          InputLabelProps={{ shrink: true }}
          sx={{ mb: 2 }}
        />

        <TextField
          label="Reason"
          fullWidth
          required
          multiline
          minRows={2}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          error={!!errors.reason}
          helperText={errors.reason ?? 'Explain why this shift is being closed manually.'}
          placeholder="e.g. Employee forgot to clock out before going on leave."
        />
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={loading}>Cancel</Button>
        <Button
          variant="contained"
          color="error"
          onClick={handleSubmit}
          disabled={loading}
          startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <StopCircle />}
        >
          {loading ? 'Closing…' : 'Confirm Force Close'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

// ── Component ─────────────────────────────────────────────────────────────────

function AdminDashboardPage() {
  const navigate    = useNavigate();
  const queryClient = useQueryClient();

  const stored      = localStorage.getItem('user');
  const currentUser = stored ? (JSON.parse(stored) as StoredUser) : null;

  // Single ticker shared by all duration cells
  const [now, setNow]               = useState(() => Date.now());
  const [closeError, setCloseError] = useState<string | null>(null);
  const [modalLog, setModalLog]     = useState<AttendanceLog | null>(null);

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  // ── Queries ───────────────────────────────────────────────────────────────

  const liveQuery = useQuery({
    queryKey:       ['admin', 'live-status'],
    queryFn:        getLiveStatus,
    refetchInterval: 30_000,
  });

  const alertsQuery = useQuery({
    queryKey:       ['admin', 'alerts'],
    queryFn:        getAlerts,
    refetchInterval: 30_000,
  });

  // Bell badge: Critical alerts raised in the last hour
  const criticalCount = alertsQuery.data?.filter((a) => {
    if (a.severity !== 'Critical') return false;
    return (Date.now() - new Date(a.createdAtUtc).getTime()) < 60 * 60 * 1000;
  }).length ?? 0;

  // Count of currently active orphan shifts (> 12h) for the warning chip
  const orphanCount = liveQuery.data?.filter(
    (l) => isOrphanShift(l.officialTimestamp)
  ).length ?? 0;

  // ── Close-shift mutation ──────────────────────────────────────────────────

  const handleCloseSuccess = useCallback(() => {
    setCloseError(null);
    setModalLog(null);
    void queryClient.invalidateQueries({ queryKey: ['admin', 'live-status'] });
    void queryClient.invalidateQueries({ queryKey: ['admin', 'alerts'] });
  }, [queryClient]);

  const closeMutation = useMutation({
    mutationFn: closeShift,
    onSuccess:  handleCloseSuccess,
    onError: (err) => {
      setModalLog(null);
      setCloseError(getApiErrorMessage(err));
    },
  });

  const handleOpenModal = (log: AttendanceLog) => {
    setCloseError(null);
    setModalLog(log);
  };

  const handleModalSubmit = (data: ManualShiftClose) => {
    closeMutation.mutate(data);
  };

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
            TimeClock — Admin Panel
          </Typography>
          {currentUser && (
            <Typography variant="body2" sx={{ mr: 2, opacity: 0.9 }}>
              {currentUser.fullName}
            </Typography>
          )}
          <IconButton color="inherit" sx={{ mr: 1 }} title="System alerts">
            <Badge badgeContent={criticalCount} color="error">
              <Notifications />
            </Badge>
          </IconButton>
          <IconButton color="inherit" onClick={handleLogout} title="Logout">
            <Logout />
          </IconButton>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 4 }}>

        {/* Error alert */}
        {closeError && (
          <Alert severity="error" onClose={() => setCloseError(null)} sx={{ mb: 3 }}>
            {closeError}
          </Alert>
        )}

        {/* Orphan-shift warning banner */}
        {orphanCount > 0 && (
          <Alert
            severity="warning"
            icon={<Warning />}
            sx={{ mb: 3 }}
          >
            <strong>{orphanCount} shift{orphanCount > 1 ? 's have' : ' has'} been open for over {ORPHAN_HOURS} hours.</strong>{' '}
            These are highlighted in red below. Please review and force-close them if necessary.
          </Alert>
        )}

        {/* ── Live Status Table ── */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ flexGrow: 1 }}>
                Currently Active Shifts
              </Typography>
              {liveQuery.data && (
                <Chip
                  label={`${liveQuery.data.length} active`}
                  color={liveQuery.data.length > 0 ? 'success' : 'default'}
                  size="small"
                />
              )}
            </Box>

            {liveQuery.isLoading ? (
              Array.from({ length: 3 }, (_, i) => (
                <Skeleton key={i} variant="rectangular" height={52} sx={{ mb: 1, borderRadius: 1 }} />
              ))
            ) : !liveQuery.data?.length ? (
              <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                No employees are currently clocked in.
              </Typography>
            ) : (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ bgcolor: 'grey.50' }}>
                      <TableCell sx={{ fontWeight: 700 }}>Name</TableCell>
                      <TableCell sx={{ fontWeight: 700 }}>Email</TableCell>
                      <TableCell sx={{ fontWeight: 700 }}>Clocked In At</TableCell>
                      <TableCell sx={{ fontWeight: 700 }}>Duration</TableCell>
                      <TableCell sx={{ fontWeight: 700 }} align="right">Action</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {liveQuery.data.map((log: AttendanceLog) => {
                      const elapsed  = Math.max(0, Math.floor((now - new Date(log.officialTimestamp).getTime()) / 1000));
                      const isOrphan = isOrphanShift(log.officialTimestamp);
                      return (
                        <TableRow
                          key={log.id}
                          hover
                          sx={isOrphan ? { bgcolor: '#FFF0F0', '&:hover': { bgcolor: '#FFE0E0' } } : {}}
                        >
                          <TableCell sx={{ fontWeight: 500 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              {log.fullName}
                              {isOrphan && (
                                <Tooltip title={`Shift open for over ${ORPHAN_HOURS} hours`}>
                                  <Warning fontSize="small" color="error" />
                                </Tooltip>
                              )}
                            </Box>
                          </TableCell>
                          <TableCell sx={{ color: 'text.secondary' }}>{log.email}</TableCell>
                          <TableCell>{formatTimestamp(log.officialTimestamp)}</TableCell>
                          <TableCell>
                            <Typography
                              fontFamily="monospace"
                              fontSize="0.85rem"
                              color={isOrphan ? 'error.main' : 'success.main'}
                              fontWeight={700}
                            >
                              {formatElapsed(elapsed)}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Button
                              variant="outlined"
                              color="error"
                              size="small"
                              disabled={closeMutation.isPending}
                              startIcon={<StopCircle />}
                              onClick={() => handleOpenModal(log)}
                            >
                              Force Close
                            </Button>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>

        {/* ── System Alerts Card ── */}
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ flexGrow: 1 }}>
                System Alerts
              </Typography>
              {criticalCount > 0 && (
                <Chip
                  label={`${criticalCount} critical in last hour`}
                  color="error"
                  size="small"
                  icon={<Notifications fontSize="small" />}
                />
              )}
            </Box>

            {alertsQuery.isLoading ? (
              Array.from({ length: 4 }, (_, i) => (
                <Skeleton key={i} variant="text" height={52} />
              ))
            ) : !alertsQuery.data?.length ? (
              <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                No alerts recorded.
              </Typography>
            ) : (
              <List disablePadding>
                {alertsQuery.data.map((alert, idx) => (
                  <Box key={alert.id}>
                    {idx > 0 && <Divider />}
                    <ListItem
                      sx={{
                        py: 1.5,
                        bgcolor: SEVERITY_BGCOLOR[alert.severity],
                        borderRadius:
                          idx === 0
                            ? '8px 8px 0 0'
                            : idx === (alertsQuery.data?.length ?? 0) - 1
                            ? '0 0 8px 8px'
                            : 0,
                      }}
                    >
                      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5, width: '100%' }}>
                        <Chip
                          label={alert.severity}
                          color={SEVERITY_COLOR[alert.severity]}
                          size="small"
                          sx={{ mt: 0.25, minWidth: 74, justifyContent: 'center' }}
                        />
                        <Box sx={{ flexGrow: 1 }}>
                          <Typography
                            variant="body2"
                            fontWeight={alert.severity === 'Critical' ? 700 : 400}
                          >
                            {alert.message}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatTimestamp(alert.createdAtUtc)}
                          </Typography>
                        </Box>
                      </Box>
                    </ListItem>
                  </Box>
                ))}
              </List>
            )}
          </CardContent>
        </Card>

      </Container>

      {/* ── Force-Close Modal ── */}
      <ForceCloseModal
        open={modalLog !== null}
        log={modalLog}
        onClose={() => setModalLog(null)}
        onSubmit={handleModalSubmit}
        loading={closeMutation.isPending}
      />

    </Box>
  );
}

export default AdminDashboardPage;
