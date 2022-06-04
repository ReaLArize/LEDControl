import {Component, NgZone, OnInit} from '@angular/core';
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";
import {NotificationService} from "../services/notification.service";
import {EventService} from "../services/event.service";
import {Light} from "../models/light";
declare var iro: any;

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  private colorPicker: any;
  private hubConnection: HubConnection;
  private colorChanging = false;
  isRainbow: boolean;
  isMusic: boolean;
  isRainbowEq: boolean;

  constructor(private ngZone: NgZone, private notificationService: NotificationService,
              private eventService: EventService) {
    this.eventService.subTitle.next("");
  }

  async ngOnInit() {
    this.initColorPicker();
    await this.initHubConnection();
  }

  async initHubConnection(){
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(environment.url + "/hubs/light")
      .withAutomaticReconnect()
      .build();
    this.hubConnection.on("UpdateLight", (light: Light) => {
      this.colorChanging = true;
      this.colorPicker.color.hexString = light.hexString;
      this.isRainbow = light.rainbowOn;
      this.isMusic = light.musicOn;
      this.isRainbowEq = light.rainbowEqOn;
      this.colorChanging = false;
    });
    await this.hubConnection.start()
      .then(res => {
        this.eventService.connectionStatus.next(true);
      })
      .catch(error => {
        this.eventService.connectionStatus.next(false);
        this.notificationService.showMessage("Connection error!");
      });
    this.hubConnection.onreconnecting(error => {
      this.notificationService.showMessage("Connection lost, try to reconnect");
      this.eventService.connectionStatus.next(false);
    })
    this.hubConnection.onreconnected(error => {
      this.notificationService.showMessage("Connection restored!", 1000);
      this.eventService.connectionStatus.next(true);
    });
  }

  initColorPicker(){
    this.colorPicker = new iro.ColorPicker("#color-picker-container", {
      color: "#000000",
      borderWidth: 2,
      borderColor: "#828282",
      wheelLightness: false
    });
    this.colorPicker.on('color:change', (color, changes) =>  this.ngZone.run(() => this.ColorChanged(color, changes)));
  }

  ColorChanged(color, changes){
    if(!this.colorChanging){
      this.hubConnection.send("ChangeLight", color.hexString);
    }
  }

  onOff(){
    this.hubConnection.send("Off");
  }

  doRainbow(){
    this.hubConnection.send("Rainbow");
  }

  doMusic(){
    this.hubConnection.send("Music");
  }

  doRainbowEq(){
    this.hubConnection.send("RainbowEq");
  }

}
