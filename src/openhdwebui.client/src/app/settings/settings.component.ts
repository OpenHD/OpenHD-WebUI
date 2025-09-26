import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ThemeService } from '../theme.service';

interface WifiInterface {
  name: string;
  driver: string;
}

interface EthernetInterface {
  name: string;
  ipAddress: string;
  netmask: string;
}

interface NetworkInfo {
  wifi: WifiInterface[];
  ethernet: EthernetInterface[];
}

interface SettingFileSummary {
  id: string;
  name: string;
  relativePath: string;
  category?: string;
}

interface SettingFileDetail extends SettingFileSummary {
  content: string;
}

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
  network?: NetworkInfo;
  settingFiles: SettingFileSummary[] = [];
  selectedSetting?: SettingFileDetail;
  settingsError?: string;
  fileError?: string;
  isLoadingSettings = false;
  isLoadingFile = false;
  isSavingFile = false;
  saveSuccess = false;

  constructor(public themeService: ThemeService, private http: HttpClient) {}

  ngOnInit(): void {
    this.loadNetworkInformation();
    this.loadSettingFiles();
  }

  onThemeToggle(): void {
    this.themeService.toggle();
  }

  get groupedSettings(): { title: string; items: SettingFileSummary[] }[] {
    const groups = new Map<string, SettingFileSummary[]>();
    for (const file of this.settingFiles) {
      const key = file.category ?? 'Ungrouped';
      if (!groups.has(key)) {
        groups.set(key, []);
      }
      groups.get(key)!.push(file);
    }

    return Array.from(groups.entries())
      .map(([title, items]) => ({ title, items: items.sort((a, b) => a.name.localeCompare(b.name)) }))
      .sort((a, b) => a.title.localeCompare(b.title));
  }

  get wifiInterfaces(): WifiInterface[] {
    return this.network?.wifi ?? [];
  }

  get ethernetInterfaces(): EthernetInterface[] {
    return this.network?.ethernet ?? [];
  }

  trackBySetting(_index: number, item: SettingFileSummary): string {
    return item.id;
  }

  selectSetting(file: SettingFileSummary): void {
    if (this.selectedSetting && this.selectedSetting.id === file.id) {
      return;
    }

    this.fetchSettingFile(file.id);
  }

  saveSelectedSetting(): void {
    if (!this.selectedSetting || this.isSavingFile) {
      return;
    }

    this.isSavingFile = true;
    this.saveSuccess = false;
    this.fileError = undefined;

    this.http
      .put<SettingFileDetail>(`/api/settings/${this.selectedSetting.id}`, { content: this.selectedSetting.content })
      .subscribe({
      next: updated => {
        this.selectedSetting = updated;
        this.isSavingFile = false;
        this.saveSuccess = true;
        this.updateSummary(updated);
        setTimeout(() => {
          this.saveSuccess = false;
        }, 2500);
      },
      error: err => {
        this.isSavingFile = false;
        this.fileError = err?.error?.title ?? 'Unable to save settings file.';
        console.error(err);
      }
      });
  }

  saveEthernet(iface: EthernetInterface): void {
    this.http.post('/api/network/ethernet', {
      interface: iface.name,
      ip: iface.ipAddress,
      netmask: iface.netmask
    }).subscribe({ error: err => console.error(err) });
  }

  private loadNetworkInformation(): void {
    this.http.get<NetworkInfo>('/api/network/info').subscribe(result => {
      this.network = result;
    }, error => console.error(error));
  }

  private loadSettingFiles(): void {
    this.isLoadingSettings = true;
    this.settingsError = undefined;
    this.http.get<SettingFileSummary[]>('/api/settings').subscribe({
      next: files => {
        this.settingFiles = files;
        this.isLoadingSettings = false;
        if (files.length > 0) {
          this.fetchSettingFile(files[0].id);
        } else {
          this.selectedSetting = undefined;
        }
      },
      error: err => {
        this.isLoadingSettings = false;
        this.settingsError = err?.error?.title ?? 'Unable to load settings files.';
        console.error(err);
      }
    });
  }

  private fetchSettingFile(id: string): void {
    this.isLoadingFile = true;
    this.fileError = undefined;
    if (!this.selectedSetting || this.selectedSetting.id !== id) {
      this.selectedSetting = undefined;
    }
    this.http.get<SettingFileDetail>(`/api/settings/${id}`).subscribe({
      next: file => {
        this.selectedSetting = file;
        this.isLoadingFile = false;
      },
      error: err => {
        this.isLoadingFile = false;
        this.fileError = err?.error?.title ?? 'Unable to load the selected settings file.';
        console.error(err);
      }
    });
  }

  private updateSummary(updated: SettingFileSummary): void {
    const index = this.settingFiles.findIndex(file => file.id === updated.id);
    if (index >= 0) {
      this.settingFiles[index] = { ...updated };
    }
  }
}
