import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-hardware',
  templateUrl: './hardware.component.html',
  styleUrls: ['./hardware.component.css']
})
export class HardwareComponent implements OnInit {
  public platform?: PlatformInfoDto;
  public wifi?: WifiInfoDto;
  public hotspot?: HotspotSettingsDto;
  public wifiProfiles?: WifiCardProfilesDto;
  public hardwareConfig?: HardwareConfigDto;
  private hardwareConfigSnapshot?: HardwareConfigDto;
  public loadingPlatform = false;
  public loadingWifi = false;
  public loadingHotspot = false;
  public loadingWifiProfiles = false;
  public loadingHardwareConfig = false;
  public hardwareConfigError?: string;
  public showRestartPrompt = false;
  public restartCommand?: SystemCommandDto;
  public restartRunning = false;
  public txPowerModalOpen = false;
  public txPowerSaving = false;
  public txPowerError?: string;
  public selectedWifiCard?: WifiCardInfoDto;
  public selectedProfileKey = '';
  public wifiProfileForm: WifiCardProfileForm = {
    vendorId: '',
    deviceId: '',
    name: '',
    powerMode: 'mw',
    lowest: '25',
    low: '100',
    mid: '200',
    high: '500'
  };
  public txPowerForm: WifiTxPowerForm = {
    interfaceName: '',
    powerLevel: ''
  };

  public readonly hotspotModeOptions: HotspotModeOption[] = [
    { value: 0, label: 'Automatic (disable when armed)' },
    { value: 1, label: 'Always off' },
    { value: 2, label: 'Always on' }
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.refreshAll();
    this.loadSystemCommands();
  }

  refreshAll(): void {
    this.loadPlatform();
    this.loadWifi();
    this.loadHotspot();
    this.loadWifiProfiles();
    this.loadHardwareConfig();
  }

  loadPlatform(): void {
    this.loadingPlatform = true;
    this.http.get<PlatformInfoDto>('/api/hardware/platform').subscribe({
      next: result => {
        this.platform = this.normalizePlatform(result as unknown as Record<string, unknown>);
        this.loadingPlatform = false;
        this.loadPlatformFallbackIfNeeded();
      },
      error: error => {
        console.error(error);
        this.platform = undefined;
        this.loadingPlatform = false;
        this.loadPlatformFallbackIfNeeded();
      }
    });
  }

  loadWifi(): void {
    this.loadingWifi = true;
    this.http.get<WifiInfoDto>('/api/hardware/wifi').subscribe({
      next: result => {
        this.wifi = result;
        this.loadingWifi = false;
      },
      error: error => {
        console.error(error);
        this.wifi = undefined;
        this.loadingWifi = false;
      }
    });
  }

  loadHotspot(): void {
    this.loadingHotspot = true;
    this.http.get<HotspotSettingsDto>('/api/hardware/hotspot').subscribe({
      next: result => {
        this.hotspot = result;
        this.loadingHotspot = false;
      },
      error: error => {
        console.error(error);
        this.hotspot = undefined;
        this.loadingHotspot = false;
      }
    });
  }

  loadWifiProfiles(): void {
    this.loadingWifiProfiles = true;
    this.http.get<WifiCardProfilesDto>('/api/hardware/wifi-profiles').subscribe({
      next: result => {
        this.wifiProfiles = result;
        this.loadingWifiProfiles = false;
        if (this.selectedWifiCard) {
          this.selectProfileForCard(this.selectedWifiCard);
        } else {
          this.selectFirstProfile();
        }
      },
      error: error => {
        console.error(error);
        this.wifiProfiles = undefined;
        this.loadingWifiProfiles = false;
      }
    });
  }

  loadHardwareConfig(): void {
    this.loadingHardwareConfig = true;
    this.http.get<HardwareConfigDto>('/api/hardware/config').subscribe({
      next: result => {
        this.hardwareConfig = result;
        this.hardwareConfigSnapshot = this.cloneHardwareConfig(result);
        this.loadingHardwareConfig = false;
        this.showRestartPrompt = false;
      },
      error: error => {
        console.error(error);
        this.hardwareConfig = undefined;
        this.hardwareConfigSnapshot = undefined;
        this.loadingHardwareConfig = false;
      }
    });
  }

