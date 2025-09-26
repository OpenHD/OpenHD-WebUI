import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { FilesComponent } from './files/files.component';
import { SystemComponent } from './system/system.component';
import { UpdateComponent } from './update/update.component';
import { SettingsComponent } from './settings/settings.component';

@NgModule(
    { 
        declarations: 
        [
            AppComponent,
            NavMenuComponent,
            FilesComponent,
            SystemComponent,
            UpdateComponent,
            SettingsComponent
        ],
        bootstrap: [AppComponent], 
        imports: 
        [
            BrowserModule,
            AppRoutingModule,
            FormsModule
        ], 
        providers: 
            [
                provideHttpClient(withInterceptorsFromDi())
            ] 
    })
export class AppModule { }
