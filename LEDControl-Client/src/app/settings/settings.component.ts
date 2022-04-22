import {Component, OnInit} from '@angular/core';
import {EventService} from "../services/event.service";
import {DeviceMode, DeviceSettings} from "../models/device-settings";

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {

  devices: DeviceSettings[] = [];

  constructor(private eventService: EventService) {
    this.eventService.subTitle.next("Settings");
  }

  ngOnInit(): void {
    let device: DeviceSettings =
      {
        Id: "1",
        Name: "Test 1",
        Mode: DeviceMode.Light
      };

    let device2: DeviceSettings =
      {
        Id: "2",
        Name: "Test 2",
        Mode: DeviceMode.EQ
      };

    let device3: DeviceSettings =
      {
        Id: "3",
        Name: "Test 3",
        Mode: DeviceMode.Pictures
      };
    this.devices.push(device, device2, device3);
  }

}
