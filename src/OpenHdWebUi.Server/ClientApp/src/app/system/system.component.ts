import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css']
})
export class SystemComponent implements OnInit {
  private _baseUrl: string;
  private _http: HttpClient;

  public commands: SystemCommandDto[] = [];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this._baseUrl = baseUrl;
    this._http = http;
    http.get<SystemCommandDto[]>(baseUrl + 'api/system/get-commands').subscribe(result => {
      this.commands = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

  onCommandClick(command: SystemCommandDto): void {
    this._http.post(this._baseUrl + 'api/system/run-command', {id : command.id}).subscribe(result => {}, error => console.error(error));
  }

}


interface SystemCommandDto {
  id: string;
  displayName: string;
}
