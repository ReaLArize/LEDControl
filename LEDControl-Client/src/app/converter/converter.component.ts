import {Component, OnInit} from '@angular/core';
import {EventService} from "../services/event.service";
import {ConvertStatus, ConvertVideo} from "../models/convert-video";
import {ConvertService} from "../services/convert.service";
import {NotificationService} from "../services/notification.service";
import {MatDialog} from "@angular/material/dialog";
import {AddVideoComponent} from "./add-video/add-video.component";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";

@Component({
  selector: 'app-converter',
  templateUrl: './converter.component.html',
  styleUrls: ['./converter.component.scss']
})
export class ConverterComponent implements OnInit {
  videos: ConvertVideo[] = [];
  private hubConnection: HubConnection;
  constructor(private eventService: EventService, private convertService: ConvertService,
              private notiService: NotificationService, public dialog: MatDialog) {
    this.eventService.subTitle.next("Youtube Converter");
  }

  async ngOnInit() {
    await this.loadVideos();
    console.log(this.videos);
    await this.initHubConnection();
  }

  async initHubConnection(){
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(environment.url + "/hubs/convert")
      .withAutomaticReconnect()
      .build();
    await this.hubConnection.start()
      .then(res => {
        this.eventService.connectionStatus.next(true);
        this.notiService.showMessage("Connected!", 1500);
      })
      .catch(error => {
        this.eventService.connectionStatus.next(false);
        this.notiService.showMessage("Connection error!");
      });
    this.hubConnection.onreconnecting(error => {
      this.notiService.showMessage("Connection lost, try to reconnect");
      this.eventService.connectionStatus.next(false);
    })
    this.hubConnection.onreconnected(error => {
      this.notiService.showMessage("Connection restored!");
      this.eventService.connectionStatus.next(true);
    });

    this.hubConnection.on("NewVideo", (video: ConvertVideo) => {
      this.videos.splice(0, 0, video);
    });
    this.hubConnection.on("Update", (updateVideo: ConvertVideo) => {
      let video = this.videos.find(p => p.id == updateVideo.id);
      if(video){
        video.convertStatus = updateVideo.convertStatus;
        video.link = updateVideo.link;
        video.hint = updateVideo.hint;
        video.downloadProgress = updateVideo.downloadProgress;
        video.convertProgress = updateVideo.convertProgress;
      }
    });
    this.hubConnection.on("UpdateDownload", (id: string, prog: number) => {
      let video = this.videos.find(p => p.id == id);
      if(video){
        video.downloadProgress = prog;
      }
    });
    this.hubConnection.on("UpdateConvert", (id: string, prog: number) => {
      let video = this.videos.find(p => p.id == id);
      if(video){
        video.convertProgress = prog;
      }
    });
  }

  async loadVideos(){
    this.videos = await this.convertService.getVideos();
  }

  openAddDialog(){
    const dialogRef = this.dialog.open(AddVideoComponent, {
      width: '350px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if(result){
        this.convertService.convertVideo(result).then(() => {
        }).catch(err => {
          console.error(err);
          this.notiService.showMessage("Error while loading the video!");
        });
      }
    });
  }

  downloadVideo(video: ConvertVideo){
    if(video.convertStatus == ConvertStatus.Done){
      this.convertService.downloadVideo(video);
    }
  }

}
