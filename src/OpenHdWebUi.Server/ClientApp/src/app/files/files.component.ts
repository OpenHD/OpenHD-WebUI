import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-files',
  templateUrl: './files.component.html',
  styleUrls: ['./files.component.css']
})
export class FilesComponent implements OnInit {
  public files: ServerFileInfo[] = [];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<ServerFileInfo[]>(baseUrl + 'api/files').subscribe(result => {
      this.files = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

}

interface ServerFileInfo {
  fileName: string;
  downloadPath: string;
  previewPath: string;
}
