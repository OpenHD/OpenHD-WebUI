import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-files',
  templateUrl: './files.component.html',
  styleUrls: ['./files.component.css']
})
export class FilesComponent implements OnInit {
  private http: HttpClient;

  public files: ServerFileInfo[] = [];

  constructor(http: HttpClient) {
    this.http = http;
    http.get<ServerFileInfo[]>('api/files').subscribe(result => {
      this.files = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

  onDeleteClick(fileName: string): void {
    if (confirm("Are you sure to delete " + fileName)) {
      this.http.delete('api/files/' + fileName).subscribe(_ => {
        this.http.get<ServerFileInfo[]>('api/files').subscribe(result => {
          this.files = result;
        }, error => console.error(error));
      }, error => console.error(error));
    }
  }
}

interface ServerFileInfo {
  fileName: string;
  downloadPath: string;
  previewPath: string;
}
