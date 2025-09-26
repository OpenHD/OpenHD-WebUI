import { Component, OnInit } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';

interface SettingFileSummary {
  id: string;
  name: string;
  rootDirectory: string;
  relativePath: string;
}

interface SettingFileContent {
  id: string;
  name: string;
  content: string;
}

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
  public isLoadingContent = false;
  public isLoadingList = false;
  public isSaving = false;
  public errorMessage: string | null = null;
  public successMessage: string | null = null;

  public files: SettingFileSummary[] = [];
  public filteredFiles: SettingFileSummary[] = [];
  public selectedFile: SettingFileSummary | null = null;
  public fileContent = '';
  public searchTerm = '';

  constructor(private httpClient: HttpClient) { }

  ngOnInit(): void {
    this.loadFiles();
  }

  loadFiles(): void {
    this.isLoadingList = true;
    this.clearMessages();
    this.httpClient.get<SettingFileSummary[]>('/api/settings/files')
      .pipe(finalize(() => {
        this.isLoadingList = false;
      }))
      .subscribe({
        next: files => {
          this.files = files;
          this.applyFilter();
        },
        error: error => this.handleError('Unable to load settings files.', error)
      });
  }

  applyFilter(): void {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      this.filteredFiles = [...this.files];
    } else {
      this.filteredFiles = this.files.filter(file =>
        file.name.toLowerCase().includes(term) ||
        file.relativePath.toLowerCase().includes(term) ||
        file.rootDirectory.toLowerCase().includes(term));
    }

    if (this.selectedFile && !this.filteredFiles.some(file => file.id === this.selectedFile?.id)) {
      this.selectedFile = null;
      this.fileContent = '';
    }

    if (!this.selectedFile && this.filteredFiles.length > 0) {
      this.selectFile(this.filteredFiles[0]);
    }
  }

  onSearchTermChange(): void {
    this.applyFilter();
  }

  selectFile(file: SettingFileSummary): void {
    if (this.selectedFile?.id === file.id && this.fileContent) {
      return;
    }

    this.selectedFile = file;
    this.fileContent = '';
    this.isLoadingContent = true;
    this.clearMessages();
    this.httpClient.get<SettingFileContent>('/api/settings/file', { params: { id: file.id } })
      .pipe(finalize(() => {
        this.isLoadingContent = false;
      }))
      .subscribe({
        next: content => {
          this.fileContent = content.content;
        },
        error: error => this.handleError('Unable to load the selected settings file.', error)
      });
  }

  formatContent(): void {
    if (!this.fileContent) {
      return;
    }

    try {
      const parsed = JSON.parse(this.fileContent);
      this.fileContent = JSON.stringify(parsed, null, 2);
      this.successMessage = 'JSON formatted successfully.';
      this.errorMessage = null;
    } catch (error) {
      this.errorMessage = 'The current content is not valid JSON and cannot be formatted.';
      this.successMessage = null;
    }
  }

  saveChanges(): void {
    if (!this.selectedFile) {
      return;
    }

    this.clearMessages();

    try {
      JSON.parse(this.fileContent);
    } catch (error) {
      this.errorMessage = 'Please fix the JSON errors before saving.';
      return;
    }

    this.isSaving = true;
    const payload = { id: this.selectedFile.id, content: this.fileContent };
    this.httpClient.put<SettingFileContent>('/api/settings/file', payload)
      .pipe(finalize(() => {
        this.isSaving = false;
      }))
      .subscribe({
        next: response => {
          this.fileContent = response.content;
          this.successMessage = 'Settings saved successfully.';
        },
        error: error => this.handleError('Unable to save the settings file.', error)
      });
  }

  clearMessages(): void {
    this.errorMessage = null;
    this.successMessage = null;
  }

  private handleError(message: string, error: unknown): void {
    console.error(message, error);
    let errorDetail = '';
    if (error instanceof HttpErrorResponse) {
      if (error.error && typeof error.error === 'object' && 'error' in error.error) {
        errorDetail = ` ${error.error.error}`;
      } else if (typeof error.error === 'string') {
        errorDetail = ` ${error.error}`;
      }
    }

    this.errorMessage = `${message}${errorDetail}`;
    this.successMessage = null;
  }
}
