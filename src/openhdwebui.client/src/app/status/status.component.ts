import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-status',
  templateUrl: './status.component.html',
  styleUrls: ['./status.component.css']
})
export class StatusComponent implements OnInit, OnDestroy {
  status?: IOpenHdStatus;
  isLoading = true;
  lastError = '';
  errorHistory: StatusEntry[] = [];
  rfControlSaving = false;
  rfControlError = '';
  rfControlSuccess = '';
  rfControlForm: RfControlForm = {
    interfaceName: '',
    frequencyMhz: '',
    channelWidthMhz: '',
    mcsIndex: '',
    powerLevel: ''
  };
  rfInterfaceOptions: string[] = [];
  readonly rfChannelOptions: number[] = [
    2312, 2332, 2352, 2372, 2392, 2412, 2432, 2452, 2472, 2484, 2492, 2512,
    2612, 2692, 2712,
    5080, 5100, 5120, 5140, 5160, 5180, 5200, 5220, 5240, 5260, 5280, 5300,
    5320, 5340, 5360, 5380, 5400, 5420, 5440, 5460, 5480, 5500, 5520, 5540,
    5560, 5580, 5600, 5620, 5640, 5660, 5680, 5700, 5720, 5745, 5765, 5785,
    5805, 5825, 5845, 5865, 5885, 5905, 5925, 5945, 5965, 5985, 6005, 6025,
    6045, 6065, 6085
  ];
  readonly rfBandwidthOptions: number[] = [10, 20, 40];
  readonly rfMcsOptions: number[] = Array.from({ length: 10 }, (_, index) => index);
  private isDestroyed = false;
  private isStreaming = false;

  constructor(
    private http: HttpClient) { }

  ngOnInit(): void {
    this.refreshStatus();
    this.startStream();
    this.loadWifiInterfaces();
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

  formatTimestamp(ms?: number): string {
    if (!ms) {
      return '—';
    }
    return new Date(ms).toLocaleTimeString();
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

  private loadWifiInterfaces(): void {
    this.http.get<WifiInfoDto>('/api/hardware/wifi')
      .subscribe({
        next: response => {
          const names = (response.cards ?? [])
            .map(card => card.interfaceName)
            .filter(name => Boolean(name && name.trim()))
            .map(name => name.trim());
          this.rfInterfaceOptions = Array.from(new Set(names)).sort();
        },
        error: () => {
          this.rfInterfaceOptions = [];
        }
      });
  }

  applyRfControl(): void {
    if (this.rfControlSaving) {
      return;
    }

    const payload: RfControlRequest = {};
    const iface = this.rfControlForm.interfaceName.trim();
    if (iface.length > 0) {
      payload.interfaceName = iface;
    }

    const frequency = this.parseOptionalInt(this.rfControlForm.frequencyMhz);
    if (frequency !== null) {
      payload.frequencyMhz = frequency;
    }

    const width = this.parseOptionalInt(this.rfControlForm.channelWidthMhz);
    if (width !== null) {
      payload.channelWidthMhz = width;
    }

    const mcs = this.parseOptionalInt(this.rfControlForm.mcsIndex);
    if (mcs !== null) {
      payload.mcsIndex = mcs;
    }

    const powerLevel = this.rfControlForm.powerLevel.trim();
    if (powerLevel.length > 0) {
      payload.powerLevel = powerLevel;
    }

    if (Object.keys(payload).length === 0) {
      this.rfControlError = 'Enter at least one value to apply.';
      this.rfControlSuccess = '';
      return;
    }

    this.rfControlSaving = true;
    this.rfControlError = '';
    this.rfControlSuccess = '';
    this.http.post<RfControlResponse>('/api/status/rf-control', payload)
      .subscribe({
        next: response => {
          this.rfControlSaving = false;
          if (response.ok) {
            this.rfControlSuccess = response.message || 'RF settings applied.';
          } else {
            this.rfControlError = response.message || 'Unable to apply RF settings.';
          }
        },
        error: () => {
          this.rfControlSaving = false;
          this.rfControlError = 'Unable to reach the RF control endpoint.';
        }
      });
  }

  private parseOptionalInt(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }
    const parsed = Number.parseInt(trimmed, 10);
    return Number.isFinite(parsed) ? parsed : null;
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

interface RfControlForm {
  interfaceName: string;
  frequencyMhz: string;
  channelWidthMhz: string;
  mcsIndex: string;
  powerLevel: string;
}

interface RfControlRequest {
  interfaceName?: string;
  frequencyMhz?: number;
  channelWidthMhz?: number;
  mcsIndex?: number;
  powerLevel?: string;
}

interface RfControlResponse {
  ok: boolean;
  message?: string;
}

interface WifiCardInfoDto {
  interfaceName: string;
}

interface WifiInfoDto {
  cards: WifiCardInfoDto[];
}
