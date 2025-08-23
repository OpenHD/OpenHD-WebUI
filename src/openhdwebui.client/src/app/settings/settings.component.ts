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

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
  network?: NetworkInfo;

  constructor(public themeService: ThemeService, private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<NetworkInfo>('/api/network/info').subscribe(result => {
      this.network = result;
    }, error => console.error(error));
  }

  onThemeToggle(): void {
    this.themeService.toggle();
  }

  saveEthernet(iface: EthernetInterface): void {
    this.http.post('/api/network/ethernet', {
      interface: iface.name,
      ip: iface.ipAddress,
      netmask: iface.netmask
    }).subscribe({ error: err => console.error(err) });
  }
}
