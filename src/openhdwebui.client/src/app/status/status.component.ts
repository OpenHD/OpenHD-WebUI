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
  runMode?: RunModeInfo;
  runModeMessage = '';
  runModeError = '';
  isUpdatingRunMode = false;
  showModeMenu = false;
  private isDestroyed = false;
  private isStreaming = false;
  private readonly partitionColors = [
    '#00c2ff',
    '#ff6d00',
    '#7c4dff',
    '#00e676',
    '#ffd600',
    '#ff1744',
    '#00bfa5',
    '#d500f9'
  ];

  constructor(
    private http: HttpClient) { }

  ngOnInit(): void {
    this.refreshStatus();
    this.startStream();
    this.loadPartitions();
    this.loadRunMode();
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

  get runModeLabel(): string {
    if (!this.runMode?.isAvailable) {
      return 'mode unavailable';
    }
    const mode = (this.runMode?.mode ?? 'unknown').toLowerCase();
    if (mode === 'air') {
      return 'Air';
    }
    if (mode === 'ground') {
      return 'Ground';
    }
    if (mode === 'record') {
      return 'Record';
    }
    return mode;
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

  partitionTypeLabel(part: Partial<PartitionEntry>): string {
    const fstype = (part.fstype ?? '').toLowerCase();
    if (!fstype) {
      return 'Unformatted';
    }
    if (fstype === 'vfat' || fstype === 'fat32' || fstype === 'fat') {
      return 'FAT32';
    }
    return fstype.toUpperCase();
  }

  partitionLabel(part: Partial<PartitionEntry>): string {
    return part.label || '';
  }

  isRecordings(part: Partial<PartitionEntry>): boolean {
    return (part.label ?? '').toLowerCase() === 'recordings';
  }

  resizableLabel(): string {
    const resizable = this.partitionReport?.resizable;
    if (!resizable) {
      return 'No resizable FAT32 partition detected.';
    }
    const typeLabel = resizable.fstype
      ? this.partitionTypeLabel({ fstype: resizable.fstype } as PartitionEntry)
      : 'Unformatted';
    const name = resizable.label ? `${resizable.label} (${typeLabel})` : typeLabel;
    return `Resize ${name} to fill ${this.formatBytes(resizable.freeBytes)} free space?`;
  }

  get showResizePrompt(): boolean {
    return !!this.partitionReport?.resizable;
  }

  get recordingsFreeLabel(): string {
    const free = this.partitionReport?.recordings?.freeBytes ?? 0;
    return this.formatBytes(free);
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
    if (!this.partitionReport?.resizable) {
      this.resizeError = 'No resizable partition detected.';
      return;
    }
    const resize = choice === 'yes';
    this.http.post('/api/partitions/resize', { resize })
      .subscribe({
        next: () => {},
        error: () => {
          this.resizeError = 'Unable to send resize request.';
        }
      });
  }

  setRunMode(mode: 'air' | 'ground' | 'record'): void {
    if (this.isUpdatingRunMode) {
      return;
    }
    this.showModeMenu = false;
    this.isUpdatingRunMode = true;
    this.runModeError = '';
    this.runModeMessage = '';
    this.http.post<RunModeUpdateResponse>('/api/air-ground', { mode })
      .subscribe({
        next: response => {
          this.isUpdatingRunMode = false;
          if (response.ok) {
            this.runModeMessage = response.message ?? 'OpenHD restart requested.';
            this.runMode = {
              isAvailable: true,
              mode: response.mode ?? mode
            };
            return;
          }
          this.runModeError = response.message ?? 'Unable to update mode.';
        },
        error: err => {
          this.isUpdatingRunMode = false;
          this.runModeError = err?.error?.message ?? 'Unable to update mode.';
        }
      });
  }

  toggleModeMenu(): void {
    if (this.isUpdatingRunMode) {
      return;
    }
    this.showModeMenu = !this.showModeMenu;
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

  private loadRunMode(): void {
    this.http.get<RunModeInfo>('/api/air-ground')
      .subscribe({
        next: response => {
          this.runMode = response;
          this.runModeError = '';
        },
        error: () => {
          this.runModeError = 'Unable to read air/ground mode.';
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
  recordings?: RecordingInfo | null;
  resizable?: PartitionResizable | null;
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
  label?: string;
  startBytes: number;
  sizeBytes: number;
}

interface PartitionEntry {
  device: string;
  mountpoint?: string;
  fstype?: string;
  label?: string;
  freeBytes?: number;
  startBytes: number;
  sizeBytes: number;
}

interface PartitionResizable {
  device: string;
  label?: string;
  fstype?: string;
  freeBytes: number;
}

interface RecordingInfo {
  freeBytes: number;
  files: string[];
}

interface RunModeInfo {
  isAvailable: boolean;
  mode: string;
}

interface RunModeUpdateResponse {
  ok: boolean;
  message?: string | null;
  mode?: string | null;
}
