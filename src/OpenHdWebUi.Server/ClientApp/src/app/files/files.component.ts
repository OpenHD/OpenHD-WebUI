import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-files',
  templateUrl: './files.component.html',
  styleUrls: ['./files.component.css']
})
export class FilesComponent implements OnInit {
  private http: HttpClient;
  private baseUrl: string;

  public files: ServerFileInfo[] = [];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
    http.get<ServerFileInfo[]>(baseUrl + 'api/files').subscribe(result => {
      this.files = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

  onDeleteClick(fileName: string): void {
    this.http.delete(this.baseUrl + 'api/files/' + fileName).subscribe(_ => {
      this.http.get<ServerFileInfo[]>(this.baseUrl + 'api/files').subscribe(result => {
        this.files = result;
      }, error => console.error(error));
    }, error => console.error(error));
  }
}

interface ServerFileInfo {
  fileName: string;
  downloadPath: string;
  previewPath: string;
}
