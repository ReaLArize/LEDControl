import {Component, OnInit, ViewChild} from '@angular/core';
import {EventService} from "../services/event.service";
import {BaseChartDirective} from "ng2-charts";
import { ChartDataset, ChartOptions} from 'chart.js';
import {NotificationService} from "../services/notification.service";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";

@Component({
  selector: 'app-music',
  templateUrl: './music.component.html',
  styleUrls: ['./music.component.scss']
})
export class MusicComponent implements OnInit {
  @ViewChild(BaseChartDirective, { static: true }) chart: BaseChartDirective;
  dataLength = 512;
  data: ChartDataset[] = [
    {data: [], label:"Music", pointRadius: 0}
  ];
  public labels = [];
  options: ChartOptions = {
    scales:{
      y:{
        min: 0,
        max: 0.2
      },
      x: {
        display: false
      }
    }
  };
  hubConnection: HubConnection;

  constructor(private eventService: EventService, private notiService: NotificationService) {
    this.eventService.subTitle.next("Music");
    for (let i = 0; i < this.dataLength; i++){
      this.labels.push(i);
    }
  }

  async ngOnInit() {
    await this.initHubConnection();
  }

  async initHubConnection(){
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(environment.url + "/hubs/music")
      .withAutomaticReconnect()
      .build();
    await this.hubConnection.start()
      .then(res => {
        this.eventService.connectionStatus.next(true);
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
      this.notiService.showMessage("Connection restored!", 1000);
      this.eventService.connectionStatus.next(true);
    });
    this.hubConnection.on("UpdateChart", (fftData: number[]) => {
      this.data[0].data = fftData;
      this.chart.update();
    });
  }
}
