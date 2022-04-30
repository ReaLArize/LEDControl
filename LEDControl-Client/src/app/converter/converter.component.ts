import {Component, OnInit} from '@angular/core';
import {EventService} from "../services/event.service";
import {ConversionPreset, ConvertVideo} from "../models/convert-video";
import {ConvertService} from "../services/convert.service";
import {NotificationService} from "../services/notification.service";

@Component({
  selector: 'app-converter',
  templateUrl: './converter.component.html',
  styleUrls: ['./converter.component.scss']
})
export class ConverterComponent implements OnInit {

  conversionPreset: ConversionPreset = ConversionPreset.VerySlow;
  link: string;

  videos: ConvertVideo[] = [];

  constructor(private eventService: EventService, private convertService: ConvertService,
              private notiService: NotificationService) {
    this.eventService.subTitle.next("Youtube Converter");
  }

  async ngOnInit() {
    await this.loadVideos();
  }

  async loadVideos(){
    this.videos = await this.convertService.getVideos();
  }

  convertVideo(){
    const video = new ConvertVideo();
    video.conversionPreset = this.conversionPreset;
    video.link = this.link;
    console.debug(video);
    this.convertService.convertVideo(video)
      .catch(err => {
        console.error(err);
        this.notiService.showMessage("Fehler beim Ermitteln des Videos!");
      });
  }

}