  refreshPlatform(): void {
    this.updatePlatform({ action: 'detect' }, () => this.loadPlatform());
  }

  refreshWifi(): void {
    this.updateWifi({ action: 'refresh' });
  }

  refreshHotspot(): void {
    this.updateHotspot({ action: 'refresh' });
  }

  refreshWifiProfiles(): void {
    this.loadWifiProfiles();
  }

  refreshHardwareConfig(): void {
    this.loadHardwareConfig();
  }

  resetHardwareConfigToDefaults(): void {
    if (!this.hardwareConfig) {
      return;
    }
    if (!confirm('Reset hardware configuration to defaults?')) {
      return;
    }
    this.hardwareConfig = this.buildDefaultHardwareConfig();
  }

  saveHotspot(): void {
    if (!this.hotspot) {
      return;
    }
    this.updateHotspot({
      action: 'set',
      hotspotMode: this.hotspot.hotspotMode,
      hotspotSsid: this.hotspot.hotspotSsid ?? '',
      hotspotPassword: this.hotspot.hotspotPassword ?? '',
      hotspotInterfaceOverride: this.hotspot.hotspotInterfaceOverride ?? ''
    });
  }

  saveHardwareConfig(): void {
    if (!this.hardwareConfig) {
      return;
    }
    if (this.hardwareConfigHasErrors()) {
      this.hardwareConfigError = 'Fix validation errors before saving.';
      return;
    }
    const hasChanges = this.hasHardwareConfigChanges();
    this.loadingHardwareConfig = true;
    this.hardwareConfigError = undefined;
    const payload: HardwareConfigUpdateRequest = {
      wifiEnableAutodetect: this.hardwareConfig.wifiEnableAutodetect,
      wifiWbLinkCards: this.hardwareConfig.wifiWbLinkCards,
      wifiHotspotCard: this.hardwareConfig.wifiHotspotCard,
      wifiMonitorCardEmulate: this.hardwareConfig.wifiMonitorCardEmulate,
      wifiForceNoLinkButHotspot: this.hardwareConfig.wifiForceNoLinkButHotspot,
      wifiLocalNetworkEnable: this.hardwareConfig.wifiLocalNetworkEnable,
      wifiLocalNetworkSsid: this.hardwareConfig.wifiLocalNetworkSsid,
      wifiLocalNetworkPassword: this.hardwareConfig.wifiLocalNetworkPassword,
      nwEthernetCard: this.hardwareConfig.nwEthernetCard,
      nwManualForwardingIps: this.hardwareConfig.nwManualForwardingIps,
      nwForwardToLocalhost58xx: this.hardwareConfig.nwForwardToLocalhost58xx,
      genEnableLastKnownPosition: this.hardwareConfig.genEnableLastKnownPosition,
      genRfMetricsLevel: this.hardwareConfig.genRfMetricsLevel,
      groundUnitIp: this.hardwareConfig.groundUnitIp,
      airUnitIp: this.hardwareConfig.airUnitIp,
      videoPort: this.hardwareConfig.videoPort,
      telemetryPort: this.hardwareConfig.telemetryPort,
      disableMicrohardDetection: this.hardwareConfig.disableMicrohardDetection,
      forceMicrohard: this.hardwareConfig.forceMicrohard,
      microhardUsername: this.hardwareConfig.microhardUsername,
      microhardPassword: this.hardwareConfig.microhardPassword,
      microhardIpAir: this.hardwareConfig.microhardIpAir,
      microhardIpGround: this.hardwareConfig.microhardIpGround,
      microhardIpRange: this.hardwareConfig.microhardIpRange,
      microhardVideoPort: this.hardwareConfig.microhardVideoPort,
      microhardTelemetryPort: this.hardwareConfig.microhardTelemetryPort
    };
    this.http.post<HardwareConfigDto>('/api/hardware/config', payload).subscribe({
      next: result => {
        this.hardwareConfig = result;
        this.hardwareConfigSnapshot = this.cloneHardwareConfig(result);
        this.loadingHardwareConfig = false;
        this.showRestartPrompt = hasChanges;
      },
      error: error => {
        console.error(error);
        this.loadingHardwareConfig = false;
        this.hardwareConfigError = 'Failed to save hardware configuration.';
      }
    });
  }

