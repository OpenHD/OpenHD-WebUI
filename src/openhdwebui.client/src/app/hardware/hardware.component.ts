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
  public loadingPlatform = false;
  public loadingWifi = false;
  public loadingHotspot = false;
  public loadingWifiProfiles = false;
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
  }

  refreshAll(): void {
    this.loadPlatform();
    this.loadWifi();
    this.loadHotspot();
    this.loadWifiProfiles();
  }

  loadPlatform(): void {
    this.loadingPlatform = true;
    this.http.get<PlatformInfoDto>('/api/hardware/platform').subscribe({
      next: result => {
        this.platform = this.normalizePlatform(result as unknown as Record<string, unknown>);
        this.loadingPlatform = false;
      },
      error: error => {
        console.error(error);
        this.platform = undefined;
        this.loadingPlatform = false;
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
        if (onSuccess) {
          onSuccess();
        }
      },
      error: error => {
        console.error(error);
        this.loadingPlatform = false;
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
