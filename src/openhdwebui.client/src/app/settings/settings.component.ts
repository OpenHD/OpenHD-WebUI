import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ThemeService } from '../theme.service';
import {
  CAMERA_TYPE_OPTIONS,
  SETTINGS_METADATA,
  SettingFieldMeta,
  SettingFieldOption,
  SettingValueType,
  SettingsFileMeta
} from './settings-metadata';

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

interface SysutilCameraInfo {
  isAvailable: boolean;
  hasCameraType: boolean;
  cameraType: number;
}

interface CameraSetupResponse {
  ok: boolean;
  applied: boolean;
  message?: string | null;
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

interface StructuredSettingField {
  key: string;
  label: string;
  description?: string;
  control: 'toggle' | 'select' | 'number' | 'text';
  value: string | number | boolean;
  valueType: SettingValueType;
  options?: SettingFieldOption[];
  min?: number;
  max?: number;
  step?: number;
  unit?: string;
  group?: string;
  hasMetadata: boolean;
}

interface StructuredSettingGroup {
  title?: string;
  fields: StructuredSettingField[];
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

  rawContent = '';
  rawParseError?: string;
  structuredGroups: StructuredSettingGroup[] = [];
  structuredUnknownEntries: { key: string; value: unknown }[] = [];
  hasStructuredView = false;

  cameraSetupOptions = CAMERA_TYPE_OPTIONS;
  selectedCameraType: number = CAMERA_TYPE_OPTIONS[0].value;
  cameraSetupStatus?: string;
  cameraSetupError?: string;
  isApplyingCameraSetup = false;
  isCameraInfoAvailable = false;

  private selectedSettingData: Record<string, unknown> | null = null;
  private suppressRawChange = false;

  constructor(public themeService: ThemeService, private http: HttpClient) {}

  ngOnInit(): void {
    this.loadNetworkInformation();
    this.loadSettingFiles();
    this.loadCameraSetupInfo();
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
        this.buildStructuredEditor(updated);
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

  applyCameraSetup(): void {
    if (this.isApplyingCameraSetup) {
      return;
    }

    this.isApplyingCameraSetup = true;
    this.cameraSetupStatus = undefined;
    this.cameraSetupError = undefined;

    this.http.post<CameraSetupResponse>('/api/camera-setup', {
      cameraType: this.selectedCameraType
    }).subscribe({
      next: result => {
        this.isApplyingCameraSetup = false;
        if (result.ok) {
          if (result.message) {
            this.cameraSetupStatus = result.message;
          } else if (result.applied) {
            this.cameraSetupStatus = 'Camera setup applied. Rebooting now.';
          } else {
            this.cameraSetupStatus = 'Camera setup requested. Rebooting soon.';
          }
          return;
        }
        this.cameraSetupError = result.message ?? 'Camera setup failed.';
      },
      error: err => {
        this.isApplyingCameraSetup = false;
        this.cameraSetupError = err?.error?.message ?? 'Camera setup failed.';
        console.error(err);
      }
    });
  }

  private loadNetworkInformation(): void {
    this.http.get<NetworkInfo>('/api/network/info').subscribe(result => {
      this.network = result;
    }, error => console.error(error));
  }

  private loadCameraSetupInfo(): void {
    this.http.get<SysutilCameraInfo>('/api/camera-setup').subscribe({
      next: info => {
        this.isCameraInfoAvailable = info.isAvailable;
        if (info.hasCameraType) {
          this.selectedCameraType = info.cameraType;
        }
      },
      error: err => {
        this.isCameraInfoAvailable = false;
        console.error(err);
      }
    });
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
        this.buildStructuredEditor(file);
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

  onFieldChange(field: StructuredSettingField, newValue: any): void {
    const value = this.castValue(newValue, field.valueType, field);
    if (value === undefined) {
      return;
    }

    field.value = value;
    if (this.selectedSettingData) {
      this.selectedSettingData[field.key] = value;
      this.syncRawContentFromStructured();
    }
  }

  onToggleChange(field: StructuredSettingField, event: Event): void {
    const target = event.target;
    const isChecked = target instanceof HTMLInputElement ? target.checked : false;
    this.onFieldChange(field, isChecked);
  }

  coerceBoolean(field: StructuredSettingField): boolean {
    const value = field.value;
    if (typeof value === 'boolean') {
      return value;
    }
    if (typeof value === 'number') {
      return value !== 0;
    }
    if (typeof value === 'string') {
      return value === 'true' || value === '1';
    }
    return false;
  }

  onRawContentChange(content: string): void {
    this.rawContent = content;
    if (this.selectedSetting) {
      this.selectedSetting.content = content;
    }

    if (this.suppressRawChange) {
      return;
    }

    try {
      const parsed = JSON.parse(content);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        this.selectedSettingData = null;
        this.structuredGroups = [];
        this.structuredUnknownEntries = [];
        this.hasStructuredView = false;
        this.rawParseError = undefined;
        return;
      }
      this.rawParseError = undefined;
      this.selectedSettingData = parsed as Record<string, unknown>;
      const file = this.selectedSetting;
      if (file) {
        this.populateStructuredView(parsed as Record<string, unknown>, file);
      }
    } catch (err) {
      this.structuredGroups = [];
      this.structuredUnknownEntries = [];
      this.hasStructuredView = false;
      this.rawParseError = 'The JSON content contains syntax errors. Fix the file to re-enable the interactive editor.';
    }
  }

  private buildStructuredEditor(file: SettingFileDetail): void {
    this.rawContent = file.content;
    this.rawParseError = undefined;
    this.structuredGroups = [];
    this.structuredUnknownEntries = [];
    this.hasStructuredView = false;

    try {
      const parsed = JSON.parse(file.content);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        this.selectedSettingData = null;
        return;
      }
      this.selectedSettingData = parsed as Record<string, unknown>;
      this.populateStructuredView(parsed as Record<string, unknown>, file);
    } catch (err) {
      this.selectedSettingData = null;
      this.rawParseError = 'Unable to parse this settings file as JSON.';
    }
  }

