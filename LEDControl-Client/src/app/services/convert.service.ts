import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {firstValueFrom} from "rxjs";
import {environment} from "../../environments/environment";
import {ConvertVideo} from "../models/convert-video";
import { saveAs } from 'file-saver';

@Injectable({
  providedIn: 'root'
})
export class ConvertService {

  constructor(private httpClient: HttpClient) { }

  convertVideo(video: ConvertVideo): Promise<Object>{
    return firstValueFrom(this.httpClient.post(environment.url  + "/convert/", video));
  }

  getVideos(): Promise<ConvertVideo[]>{
    return firstValueFrom(this.httpClient.get<ConvertVideo[]>(environment.url  + "/convert/"));
  }

  downloadVideo(video: ConvertVideo){
    saveAs(environment.url  + "/convert/" + video.id + "/download", video.title + ".mp3");
  }
}
