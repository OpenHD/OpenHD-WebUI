import { Component, OnInit } from '@angular/core';

import {SignalrService} from "../signalr.service"

@Component({
  selector: 'app-fpv',
  templateUrl: './fpv.component.html',
  styleUrls: ['./fpv.component.css']
})
export class FpvComponent implements OnInit {

  public demoText: string;

  constructor(signalrService: SignalrService) {
    this.demoText = "aaaa";
  }

  ngOnInit(): void {
  }

}