  private populateStructuredView(data: Record<string, unknown>, file: SettingFileDetail): void {
    const meta = this.resolveMetadata(file);
    const fields: StructuredSettingField[] = [];
    const unknown: { key: string; value: unknown }[] = [];

    const orderedEntries = Object.entries(data);
    for (const [key, value] of orderedEntries) {
      const field = this.createFieldDescriptor(key, value, meta?.fields[key]);
      if (field) {
        fields.push(field);
      } else {
        unknown.push({ key, value });
      }
    }

    const sortedFields = this.sortFields(fields, meta);
    this.structuredGroups = this.groupFields(sortedFields, meta);
    this.structuredUnknownEntries = unknown;
    this.hasStructuredView = this.structuredGroups.some(group => group.fields.length > 0);
  }

  private resolveMetadata(file: SettingFileSummary): SettingsFileMeta | undefined {
    const normalized = file.relativePath?.replace(/\\/g, '/').toLowerCase();
    if (!normalized) {
      return undefined;
    }
    if (SETTINGS_METADATA[normalized]) {
      return SETTINGS_METADATA[normalized];
    }
    const match = Object.entries(SETTINGS_METADATA).find(([key]) => normalized.endsWith(key));
    return match?.[1];
  }

  private createFieldDescriptor(key: string, value: unknown, meta?: SettingFieldMeta): StructuredSettingField | undefined {
    if (value === null || typeof value === 'object') {
      return undefined;
    }

    const valueType: SettingValueType = meta?.valueType ?? this.detectValueType(value);
    if (!valueType) {
      return undefined;
    }

    const label = meta?.label ?? this.toTitleCase(key.replace(/_/g, ' '));
    const control = this.resolveControl(meta, valueType);
    const options = meta?.options ? [...meta.options] : undefined;
    const coercedValue = this.castValue(value, valueType, meta);
    if (coercedValue === undefined) {
      return undefined;
    }

    return {
      key,
      label,
      description: meta?.description,
      control,
      value: coercedValue,
      valueType,
      options,
      min: meta?.min,
      max: meta?.max,
      step: meta?.step,
      unit: meta?.unit,
      group: meta?.group,
      hasMetadata: !!meta
    };
  }

