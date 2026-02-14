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
  rfControlDebug?: RfControlDebugInfo;
  showRfDebug = false;
  rfControlForm: RfControlForm = {
    interfaceName: '',
    frequencyMhz: '',
    channelWidthMhz: '',
    mcsIndex: '',
    powerLevel: ''
  };
  rfInterfaceOptions: RfInterfaceOption[] = [];
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
  rfCurrent: RfCurrentValues = {};
  private isDestroyed = false;
  private isStreaming = false;

  constructor(
    private http: HttpClient) { }

  ngOnInit(): void {
    this.refreshStatus();
    this.startStream();
    this.loadWifiInterfaces();
    this.loadRfCurrentSettings();
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
          const options = (response.cards ?? [])
            .map(card => {
              const iface = (card.interfaceName ?? '').trim();
              if (!iface) {
                return null;
              }
              const cardName = (card.cardName ?? '').trim();
              const detectedType = (card.detectedType ?? '').trim();
              const driverName = (card.driverName ?? '').trim();
              const descriptor = cardName || detectedType || driverName;
              const label = descriptor ? `${descriptor} (${iface})` : iface;
              return { value: iface, label };
            })
            .filter((option): option is RfInterfaceOption => option !== null);
          const unique = new Map<string, RfInterfaceOption>();
          options.forEach(option => {
            if (!unique.has(option.value)) {
              unique.set(option.value, option);
            }
          });
          this.rfInterfaceOptions = Array.from(unique.values()).sort((a, b) => a.label.localeCompare(b.label));
        },
        error: () => {
          this.rfInterfaceOptions = [];
        }
      });
  }

  private loadRfCurrentSettings(): void {
    this.http.get<SettingFileSummary[]>('/api/settings')
      .subscribe({
        next: summaries => {
          const target = summaries.find(item =>
            item.name === 'wifibroadcast_settings.json' &&
            (item.relativePath?.replace(/\\/g, '/').includes('interface') ?? false));
          if (!target) {
            return;
          }
          this.http.get<SettingFileDetail>(`/api/settings/${target.id}`)
            .subscribe({
              next: file => {
                if (!file?.content) {
                  return;
                }
                try {
                  const parsed = JSON.parse(file.content) as Record<string, unknown>;
                  this.applyRfCurrentSettings(parsed);
                } catch {
                  // Ignore invalid JSON.
                }
              }
            });
        }
      });
  }

  private applyRfCurrentSettings(parsed: Record<string, unknown>): void {
    const frequency = this.parseUnknownInt(parsed['wb_frequency']);
    if (frequency !== null) {
      this.rfCurrent.frequencyMhz = frequency;
    }
    const channelWidth = this.parseUnknownInt(parsed['wb_air_tx_channel_width']);
    if (channelWidth !== null) {
      this.rfCurrent.channelWidthMhz = channelWidth;
    }
    const mcsIndex = this.parseUnknownInt(parsed['wb_air_mcs_index']);
    if (mcsIndex !== null) {
      this.rfCurrent.mcsIndex = mcsIndex;
    }
    const powerLevelRaw = this.parseUnknownInt(parsed['wb_tx_power_level']);
    if (powerLevelRaw !== null) {
      this.rfCurrent.powerLevel = this.mapPowerLevel(powerLevelRaw);
    }
  }

  get rfCurrentInterfaceLabel(): string {
    const iface = (this.rfCurrent.interfaceName ?? '').trim();
    if (!iface) {
      return 'Auto';
    }
    const match = this.rfInterfaceOptions.find(option => option.value === iface);
    return match?.label ?? iface;
  }

  get rfCurrentChannelLabel(): string {
    if (this.rfCurrent.frequencyMhz) {
      return `${this.rfCurrent.frequencyMhz} MHz`;
    }
    return 'Unknown';
  }

  get rfCurrentBandwidthLabel(): string {
    if (this.rfCurrent.channelWidthMhz) {
      return `${this.rfCurrent.channelWidthMhz} MHz`;
    }
    return 'Unknown';
  }

  get rfCurrentMcsLabel(): string {
    if (this.rfCurrent.mcsIndex !== undefined && this.rfCurrent.mcsIndex !== null) {
      return `${this.rfCurrent.mcsIndex}`;
    }
    return 'Unknown';
  }

  get rfCurrentPowerLabel(): string {
    const level = (this.rfCurrent.powerLevel ?? '').toLowerCase();
    if (!level) {
      return 'Unknown';
    }
    if (level === 'disabled' || level === 'auto') {
      return 'Disabled';
    }
    return `${level.charAt(0).toUpperCase()}${level.slice(1)}`;
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
    this.rfControlDebug = undefined;
    this.http.post<RfControlResponse>('/api/status/rf-control', payload)
      .subscribe({
        next: response => {
          this.rfControlSaving = false;
          this.rfControlDebug = response.debug;
          this.showRfDebug = !response.ok;
          if (response.ok) {
            this.rfControlSuccess = response.message || 'RF settings applied.';
            if (payload.interfaceName) {
              this.rfCurrent.interfaceName = payload.interfaceName;
            }
            if (payload.frequencyMhz !== undefined) {
              this.rfCurrent.frequencyMhz = payload.frequencyMhz;
            }
            if (payload.channelWidthMhz !== undefined) {
              this.rfCurrent.channelWidthMhz = payload.channelWidthMhz;
            }
            if (payload.mcsIndex !== undefined) {
              this.rfCurrent.mcsIndex = payload.mcsIndex;
            }
            if (payload.powerLevel) {
              this.rfCurrent.powerLevel = payload.powerLevel.toLowerCase();
            }
          } else {
            this.rfControlError = response.message || 'Unable to apply RF settings.';
          }
        },
        error: () => {
          this.rfControlSaving = false;
          this.rfControlError = 'Unable to reach the RF control endpoint.';
          this.rfControlDebug = undefined;
          this.showRfDebug = true;
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

  private parseUnknownInt(value: unknown): number | null {
    if (typeof value === 'number' && Number.isFinite(value)) {
      return Math.trunc(value);
    }
    if (typeof value === 'string') {
      const trimmed = value.trim();
      if (!trimmed) {
        return null;
      }
      const parsed = Number.parseInt(trimmed, 10);
      return Number.isFinite(parsed) ? parsed : null;
    }
    return null;
  }

  private mapPowerLevel(value: number): string {
    switch (value) {
      case 0:
        return 'lowest';
      case 1:
        return 'low';
      case 2:
        return 'mid';
      case 3:
        return 'high';
      case -1:
        return 'disabled';
      default:
        return '';
    }
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
  debug?: RfControlDebugInfo;
}

interface RfControlDebugInfo {
  requestPayload?: string;
  responsePayload?: string;
  attempts?: number;
  elapsedMs?: number;
  socketAvailable?: boolean;
}

interface WifiCardInfoDto {
  interfaceName: string;
  cardName?: string;
  detectedType?: string;
  driverName?: string;
}

interface WifiInfoDto {
  cards: WifiCardInfoDto[];
}

interface SettingFileSummary {
  id: string;
  name: string;
  relativePath?: string;
}

interface SettingFileDetail {
  id: string;
  name: string;
  relativePath?: string;
  content: string;
}

interface RfInterfaceOption {
  value: string;
  label: string;
}

interface RfCurrentValues {
  interfaceName?: string;
  frequencyMhz?: number;
  channelWidthMhz?: number;
  mcsIndex?: number;
  powerLevel?: string;
}
