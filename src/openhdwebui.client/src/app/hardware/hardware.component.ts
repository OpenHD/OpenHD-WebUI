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
  public platformTypeOverride = '';
  public platformNameOverride = '';
  public loadingPlatform = false;
  public loadingWifi = false;
  public loadingHotspot = false;

  public readonly hotspotModeOptions: HotspotModeOption[] = [
    { value: 0, label: 'Automatic (disable when armed)' },
    { value: 1, label: 'Always off' },
    { value: 2, label: 'Always on' }
  ];

  public readonly overrideOptions: OverrideOption[] = [
    { value: 'AUTO', label: 'Auto (Detected)' },
    { value: 'DISABLED', label: 'Disabled' },
    { value: 'OPENHD_RTL_88X2AU', label: 'OPENHD_RTL_88X2AU' },
    { value: 'OPENHD_RTL_88X2BU', label: 'OPENHD_RTL_88X2BU' },
    { value: 'OPENHD_RTL_88X2CU', label: 'OPENHD_RTL_88X2CU' },
    { value: 'OPENHD_RTL_88X2EU', label: 'OPENHD_RTL_88X2EU' },
    { value: 'OPENHD_RTL_8852BU', label: 'OPENHD_RTL_8852BU' },
    { value: 'RTL_88X2AU', label: 'RTL_88X2AU' },
    { value: 'RTL_88X2BU', label: 'RTL_88X2BU' },
    { value: 'ATHEROS', label: 'ATHEROS' },
    { value: 'MT_7921u', label: 'MT_7921u' },
    { value: 'RALINK', label: 'RALINK' },
    { value: 'INTEL', label: 'INTEL' },
    { value: 'BROADCOM', label: 'BROADCOM' },
    { value: 'AIC', label: 'AIC' },
    { value: 'QUALCOMM', label: 'QUALCOMM' },
    { value: 'UNKNOWN', label: 'UNKNOWN' }
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.refreshAll();
  }

  refreshAll(): void {
    this.loadPlatform();
    this.loadWifi();
    this.loadHotspot();
  }

  loadPlatform(): void {
    this.loadingPlatform = true;
    this.http.get<PlatformInfoDto>('/api/hardware/platform').subscribe({
      next: result => {
        this.platform = result;
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

  refreshPlatform(): void {
    this.updatePlatform({ action: 'refresh' });
  }

  clearPlatformOverride(): void {
    this.updatePlatform({ action: 'clear' });
  }

  applyPlatformOverride(): void {
    const typeValue = this.platformTypeOverride.trim();
    if (!typeValue) {
      return;
    }
    const parsed = Number(typeValue);
    if (Number.isNaN(parsed)) {
      return;
    }
    this.updatePlatform({
      action: 'set',
      platformType: parsed,
      platformName: this.platformNameOverride.trim() || undefined
    });
  }

  refreshWifi(): void {
    this.updateWifi({ action: 'refresh' });
  }

  refreshHotspot(): void {
    this.updateHotspot({ action: 'refresh' });
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

  applyWifiOverride(card: WifiCardInfoDto, value: string): void {
    this.updateWifi({
      action: 'set',
      interface: card.interfaceName,
      overrideType: value
    });
  }

  clearWifiOverride(card: WifiCardInfoDto): void {
    this.updateWifi({
      action: 'clear',
      interface: card.interfaceName
    });
  }

  private updatePlatform(request: PlatformUpdateRequest): void {
    this.loadingPlatform = true;
    this.http.post<PlatformInfoDto>('/api/hardware/platform', request).subscribe({
      next: result => {
        this.platform = result;
        this.loadingPlatform = false;
      },
      error: error => {
        console.error(error);
        this.loadingPlatform = false;
      }
    });
  }

  private updateWifi(request: WifiUpdateRequest): void {
    this.loadingWifi = true;
    this.http.post<WifiInfoDto>('/api/hardware/wifi', request).subscribe({
      next: result => {
        this.wifi = result;
        this.loadingWifi = false;
      },
      error: error => {
        console.error(error);
        this.loadingWifi = false;
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

interface OverrideOption {
  value: string;
  label: string;
}

interface HotspotModeOption {
  value: number;
  label: string;
}
