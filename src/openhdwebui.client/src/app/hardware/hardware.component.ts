import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-hardware',
  templateUrl: './hardware.component.html',
  styleUrls: ['./hardware.component.css']
})
export class HardwareComponent implements OnInit, OnDestroy {
  public status?: IOpenHdStatus;
  public errorHistory: StatusEntry[] = [];
  public wifi?: WifiInfoDto;
  public hotspot?: HotspotSettingsDto;
  public wifiProfiles?: WifiCardProfilesDto;
  public hardwareConfig?: HardwareConfigDto;
  private hardwareConfigSnapshot?: HardwareConfigDto;
  public loadingWifi = false;
  public loadingHotspot = false;
  public loadingWifiProfiles = false;
  public wifiProfilesImporting = false;
  public wifiProfilesImportError?: string;
  public wifiProfilesImportName?: string;
  public loadingHardwareConfig = false;
  public hardwareConfigError?: string;
  public showRestartPrompt = false;
  public restartCommand?: SystemCommandDto;
  public restartRunning = false;
  public txPowerModalOpen = false;
  public txPowerSaving = false;
  public txPowerError?: string;
  public isUpdatingSysutilDebug = false;
  public sysutilDebug?: SysutilDebugInfo;
  public sysutilDebugStatus?: string;
  public sysutilDebugError?: string;
  public artosynRfSaving = false;
  public artosynRfError = '';
  public artosynRfSuccess = '';
  public artosynRfDebug?: RfControlDebugInfo;
  public showArtosynRfDebug = false;
  public selectedWifiCard?: WifiCardInfoDto;
  public selectedProfileKey = '';
  private isDestroyed = false;
  private isStatusStreaming = false;
  public wifiProfileForm: WifiCardProfileForm = {
    vendorId: '',
    deviceId: '',
    chipset: '',
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
  public artosynForm: ArtosynSettingsForm = {
    interfaceName: '',
    overrideType: 'AUTO',
    cardName: '',
    powerLevel: 'AUTO'
  };
  public artosynRfForm: ArtosynRfControlForm = {
    interfaceName: '',
    frequencyMhz: '',
    channelWidthMhz: '',
    mcsIndex: '',
    powerLevel: ''
  };

  public readonly hotspotModeOptions: HotspotModeOption[] = [
    { value: 0, label: 'Automatic (disable when armed)' },
    { value: 1, label: 'Always off' },
    { value: 2, label: 'Always on' }
  ];
  public readonly artosynOverrideOptions: string[] = ['AUTO', 'ARTOSYN', 'DISABLED'];
  public readonly artosynPowerLevelOptions: string[] = ['AUTO', 'LOWEST', 'LOW', 'MID', 'HIGH'];
  public readonly rfChannelOptions: number[] = [
    2312, 2332, 2352, 2372, 2392, 2412, 2432, 2452, 2472, 2484, 2492, 2512,
    2612, 2692, 2712,
    5080, 5100, 5120, 5140, 5160, 5180, 5200, 5220, 5240, 5260, 5280, 5300,
    5320, 5340, 5360, 5380, 5400, 5420, 5440, 5460, 5480, 5500, 5520, 5540,
    5560, 5580, 5600, 5620, 5640, 5660, 5680, 5700, 5720, 5745, 5765, 5785,
    5805, 5825, 5845, 5865, 5885, 5905, 5925, 5945, 5965, 5985, 6005, 6025,
    6045, 6065, 6085
  ];
  public readonly rfBandwidthOptions: number[] = [10, 20];
  public readonly rfMcsOptions: number[] = Array.from({ length: 10 }, (_, index) => index);

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.refreshAll();
    this.loadSystemCommands();
    this.refreshStatus();
    this.startStatusStream();
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
  }

  refreshAll(): void {
    this.loadWifi();
    this.loadHotspot();
    this.loadWifiProfiles();
    this.loadHardwareConfig();
    this.loadSysutilDebug();
  }

