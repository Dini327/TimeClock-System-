import apiClient from './axiosClient';
import type { AttendanceLog, SystemAlert, ManualShiftClose } from '../types';

export async function getLiveStatus(): Promise<AttendanceLog[]> {
  const res = await apiClient.get<AttendanceLog[]>('/admin/live-status');
  return res.data;
}

export async function getAlerts(): Promise<SystemAlert[]> {
  const res = await apiClient.get<SystemAlert[]>('/admin/alerts');
  return res.data;
}

export async function closeShift(data: ManualShiftClose): Promise<AttendanceLog> {
  const res = await apiClient.post<AttendanceLog>('/admin/close-shift', data);
  return res.data;
}
