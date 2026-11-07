import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = localStorage.getItem('hadm-token');
  const language = localStorage.getItem('hadm-language') ?? 'en';
  if (request.url.includes('/api/auth/')) return next(request);
  const headers: Record<string, string> = { 'Accept-Language': language };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return next(request.clone({ setHeaders: headers }));
};
