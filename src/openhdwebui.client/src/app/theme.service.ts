import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  isDark = true;

  constructor() {
    document.body.classList.add('dark-theme');
  }

  toggle(): void {
    this.isDark = !this.isDark;
    const body = document.body;
    if (this.isDark) {
      body.classList.add('dark-theme');
    } else {
      body.classList.remove('dark-theme');
    }
  }
}
