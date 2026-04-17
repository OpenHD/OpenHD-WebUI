import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-update',
  templateUrl: './update.component.html',
  styleUrls: ['./update.component.css']
})
export class UpdateComponent implements OnInit, OnDestroy {
  info?: SysutilUpdateInfo;
  infoError = '';
  actionError = '';
  actionMessage = '';
  isLoading = true;
  isStartingUpdate = false;
  isUploadingZip = false;
  selectedFile?: File;
  private pollHandle?: number;
  private isDestroyed = false;

  constructor(private readonly http: HttpClient) {}

  ngOnInit(): void {
    this.loadInfo(true);
    this.pollHandle = window.setInterval(() => {
      if (!this.isDestroyed) {
        this.loadInfo(false);
      }
    }, 2500);
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    if (this.pollHandle !== undefined) {
      window.clearInterval(this.pollHandle);
      this.pollHandle = undefined;
    }
  }

  get updateStateLabel(): string {
    return this.info?.isUpdating ? 'Running' : 'Idle';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input?.files;
    this.selectedFile = files && files.length > 0 ? files[0] : undefined;
    this.actionError = '';
    this.actionMessage = '';
  }

  uploadZip(): void {
    if (!this.selectedFile || this.isUploadingZip) {
      return;
    }
    this.isUploadingZip = true;
    this.actionError = '';
    this.actionMessage = '';

    this.http.post('/api/update/upload', this.selectedFile).subscribe({
      next: () => {
        this.isUploadingZip = false;
        this.actionMessage = 'ZIP uploaded. You can start the update now.';
      },
      error: () => {
        this.isUploadingZip = false;
        this.actionError = 'Unable to upload update ZIP.';
      }
    });
  }

  startRegularUpdate(): void {
    if (this.isStartingUpdate || this.info?.isUpdating) {
      return;
    }
    this.isStartingUpdate = true;
    this.actionError = '';
    this.actionMessage = '';

    this.http.post<SysutilUpdateRunResponse>('/api/update/run', {}).subscribe({
      next: response => {
        this.isStartingUpdate = false;
        if (response.accepted) {
          this.actionMessage = response.message || 'Update request accepted.';
          this.loadInfo(false);
          return;
        }
        this.actionError = response.message || 'Update request was rejected.';
      },
      error: err => {
        this.isStartingUpdate = false;
        this.actionError = err?.error?.message ?? 'Unable to start update.';
      }
    });
  }

  refresh(): void {
    this.loadInfo(true);
  }

  private loadInfo(showSpinner: boolean): void {
    if (showSpinner) {
      this.isLoading = true;
    }
    this.http.get<SysutilUpdateInfo>('/api/update/info').subscribe({
      next: response => {
        this.info = response;
        this.isLoading = false;
        this.infoError = response.isAvailable ? '' : (response.message || 'Sysutils update state is unavailable.');
      },
      error: () => {
        this.isLoading = false;
        this.infoError = 'Unable to load update status.';
      }
    });
  }
}

interface SysutilUpdateInfo {
  isAvailable: boolean;
  isUpdating: boolean;
  message: string;
}

interface SysutilUpdateRunResponse {
  accepted: boolean;
  message: string;
}
