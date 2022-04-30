import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {HomeComponent} from "./home/home.component";
import {SettingsComponent} from "./settings/settings.component";
import {ConverterComponent} from "./converter/converter.component";
import {MusicComponent} from "./music/music.component";

const routes: Routes = [
  {path: "settings", component: SettingsComponent},
  {path: "converter", component: ConverterComponent},
  {path: "music", component: MusicComponent},
  {path: "**", component: HomeComponent},
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
