import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, retry, timeout } from 'rxjs';
import { WeatherForecast } from '../models/weather-forecast';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WeatherService {
  // API URL loaded from environment configuration (can be overridden via ConfigMaps/env vars)
  private apiUrl = `${environment.apiBaseUrl}/weatherforecast`;
  private configUrl = `${environment.apiBaseUrl}/weatherforecast/config`;
  private createUrl = `${environment.apiBaseUrl}/weatherforecast`;

  constructor(private http: HttpClient) {
    console.log(`[WeatherService] Initialized with API URL: ${this.apiUrl}`);
    console.log(`[WeatherService] Timeout: ${environment.apiTimeout}ms, Max Retries: ${environment.maxRetries}`);
  }

  getWeatherForecast(): Observable<WeatherForecast[]> {
    return this.http.get<WeatherForecast[]>(this.apiUrl).pipe(
      timeout(environment.apiTimeout),
    retry({
        count: environment.maxRetries,
        delay: environment.retryDelayMs
      })
    );
  }

  /// <summary>
  /// Get current API configuration (useful for debugging ConfigMap settings)
  /// </summary>
  getApiConfig(): Observable<any> {
    return this.http.get<any>(this.configUrl);
  }

  saveForecast(forecast: WeatherForecast): Observable<WeatherForecast> {
    return this.http.post<WeatherForecast>(this.createUrl, forecast);
  }
}
