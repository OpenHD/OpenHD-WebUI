import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-status',
  templateUrl: './status.component.html',
  styleUrls: ['./status.component.css']
})
export class StatusComponent implements OnInit, OnDestroy {
  status?: IOpenHdStatus;
  partitionReport?: PartitionReport;
  partitionError = '';
  resizeError = '';
  isLoading = true;
  lastError = '';
  errorHistory: StatusEntry[] = [];
  resizeChoice: 'yes' | 'no' | null = null;
  private isDestroyed = false;
  private isStreaming = false;
  private readonly partitionColors = [
    '#4cc3ff',
    '#3ddc97',
    '#ffc857',
    '#ff7a7a',
    '#8b7bff',
    '#f78c6b'
  ];

  constructor(
    private http: HttpClient) { }

  ngOnInit(): void {
    this.refreshStatus();
    this.startStream();
    this.loadPartitions();
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
  }

  get statusBadge(): string {
    if (this.isLoading) {
      return 'Checking';
    }
    if (!this.status?.isAvailable) {
      return 'Offline';
    }
    if (!this.status.hasData) {
      return 'Starting';
    }
    return this.status.hasError ? 'Issue' : 'Ready';
  }

  get statusTitle(): string {
    if (this.isLoading) {
      return 'Checking OpenHD startup status...';
    }
    if (!this.status?.isAvailable) {
      return 'Sysutils socket is not reachable.';
    }
    if (!this.status.hasData) {
      return 'OpenHD is still starting.';
    }
    return this.status.hasError ? 'OpenHD reported an error.' : 'OpenHD is running cleanly.';
  }

  get statusSummary(): string {
    if (this.status?.description) {
      return this.status.description;
    }
    if (this.status?.message) {
      return this.status.message;
    }
    if (this.status?.state) {
      return this.status.state;
    }
    if (!this.status?.isAvailable) {
      return 'Check that openhd_sys_utils is running and the socket is available.';
    }
    return 'Waiting for the first status update from OpenHD.';
  }

  get isPartitioning(): boolean {
    return (this.status?.state ?? '').toLowerCase() === 'partitioning';
  }

  get resizeChoiceLabel(): string {
    if (this.resizeChoice === 'yes') {
      return 'Resize requested';
    }
    if (this.resizeChoice === 'no') {
      return 'Resize skipped';
    }
    return 'No selection yet';
  }

  formatTimestamp(ms?: number): string {
    if (!ms) {
      return '—';
    }
    return new Date(ms).toLocaleTimeString();
  }

  formatBytes(bytes?: number): string {
    if (!bytes || bytes <= 0) {
      return '0 B';
    }
    const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB'];
    let value = bytes;
    let unitIndex = 0;
    while (value >= 1024 && unitIndex < units.length - 1) {
      value /= 1024;
      unitIndex += 1;
    }
    return `${value.toFixed(value < 10 ? 2 : 1)} ${units[unitIndex]}`;
  }

  segmentPercent(segment: PartitionSegment, disk: PartitionDisk): number {
    if (!disk.sizeBytes || disk.sizeBytes <= 0) {
      return 0;
    }
    return Math.max(0, (segment.sizeBytes / disk.sizeBytes) * 100);
  }

  partitionColor(device: string | undefined, disk: PartitionDisk): string {
    if (!device) {
      return '#93a4b5';
    }
    const index = disk.partitions.findIndex(part => part.device === device);
    if (index < 0) {
      return '#93a4b5';
    }
    return this.partitionColors[index % this.partitionColors.length];
  }

  segmentStyle(segment: PartitionSegment, disk: PartitionDisk): { [key: string]: string } {
    if (segment.kind === 'free') {
      return {};
    }
    return { backgroundColor: this.partitionColor(segment.device, disk) };
  }

  setResizeChoice(choice: 'yes' | 'no'): void {
    this.resizeChoice = choice;
    this.resizeError = '';
    const resize = choice === 'yes';
    this.http.post('/api/partitions/resize', { resize })
      .subscribe({
        next: () => {},
        error: () => {
          this.resizeError = 'Unable to send resize request.';
        }
      });
  }

  private refreshStatus(): void {
    this.http.get<IOpenHdStatus>('/api/status')
      .subscribe({
        next: response => {
          this.applyStatus(response);
          this.isLoading = false;
          this.lastError = '';
        },
        error: () => {
          this.isLoading = false;
          this.lastError = 'Unable to reach the status endpoint. Retrying...';
        }
      });
  }

  private startStream(): void {
    if (this.isStreaming || this.isDestroyed) {
      return;
    }
    this.isStreaming = true;
    const since = this.status?.updatedMs ?? 0;

    this.http.get<IOpenHdStatus>(`/api/status/stream?since=${since}`)
      .subscribe({
        next: response => {
          this.applyStatus(response);
          this.isLoading = false;
          this.lastError = '';
          this.isStreaming = false;
          this.startStream();
        },
        error: () => {
          this.isStreaming = false;
          this.lastError = 'Unable to reach the status endpoint. Retrying...';
          if (!this.isDestroyed) {
            window.setTimeout(() => this.startStream(), 2000);
          }
        }
      });
  }

  private applyStatus(response: IOpenHdStatus): void {
    this.status = response;
    if (response.hasError) {
      const entry: StatusEntry = {
        state: response.state ?? '',
        description: response.description ?? '',
        message: response.message ?? '',
        severity: response.severity,
        updatedMs: response.updatedMs
      };
      this.addErrorEntry(entry);
    }
  }

  private addErrorEntry(entry: StatusEntry): void {
    const key = `${entry.state}|${entry.description}|${entry.message}|${entry.severity}|${entry.updatedMs}`;
    if (this.errorHistory.some(item => item.key === key)) {
      return;
    }
    const withKey = { ...entry, key };
    this.errorHistory = [withKey, ...this.errorHistory].slice(0, 6);
  }

  private loadPartitions(): void {
    this.http.get<PartitionReport>('/api/partitions')
      .subscribe({
        next: response => {
          this.partitionReport = response;
          this.partitionError = '';
        },
        error: () => {
          this.partitionError = 'Unable to read partition layout.';
        }
      });
  }
}

interface IOpenHdStatus {
  isAvailable: boolean;
  hasData: boolean;
  hasError: boolean;
  state?: string;
  description?: string;
  message?: string;
  severity: number;
  updatedMs: number;
}

interface StatusEntry {
  key?: string;
  state: string;
  description: string;
  message: string;
  severity: number;
  updatedMs: number;
}

interface PartitionReport {
  disks: PartitionDisk[];
}

interface PartitionDisk {
  name: string;
  sizeBytes: number;
  segments: PartitionSegment[];
  partitions: PartitionEntry[];
}

interface PartitionSegment {
  kind: string;
  device?: string;
  mountpoint?: string;
  fstype?: string;
  startBytes: number;
  sizeBytes: number;
}

interface PartitionEntry {
  device: string;
  mountpoint?: string;
  fstype?: string;
  startBytes: number;
  sizeBytes: number;
}
