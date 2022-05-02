import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {HomeComponent} from './home/home.component';
import {FlexLayoutModule} from "@angular/flex-layout";
import {MatButtonModule} from "@angular/material/button";
import {ToolbarComponent} from './toolbar/toolbar.component';
import {MatToolbarModule} from "@angular/material/toolbar";
import {MatSidenavModule} from "@angular/material/sidenav";
import {MatSnackBarModule} from "@angular/material/snack-bar";
import {MatIconModule} from "@angular/material/icon";
import {MatTooltipModule} from "@angular/material/tooltip";
import {MatListModule} from "@angular/material/list";
import {LayoutModule} from "@angular/cdk/layout";
import { ConverterComponent } from './converter/converter.component';
import { SettingsComponent } from './settings/settings.component';
import {MatButtonToggleModule} from "@angular/material/button-toggle";
import {FormsModule} from "@angular/forms";
import { AddDeviceComponent } from './settings/add-device/add-device.component';
import {HttpClientModule} from "@angular/common/http";
import {MatInputModule} from "@angular/material/input";
import {MatDialogModule} from "@angular/material/dialog";
import {MatSelectModule} from "@angular/material/select";
import { MusicComponent } from './music/music.component';
import { AddVideoComponent } from './converter/add-video/add-video.component';
import {MatProgressBarModule} from "@angular/material/progress-bar";
import {NgChartsModule} from "ng2-charts";

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    ToolbarComponent,
    ConverterComponent,
    SettingsComponent,
    AddDeviceComponent,
    MusicComponent,
    AddVideoComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    FlexLayoutModule,
    MatButtonModule,
    MatToolbarModule,
    MatSidenavModule,
    MatSnackBarModule,
    MatIconModule,
    MatTooltipModule,
    MatListModule,
    LayoutModule,
    MatButtonToggleModule,
    FormsModule,
    HttpClientModule,
    MatInputModule,
    MatDialogModule,
    MatSelectModule,
    MatProgressBarModule,
    NgChartsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
}
