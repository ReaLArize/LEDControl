import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Device} from "../models/device";
import {firstValueFrom} from "rxjs";
import {environment} from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class DeviceService {

  constructor(private httpClient: HttpClient) { }

  getDevices(): Promise<Device[]>{
    return firstValueFrom(this.httpClient.get<Device[]>(environment.url  + "/device/"));
  }

  addDevice(device: Device): Promise<Device>{
    return firstValueFrom(this.httpClient.post<Device>(environment.url  + "/device/", device));
  }

  deleteDevice(deviceId: string): Promise<object>{
    return firstValueFrom(this.httpClient.delete(environment.url + "/device/" + deviceId));
  }
}
