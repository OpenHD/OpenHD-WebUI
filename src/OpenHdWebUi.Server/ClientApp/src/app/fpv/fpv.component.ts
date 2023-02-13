import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-fpv',
  templateUrl: './fpv.component.html',
  styleUrls: ['./fpv.component.css']
})
export class FpvComponent implements OnInit {

  public demoText: string;

  constructor() {
    this.demoText = "aaaa";
  }

  ngOnInit(): void {
  }

}
