import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css']
})
export class SystemComponent implements OnInit {
  private baseUrl: string;
  private httpClient: HttpClient;

  public commands: SystemCommandDto[] = [];
  public files: SystemFileDto[] = [];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
    this.httpClient = http;
    http.get<SystemCommandDto[]>(baseUrl + 'api/system/get-commands').subscribe(result => {
      this.commands = result;
    }, error => console.error(error));

    http.get<SystemFileDto[]>(baseUrl + 'api/system/get-files').subscribe(result => {
      this.files = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

  onCommandClick(command: SystemCommandDto): void {
    this.httpClient.post(this.baseUrl + 'api/system/run-command', {id : command.id}).subscribe(result => {}, error => console.error(error));
  }

  onFileClick(file: SystemFileDto): void {
    //this.httpClient.get(this.baseUrl + 'api/system/run-command', { id: command.id }).subscribe(result => { }, error => console.error(error));
  }

}


interface SystemCommandDto {
  id: string;
  displayName: string;
}

interface SystemFileDto {
  id: string;
  displayName: string;
}
