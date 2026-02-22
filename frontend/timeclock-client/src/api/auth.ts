import apiClient from './axiosClient';
import type { LoginRequest, LoginResponse } from '../types';

export async function login(data: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/auth/login', data);
  return response.data;
}
