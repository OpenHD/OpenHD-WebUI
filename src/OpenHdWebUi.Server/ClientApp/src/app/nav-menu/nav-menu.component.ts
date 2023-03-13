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

  constructor(
    @Inject("BASE_URL") baseUrl: string,
    http: HttpClient) {
    http.get<IAirGroundStatus>(baseUrl + 'api/info/ag-state')
      .subscribe({
        next: obj => {
          this.isAir = obj.isAir;
          this.isGround = obj.isGround;
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
}


interface IAirGroundStatus {
  isAir: boolean;
  isGround: boolean;
}