  private sortFields(fields: StructuredSettingField[], meta?: SettingsFileMeta): StructuredSettingField[] {
    if (!meta?.order || meta.order.length === 0) {
      return [...fields].sort((a, b) => a.label.localeCompare(b.label));
    }

    const order = meta.order;
    const indexFor = (key: string): number => {
      const idx = order.indexOf(key);
      return idx === -1 ? Number.MAX_SAFE_INTEGER : idx;
    };

    return [...fields].sort((a, b) => {
      const aIndex = indexFor(a.key);
      const bIndex = indexFor(b.key);
      if (aIndex !== bIndex) {
        return aIndex - bIndex;
      }
      return a.label.localeCompare(b.label);
    });
  }

  private groupFields(fields: StructuredSettingField[], meta?: SettingsFileMeta): StructuredSettingGroup[] {
    const buckets = new Map<string, StructuredSettingField[]>();
    for (const field of fields) {
      const key = field.group ?? '';
      if (!buckets.has(key)) {
        buckets.set(key, []);
      }
      buckets.get(key)!.push(field);
    }

    const groupOrder = meta?.groupOrder ?? [];
    const orderedKeys: string[] = [];
    for (const entry of groupOrder) {
      if (buckets.has(entry)) {
        orderedKeys.push(entry);
      }
    }
    for (const key of buckets.keys()) {
      if (!orderedKeys.includes(key)) {
        orderedKeys.push(key);
      }
    }

    return orderedKeys.map(key => ({
      title: key || undefined,
      fields: buckets.get(key) ?? []
    })).filter(group => group.fields.length > 0);
  }

  private detectValueType(value: unknown): SettingValueType {
    switch (typeof value) {
      case 'boolean':
        return 'boolean';
      case 'number':
        return 'number';
      default:
        return 'string';
    }
  }

  private resolveControl(meta: SettingFieldMeta | undefined, valueType: SettingValueType): StructuredSettingField['control'] {
    if (meta?.control) {
      return meta.control;
    }
    if (meta?.options && meta.options.length > 0) {
      return 'select';
    }
    if (valueType === 'boolean') {
      return 'toggle';
    }
    if (valueType === 'number') {
      return 'number';
    }
    return 'text';
  }

  private castValue(
    value: any,
    valueType: SettingValueType,
    meta?: { min?: number; max?: number }
  ): string | number | boolean | undefined {
    if (valueType === 'boolean') {
      if (typeof value === 'string') {
        return value === 'true' || value === '1';
      }
      if (typeof value === 'number') {
        return value !== 0;
      }
      return Boolean(value);
    }

    if (valueType === 'number') {
      const numeric = typeof value === 'number' ? value : Number(value);
      if (Number.isNaN(numeric)) {
        return undefined;
      }
      let constrained = numeric;
      if (meta?.min !== undefined && constrained < meta.min) {
        constrained = meta.min;
      }
      if (meta?.max !== undefined && constrained > meta.max) {
        constrained = meta.max;
      }
      return constrained;
    }

    if (typeof value === 'string') {
      return value;
    }
    if (value === null || value === undefined) {
      return '';
    }
    return String(value);
  }

  private syncRawContentFromStructured(): void {
    if (!this.selectedSetting || !this.selectedSettingData) {
      return;
    }
    this.suppressRawChange = true;
    const formatted = JSON.stringify(this.selectedSettingData, null, 2);
    this.rawContent = formatted;
    this.selectedSetting.content = formatted;
    setTimeout(() => {
      this.suppressRawChange = false;
    });
  }

  private toTitleCase(text: string): string {
    return text
      .split(' ')
      .filter(part => part.length > 0)
      .map(part => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' ');
  }
}
