import { Component, Inject } from '@angular/core';
import { HttpClient } from "@angular/common/http";

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
  isExpanded = false;
  isAir = false;
  isGround = false;
  version = "";
  isDarkTheme = false;

  constructor(
    http: HttpClient) {
    http.get<IAirGroundStatus>('/api/info/ag-state')
      .subscribe({
        next: obj => {
          this.isAir = obj.isAir;
          this.isGround = obj.isGround;
        },
        error: err => console.error(err)
      });


    const responseType = 'text';

    http.get('api/info/web-ui-version', { responseType })
      .subscribe({
        next: obj => {
          this.version = obj;
        },
        error: err => console.error(err)
      });
  }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }

  toggleTheme() {
    this.isDarkTheme = !this.isDarkTheme;
    if (this.isDarkTheme) {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
  }
}


interface IAirGroundStatus {
  isAir: boolean;
  isGround: boolean;
}
