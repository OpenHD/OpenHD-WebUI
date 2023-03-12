import { Component, OnInit, ViewChild, AfterViewInit, ElementRef, Inject } from '@angular/core';
import { SignalrService } from "../signalr.service"

@Component({
  selector: 'app-fpv',
  templateUrl: './fpv.component.html',
  styleUrls: ['./fpv.component.css']
})
export class FpvComponent implements OnInit, AfterViewInit {
  @ViewChild("fpvVideo") video?: ElementRef<HTMLVideoElement>;

  private baseUrl: string;
  private pc?: RTCPeerConnection;
  private ws?: WebSocket;

  public demoText = "tests";


  constructor(@Inject('BASE_URL') baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  ngOnInit(): void {
  }

  ngAfterViewInit() {
    this.start()
      .then(() => console.log("Started") )
      .catch(reason => console.log("Error",reason));
  }
  async start() {
    if (this.ws != null) await this.ws.close();
    if (this.pc != null) await this.pc.close();

    //this.pc = new RTCPeerConnection({ iceServers: [{ urls: STUN_URL }] });
    this.pc = new RTCPeerConnection();

    this.pc.ontrack = evt => this.video!.nativeElement.srcObject = evt.streams[0];
    this.pc.onicecandidate = evt => evt.candidate && this.ws!.send(JSON.stringify(evt.candidate));

    // Diagnostics.
    this.pc.onicegatheringstatechange = () => console.log("onicegatheringstatechange: " + this.pc!.iceGatheringState);
    this.pc.oniceconnectionstatechange = () => console.log("oniceconnectionstatechange: " + this.pc!.iceConnectionState);
    this.pc.onsignalingstatechange = () => console.log("onsignalingstatechange: " + this.pc!.signalingState);
    this.pc.onconnectionstatechange = () => console.log("onconnectionstatechange: " + this.pc!.connectionState);

    let url = new URL(this.baseUrl);
    this.ws = new WebSocket(`ws://${ url.hostname }:8081/`, []);

    let _this = this;
    this.ws.onmessage = async function (evt) {
      if (/^[\{"'\s]*candidate/.test(evt.data)) {
        _this.pc!.addIceCandidate(JSON.parse(evt.data));
      }
      else {
        await _this.pc!.setRemoteDescription(new RTCSessionDescription(JSON.parse(evt.data)));
        console.log("remote sdp:\n" + _this.pc!.remoteDescription!.sdp);
        _this.pc!.createAnswer()
          .then((answer) => _this.pc!.setLocalDescription(answer))
          .then(() => _this.ws!.send(JSON.stringify(_this.pc!.localDescription)));
      }
    };
  };

  async closePeer() {
    await this.pc!.close();
    await this.ws!.close();
  };


}
