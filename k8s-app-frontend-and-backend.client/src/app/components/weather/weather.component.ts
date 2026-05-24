import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeatherService } from '../../services/weather.service';
import { WeatherForecast } from '../../models/weather-forecast';

@Component({
  selector: 'app-weather',
standalone: false,
  templateUrl: './weather.component.html',
  styleUrl: './weather.component.css'
})
export class WeatherComponent implements OnInit {
  weatherForecasts: WeatherForecast[] = [];
  isLoading = false;
  error: string | null = null;

  constructor(private weatherService: WeatherService) { }

  ngOnInit(): void {
    this.loadWeather();
  }

  loadWeather(): void {
    this.isLoading = true;
    this.error = null;
    
    this.weatherService.getWeatherForecast().subscribe({
      next: (data) => {
        this.weatherForecasts = data;
 this.isLoading = false;
  },
      error: (err) => {
        console.error('Error loading weather data:', err);
        this.error = 'Failed to load weather data. Make sure the API is running.';
   this.isLoading = false;
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
  weekday: 'short',
      month: 'short',
    day: 'numeric',
      year: 'numeric'
    });
  }

  getTemperatureClass(tempC: number): string {
    if (tempC <= 0) return 'cold';
    if (tempC <= 10) return 'cool';
    if (tempC <= 20) return 'mild';
    if (tempC <= 30) return 'warm';
    return 'hot';
  }

  getSummaryClass(summary: string | null): string {
    if (!summary) return '';
    return summary.toLowerCase().replace(/\s+/g, '');
  }

  onSubmit(newForecast: WeatherForecast) {
    this.weatherService.saveForecast(newForecast).subscribe({
      next: (response) => console.log('Saved in db', response),
      error: (error) => console.error('Error', error)
    });
  }
}
