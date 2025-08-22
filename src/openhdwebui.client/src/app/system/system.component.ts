import { Component, OnInit, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Terminal } from 'xterm';
import { FitAddon } from 'xterm-addon-fit';

@Component({
  selector: 'app-system',
  templateUrl: './system.component.html',
  styleUrls: ['./system.component.css']
})
export class SystemComponent implements OnInit, AfterViewInit {
  private httpClient: HttpClient;

  public commands: SystemCommandDto[] = [];
  public files: SystemFileDto[] = [];
  private term: Terminal = new Terminal({ cols: 80, rows: 24 });
  private fitAddon: FitAddon = new FitAddon();
  private currentInput = '';

  @ViewChild('terminal') terminalDiv!: ElementRef;

  constructor(http: HttpClient) {
    
    this.httpClient = http;
    http.get<SystemCommandDto[]>('/api/system/get-commands').subscribe(result => {
      this.commands = result;
    }, error => console.error(error));

    http.get<SystemFileDto[]>('/api/system/get-files').subscribe(result => {
      this.files = result;
    }, error => console.error(error));
  }

  ngOnInit(): void {
  }

  ngAfterViewInit(): void {
    this.term.loadAddon(this.fitAddon);
    this.term.open(this.terminalDiv.nativeElement);
    this.fitAddon.fit();
    this.prompt();

    this.term.onData((data: string) => {
      switch (data) {
        case '\r':
          const command = this.currentInput.trim();
          this.currentInput = '';
          this.httpClient.post('/api/system/run-terminal', { command }, { responseType: 'text' })
            .subscribe(output => {
              if (output) {
                this.term.writeln(`\r\n${output}`);
              }
              this.prompt();
            }, error => {
              console.error(error);
              this.prompt();
            });
          break;
        case '\u007F':
          if (this.currentInput.length > 0) {
            this.currentInput = this.currentInput.slice(0, -1);
            this.term.write('\b \b');
          }
          break;
        default:
          this.currentInput += data;
          this.term.write(data);
      }
    });

    window.addEventListener('resize', () => this.fitAddon.fit());
  }

  private prompt(): void {
    this.term.write('\r\n$ ');
  }

  onCommandClick(command: SystemCommandDto): void {
    this.httpClient.post('/api/system/run-command', {id : command.id}).subscribe(result => {}, error => console.error(error));
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
