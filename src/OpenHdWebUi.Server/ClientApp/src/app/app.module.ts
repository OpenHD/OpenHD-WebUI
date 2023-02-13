import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { FilesComponent } from './files/files.component';
import { SystemComponent } from './system/system.component';
import { FpvComponent } from './fpv/fpv.component';
import { SignalrService } from "./signalr.service"

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    FilesComponent,
    SystemComponent,
    FpvComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    RouterModule.forRoot([
      { path: '', component: FilesComponent, pathMatch: 'full' },
      { path: 'files', component: FilesComponent },
      { path: 'system', component: SystemComponent },
      { path: 'fpv', component: FpvComponent }
    ])
  ],
  providers: [SignalrService],
  bootstrap: [AppComponent]
})
export class AppModule { }
