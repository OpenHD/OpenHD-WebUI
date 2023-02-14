import { Component, OnInit, ViewChild, AfterViewInit, ElementRef } from '@angular/core';
import {SignalrService} from "../signalr.service"

@Component({
  selector: 'app-fpv',
  templateUrl: './fpv.component.html',
  styleUrls: ['./fpv.component.css']
})
export class FpvComponent implements OnInit, AfterViewInit {
  @ViewChild("fpvVideo") video?: ElementRef<HTMLVideoElement>;
  private signalRService: SignalrService;
  private peerConnection: RTCPeerConnection;
  private webSocket: WebSocket;

  public demoText: string;


  constructor(signalrService: SignalrService) {
    this.demoText = "aaaa";
    this.signalRService = signalrService;

    this.webSocket = new WebSocket("ws://127.0.0.1:8081/");

    this.peerConnection = new RTCPeerConnection();


    //.then(offer => {
    //  this.peerConnection.setLocalDescription(offer);
    //  this.demoText = JSON.stringify(offer);
    //});

  }

  ngOnInit(): void {
    //let offer = await this.peerConnection.createOffer({ offerToReceiveVideo: true, offerToReceiveAudio: false });
    //await this.peerConnection.setLocalDescription(offer);
    //await this.peerConnection.getReceivers()
    //this.demoText = JSON.stringify(await this.peerConnection.createOffer());

  }

  ngAfterViewInit() {
    // ElementRef { nativeElement: <input> }
    this.peerConnection.ontrack = evt => this.video!.nativeElement.srcObject = evt.streams[0];
    this.peerConnection.onicecandidate = evt => evt.candidate && this.webSocket.send(JSON.stringify(evt.candidate));

    // Diagnostics.
    this.peerConnection.onicegatheringstatechange = () => console.log("onicegatheringstatechange: " + this.peerConnection.iceGatheringState);
    this.peerConnection.oniceconnectionstatechange = () => console.log("oniceconnectionstatechange: " + this.peerConnection.iceConnectionState);
    this.peerConnection.onsignalingstatechange = () => console.log("onsignalingstatechange: " + this.peerConnection.signalingState);
    this.peerConnection.onconnectionstatechange = () => console.log("onconnectionstatechange: " + this.peerConnection.connectionState);
  }

}
