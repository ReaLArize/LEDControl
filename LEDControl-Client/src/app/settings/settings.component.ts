import {Component, OnInit} from '@angular/core';
import {EventService} from "../services/event.service";
import {Device} from "../models/device";
import {DeviceService} from "../services/device.service";
import {AddDeviceComponent} from "./add-device/add-device.component";
import {MatDialog} from "@angular/material/dialog";
import {NotificationService} from "../services/notification.service";

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {

  devices: Device[] = [];

  constructor(private eventService: EventService, private deviceService: DeviceService,
              public dialog: MatDialog, private notiService: NotificationService) {
    this.eventService.subTitle.next("Settings");
  }

  async ngOnInit() {
    await this.loadDevices();
  }

  openAddDialog(){
    const dialogRef = this.dialog.open(AddDeviceComponent, {
      data: new Device(),
      width: '350px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if(result){
        this.deviceService.addDevice(result).then(() => {
          this.loadDevices();
          this.notiService.showMessage("Device successfully created!")
        }).catch(err => {
          console.error(err);
          this.notiService.showMessage("Error while creating the device!");
        });
      }
    });
  }

  async loadDevices(){
    this.devices = await this.deviceService.getDevices();
  }

  async deleteDevice(deviceId: string){
    this.deviceService.deleteDevice(deviceId).then(() => {
      this.loadDevices();
      this.notiService.showMessage("Device successfully deleted!");
    }).catch(err =>{
      console.error(err);
      this.notiService.showMessage("Error while deleting the device!");
    });
  }
}
