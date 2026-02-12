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
  public loadingPlatform = false;
  public loadingWifi = false;
  public loadingHotspot = false;

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

interface HotspotModeOption {
  value: number;
  label: string;
}
