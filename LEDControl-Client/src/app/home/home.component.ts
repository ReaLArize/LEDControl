import {Component, NgZone, OnInit} from '@angular/core';
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";
import {NotificationService} from "../services/notification.service";
import {EventService} from "../services/event.service";
declare var iro: any;

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  private colorPicker: any;
  private hubConnection: HubConnection;
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
    await this.hubConnection.start()
      .then(res => {
        this.eventService.connectionStatus.next(true);
        this.notificationService.showMessage("Connected!", 1500);
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
      this.notificationService.showMessage("Connection restored!");
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
    this.hubConnection.send("ChangeLight", color.hexString);
  }

  onOff(){
    this.hubConnection.send("Off");
  }

}