  runRestartCommand(): void {
    if (!this.restartCommand) {
      return;
    }
    this.restartRunning = true;
    this.http.post('/api/system/run-command', { id: this.restartCommand.id }).subscribe({
      next: () => {
        this.restartRunning = false;
      },
      error: error => {
        console.error(error);
        this.restartRunning = false;
      }
    });
  }

  isInvalidIp(value?: string): boolean {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return false;
    }
    return !this.isValidIp(trimmed);
  }

  isInvalidIpList(value?: string): boolean {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return false;
    }
    const parts = trimmed.split(/[,\s;]+/).map(part => part.trim()).filter(Boolean);
    if (parts.length === 0) {
      return false;
    }
    return parts.some(part => !this.isValidIp(part));
  }

  isInvalidIpRange(value?: string): boolean {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return false;
    }
    const parts = trimmed.split('.').filter(Boolean);
    if (parts.length < 1 || parts.length > 3) {
      return !this.isValidIp(trimmed);
    }
    return parts.some(part => {
      const num = Number(part);
      return !Number.isInteger(num) || num < 0 || num > 255;
    });
  }

  isInvalidPort(value?: number): boolean {
    if (value === null || value === undefined) {
      return false;
    }
    if (!Number.isFinite(value)) {
      return true;
    }
    return value <= 0 || value > 65535;
  }

  hardwareConfigHasErrors(): boolean {
    if (!this.hardwareConfig) {
      return false;
    }
    return this.isInvalidIp(this.hardwareConfig.groundUnitIp) ||
      this.isInvalidIp(this.hardwareConfig.airUnitIp) ||
      this.isInvalidIpList(this.hardwareConfig.nwManualForwardingIps) ||
      this.isInvalidIp(this.hardwareConfig.microhardIpAir) ||
      this.isInvalidIp(this.hardwareConfig.microhardIpGround) ||
      this.isInvalidIpRange(this.hardwareConfig.microhardIpRange) ||
      this.isInvalidPort(this.hardwareConfig.videoPort) ||
      this.isInvalidPort(this.hardwareConfig.telemetryPort) ||
      this.isInvalidPort(this.hardwareConfig.microhardVideoPort) ||
      this.isInvalidPort(this.hardwareConfig.microhardTelemetryPort);
  }

  clearHotspotOverrides(): void {
    this.updateHotspot({ action: 'clear' });
  }

  openTxPowerModal(card: WifiCardInfoDto): void {
    const level = (card.powerLevel ?? '').toLowerCase();
    this.selectedWifiCard = card;
    this.txPowerForm = {
      interfaceName: card.interfaceName,
      powerLevel: level === 'auto' ? '' : level
    };
    this.txPowerSaving = false;
    this.txPowerError = undefined;
    if (!this.wifiProfiles) {
      this.loadWifiProfiles();
    } else {
      this.selectProfileForCard(card);
    }
    this.txPowerModalOpen = true;
  }

  closeTxPowerModal(): void {
    this.txPowerModalOpen = false;
    this.txPowerSaving = false;
    this.txPowerError = undefined;
  }

  saveTxPower(): void {
    if (!this.txPowerForm.interfaceName) {
      return;
    }
    this.txPowerSaving = true;
    this.updateWifi(
      {
        action: 'set',
        interface: this.txPowerForm.interfaceName,
        powerLevel: this.txPowerForm.powerLevel ?? ''
      },
      () => {
        this.txPowerSaving = false;
        this.txPowerModalOpen = false;
      },
      () => {
        this.txPowerSaving = false;
        this.txPowerError = 'Failed to save TX power settings.';
      }
    );
  }

  selectProfile(key: string): void {
    this.selectedProfileKey = key;
    const profile = this.findProfileByKey(key);
    if (!profile) {
      return;
    }
    this.wifiProfileForm = {
      vendorId: profile.vendorId,
      deviceId: profile.deviceId,
      name: profile.name,
      powerMode: profile.powerMode ?? 'mw',
      lowest: profile.lowest?.toString() ?? '',
      low: profile.low?.toString() ?? '',
      mid: profile.mid?.toString() ?? '',
      high: profile.high?.toString() ?? ''
    };
  }

  saveWifiProfile(): void {
    if (this.isFixedProfile()) {
      return;
    }
    if (!this.wifiProfileForm.vendorId || !this.wifiProfileForm.deviceId) {
      return;
    }
    this.loadingWifiProfiles = true;
    const lowest = this.parsePowerValue(this.wifiProfileForm.lowest);
    const low = this.parsePowerValue(this.wifiProfileForm.low);
    const mid = this.parsePowerValue(this.wifiProfileForm.mid);
    const high = this.parsePowerValue(this.wifiProfileForm.high);
    const payload: WifiCardProfileUpdateRequest = {
      vendorId: this.wifiProfileForm.vendorId,
      deviceId: this.wifiProfileForm.deviceId,
      name: this.wifiProfileForm.name,
      powerMode: this.wifiProfileForm.powerMode,
      lowest,
      low,
      mid,
      high
    };
    this.http.post<WifiCardProfilesDto>('/api/hardware/wifi-profiles', payload).subscribe({
      next: result => {
        this.wifiProfiles = result;
        this.loadingWifiProfiles = false;
        this.selectProfile(this.buildProfileKey(payload.vendorId, payload.deviceId));
        this.refreshWifi();
      },
      error: error => {
        console.error(error);
        this.loadingWifiProfiles = false;
      }
    });
  }

  private updatePlatform(request: PlatformUpdateRequest, onSuccess?: () => void): void {
    this.loadingPlatform = true;
    this.http.post<PlatformInfoDto>('/api/hardware/platform', request).subscribe({
      next: result => {
        this.platform = this.normalizePlatform(result as unknown as Record<string, unknown>);
        this.loadingPlatform = false;
        this.loadPlatformFallbackIfNeeded();
        if (onSuccess) {
          onSuccess();
        }
      },
      error: error => {
        console.error(error);
        this.loadingPlatform = false;
        this.loadPlatformFallbackIfNeeded();
      }
    });
  }

  private updateWifi(request: WifiUpdateRequest, onSuccess?: () => void, onError?: () => void): void {
    this.loadingWifi = true;
    this.http.post<WifiInfoDto>('/api/hardware/wifi', request).subscribe({
      next: result => {
        this.wifi = result;
        this.loadingWifi = false;
        if (onSuccess) {
          onSuccess();
        }
      },
      error: error => {
        console.error(error);
        this.loadingWifi = false;
        if (onError) {
          onError();
        }
      }
    });
  }

  private updateHotspot(request: HotspotSettingsUpdateRequest): void {
    this.loadingHotspot = true;
    this.http.post<HotspotSettingsDto>('/api/hardware/hotspot', request).subscribe({
      next: result => {
        this.hotspot = result;
        this.loadingHotspot = false;
      },
      error: error => {
        console.error(error);
        this.loadingHotspot = false;
      }
    });
  }

  private normalizePlatform(raw?: Record<string, unknown>): PlatformInfoDto | undefined {
    if (!raw) {
      return undefined;
    }
    const isAvailable =
      (raw['isAvailable'] as boolean | undefined) ??
      (raw['IsAvailable'] as boolean | undefined) ??
      false;
    const platformType =
      (raw['platformType'] as number | undefined) ??
      (raw['PlatformType'] as number | undefined) ??
      (raw['platform_type'] as number | undefined) ??
      0;
    const platformName =
      (raw['platformName'] as string | undefined) ??
      (raw['PlatformName'] as string | undefined) ??
      (raw['platform_name'] as string | undefined) ??
      'Unknown';
    const action =
      (raw['action'] as string | null | undefined) ??
      (raw['Action'] as string | null | undefined) ??
      null;
    return {
      isAvailable,
      platformType,
      platformName,
      action
    };
  }

  private loadPlatformFallbackIfNeeded(): void {
    const current = this.platform;
    const needsFallback =
      !current ||
      !current.isAvailable ||
      !current.platformName ||
      current.platformName.toLowerCase() === 'unknown' ||
      current.platformType === 0;
    if (!needsFallback) {
      return;
    }

    this.http.get<SettingFileSummary[]>('/api/settings').subscribe({
      next: summaries => {
        const platformFile = summaries.find(item =>
          item.name === 'platform.json' ||
          (item.relativePath?.replace(/\\/g, '/').endsWith('/platform.json') ?? false));
        if (!platformFile) {
          return;
        }
        this.http.get<SettingFileDetail>(`/api/settings/${platformFile.id}`).subscribe({
          next: file => {
            if (!file?.content) {
              return;
            }
            try {
              const parsed = JSON.parse(file.content) as Record<string, unknown>;
              const fallbackType =
                this.parseUnknownInt(parsed['platform_type']) ??
                this.parseUnknownInt(parsed['platformType']);
              const fallbackName =
                this.parseUnknownString(parsed['platform_name']) ??
                this.parseUnknownString(parsed['platformName']);

              if (!this.platform) {
                this.platform = {
                  isAvailable: true,
                  platformType: fallbackType ?? 0,
                  platformName: fallbackName ?? 'Unknown',
                  action: 'config'
                };
                return;
              }

              if (this.platform.platformType === 0 && fallbackType !== null) {
                this.platform.platformType = fallbackType;
              }
              if (!this.platform.platformName ||
                  this.platform.platformName.toLowerCase() === 'unknown') {
                this.platform.platformName = fallbackName ?? this.platform.platformName;
              }
              if (!this.platform.action) {
                this.platform.action = 'config';
              }
            } catch (error) {
              console.error(error);
            }
          }
        });
      }
    });
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

  private parseUnknownString(value: unknown): string | null {
    if (typeof value === 'string') {
      const trimmed = value.trim();
      return trimmed ? trimmed : null;
    }
    return null;
  }

  public hasPowerProfile(card?: WifiCardInfoDto): boolean {
    if (!card) {
      return false;
    }
    return Boolean(card.powerLowest || card.powerLow || card.powerMid || card.powerHigh);
  }

  public canSaveTxPower(): boolean {
    if (this.isFixedProfile()) {
      return false;
    }
    if (this.hasPowerProfile(this.selectedWifiCard)) {
      return true;
    }
    return (this.txPowerForm.powerLevel ?? '') === '';
  }

  private selectFirstProfile(): void {
    if (!this.wifiProfiles || this.wifiProfiles.cards.length === 0) {
      return;
    }
    if (this.selectedWifiCard) {
      this.selectProfileForCard(this.selectedWifiCard);
      return;
    }
    const first = this.wifiProfiles.cards[0];
    this.selectProfile(this.buildProfileKey(first.vendorId, first.deviceId));
  }

  public buildProfileKey(vendorId: string, deviceId: string): string {
    return `${vendorId}|${deviceId}`;
  }

  public findProfileByKey(key: string): WifiCardProfileDto | undefined {
    if (!this.wifiProfiles) {
      return undefined;
    }
    return this.wifiProfiles.cards.find(profile => this.buildProfileKey(profile.vendorId, profile.deviceId) === key);
  }

  public getSelectedProfileName(): string {
    const profile = this.findProfileByKey(this.selectedProfileKey);
    return profile?.name ?? '';
  }

  public isFixedProfile(): boolean {
    const profile = this.findProfileByKey(this.selectedProfileKey);
    return (profile?.powerMode ?? '').toLowerCase() === 'fixed';
  }

  public getSelectedProfileMode(): string {
    const profile = this.findProfileByKey(this.selectedProfileKey);
    return profile?.powerMode ?? '';
  }

  private parsePowerValue(value: string): number {
    const parsed = Number.parseInt((value ?? '').trim(), 10);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private selectProfileForCard(card: WifiCardInfoDto): void {
    if (!this.wifiProfiles) {
      return;
    }
    const match = this.wifiProfiles.cards.find(profile =>
      profile.vendorId.toLowerCase() === (card.vendorId || '').toLowerCase() &&
      profile.deviceId.toLowerCase() === (card.deviceId || '').toLowerCase()
    );
    if (match) {
      this.selectProfile(this.buildProfileKey(match.vendorId, match.deviceId));
      return;
    }
    if (this.wifiProfiles.cards.length > 0) {
      const first = this.wifiProfiles.cards[0];
      this.selectProfile(this.buildProfileKey(first.vendorId, first.deviceId));
    }
  }

  private loadSystemCommands(): void {
    this.http.get<SystemCommandDto[]>('/api/system/get-commands').subscribe({
      next: result => {
        const match = result.find(command =>
          command.displayName.toLowerCase().includes('restart openhd') ||
          command.id.toLowerCase().includes('openhd'));
        this.restartCommand = match;
      },
      error: error => {
        console.error(error);
      }
    });
  }

  private hasHardwareConfigChanges(): boolean {
    if (!this.hardwareConfig || !this.hardwareConfigSnapshot) {
      return false;
    }
    const current = this.hardwareConfig;
    const saved = this.hardwareConfigSnapshot;
    return current.wifiEnableAutodetect !== saved.wifiEnableAutodetect ||
      current.wifiWbLinkCards !== saved.wifiWbLinkCards ||
      current.wifiHotspotCard !== saved.wifiHotspotCard ||
      current.wifiMonitorCardEmulate !== saved.wifiMonitorCardEmulate ||
      current.wifiForceNoLinkButHotspot !== saved.wifiForceNoLinkButHotspot ||
      current.wifiLocalNetworkEnable !== saved.wifiLocalNetworkEnable ||
      current.wifiLocalNetworkSsid !== saved.wifiLocalNetworkSsid ||
      current.wifiLocalNetworkPassword !== saved.wifiLocalNetworkPassword ||
      current.nwEthernetCard !== saved.nwEthernetCard ||
      current.nwManualForwardingIps !== saved.nwManualForwardingIps ||
      current.nwForwardToLocalhost58xx !== saved.nwForwardToLocalhost58xx ||
      current.genEnableLastKnownPosition !== saved.genEnableLastKnownPosition ||
      current.genRfMetricsLevel !== saved.genRfMetricsLevel ||
      current.groundUnitIp !== saved.groundUnitIp ||
      current.airUnitIp !== saved.airUnitIp ||
      current.videoPort !== saved.videoPort ||
      current.telemetryPort !== saved.telemetryPort ||
      current.disableMicrohardDetection !== saved.disableMicrohardDetection ||
      current.forceMicrohard !== saved.forceMicrohard ||
      current.microhardUsername !== saved.microhardUsername ||
      current.microhardPassword !== saved.microhardPassword ||
      current.microhardIpAir !== saved.microhardIpAir ||
      current.microhardIpGround !== saved.microhardIpGround ||
      current.microhardIpRange !== saved.microhardIpRange ||
      current.microhardVideoPort !== saved.microhardVideoPort ||
      current.microhardTelemetryPort !== saved.microhardTelemetryPort;
  }

  private buildDefaultHardwareConfig(): HardwareConfigDto {
    return {
      isAvailable: true,
      wifiEnableAutodetect: true,
      wifiWbLinkCards: '',
      wifiHotspotCard: '',
      wifiMonitorCardEmulate: false,
      wifiForceNoLinkButHotspot: false,
      wifiLocalNetworkEnable: false,
      wifiLocalNetworkSsid: '',
      wifiLocalNetworkPassword: '',
      nwEthernetCard: 'RPI_ETHERNET_ONLY',
      nwManualForwardingIps: '',
      nwForwardToLocalhost58xx: false,
      genEnableLastKnownPosition: false,
      genRfMetricsLevel: 0,
      groundUnitIp: '',
      airUnitIp: '',
      videoPort: 5000,
      telemetryPort: 5600,
      disableMicrohardDetection: false,
      forceMicrohard: false,
      microhardUsername: 'admin',
      microhardPassword: 'qwertz1',
      microhardIpAir: '',
      microhardIpGround: '',
      microhardIpRange: '',
      microhardVideoPort: 5910,
      microhardTelemetryPort: 5920
    };
  }

  private cloneHardwareConfig(source: HardwareConfigDto): HardwareConfigDto {
    return { ...source };
  }

  private isValidIp(value: string): boolean {
    const parts = value.split('.');
    if (parts.length !== 4) {
      return false;
    }
    return parts.every(part => {
      if (!/^\d+$/.test(part)) {
        return false;
      }
      const num = Number(part);
      return num >= 0 && num <= 255;
    });
  }
}

interface PlatformInfoDto {
  isAvailable: boolean;
  platformType: number;
  platformName: string;
  action?: string | null;
}

interface PlatformUpdateRequest {
  action: string;
  platformType?: number;
  platformName?: string;
}

interface WifiCardInfoDto {
  interfaceName: string;
  driverName: string;
  mac: string;
  phyIndex: number;
  vendorId: string;
  deviceId: string;
  detectedType: string;
  overrideType: string;
  effectiveType: string;
  disabled: boolean;
  txPower?: string;
  txPowerHigh?: string;
  txPowerLow?: string;
  cardName?: string;
  powerLevel?: string;
  powerLowest?: string;
  powerLow?: string;
  powerMid?: string;
  powerHigh?: string;
  powerMin?: string;
  powerMax?: string;
}

interface WifiInfoDto {
  isAvailable: boolean;
  cards: WifiCardInfoDto[];
  action?: string | null;
}

interface WifiUpdateRequest {
  action: string;
  interface?: string;
  overrideType?: string;
  txPower?: string;
  txPowerHigh?: string;
  txPowerLow?: string;
  cardName?: string;
  powerLevel?: string;
}

interface WifiCardProfilesDto {
  isAvailable: boolean;
  cards: WifiCardProfileDto[];
  action?: string | null;
}

interface WifiCardProfileDto {
  vendorId: string;
  deviceId: string;
  name: string;
  powerMode: string;
  minMw: number;
  maxMw: number;
  lowest: number;
  low: number;
  mid: number;
  high: number;
}

interface WifiCardProfileUpdateRequest {
  vendorId: string;
  deviceId: string;
  name: string;
  powerMode: string;
  lowest: number;
  low: number;
  mid: number;
  high: number;
}

interface HotspotSettingsDto {
  isAvailable: boolean;
  hotspotMode: number;
  hotspotSsid: string;
  hotspotPassword: string;
  hotspotInterfaceOverride: string;
  action?: string | null;
}

interface HotspotSettingsUpdateRequest {
  action: string;
  hotspotMode?: number;
  hotspotSsid?: string;
  hotspotPassword?: string;
  hotspotInterfaceOverride?: string;
}

interface HotspotModeOption {
  value: number;
  label: string;
}

interface HardwareConfigDto {
  isAvailable: boolean;
  wifiEnableAutodetect: boolean;
  wifiWbLinkCards: string;
  wifiHotspotCard: string;
  wifiMonitorCardEmulate: boolean;
  wifiForceNoLinkButHotspot: boolean;
  wifiLocalNetworkEnable: boolean;
  wifiLocalNetworkSsid: string;
  wifiLocalNetworkPassword: string;
  nwEthernetCard: string;
  nwManualForwardingIps: string;
  nwForwardToLocalhost58xx: boolean;
  genEnableLastKnownPosition: boolean;
  genRfMetricsLevel: number;
  groundUnitIp: string;
  airUnitIp: string;
  videoPort: number;
  telemetryPort: number;
  disableMicrohardDetection: boolean;
  forceMicrohard: boolean;
  microhardUsername: string;
  microhardPassword: string;
  microhardIpAir: string;
  microhardIpGround: string;
  microhardIpRange: string;
  microhardVideoPort: number;
  microhardTelemetryPort: number;
}

interface HardwareConfigUpdateRequest {
  wifiEnableAutodetect?: boolean;
  wifiWbLinkCards?: string;
  wifiHotspotCard?: string;
  wifiMonitorCardEmulate?: boolean;
  wifiForceNoLinkButHotspot?: boolean;
  wifiLocalNetworkEnable?: boolean;
  wifiLocalNetworkSsid?: string;
  wifiLocalNetworkPassword?: string;
  nwEthernetCard?: string;
  nwManualForwardingIps?: string;
  nwForwardToLocalhost58xx?: boolean;
  genEnableLastKnownPosition?: boolean;
  genRfMetricsLevel?: number;
  groundUnitIp?: string;
  airUnitIp?: string;
  videoPort?: number;
  telemetryPort?: number;
  disableMicrohardDetection?: boolean;
  forceMicrohard?: boolean;
  microhardUsername?: string;
  microhardPassword?: string;
  microhardIpAir?: string;
  microhardIpGround?: string;
  microhardIpRange?: string;
  microhardVideoPort?: number;
  microhardTelemetryPort?: number;
}

interface SystemCommandDto {
  id: string;
  displayName: string;
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

interface WifiTxPowerForm {
  interfaceName: string;
  powerLevel: string;
}

interface WifiCardProfileForm {
  vendorId: string;
  deviceId: string;
  name: string;
  powerMode: string;
  lowest: string;
  low: string;
  mid: string;
  high: string;
}