  loadWifi(): void {
    this.loadingWifi = true;
    this.http.get<WifiInfoDto>('/api/hardware/wifi').subscribe({
      next: result => {
        this.wifi = result;
        this.syncArtosynFormFromCards();
        this.syncArtosynRfInterface();
        this.loadingWifi = false;
      },
      error: error => {
        console.error(error);
        this.wifi = undefined;
        this.syncArtosynFormFromCards();
        this.syncArtosynRfInterface();
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

  private refreshStatus(): void {
    this.http.get<IOpenHdStatus>('/api/status')
      .subscribe({
        next: response => {
          this.applyStatus(response);
        },
        error: () => {
          // Ignore status fetch errors; connection card will show unavailable.
        }
      });
  }

  private startStatusStream(): void {
    if (this.isStatusStreaming || this.isDestroyed) {
      return;
    }
    this.isStatusStreaming = true;
    const since = this.status?.updatedMs ?? 0;

    this.http.get<IOpenHdStatus>(`/api/status/stream?since=${since}`)
      .subscribe({
        next: response => {
          this.applyStatus(response);
          this.isStatusStreaming = false;
          this.startStatusStream();
        },
        error: () => {
          this.isStatusStreaming = false;
          if (!this.isDestroyed) {
            window.setTimeout(() => this.startStatusStream(), 2000);
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

  formatTimestamp(ms?: number): string {
    if (!ms) {
      return '-';
    }
    return new Date(ms).toLocaleTimeString();
  }

  refreshWifi(): void {
    this.updateWifi({ action: 'refresh' });
  }

  refreshHotspot(): void {
    this.updateHotspot({ action: 'refresh' });
  }

  public get artosynCards(): WifiCardInfoDto[] {
    return (this.wifi?.cards ?? []).filter(card => this.isArtosynCard(card));
  }

  public hasArtosynDevice(): boolean {
    return this.artosynCards.length > 0;
  }

  public get selectedArtosynCard(): WifiCardInfoDto | undefined {
    const cards = this.artosynCards;
    if (cards.length === 0) {
      return undefined;
    }
    return cards.find(card => card.interfaceName === this.artosynForm.interfaceName) ?? cards[0];
  }

  public get artosynDaemonRunning(): boolean {
    return this.artosynCards.some(card => card.artosynDaemonRunning === true);
  }

  public get artosynDaemonDetail(): string {
    const details = this.artosynCards
      .map(card => (card.artosynDaemonDetail ?? '').trim())
      .filter(detail => !!detail);
    const unique = Array.from(new Set(details));
    return unique.length > 0 ? unique.join(', ') : '-';
  }

  public onArtosynInterfaceChange(interfaceName: string): void {
    const selected = this.artosynCards.find(card => card.interfaceName === interfaceName);
    if (!selected) {
      return;
    }
    this.artosynForm.interfaceName = selected.interfaceName;
    this.artosynRfForm.interfaceName = selected.interfaceName;
    this.artosynForm.overrideType = (selected.overrideType ?? '').trim().toUpperCase() || 'AUTO';
    this.artosynForm.cardName = selected.cardName ?? '';
    this.artosynForm.powerLevel = (selected.powerLevel ?? '').trim().toUpperCase() || 'AUTO';
  }

  public saveArtosynSettings(): void {
    const selected = this.selectedArtosynCard;
    if (!selected) {
      return;
    }

    const overrideType = this.normalizeArtosynOption(this.artosynForm.overrideType);
    const powerLevel = this.normalizeArtosynOption(this.artosynForm.powerLevel);

    this.updateWifi({
      action: 'set',
      interface: selected.interfaceName,
      overrideType,
      cardName: (this.artosynForm.cardName ?? '').trim(),
      powerLevel
    });
  }

  public toggleSysutilDebug(): void {
    if (this.isUpdatingSysutilDebug || !this.sysutilDebug?.isAvailable) {
      return;
    }
    this.isUpdatingSysutilDebug = true;
    this.sysutilDebugStatus = undefined;
    this.sysutilDebugError = undefined;
    const nextValue = !this.sysutilDebug.debug;

    this.http.post<SysutilDebugUpdateResponse>('/api/sysutil/debug', { debug: nextValue })
      .subscribe({
        next: response => {
          this.isUpdatingSysutilDebug = false;
          if (response.ok) {
            this.sysutilDebug = { isAvailable: true, debug: response.debug };
            this.sysutilDebugStatus = response.debug ? 'Sysutils debug enabled.' : 'Sysutils debug disabled.';
            return;
          }
          this.sysutilDebugError = response.message ?? 'Unable to update sysutils debug mode.';
        },
        error: err => {
          this.isUpdatingSysutilDebug = false;
          this.sysutilDebugError = err?.error?.message ?? 'Unable to update sysutils debug mode.';
        }
      });
  }

  public applyArtosynRfControl(): void {
    if (this.artosynRfSaving) {
      return;
    }

    const payload: RfControlRequest = {};
    const iface = (this.artosynRfForm.interfaceName ?? '').trim();
    if (iface) {
      payload.interfaceName = iface;
    }

    const frequency = this.parseOptionalInt(this.artosynRfForm.frequencyMhz);
    if (frequency !== null) {
      payload.frequencyMhz = frequency;
    }
    const bandwidth = this.parseOptionalInt(this.artosynRfForm.channelWidthMhz);
    if (bandwidth !== null) {
      payload.channelWidthMhz = bandwidth;
    }
    const mcs = this.parseOptionalInt(this.artosynRfForm.mcsIndex);
    if (mcs !== null) {
      payload.mcsIndex = mcs;
    }
    const powerLevel = (this.artosynRfForm.powerLevel ?? '').trim().toLowerCase();
    if (powerLevel) {
      payload.powerLevel = powerLevel;
    }

    if (Object.keys(payload).length === 0) {
      this.artosynRfError = 'Enter at least one RF field to apply.';
      this.artosynRfSuccess = '';
      return;
    }

    this.artosynRfSaving = true;
    this.artosynRfError = '';
    this.artosynRfSuccess = '';
    this.artosynRfDebug = undefined;

    this.http.post<RfControlResponse>('/api/status/rf-control', payload)
      .subscribe({
        next: response => {
          this.artosynRfSaving = false;
          this.artosynRfDebug = response.debug;
          this.showArtosynRfDebug = !response.ok;
          if (response.ok) {
            this.artosynRfSuccess = response.message || 'RF settings applied.';
            return;
          }
          this.artosynRfError = response.message || 'Unable to apply RF settings.';
        },
        error: () => {
          this.artosynRfSaving = false;
          this.artosynRfError = 'Unable to reach the RF control endpoint.';
          this.artosynRfDebug = undefined;
          this.showArtosynRfDebug = true;
        }
      });
  }

  refreshWifiProfiles(): void {
    this.loadWifiProfiles();
  }

  exportWifiProfilesJson(): void {
    if (!this.wifiProfiles || this.wifiProfiles.cards.length === 0) {
      return;
    }

    const payload = this.buildWifiProfilesFile();
    const json = JSON.stringify(payload, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'wifi_cards.json';
    anchor.click();
    window.URL.revokeObjectURL(url);
  }

  importWifiProfiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }
    if (!confirm('Replace all Wi-Fi profiles with the imported JSON file?')) {
      return;
    }

    this.wifiProfilesImporting = true;
    this.wifiProfilesImportError = undefined;
    this.wifiProfilesImportName = file.name;

    const reader = new FileReader();
    reader.onload = () => {
      const content = typeof reader.result === 'string' ? reader.result : '';
      this.sendWifiProfilesImport(content);
    };
    reader.onerror = () => {
      this.wifiProfilesImporting = false;
      this.wifiProfilesImportError = 'Unable to read the selected file.';
    };
    reader.readAsText(file);
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
      chipset: profile.chipset ?? '',
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
      chipset: this.wifiProfileForm.chipset,
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
        this.selectProfile(this.buildProfileKey(payload.vendorId, payload.deviceId, payload.chipset ?? ''));
        this.refreshWifi();
      },
      error: error => {
        console.error(error);
        this.loadingWifiProfiles = false;
      }
    });
  }

  private updateWifi(request: WifiUpdateRequest, onSuccess?: () => void, onError?: () => void): void {
    this.loadingWifi = true;
    this.http.post<WifiInfoDto>('/api/hardware/wifi', request).subscribe({
      next: result => {
        this.wifi = result;
        this.syncArtosynFormFromCards();
        this.syncArtosynRfInterface();
        this.loadingWifi = false;
        if (onSuccess) {
          onSuccess();
        }
      },
      error: error => {
        console.error(error);
        this.syncArtosynFormFromCards();
        this.syncArtosynRfInterface();
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

  public hasMatchingProfile(card?: WifiCardInfoDto): boolean {
    if (!card) {
      return false;
    }
    if ((card.powerMode ?? '').trim()) {
      return true;
    }
    if (!this.wifiProfiles) {
      return false;
    }
    const cardVendor = (card.vendorId || '').toLowerCase();
    const cardDevice = (card.deviceId || '').toLowerCase();
    const cardChipset = (card.detectedType || '').toLowerCase();
    return this.wifiProfiles.cards.some(profile => {
      if (profile.vendorId.toLowerCase() !== cardVendor ||
          profile.deviceId.toLowerCase() !== cardDevice) {
        return false;
      }
      const profileChipset = (profile.chipset ?? '').toLowerCase();
      return profileChipset ? profileChipset === cardChipset : true;
    });
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
    this.selectProfile(this.buildProfileKey(first.vendorId, first.deviceId, first.chipset ?? ''));
  }

  public buildProfileKey(vendorId: string, deviceId: string, chipset: string = ''): string {
    return `${vendorId}|${deviceId}|${chipset ?? ''}`;
  }

  public findProfileByKey(key: string): WifiCardProfileDto | undefined {
    if (!this.wifiProfiles) {
      return undefined;
    }
    return this.wifiProfiles.cards.find(profile =>
      this.buildProfileKey(profile.vendorId, profile.deviceId, profile.chipset ?? '') === key);
  }

  public getSelectedProfileName(): string {
    const profile = this.findProfileByKey(this.selectedProfileKey);
    return profile?.name ?? '';
  }

  public isFixedProfile(): boolean {
    const profile = this.findProfileByKey(this.selectedProfileKey);
    return (profile?.powerMode ?? '').toLowerCase() === 'fixed';
  }

  public isFixedProfileEntry(profile?: WifiCardProfileDto): boolean {
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

  private parseOptionalInt(value: string): number | null {
    const trimmed = (value ?? '').trim();
    if (!trimmed) {
      return null;
    }
    const parsed = Number.parseInt(trimmed, 10);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private sendWifiProfilesImport(content: string): void {
    const payload: WifiCardProfilesImportRequest = { content };
    this.http.post<WifiCardProfilesDto>('/api/hardware/wifi-profiles/import', payload).subscribe({
      next: result => {
        this.wifiProfiles = result;
        this.wifiProfilesImporting = false;
        this.refreshWifi();
      },
      error: error => {
        console.error(error);
        const message = typeof error?.error === 'string'
          ? error.error
          : error?.error?.title ?? 'Failed to import Wi-Fi profiles.';
        this.wifiProfilesImportError = message;
        this.wifiProfilesImporting = false;
      }
    });
  }

  private buildWifiProfilesFile(): WifiCardProfilesFile {
    return {
      cards: (this.wifiProfiles?.cards ?? []).map(profile => {
        const powerMode = profile.powerMode || 'mw';
        const entry: WifiCardProfileFileEntry = {
          vendor_id: profile.vendorId,
          device_id: profile.deviceId,
          chipset: (profile.chipset ?? '').trim() ? profile.chipset : undefined,
          name: profile.name,
          power_mode: powerMode
        };
        if (powerMode.toLowerCase() !== 'fixed') {
          entry.min_mw = profile.minMw ?? 0;
          entry.max_mw = profile.maxMw ?? 0;
          entry.levels_mw = {
            lowest: profile.lowest ?? 0,
            low: profile.low ?? 0,
            mid: profile.mid ?? 0,
            high: profile.high ?? 0
          };
        }
        return entry;
      })
    };
  }

  private selectProfileForCard(card: WifiCardInfoDto): void {
    if (!this.wifiProfiles) {
      return;
    }
    const cardVendor = (card.vendorId || '').toLowerCase();
    const cardDevice = (card.deviceId || '').toLowerCase();
    const cardChipset = (card.detectedType || '').toLowerCase();
    const match = this.wifiProfiles.cards.find(profile =>
      profile.vendorId.toLowerCase() === cardVendor &&
      profile.deviceId.toLowerCase() === cardDevice &&
      (profile.chipset ?? '').toLowerCase() === cardChipset
    );
    if (match) {
      this.selectProfile(this.buildProfileKey(match.vendorId, match.deviceId, match.chipset ?? ''));
      return;
    }
    const fallback = this.wifiProfiles.cards.find(profile =>
      profile.vendorId.toLowerCase() === cardVendor &&
      profile.deviceId.toLowerCase() === cardDevice &&
      !(profile.chipset ?? '').trim()
    );
    if (fallback) {
      this.selectProfile(this.buildProfileKey(fallback.vendorId, fallback.deviceId, fallback.chipset ?? ''));
      return;
    }
    if (this.wifiProfiles.cards.length > 0) {
      const first = this.wifiProfiles.cards[0];
      this.selectProfile(this.buildProfileKey(first.vendorId, first.deviceId, first.chipset ?? ''));
    }
  }

  private syncArtosynFormFromCards(): void {
    const cards = this.artosynCards;
    if (cards.length === 0) {
      this.artosynForm = {
        interfaceName: '',
        overrideType: 'AUTO',
        cardName: '',
        powerLevel: 'AUTO'
      };
      return;
    }
    const selected = cards.find(card => card.interfaceName === this.artosynForm.interfaceName) ?? cards[0];
    this.artosynForm = {
      interfaceName: selected.interfaceName,
      overrideType: (selected.overrideType ?? '').trim().toUpperCase() || 'AUTO',
      cardName: selected.cardName ?? '',
      powerLevel: (selected.powerLevel ?? '').trim().toUpperCase() || 'AUTO'
    };
  }

  private syncArtosynRfInterface(): void {
    const selected = this.selectedArtosynCard;
    if (!selected) {
      this.artosynRfForm.interfaceName = '';
      return;
    }
    if (!this.artosynRfForm.interfaceName ||
        !this.artosynCards.some(card => card.interfaceName === this.artosynRfForm.interfaceName)) {
      this.artosynRfForm.interfaceName = selected.interfaceName;
    }
  }

  private loadSysutilDebug(): void {
    this.http.get<SysutilDebugInfo>('/api/sysutil/debug').subscribe({
      next: info => {
        this.sysutilDebug = info;
      },
      error: err => {
        this.sysutilDebug = { isAvailable: false, debug: false };
        console.error(err);
      }
    });
  }

  private normalizeArtosynOption(value: string): string {
    const normalized = (value ?? '').trim().toUpperCase();
    return !normalized || normalized === 'AUTO' ? '' : normalized;
  }

  private isArtosynCard(card?: WifiCardInfoDto): boolean {
    if (!card) {
      return false;
    }
    const vendor = (card.vendorId ?? '').toLowerCase();
    const device = (card.deviceId ?? '').toLowerCase();
    if (vendor === '0x4152' && device === '0x8030') {
      return true;
    }

    const hints = [
      card.interfaceName,
      card.driverName,
      card.detectedType,
      card.overrideType,
      card.effectiveType,
      card.cardName
    ]
      .join(' ')
      .toLowerCase();
    return hints.includes('artosyn') || hints.includes('artlink') || hints.includes('ar_mdev');
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
  powerMode?: string;
  powerLevel?: string;
  powerLowest?: string;
  powerLow?: string;
  powerMid?: string;
  powerHigh?: string;
  powerMin?: string;
  powerMax?: string;
  artosynDaemonRunning?: boolean;
  artosynDaemonDetail?: string;
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
  chipset: string;
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
  chipset?: string;
  name: string;
  powerMode: string;
  lowest: number;
  low: number;
  mid: number;
  high: number;
}

interface WifiCardProfilesImportRequest {
  content: string;
}

interface WifiCardProfilesFile {
  cards: WifiCardProfileFileEntry[];
}

interface WifiCardProfileFileEntry {
  vendor_id: string;
  device_id: string;
  chipset?: string;
  name: string;
  power_mode: string;
  min_mw?: number;
  max_mw?: number;
  levels_mw?: WifiCardProfileLevelsFile;
}

interface WifiCardProfileLevelsFile {
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

interface WifiTxPowerForm {
  interfaceName: string;
  powerLevel: string;
}

interface ArtosynSettingsForm {
  interfaceName: string;
  overrideType: string;
  cardName: string;
  powerLevel: string;
}

interface ArtosynRfControlForm {
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

interface SysutilDebugInfo {
  isAvailable: boolean;
  debug: boolean;
}

interface SysutilDebugUpdateResponse {
  ok: boolean;
  debug: boolean;
  message?: string;
}

interface WifiCardProfileForm {
  vendorId: string;
  deviceId: string;
  chipset: string;
  name: string;
  powerMode: string;
  lowest: string;
  low: string;
  mid: string;
  high: string;
}
