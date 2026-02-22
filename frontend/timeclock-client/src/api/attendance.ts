import apiClient from './axiosClient';
import type { AttendanceLog, UserStatus } from '../types';

export async function getStatus(): Promise<UserStatus> {
  const res = await apiClient.get<UserStatus>('/attendance/status');
  return res.data;
}

export async function getHistory(): Promise<AttendanceLog[]> {
  const res = await apiClient.get<AttendanceLog[]>('/attendance/history');
  return res.data;
}

export async function clockIn(): Promise<AttendanceLog> {
  const res = await apiClient.post<AttendanceLog>('/attendance/clock-in');
  return res.data;
}

export async function clockOut(): Promise<AttendanceLog> {
  const res = await apiClient.post<AttendanceLog>('/attendance/clock-out');
  return res.data;
}
