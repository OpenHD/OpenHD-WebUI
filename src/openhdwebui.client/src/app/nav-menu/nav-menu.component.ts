import { Component } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { ThemeService } from '../theme.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
  isExpanded = false;
  isAir = false;
  isGround = false;
  hasRecordings = false;
  version = "";

  constructor(
    private http: HttpClient,
    public themeService: ThemeService) {
    this.http.get<IAirGroundStatus>('/api/info/ag-state')
      .subscribe({
        next: obj => {
          this.isAir = obj.isAir;
          this.isGround = obj.isGround;
        },
        error: err => console.error(err)
      });

    this.http.get<IPartitionReport>('/api/partitions')
      .subscribe({
        next: report => {
          this.hasRecordings = (report.recordings?.files?.length ?? 0) > 0;
        },
        error: err => console.error(err)
      });


    const responseType = 'text';

    this.http.get('api/info/web-ui-version', { responseType })
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
}


interface IAirGroundStatus {
  isAir: boolean;
  isGround: boolean;
}

interface IPartitionReport {
  recordings?: {
    files?: string[];
  } | null;
}
