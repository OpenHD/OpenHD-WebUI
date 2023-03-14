import { Component, ViewChild, ElementRef, Inject } from '@angular/core';
import { WebRTCPlayer } from "@eyevinn/webrtc-player";
import { HttpClient } from "@angular/common/http";

@Component({
  selector: 'app-fpv',
  templateUrl: './fpv.component.html',
  styleUrls: ['./fpv.component.css']
})
export class FpvComponent {
  @ViewChild("fpvVideo") video?: ElementRef<HTMLVideoElement>;
  private player?: WebRTCPlayer;

  constructor(
    @Inject("BASE_URL") private baseUrl: string,
    private http: HttpClient) {
  }

  onStartVideoClick() {
    this.startWhep()
      .then(() => console.log("Started"))
      .catch(reason => console.log("Error", reason));
  }

  onStopAll() {
    this.http
      .post(this.baseUrl + 'api/video/stop', null)
      .subscribe({
        error: err => console.error(err)
      });
  }

  async startWhep() {
    this.player = new WebRTCPlayer({
      video: this.video!.nativeElement,
      type: 'whep',
      debug: true,
      statsTypeFilter: '^candidate-*|^inbound-rtp'
    });

    await this.player.load(new URL(this.baseUrl + 'api/video/sdp'));
    this.player.on('no-media', () => {
      console.log('media timeout occured');
    });
    this.player.on('media-recovered', () => {
      console.log('media recovered');
    });

    // Subscribe for RTC stats: `stats:${RTCStatsType}`
    this.player.on('stats:inbound-rtp', (report) => {
      if (report.kind === 'video') {
        console.log(report);
      }
    });
  }
}
