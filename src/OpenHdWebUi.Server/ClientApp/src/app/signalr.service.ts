import { Injectable, Inject } from '@angular/core';
import * as signalR from "@microsoft/signalr"

@Injectable({
  providedIn: 'root'
})
export class SignalrService {

  private hubConnection: signalR.HubConnection;

  constructor(@Inject('BASE_URL') baseUrl: string) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl( baseUrl + 'videoHub')
      .build();
    this.hubConnection
      .start()
      .then(() => console.log('Connection started'))
      .catch(err => console.log('Error while starting connection: ' + err));
  }

  //public addTransferChartDataListener = () => {
  //  this.hubConnection.on('transferchartdata', (data) => {
  //    this.data = data;
  //    console.log(data);
  //  });
  //}
}
